# Telesto Ymir core wrapper

This DLL embeds [ymir-emu/Ymir](https://github.com/ymir-emu/Ymir)'s `ymir::ymir-core` library in Telesto through a small C ABI. Telesto owns the window, audio queue, video surface, BIOS/disc paths, save folders, and digital pad input.

The supported development baseline is Ymir **v0.4.1**, pinned by tag and full commit in [`../ymir-source.json`](../ymir-source.json). Older 0.3.1 checkouts need the original adapter and are no longer supported by this build. Weekly releases resolve the latest stable upstream release; see [the release guide](../../docs/releases.md).

## Build on Windows

Install Visual Studio 2022 C++ Build Tools, a Windows SDK including DXC, Clang, CMake 3.28+ and Ninja. The core-only build does not require vcpkg. Use a fresh build directory when changing upstream versions or toolchains.

From the Telesto repository root:

```powershell
git clone --branch v0.4.1 --depth 1 https://github.com/ymir-emu/Ymir.git .build/ymir
./tools/release/Build-Ymir.ps1
```

The script initializes upstream's vendor submodules, configures a Release build using `clang-cl`, builds `.build/native/telesto-ymir-core.dll`, and runs CTest. It uses upstream's supported `Ymir_LIBRARY_ONLY` switch and does not rewrite upstream source files. Optional libchdr LZMA assembly is disabled.

To configure manually, run from a Visual Studio x64 developer shell where Clang, Ninja, CMake and DXC are on PATH:

```powershell
cmake -S native/ymir-telesto-core -B .build/native -G Ninja `
  -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl `
  "-DTELESTO_YMIR_SOURCE_DIR=$PWD/.build/ymir" -DBUILD_TESTING=ON
cmake --build .build/native --parallel 4
ctest --test-dir .build/native --output-on-failure
dotnet build Emutastic.sln -c Release "/p:YmirNativeDir=$PWD/.build/native"
```

## Current API

- Context create/destroy and last-error reporting.
- Software video and stereo audio callbacks.
- IPL/disc loading, reset and frame stepping.
- Internal backup RAM and 32 Mbit backup RAM cartridge images.
- Digital Saturn pads on ports 1 and 2.

The managed side uses `YmirNativeCore` and `YmirEmulatorWindow`. Save states, serialization, disc swapping, shaders, achievements, screenshots and recording remain deferred. The lifecycle smoke test does not require BIOS files or games and does not certify gameplay compatibility.
