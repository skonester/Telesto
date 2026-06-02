using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Emutastic.Services
{
    public sealed class YmirNativeCore : IDisposable
    {
        public const string DllName = "telesto-ymir-core.dll";

        [Flags]
        public enum Buttons : ushort
        {
            Right = 1 << 15,
            Left = 1 << 14,
            Down = 1 << 13,
            Up = 1 << 12,
            Start = 1 << 11,
            A = 1 << 10,
            C = 1 << 9,
            B = 1 << 8,
            R = 1 << 7,
            X = 1 << 6,
            Y = 1 << 5,
            Z = 1 << 4,
            L = 1 << 3
        }

        public enum Result
        {
            Ok = 0,
            InvalidArgument = 1,
            FileNotFound = 2,
            InvalidIpl = 3,
            DiscLoadFailed = 4,
            CoreError = 5
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void VideoCallback(IntPtr userData, IntPtr xrgb8888, uint width, uint height);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void AudioCallback(IntPtr userData, short left, short right);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateDelegate();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DestroyDelegate(IntPtr ctx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr LastErrorDelegate(IntPtr ctx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SetVideoCallbackDelegate(IntPtr ctx, VideoCallback callback, IntPtr userData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SetAudioCallbackDelegate(IntPtr ctx, AudioCallback callback, IntPtr userData);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Result LoadPathDelegate(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ResetDelegate(IntPtr ctx, int hard);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void RunFrameDelegate(IntPtr ctx);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SetControlPadStateDelegate(IntPtr ctx, uint port, ushort pressedButtons);

        private readonly IntPtr _library;
        private readonly IntPtr _ctx;

        private readonly CreateDelegate _create;
        private readonly DestroyDelegate _destroy;
        private readonly LastErrorDelegate _lastError;
        private readonly SetVideoCallbackDelegate _setVideoCallback;
        private readonly SetAudioCallbackDelegate _setAudioCallback;
        private readonly LoadPathDelegate _loadIpl;
        private readonly LoadPathDelegate _loadDisc;
        private readonly LoadPathDelegate _loadInternalBackupRam;
        private readonly LoadPathDelegate _insertBackupRamCartridge;
        private readonly ResetDelegate _reset;
        private readonly RunFrameDelegate _runFrame;
        private readonly SetControlPadStateDelegate _setControlPadState;

        private bool _disposed;

        public string CorePath { get; }

        public static bool IsAvailable() => GetCorePath() != null;

        public static string? GetCorePath()
        {
            foreach (string dir in GetCandidateFolders())
            {
                string dll = Path.Combine(dir, DllName);
                if (File.Exists(dll))
                    return dll;
            }

            return null;
        }

        public YmirNativeCore(string? corePath = null)
        {
            CorePath = corePath ?? GetCorePath()
                ?? throw new FileNotFoundException("Telesto Ymir native core was not found.", DllName);

            _library = NativeLibrary.Load(CorePath);

            _create = GetExport<CreateDelegate>("telesto_ymir_create");
            _destroy = GetExport<DestroyDelegate>("telesto_ymir_destroy");
            _lastError = GetExport<LastErrorDelegate>("telesto_ymir_last_error");
            _setVideoCallback = GetExport<SetVideoCallbackDelegate>("telesto_ymir_set_video_callback");
            _setAudioCallback = GetExport<SetAudioCallbackDelegate>("telesto_ymir_set_audio_callback");
            _loadIpl = GetExport<LoadPathDelegate>("telesto_ymir_load_ipl");
            _loadDisc = GetExport<LoadPathDelegate>("telesto_ymir_load_disc");
            _loadInternalBackupRam = GetExport<LoadPathDelegate>("telesto_ymir_load_internal_backup_ram");
            _insertBackupRamCartridge = GetExport<LoadPathDelegate>("telesto_ymir_insert_backup_ram_cartridge");
            _reset = GetExport<ResetDelegate>("telesto_ymir_reset");
            _runFrame = GetExport<RunFrameDelegate>("telesto_ymir_run_frame");
            _setControlPadState = GetExport<SetControlPadStateDelegate>("telesto_ymir_set_control_pad_state");

            _ctx = _create();
            if (_ctx == IntPtr.Zero)
                throw new InvalidOperationException("Ymir native core failed to create a context.");
        }

        public void SetVideoCallback(VideoCallback callback, IntPtr userData)
            => _setVideoCallback(_ctx, callback, userData);

        public void SetAudioCallback(AudioCallback callback, IntPtr userData)
            => _setAudioCallback(_ctx, callback, userData);

        public void LoadIpl(string path) => ThrowIfFailed(_loadIpl(_ctx, path), "load IPL ROM");
        public void LoadDisc(string path) => ThrowIfFailed(_loadDisc(_ctx, path), "load disc image");
        public void LoadInternalBackupRam(string path) => ThrowIfFailed(_loadInternalBackupRam(_ctx, path), "load internal backup RAM");
        public void InsertBackupRamCartridge(string path) => ThrowIfFailed(_insertBackupRamCartridge(_ctx, path), "insert backup RAM cartridge");
        public void Reset(bool hard) => _reset(_ctx, hard ? 1 : 0);
        public void RunFrame() => _runFrame(_ctx);
        public void SetControlPadState(uint port, Buttons pressedButtons)
            => _setControlPadState(_ctx, port, (ushort)pressedButtons);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_ctx != IntPtr.Zero)
                _destroy(_ctx);
            if (_library != IntPtr.Zero)
                NativeLibrary.Free(_library);
        }

        private T GetExport<T>(string name) where T : Delegate
        {
            IntPtr address = NativeLibrary.GetExport(_library, name);
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        private void ThrowIfFailed(Result result, string operation)
        {
            if (result == Result.Ok)
                return;

            string message = Marshal.PtrToStringUTF8(_lastError(_ctx)) ?? result.ToString();
            throw new InvalidOperationException($"Ymir failed to {operation}: {message}");
        }

        private static IEnumerable<string> GetCandidateFolders()
        {
            string exeFolder = AppPaths.GetExeFolder();
            yield return exeFolder;
            yield return AppContext.BaseDirectory;
            yield return Path.Combine(exeFolder, "ymircore");
            yield return Path.Combine(AppContext.BaseDirectory, "ymircore");
            yield return Path.Combine(AppPaths.GetNativeFolder(), "ymircore");

            string? current = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(current); i++)
            {
                yield return Path.Combine(current, "portable", "ymircore");
                yield return Path.Combine(current, "native", "ymir-telesto-core", "build-release");
                yield return Path.Combine(current, "native", "ymir-telesto-core", "build");
                current = Directory.GetParent(current)?.FullName;
            }
        }
    }
}
