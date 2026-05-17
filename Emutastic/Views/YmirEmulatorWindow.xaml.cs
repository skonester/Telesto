using Emutastic.Models;
using Emutastic.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Emutastic.Views
{
    public partial class YmirEmulatorWindow : Window
    {
        private readonly Game _game;
        private readonly ControllerManager _controller;
        private readonly HashSet<Key> _keysDown = new();
        private readonly object _frameLock = new();

        private YmirNativeCore? _core;
        private AudioPlayer? _audioPlayer;
        private Thread? _emuThread;
        private volatile bool _stopRequested;
        private volatile bool _videoPending;
        private byte[] _frameBuffer = Array.Empty<byte>();
        private WriteableBitmap? _bitmap;
        private uint _videoWidth;
        private uint _videoHeight;

        private readonly YmirNativeCore.VideoCallback _videoCallback;
        private readonly YmirNativeCore.AudioCallback _audioCallback;

        public YmirEmulatorWindow(Game game)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _videoCallback = OnVideoFrame;
            _audioCallback = OnAudioSample;

            InitializeComponent();
            GameTitleText.Text = $"{game.Title} - Ymir";
            FooterText.Text = "Ymir embedded";
            Title = $"{game.Title} - Ymir";

            _controller = new ControllerManager(
                App.Configuration ?? throw new InvalidOperationException("Configuration not initialized"),
                null,
                game.Console);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _emuThread = new Thread(RunEmulator)
            {
                IsBackground = true,
                Name = "YmirEmuThread",
                Priority = ThreadPriority.AboveNormal
            };
            _emuThread.SetApartmentState(ApartmentState.MTA);
            _emuThread.Start();
        }

        private void RunEmulator()
        {
            try
            {
                string iplPath = FindIplPath();
                string backupPath = GetBackupRamPath();
                string cartridgeBackupPath = GetBackupRamCartridgePath();
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                Directory.CreateDirectory(Path.GetDirectoryName(cartridgeBackupPath)!);

                _core = new YmirNativeCore();
                _core.SetVideoCallback(_videoCallback, IntPtr.Zero);
                _core.SetAudioCallback(_audioCallback, IntPtr.Zero);
                _core.LoadIpl(iplPath);
                _core.LoadInternalBackupRam(backupPath);
                _core.InsertBackupRamCartridge(cartridgeBackupPath);
                _core.LoadDisc(_game.RomPath);

                _audioPlayer = new AudioPlayer(44100) { DesiredLatencyMs = 90 };
                _audioPlayer.Start();
                _audioPlayer.BeginPlayback();

                Dispatcher.BeginInvoke(() => StatusText.Visibility = Visibility.Collapsed);

                double fps = string.Equals(RomService.DetectRegion(_game.RomPath), "Europe", StringComparison.OrdinalIgnoreCase)
                    ? 50.0
                    : 60.0;
                long ticksPerFrame = (long)(Stopwatch.Frequency / fps);
                long nextTick = Stopwatch.GetTimestamp();

                while (!_stopRequested)
                {
                    UpdateInput();
                    _core.RunFrame();

                    nextTick += ticksPerFrame;
                    long delayTicks = nextTick - Stopwatch.GetTimestamp();
                    if (delayTicks > 0)
                    {
                        int delayMs = (int)(delayTicks * 1000 / Stopwatch.Frequency);
                        if (delayMs > 1)
                            Thread.Sleep(delayMs - 1);
                        while (Stopwatch.GetTimestamp() < nextTick && !_stopRequested)
                            Thread.SpinWait(64);
                    }
                    else if (delayTicks < -Stopwatch.Frequency)
                    {
                        nextTick = Stopwatch.GetTimestamp();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[YmirEmulatorWindow] " + ex);
                Dispatcher.BeginInvoke(() =>
                {
                    StatusText.Text = "Ymir failed to start";
                    MessageBox.Show(this, ex.Message, "Ymir Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                });
            }
        }

        private void OnVideoFrame(IntPtr userData, IntPtr xrgb8888, uint width, uint height)
        {
            if (xrgb8888 == IntPtr.Zero || width == 0 || height == 0 || _videoPending)
                return;

            int byteCount = checked((int)(width * height * 4));
            lock (_frameLock)
            {
                if (_frameBuffer.Length != byteCount)
                    _frameBuffer = new byte[byteCount];
                Marshal.Copy(xrgb8888, _frameBuffer, 0, byteCount);
            }

            _videoPending = true;
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (_bitmap == null || _videoWidth != width || _videoHeight != height)
                    {
                        _videoWidth = width;
                        _videoHeight = height;
                        _bitmap = new WriteableBitmap((int)width, (int)height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
                        GameScreen.Source = _bitmap;
                    }

                    _bitmap.Lock();
                    try
                    {
                        lock (_frameLock)
                        {
                            Marshal.Copy(_frameBuffer, 0, _bitmap.BackBuffer, byteCount);
                        }
                        _bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)width, (int)height));
                    }
                    finally
                    {
                        _bitmap.Unlock();
                    }
                }
                finally
                {
                    _videoPending = false;
                }
            }, DispatcherPriority.Render);
        }

        private void OnAudioSample(IntPtr userData, short left, short right)
        {
            try { _audioPlayer?.QueueSample(left, right); }
            catch { }
        }

        private void UpdateInput()
        {
            YmirNativeCore.Buttons buttons = 0;

            if (IsPressed(Key.Up) || _controller.GetButtonState(LibretroInput.JOYPAD_UP)) buttons |= YmirNativeCore.Buttons.Up;
            if (IsPressed(Key.Down) || _controller.GetButtonState(LibretroInput.JOYPAD_DOWN)) buttons |= YmirNativeCore.Buttons.Down;
            if (IsPressed(Key.Left) || _controller.GetButtonState(LibretroInput.JOYPAD_LEFT)) buttons |= YmirNativeCore.Buttons.Left;
            if (IsPressed(Key.Right) || _controller.GetButtonState(LibretroInput.JOYPAD_RIGHT)) buttons |= YmirNativeCore.Buttons.Right;
            if (IsPressed(Key.Enter) || _controller.GetButtonState(LibretroInput.JOYPAD_START)) buttons |= YmirNativeCore.Buttons.Start;

            if (IsPressed(Key.Z) || _controller.GetButtonState(LibretroInput.JOYPAD_B)) buttons |= YmirNativeCore.Buttons.A;
            if (IsPressed(Key.X) || _controller.GetButtonState(LibretroInput.JOYPAD_A)) buttons |= YmirNativeCore.Buttons.B;
            if (IsPressed(Key.C) || _controller.GetButtonState(LibretroInput.JOYPAD_R)) buttons |= YmirNativeCore.Buttons.C;
            if (IsPressed(Key.A) || _controller.GetButtonState(LibretroInput.JOYPAD_Y)) buttons |= YmirNativeCore.Buttons.X;
            if (IsPressed(Key.S) || _controller.GetButtonState(LibretroInput.JOYPAD_X)) buttons |= YmirNativeCore.Buttons.Y;
            if (IsPressed(Key.D) || _controller.GetButtonState(LibretroInput.JOYPAD_L)) buttons |= YmirNativeCore.Buttons.Z;
            if (IsPressed(Key.Q) || _controller.GetButtonState(LibretroInput.JOYPAD_L2)) buttons |= YmirNativeCore.Buttons.L;
            if (IsPressed(Key.W) || _controller.GetButtonState(LibretroInput.JOYPAD_R2)) buttons |= YmirNativeCore.Buttons.R;

            _core?.SetControlPadState(0, buttons);
        }

        private bool IsPressed(Key key)
        {
            lock (_keysDown)
                return _keysDown.Contains(key);
        }

        private string FindIplPath()
        {
            string systemDir = AppPaths.GetFolder("System");
            string region = RomService.DetectRegion(_game.RomPath);

            var candidates = new List<string>();
            if (string.Equals(region, "Japan", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(Path.Combine(systemDir, "sega_101.bin"));
                candidates.Add(Path.Combine(systemDir, "mpr-17933.bin"));
            }
            else
            {
                candidates.Add(Path.Combine(systemDir, "mpr-17941.bin"));
            }

            candidates.Add(Path.Combine(systemDir, "sega_101.bin"));
            candidates.Add(Path.Combine(systemDir, "mpr-17933.bin"));
            candidates.Add(Path.Combine(systemDir, "mpr-17941.bin"));

            string? nativeCoreDir = Path.GetDirectoryName(YmirNativeCore.GetCorePath() ?? "");
            if (!string.IsNullOrEmpty(nativeCoreDir))
                candidates.Add(Path.Combine(nativeCoreDir, "ipl.bin"));

            string? standaloneDir = Path.GetDirectoryName(YmirLauncher.GetExecutablePath() ?? "");
            if (!string.IsNullOrEmpty(standaloneDir))
                candidates.Add(Path.Combine(standaloneDir, "ipl.bin"));

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException("No Saturn IPL BIOS was found. Add sega_101.bin, mpr-17933.bin, or mpr-17941.bin to Telesto's System folder.");
        }

        private string GetBackupRamPath()
        {
            string stem = Path.GetFileNameWithoutExtension(_game.RomPath);
            foreach (char c in Path.GetInvalidFileNameChars())
                stem = stem.Replace(c, '_');
            return Path.Combine(AppPaths.GetFolder("BatterySaves", "Saturn", "Ymir"), stem + ".bup");
        }

        private string GetBackupRamCartridgePath()
        {
            string stem = Path.GetFileNameWithoutExtension(_game.RomPath);
            foreach (char c in Path.GetInvalidFileNameChars())
                stem = stem.Replace(c, '_');
            return Path.Combine(AppPaths.GetFolder("BatterySaves", "Saturn", "Ymir", "Cartridges"), stem + ".bup");
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }

            lock (_keysDown)
                _keysDown.Add(e.Key == Key.System ? e.SystemKey : e.Key);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            lock (_keysDown)
                _keysDown.Remove(e.Key == Key.System ? e.SystemKey : e.Key);
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _stopRequested = true;
            try { _emuThread?.Join(1000); } catch { }
            _audioPlayer?.Dispose();
            _core?.Dispose();
            _controller.Dispose();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaxBtn_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}
