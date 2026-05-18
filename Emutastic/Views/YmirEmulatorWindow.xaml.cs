using Emutastic.Models;
using Emutastic.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Emutastic.Views
{
    public partial class YmirEmulatorWindow : Window
    {
        private readonly Game _game;
        private readonly ControllerManager _controller;
        private readonly DatabaseService _db;
        private readonly string _saveStatePath;
        private readonly string? _pendingInitialLoadStatePath;
        private readonly HashSet<Key> _keysDown = new();
        private readonly object _frameLock = new();
        private readonly object _pendingStateLock = new();

        private YmirNativeCore? _core;
        private AudioPlayer? _audioPlayer;
        private Thread? _emuThread;
        private volatile bool _stopRequested;
        private volatile bool _videoPending;
        private volatile bool _paused;
        private volatile bool _resetPending;
        private volatile bool _hardResetPending;
        private volatile bool _saveStatePending;
        private volatile bool _loadStatePending;
        private string _pendingSaveName = "";
        private string _pendingLoadPath = "";
        private string _pendingLoadName = "";
        private byte[] _frameBuffer = Array.Empty<byte>();
        private WriteableBitmap? _bitmap;
        private uint _videoWidth;
        private uint _videoHeight;

        private readonly YmirNativeCore.VideoCallback _videoCallback;
        private readonly YmirNativeCore.AudioCallback _audioCallback;

        public YmirEmulatorWindow(Game game, string? pendingLoadStatePath = null)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _pendingInitialLoadStatePath = pendingLoadStatePath;
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
            _db = new DatabaseService();
            _saveStatePath = AppPaths.GetFolder("Save States",
                SanitizeFileName(game.Console), SanitizeFileName(game.Title));

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

                if (!string.IsNullOrWhiteSpace(_pendingInitialLoadStatePath))
                {
                    QueueLoadState(_pendingInitialLoadStatePath, Path.GetFileNameWithoutExtension(_pendingInitialLoadStatePath));
                }

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
                    if (_paused)
                    {
                        Thread.Sleep(16);
                        nextTick = Stopwatch.GetTimestamp();
                        continue;
                    }

                    UpdateInput();
                    ProcessPendingReset();
                    _core.RunFrame();
                    ProcessPendingSaveState();
                    ProcessPendingLoadState();

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
                _videoWidth = width;
                _videoHeight = height;

                // Ymir's software callback is consumed as XBGR by its SDL frontend; WPF wants BGRA.
                for (int i = 0; i < byteCount; i += 4)
                {
                    byte red = _frameBuffer[i];
                    _frameBuffer[i] = _frameBuffer[i + 2];
                    _frameBuffer[i + 2] = red;
                    _frameBuffer[i + 3] = 0xFF;
                }
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
                        _bitmap = new WriteableBitmap((int)width, (int)height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
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

        private void RequestReset(bool hard)
        {
            _hardResetPending = hard;
            _resetPending = true;
            ShowFooterMessage(hard ? "Hard reset..." : "Reset...");
        }

        private void ProcessPendingReset()
        {
            if (!_resetPending || _core == null)
                return;

            bool hard = _hardResetPending;
            _resetPending = false;
            _hardResetPending = false;

            try
            {
                _core.Reset(hard);
                ShowFooterMessage(hard ? "Hard reset" : "Reset");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[YmirEmulatorWindow] Reset failed: " + ex);
                ShowFooterMessage("Reset failed");
            }
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Dispatcher.BeginInvoke(() =>
            {
                PauseResumeBtn.Content = _paused ? "\uE768" : "\uE769";
                PauseResumeBtn.ToolTip = _paused ? "Resume" : "Pause";
                FooterText.Text = _paused ? "Paused" : "Ymir embedded";
            });
        }

        private void RequestSaveState()
        {
            string name = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            lock (_pendingStateLock)
            {
                _pendingSaveName = name;
                _saveStatePending = true;
            }
            ShowFooterMessage("Saving state...");
        }

        private void RequestLoadLatestState()
        {
            var state = _db.GetSaveStatesByGame(_game.Id)
                .FirstOrDefault(s => string.Equals(s.CoreName, YmirLauncher.EmbeddedCoreId, StringComparison.OrdinalIgnoreCase));
            if (state == null)
            {
                ShowFooterMessage("No embedded Ymir save states yet");
                return;
            }

            QueueLoadState(state.StatePath, state.Name);
        }

        private void QueueLoadState(string path, string name)
        {
            lock (_pendingStateLock)
            {
                _pendingLoadPath = path;
                _pendingLoadName = name;
                _loadStatePending = true;
            }
            ShowFooterMessage("Loading state...");
        }

        private void ProcessPendingSaveState()
        {
            if (!_saveStatePending || _core == null)
                return;

            string name;
            lock (_pendingStateLock)
            {
                name = _pendingSaveName;
                _saveStatePending = false;
                _pendingSaveName = "";
            }

            try
            {
                if (!_core.SupportsSaveStates)
                {
                    ShowFooterMessage("Rebuild telesto-ymir-core.dll for save states");
                    return;
                }

                string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(name) ? "state" : name);
                string statePath = Path.Combine(_saveStatePath, safeName + ".state");
                string pngPath = Path.Combine(_saveStatePath, safeName + ".png");
                string jsonPath = Path.Combine(_saveStatePath, safeName + ".json");

                Directory.CreateDirectory(_saveStatePath);
                _core.SaveState(statePath);
                SaveScreenshot(pngPath);
                WriteSaveStateMetadata(name, statePath, pngPath, jsonPath);
                ShowFooterMessage($"Saved: {name}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[YmirEmulatorWindow] Save state failed: " + ex);
                ShowFooterMessage("Save state failed");
            }
        }

        private void ProcessPendingLoadState()
        {
            if (!_loadStatePending || _core == null)
                return;

            string path;
            string name;
            lock (_pendingStateLock)
            {
                path = _pendingLoadPath;
                name = _pendingLoadName;
                _loadStatePending = false;
                _pendingLoadPath = "";
                _pendingLoadName = "";
            }

            try
            {
                if (!_core.SupportsSaveStates)
                {
                    ShowFooterMessage("Rebuild telesto-ymir-core.dll for save states");
                    return;
                }

                _core.LoadState(path);
                ShowFooterMessage($"Loaded: {name}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("[YmirEmulatorWindow] Load state failed: " + ex);
                ShowFooterMessage("Load state failed");
            }
        }

        private void SaveScreenshot(string pngPath)
        {
            byte[] pixels;
            int width;
            int height;

            lock (_frameLock)
            {
                if (_frameBuffer.Length == 0 || _videoWidth == 0 || _videoHeight == 0)
                    return;

                pixels = (byte[])_frameBuffer.Clone();
                width = (int)_videoWidth;
                height = (int)_videoHeight;
            }

            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            if (bitmap.CanFreeze)
                bitmap.Freeze();

            using var fs = new FileStream(pngPath, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fs);
        }

        private void WriteSaveStateMetadata(string name, string statePath, string pngPath, string jsonPath)
        {
            var meta = new
            {
                Name = name,
                GameTitle = _game.Title,
                ConsoleName = _game.Console,
                CoreName = YmirLauncher.EmbeddedCoreId,
                RomHash = _game.RomHash ?? "",
                CreatedAt = DateTime.Now.ToString("o"),
            };

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));

            var saveState = new SaveState
            {
                GameId = _game.Id,
                Name = name,
                GameTitle = _game.Title,
                ConsoleName = _game.Console,
                CoreName = YmirLauncher.EmbeddedCoreId,
                RomHash = _game.RomHash ?? "",
                StatePath = statePath,
                ScreenshotPath = File.Exists(pngPath) ? pngPath : "",
                CreatedAt = DateTime.Now,
            };

            var existing = _db.GetSaveStateByGameAndName(_game.Id, name);
            if (existing != null)
            {
                _db.UpdateSaveStateName(existing.Id, name, statePath, saveState.ScreenshotPath);
            }
            else
            {
                _db.InsertSaveState(saveState);
                _db.RecalcSaveCount(_game.Id);
                _game.SaveCount++;
            }
        }

        private void ShowFooterMessage(string text)
        {
            Dispatcher.BeginInvoke(() => FooterText.Text = text);
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
            return Path.Combine(AppPaths.GetFolder("BatterySaves", "Saturn", "Ymir"), "bup-int.bin");
        }

        private string GetBackupRamCartridgePath()
        {
            string stem = Path.GetFileNameWithoutExtension(_game.RomPath);
            foreach (char c in Path.GetInvalidFileNameChars())
                stem = stem.Replace(c, '_');
            return Path.Combine(AppPaths.GetFolder("BatterySaves", "Saturn", "Ymir", "Cartridges"), stem + ".bup");
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }

            if (e.Key == Key.F5)
            {
                RequestSaveState();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F6)
            {
                TogglePause();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F8)
            {
                RequestLoadLatestState();
                e.Handled = true;
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
        private void PauseResumeBtn_Click(object sender, RoutedEventArgs e) => TogglePause();
        private void ResetBtn_Click(object sender, RoutedEventArgs e) => RequestReset(hard: true);
        private void SaveStateBtn_Click(object sender, RoutedEventArgs e) => RequestSaveState();
        private void LoadLatestStateBtn_Click(object sender, RoutedEventArgs e) => RequestLoadLatestState();

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}
