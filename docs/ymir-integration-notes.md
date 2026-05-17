# Ymir Integration Notes

## Current Telesto Shape

Telesto currently treats emulator backends as libretro cores:

- `CoreManager` resolves console tags to libretro DLL names under `AppPaths.GetCoresFolder()`.
- Saturn currently resolves to `mednafen_saturn_libretro.dll`, `kronos_libretro.dll`, then `yabause_libretro.dll`.
- `EmulatorWindow` owns the libretro lifecycle, including `retro_*` callbacks, AV info, save RAM, save states, input polling, rendering, audio, and core options.

That means Ymir cannot be added by only appending `ymir-core.dll` to the Saturn core list unless that DLL exports the libretro ABI.

## What The Local Ymir Payload Contains

The current repo has a Ymir payload at `portable/ymircore`:

- `ymir-sdl3.exe`
- `ymir-core.dll`
- `Ymir.toml`
- `gamecontrollerdb.txt`
- `ipl.bin`
- `z.dll`
- `zd.dll`
- `zstd.dll`

`ymir-sdl3.exe --help` works and reports the standalone app CLI:

```text
Ymir [OPTION...] path to disc image
  -d, --disc arg
  -p, --profile arg
  -u, --user
  -f, --fullscreen
  -P, --paused
  -F, --fast-forward
  -D, --debug
  -E, --exceptions
```

## What We Proved About `ymir-core.dll`

`ymir-core.dll` is not a libretro core. It does not export `retro_init`, `retro_api_version`, `retro_get_system_info`, `retro_load_game`, or `retro_run`.

It exports a separate Ymir C API:

```text
ymir_create_context
ymir_destroy_context
ymir_eject_disc
ymir_enable_debug_tracing
ymir_enable_sh2_cache
ymir_get_disc_hash
ymir_get_framebuffer
ymir_get_ipl_hash
ymir_load_cdblock_rom
ymir_load_disc
ymir_load_ipl
ymir_load_state
ymir_reset
ymir_run_frame
ymir_save_state
ymir_set_audio_callback
ymir_set_clock_speed
ymir_set_input_state
ymir_set_video_callback
ymir_set_video_standard
```

A guessed no-argument call to `ymir_create_context` crashed the probe process, so we should not P/Invoke this API by guesswork. We need the exact matching header/source for this DLL build before integrating it in-process.

The current `ymir-core.dll` also imports debug Visual C++ runtime DLLs:

```text
MSVCP140D.dll
VCRUNTIME140D.dll
VCRUNTIME140_1D.dll
ucrtbased.dll
```

That is fine for a developer machine with Visual Studio components installed, but it is not shippable. A release integration needs a Release-built Ymir core that imports the redistributable runtime DLLs instead.

## Practical Integration Paths

### 1. Standalone Launch, Fastest Proof Of User Value

Launch `ymir-sdl3.exe` externally for Saturn games when the user selects Ymir.

Pros:

- Uses the known-good Ymir frontend.
- Avoids reverse-engineering native callbacks.
- Can be implemented quickly as an alternate Saturn launch route.
- Proves game loading, BIOS/profile layout, and build packaging first.

Cons:

- Telesto overlays, save-state UI, screenshots, recordings, RetroAchievements hooks, and unified input do not control the emulator window.
- Ymir saves/config live in a Ymir profile folder unless carefully routed.

Minimum implementation:

- Add a Ymir payload path helper.
- Add a Saturn core choice named something like `Ymir (standalone)`.
- On Saturn launch, start `ymir-sdl3.exe --disc "<game path>" --profile "<Telesto data>/YmirProfiles/default"`.
- Route Ymir IPL/BIOS into the profile or generate profile config paths that point at Telesto's `System` folder.

### 2. Native In-Process Adapter, Real Telesto Integration

Create a separate `YmirCore` adapter rather than trying to force Ymir through `LibretroCore`.

Required surfaces:

- Native DLL loader that adds the Ymir folder to the DLL search path.
- Exact P/Invoke delegates from the Ymir header.
- Context lifecycle: create, load IPL, load disc, run frame, reset, destroy.
- Video callback or framebuffer pull path into Telesto's existing display surface.
- Audio callback into Telesto's `AudioPlayer` or a sibling audio queue.
- Input translation from Telesto controller mappings to `ymir_set_input_state`.
- Save-state mapping to Telesto's save-state database.
- Save RAM / backup memory mapping, if exposed by the API or profile files.
- Packaging/publish rules for Ymir DLLs and data files.

This is the right long-term route, but only after we have the exact C API contract.

Upstream Ymir already has the library boundary we need:

- `libs/ymir-core` builds the emulator library target.
- `ymir::Saturn` is the facade object for reset, IPL loading, disc loading, frame stepping, save states, VDP, SCSP, SMPC input, and configuration.
- The software renderer exposes a 32-bit frame callback. Ymir's SDL frontend uploads it to an `SDL_PIXELFORMAT_XBGR8888` texture; Telesto converts the channel order before writing to WPF `Bgra32`.
- SCSP exposes a stereo sample callback.
- SMPC peripheral ports can connect Saturn Control Pads and provide reports through callbacks.

The custom local `ymir-core.dll` is not an upstream target, so the safer path is to build our own thin wrapper DLL from upstream source instead of reverse-engineering that DLL.

## Implemented Native Wrapper Scaffold

A native wrapper scaffold now lives at `native/ymir-telesto-core`.

It builds `telesto-ymir-core.dll` by linking against upstream `ymir::ymir-core` and exporting a small C ABI for Telesto:

- Create/destroy context.
- Set video callback.
- Set audio callback.
- Load IPL ROM.
- Load disc image.
- Load internal backup RAM image.
- Set Saturn control pad button masks for ports 1 and 2.
- Hard/soft reset.
- Run one emulated frame.

The wrapper intentionally skips Ymir SDL app features: settings windows, update checks, debugger, rewind, MIDI configuration, SDL input, and exotic controller UI.

Expected build flow:

```powershell
git clone --recurse-submodules https://github.com/StrikerX3/Ymir C:\src\Ymir

cmake -S native\ymir-telesto-core -B native\ymir-telesto-core\build -G Ninja `
  -DCMAKE_BUILD_TYPE=Release `
  -DTELESTO_YMIR_SOURCE_DIR=C:\src\Ymir `
  -DCMAKE_TOOLCHAIN_FILE=C:\src\Ymir\vcpkg\scripts\buildsystems\vcpkg.cmake `
  -DVCPKG_TARGET_TRIPLET=x64-win-llvm-static-md `
  -DVCPKG_OVERLAY_TRIPLETS=C:\src\Ymir\vcpkg-triplets `
  -DVCPKG_INSTALLED_DIR=C:\src\Ymir\vcpkg_installed `
  -DCMAKE_PREFIX_PATH=C:\src\Ymir\vcpkg_installed\x64-win-llvm-static-md

cmake --build native\ymir-telesto-core\build --config Release --parallel
```

Working local proof on 2026-05-16:

- Upstream Ymir was cloned to `C:\tmp\Ymir` with submodules.
- Ymir's vcpkg bootstrap and manifest install completed for `x64-win-llvm-static-md`.
- `telesto-ymir-core.dll` built successfully from `native/ymir-telesto-core`.
- The resulting DLL imports release Visual C++ runtime DLLs (`MSVCP140.dll`, `VCRUNTIME140.dll`, and UCRT API set DLLs), not debug runtime DLLs.
- Export verification showed the intended C ABI: `telesto_ymir_create`, `telesto_ymir_destroy`, `telesto_ymir_last_error`, `telesto_ymir_load_ipl`, `telesto_ymir_load_disc`, `telesto_ymir_load_internal_backup_ram`, `telesto_ymir_insert_backup_ram_cartridge`, `telesto_ymir_reset`, `telesto_ymir_run_frame`, `telesto_ymir_set_video_callback`, `telesto_ymir_set_audio_callback`, and `telesto_ymir_set_control_pad_state`.

The wrapper sets `WITH_LZMA_ASM=OFF` so libchdr does not require `ml64.exe`. That is acceptable for the first casual-user embedded path; it keeps CHD support through the C decoder while avoiding a full Visual Studio assembler requirement.

Telesto now has a managed `YmirNativeCore` loader and a first-pass `YmirEmulatorWindow` that runs the wrapper in a Telesto-owned WPF window. It supports:

- IPL discovery from Telesto's `System` folder or the packaged `ymircore/ipl.bin`.
- Disc loading through the native wrapper.
- Internal backup RAM creation/loading through Ymir's backup memory formatter.
- A per-game 32 Mbit backup RAM cartridge image for titles or BIOS screens that expect cartridge memory.
- Software video callback presentation into a WPF `WriteableBitmap`, with red/blue channel conversion for the embedded path.
- Stereo sample playback through Telesto's `AudioPlayer`.
- Keyboard and controller mapping for the Saturn digital pad.
- Separate Saturn core choices for `Ymir (embedded experimental)` and `Ymir (standalone fallback)`.

Still intentionally missing from the embedded path:

- Telesto save-state loading/saving.
- Recording, screenshots, shaders, achievements, and the full pause overlay.
- Automatic DRAM/ROM cartridge selection for the games that require those cartridge types.
- Region/frame-rate refinement beyond the first NTSC/PAL guess.

The embedded wrapper now explicitly closes the virtual tray after disc load/reset. That should keep the Saturn BIOS from sitting at the orange startup screen because it believes the drive is open.

Next engineering step: add automatic recommended cartridge selection for DRAM and ROM-cart titles, then move the shared emulator-window chrome/HUD pieces behind a backend-neutral interface so Ymir can reuse more of the regular `EmulatorWindow` experience.

### 3. Libretro Wrapper, Highest Maintenance

Write or adopt a libretro wrapper around Ymir, then Telesto can keep using `LibretroCore`.

Pros:

- Telesto integration becomes mostly automatic.
- Also benefits other libretro frontends.

Cons:

- More complex than integrating the provided Ymir C API.
- Requires maintaining a new native wrapper and libretro ABI surface.

## Recommended Next Step

Use path 1 first to validate the local Ymir build and the Saturn game launch workflow. In parallel, obtain or generate the exact header for `ymir-core.dll`; once that exists, path 2 becomes straightforward engineering instead of native-call roulette.

## Implemented Standalone Path

Telesto now exposes `Ymir (standalone fallback)` as a Saturn core preference alongside the embedded experimental core. When selected, the game detail Play button launches `ymir-sdl3.exe` with:

```text
--disc "<game path>" --profile "<Telesto data>/YmirProfiles/default"
```

The standalone payload is copied to `ymircore/` in build and publish output. This route uses `ymir-sdl3.exe` or `ymir.exe`; it deliberately does not copy or load the debug-built `ymir-core.dll`.

The game detail overflow menu also exposes `Play with Ymir standalone` for Saturn games when `ymir-sdl3.exe` or `ymir.exe` is available, so users can bypass the embedded experimental wrapper without changing their global core preference.

On launch, Telesto seeds and maintains a Ymir profile at `<Telesto data>/YmirProfiles/default`:

- Copies the packaged `Ymir.toml` on first use.
- Disables Ymir update checks in the Telesto-owned profile.
- Enables Ymir's per-game internal backup RAM setting.
- Copies Saturn IPL BIOS candidates from Telesto's `System` folder into `roms/ipl`.

Telesto save-state loading is not available through this standalone route. If a user tries to load a Telesto save state while Ymir standalone is selected, Telesto shows an explanatory message and asks them to use Play Game or choose a libretro Saturn core.

Before any release:

- Rebuild `ymir-core.dll` as Release.
- Verify it loads on a clean Windows machine with only the Visual C++ Redistributable installed.
- Decide whether Ymir's GPL-3.0 license is compatible with the intended Telesto distribution model.
