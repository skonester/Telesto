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

Telesto now exposes `Ymir (standalone)` as a Saturn core preference. When selected, the game detail Play button launches `ymir-sdl3.exe` with:

```text
--disc "<game path>" --profile "<Telesto data>/YmirProfiles/default"
```

The standalone payload is copied to `ymircore/` in build and publish output. This route uses `ymir-sdl3.exe` only; it deliberately does not copy or load the debug-built `ymir-core.dll`.

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
