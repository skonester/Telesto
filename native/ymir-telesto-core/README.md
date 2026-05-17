# Telesto Ymir Core Wrapper

This project builds a small native `telesto-ymir-core.dll` around upstream
`StrikerX3/Ymir`'s `libs/ymir-core` target. It is intentionally not the full
Ymir SDL app. Telesto supplies the window, input, audio queue, video surface,
BIOS path, disc path, and save folders.

## Build

Clone Ymir with submodules somewhere outside this repo:

```powershell
git clone --recurse-submodules https://github.com/StrikerX3/Ymir C:\src\Ymir
```

Configure the wrapper with Ymir's vcpkg toolchain:

```powershell
cmake -S native\ymir-telesto-core -B native\ymir-telesto-core\build -G Ninja `
  -DCMAKE_BUILD_TYPE=Release `
  -DTELESTO_YMIR_SOURCE_DIR=C:\src\Ymir `
  -DCMAKE_TOOLCHAIN_FILE=C:\src\Ymir\vcpkg\scripts\buildsystems\vcpkg.cmake `
  -DVCPKG_TARGET_TRIPLET=x64-win-llvm-static-md `
  -DVCPKG_OVERLAY_TRIPLETS=C:\src\Ymir\vcpkg-triplets `
  -DVCPKG_INSTALLED_DIR=C:\src\Ymir\vcpkg_installed `
  -DCMAKE_PREFIX_PATH=C:\src\Ymir\vcpkg_installed\x64-win-llvm-static-md
```

Build:

```powershell
cmake --build native\ymir-telesto-core\build --config Release --parallel
```

If vcpkg dependencies are not installed yet, bootstrap Ymir's vcpkg checkout and
install the manifest first:

```powershell
C:\src\Ymir\vcpkg\bootstrap-vcpkg.bat
C:\src\Ymir\vcpkg\vcpkg.exe install --triplet x64-win-llvm-static-md `
  --x-manifest-root=C:\src\Ymir `
  --overlay-triplets=C:\src\Ymir\vcpkg-triplets
```

The output DLL is the native dependency Telesto should load for the in-process
Ymir path.

The wrapper disables libchdr's optional LZMA assembly path so the build does not
require `ml64.exe` from a full Visual Studio developer environment.

## MVP Surface

The exported C ABI currently covers:

- Create/destroy context
- Set software video callback
- Set stereo sample callback
- Load IPL ROM
- Load disc image
- Load internal backup RAM image
- Insert a 32 Mbit backup RAM cartridge
- Set Saturn control pad pressed-state masks for ports 1 and 2
- Hard/soft reset
- Run one emulated frame

That is enough for a first casual-user Saturn core inside Telesto's own emulator
window. It deliberately skips Ymir app features such as settings UI, update
checks, rewind, debugger, MIDI devices, exotic controllers, and Ymir's SDL input
stack.

## Telesto Integration Steps

1. Build and ship `telesto-ymir-core.dll`.
2. Add a managed `YmirNativeCore` P/Invoke wrapper.
3. Add an emulator window path that accepts a non-libretro backend.
4. Feed software-rendered frames from the native video callback into Telesto's
   existing `Image`/bitmap presentation path, converting the channel order for
   WPF presentation.
5. Feed audio samples into Telesto's existing audio queue.
6. Map Telesto Saturn controls into the exported `TelestoYmirButton` mask.
7. Keep save states disabled until Ymir's cereal serializers are included in
   this wrapper or a stable wrapper-owned state format is chosen.

The managed side currently loads this DLL through `YmirNativeCore` and runs it
in `YmirEmulatorWindow`. That window is intentionally minimal: video, audio,
disc boot, BIOS discovery, internal backup RAM, a simple backup RAM cartridge,
and digital Saturn pad input only.
