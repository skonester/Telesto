# Telesto <img src="GPL3.png" align="right" width="136" height="68" />

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![GitHub Contributors](https://img.shields.io/github/contributors/skonester/telesto.svg)](https://github.com/skonester/telesto/graphs/contributors)
[![GitHub Downloads](https://img.shields.io/github/downloads/skonester/telesto/total.svg)](https://github.com/skonester/telesto/releases)
[![Build and release](https://github.com/skonester/Telesto/actions/workflows/release.yml/badge.svg)](https://github.com/skonester/Telesto/actions/workflows/release.yml)
[![Latest release](https://img.shields.io/github/v/release/skonester/Telesto)](https://github.com/skonester/Telesto/releases/latest)

**Telesto** is a Windows emulator frontend forked from **Emutastic**, with development focused on **Sega Saturn emulation and Ymir integration**. It retains Emutastic's multi-system libretro library, import tools, themes, and controller configuration.

Named after Saturn's moon, Telesto builds on the work of Emutastic creator **codingncaffeine** and the OpenEmu-inspired Windows frontend. See [AUTHORS.md](AUTHORS.md) for project attribution and [CHANGELOG.md](CHANGELOG.md) for recent changes.

**[Download for Windows x64](https://github.com/skonester/Telesto/releases/latest)** · [Report a problem](https://github.com/skonester/Telesto/issues/new/choose) · [Contribute](CONTRIBUTING.md)

Weekly builds are scheduled for Mondays at 09:23 UTC. Each successful release has a unique version such as `2026.11.17.42`, a ZIP and installer, SHA256 checksums, and `build-info.json` identifying the exact Telesto and Ymir source commits. Weekly builds follow the latest stable upstream Ymir release; embedded Ymir remains experimental. See the [release guide](docs/releases.md) for activation, version history, and manual builds.

![Telesto Banner](Emutastic/Assets/banners%20and%20icons/emutastic-banner-scaled.png)

## Current Status

- **Libretro gameplay:** automatic selection from installed cores, configurable core preferences, save states, screenshots, recording, shaders, and RetroAchievements where supported by the core and game.
- **Embedded Ymir:** experimental Saturn emulation inside a Telesto window, with software video, audio, digital pad input, and per-game backup RAM.
- **Standalone Ymir:** an alternate Saturn launch path using Ymir's own window and controls.

The embedded Ymir runtime was restored to the working `1f566b5` baseline after post-boot native crashes. Save-state integration, serialization work, and pause/reset toolbar changes were reverted or deferred. Embedded Ymir does **not** currently offer the full libretro gameplay feature set; see [Sega Saturn and Ymir](#sega-saturn-and-ymir).

## Getting Started

1. Download a Windows x64 package from [Releases](https://github.com/skonester/telesto/releases), install it or extract the ZIP, and run `Telesto.exe`.
2. Open **Preferences > Cores / Extras** to download the libretro cores you need and DAT files for ROM identification. Optional downloads include `SDL3.dll` for controller names and `ffmpeg.exe` for recording.
3. Add BIOS files for your chosen systems to the data folder's `System` directory. **Preferences > System Files** shows BIOS status.
4. Drag ROMs or folders onto the library, or use **Import ROMs**. Configure input in **Preferences > Controls** and choose installed backends in **Preferences > Cores / Extras**.

### Requirements

- **Windows x64.** The WPF project targets `net8.0-windows10.0.22621.0` (Windows SDK build 22621); earlier Windows builds are not an established compatibility target in this checkout.
- **Visual C++ x64 runtime** for native emulator components: [Redistributable download](https://aka.ms/vs/17/release/vc_redist.x64.exe).
- **.NET 8 Desktop Runtime** for framework-dependent builds. The repository's `Release-win-x64` publish profile is self-contained and includes .NET, so packages built with that profile do not require a separate .NET installation.
- ROM/disc images, any required BIOS files, and an installed emulator backend. Ymir runtimes are supplied separately or included during packaging; the libretro downloader does not build or install the embedded Ymir wrapper.

## Supported Systems and Core Selection

The following table reflects the configured libretro core mappings in [CoreManager.cs](Emutastic/Services/CoreManager.cs). Names omit the `_libretro.dll` suffix. Selection uses installed cores and configured preferences; availability in this table is not a compatibility guarantee for every game.

| System | Library tag | Libretro cores, in default priority order |
| --- | --- | --- |
| NES | NES | nestopia, quicknes, fceumm |
| Famicom Disk System | FDS | nestopia |
| SNES | SNES | snes9x, bsnes |
| Nintendo 64 | N64 | parallel_n64, mupen64plus_next |
| GameCube | GameCube | dolphin |
| Game Boy | GB | mgba, gambatte, sameboy |
| Game Boy Color | GBC | mgba, gambatte, sameboy |
| Game Boy Advance | GBA | mgba |
| Nintendo DS | NDS | desmume, melonds |
| Nintendo 3DS | 3DS | azahar |
| Virtual Boy | VirtualBoy | mednafen_vb |
| Genesis / Mega Drive | Genesis | genesis_plus_gx, picodrive |
| Sega CD / Mega CD | SegaCD | genesis_plus_gx |
| Sega 32X | Sega32X | picodrive |
| Sega Saturn | Saturn | mednafen_saturn, kronos, yabause; Ymir uses separate launch paths below |
| Master System | SMS | genesis_plus_gx, picodrive |
| Game Gear | GameGear | genesis_plus_gx |
| SG-1000 | SG1000 | genesis_plus_gx |
| Dreamcast | Dreamcast | flycast |
| PlayStation | PS1 | mednafen_psx_hw, mednafen_psx |
| PSP | PSP | ppsspp |
| TurboGrafx-16 | TG16 | mednafen_pce, mednafen_pce_fast |
| TurboGrafx-CD | TGCD | mednafen_pce, mednafen_pce_fast |
| Neo Geo Pocket / Color | NGP | mednafen_ngp |
| Neo Geo | NeoGeo | geolith |
| Arcade | Arcade | fbneo, mame2003_plus |
| Atari 2600 | Atari2600 | stella |
| Atari 7800 | Atari7800 | prosystem |
| Atari Jaguar | Jaguar | virtualjaguar |
| ColecoVision | ColecoVision | gearcoleco, bluemsx |
| Vectrex | Vectrex | vecx |
| 3DO | 3DO | opera |
| Philips CD-i | CDi | same_cdi |

Arcade imports can route to FBNeo or MAME 2003-Plus using DAT matches and per-game core preferences. Neo Geo Pocket `.ngp` and `.ngc` files map to `NGP` by extension; a separate `NGPC` tag exists in metadata but currently has no entry in `ConsoleCoreMap`.

## BIOS Files

Place BIOS files in `%AppData%\Telesto\System\`, your custom data folder's `System` directory, or `PortableData\System\` in portable mode. Libretro launch checks also search the ROM directory and its immediate subdirectories. Embedded Ymir has its own IPL lookup, so use the central `System` directory for Saturn BIOS files.

Telesto's configured BIOS checks include:

| System | Filenames checked |
| --- | --- |
| Famicom Disk System | `disksys.rom` |
| Sega CD | `bios_CD_U.bin`, `bios_CD_E.bin`, or `bios_CD_J.bin`, selected by game region |
| Saturn | `sega_101.bin`, `mpr-17933.bin`, `mpr-17941.bin`; libretro checks also accept `kronos/saturn_bios.bin` |
| PlayStation | `scph5500.bin` (Japan), `scph5501.bin` / `scph1001.bin` / `scph7001.bin` (USA), `scph5502.bin` (Europe) |
| TurboGrafx-CD | `syscard3.pce`, `syscard2.pce`, or `syscard1.pce` |
| 3DO | `panafz10.bin`, `panafz1j.bin`, or `goldstar.bin` |
| Neo Geo / Geolith | Both `neogeo.zip` and `aes.zip` |

These are the frontend's filename checks. Firmware requirements and accepted files also depend on the selected core and game; an unlisted system is not a promise that it needs no firmware.

## ROM Import

- Drag and drop individual ROMs or folders, or use **Import ROMs**.
- Identification uses file extensions and DAT lookups, including SHA1 matching for supported formats. Download DATs before importing for better identification.
- Ambiguous formats such as `.chd`, `.iso`, `.cue`, and `.bin` can prompt for a console when identification does not resolve the system.
- Recognized multi-disc sets can be bundled into one library entry by generating an `.m3u` playlist alongside the disc files. Existing hand-authored playlists are honored.

Playlist import and in-game disc swapping depend on the backend. Embedded Ymir currently has no Telesto disc-swap UI.

## Sega Saturn and Ymir

**Beetle Saturn (`mednafen_saturn`) is the first default libretro choice**, followed by Kronos and Yabause. Select **Ymir (embedded experimental)** or **Ymir (standalone fallback)** in core preferences to use Ymir. When no libretro Saturn core resolves, the game detail Play action can use an available Ymir runtime, preferring embedded over standalone. A failure after embedded startup does not automatically retry with another backend.

### Embedded (`ymir_embedded`)

Telesto loads `telesto-ymir-core.dll` through its managed `YmirNativeCore` adapter. This is a separate native backend, not a libretro core.

Currently implemented:

- Software-rendered video and stereo audio in a Telesto-owned window.
- Player-one Saturn digital pad input using controller mappings and fixed keyboard bindings: arrows, Enter, Z/X/C, A/S/D, and Q/W. Escape closes the game window.
- Per-game internal backup RAM in `BatterySaves\Saturn\Ymir\` and a 32 Mbit backup RAM cartridge in its `Cartridges` subfolder.
- IPL discovery from `System` and fallback `ipl.bin` files beside discovered Ymir runtimes; virtual tray closing during disc boot.

Currently missing: save-state saving/loading, the full pause overlay and pause/reset toolbar, screenshots, recording, shaders, achievements, disc-swap controls, and automatic DRAM/ROM cartridge selection. In-game backup RAM saves are separate from save states and remain supported.

### Standalone (`ymir_standalone`)

Telesto discovers `ymir.sdl3.exe`, `ymir-sdl3.exe`, or `ymir.exe`, normally under `ymircore` beside `Telesto.exe` or under the data folder's `Native\ymircore`. The Saturn game detail menu also offers **Play with Ymir standalone** when an executable is available.

The launcher uses `--disc` and `--profile`, with the profile at `YmirProfiles\default` under the data folder. It seeds a supplied `Ymir.toml`, disables update checks and enables per-game internal backup RAM in that config, and copies missing Saturn BIOS files from `System` into the profile's `roms\ipl` directory.

Gameplay runs in Ymir's own window. Telesto's input mappings, overlays, capture tools, achievements, and save-state UI do not control that window.

## Frontend Features

- **Themes:** Dark, Light, OLED Black, and Midnight Blue; a visual color editor, custom backgrounds, and `.emutheme` import/export.
- **Controls:** keyboard configuration and XInput controller polling, with optional SDL3 device-name detection in **Preferences > Controls**.
- **Libretro gameplay:** core options, save states, screenshots, video recording, shaders, and disk swapping on supported cores. Configure core settings in **Preferences > Core Options**.
- **RetroAchievements:** account login in **Preferences > Achievements**, with achievement notifications for supported libretro games.
- **About and updates:** version and credits in **Preferences > About**, with a GitHub release check and download link when a newer version is available. Updates are downloaded manually.

The libretro gameplay features above do not imply equivalent support in embedded or standalone Ymir.

## Data and Portable Mode

Normal installations keep `config.json` in `%AppData%\Telesto\`. Library data defaults to that directory and can be redirected through **Preferences > Folders**. Libretro cores normally live in `Cores` under the application base directory; they move under `PortableData` in portable mode.

```text
<DataRoot>\
    library.db
    Native\                   SDL3.dll, ffmpeg.exe, optional ymircore\
    DATs\                     ROM identification data
    System\                   BIOS files
    BatterySaves\             Includes Saturn\Ymir\ backup RAM
    Save States\
    Screenshots\
    Recordings\
    Artwork\
    YmirProfiles\default\     Standalone Ymir profile
```

To enable portable mode, create an empty `portable.txt` beside `Telesto.exe` or launch with `--portable`. Use a writable folder: config and data then live in `PortableData` beside the executable, including `Cores` and newly imported ROMs under `Roms\<Console>`. Paths inside the data root are stored relatively so they can survive drive-letter changes.

Existing ROM references outside the data root remain absolute. Custom screenshot/recording destinations can also point elsewhere, so enabling portable mode does not by itself move every existing file onto a USB drive.

## Building from Source

Build on Windows with the **.NET 8 SDK**. Visual Studio 2022 with the **.NET desktop development** workload is an optional IDE. The solution and source directory retain the upstream `Emutastic` names; the output application is `Telesto.exe`.

```powershell
git clone https://github.com/skonester/telesto.git
cd telesto
dotnet build .\Emutastic.sln -c Release
```

Publish a self-contained Windows x64 package using the checked-in profile:

```powershell
dotnet publish .\Emutastic\Emutastic.csproj /p:PublishProfile=Release-win-x64
```

Output goes to `Emutastic\bin\Publish\win-x64\`. The release workflow also downloads OpenVGDB into `Emutastic\Assets` before publishing; a plain local build does not fetch that optional database.

### Including Ymir

The managed build does **not** compile the native Ymir wrapper. Follow the [native wrapper build instructions](native/ymir-telesto-core/README.md) using a separate Ymir checkout, CMake 3.28+, Ninja, Clang, and the Visual Studio C++ tools/Windows SDK. The library-only build needs no vcpkg dependencies.

The release workflow checks out a resolved upstream Ymir commit, builds and smoke tests `telesto-ymir-core.dll`, and downloads the matching official standalone package. Successful pushes to `main` also publish a version; pull requests verify the complete package without publishing a release.

For local builds, the app defaults to the wrapper under `native\ymir-telesto-core\build-release` and the legacy standalone files under `portable\ymircore`. Override `/p:YmirNativeDir=...` and `/p:YmirRuntimeDir=...` to use freshly built/downloaded runtimes. The old standalone executable reports Ymir `0.3.1`; its exact source commit is unknown. Firmware and the legacy debug `ymir-core.dll` are excluded from new packages.

---

<details>
<summary><strong>Credits</strong></summary>

### Libretro Cores

Libretro cores are maintained by their upstream authors and can be downloaded through the in-app core manager. The Saturn integration also uses Ymir through a separate native wrapper or standalone executable. Please support these projects directly.

| Core                                 | Upstream author(s)                                      |
| ------------------------------------ | ------------------------------------------------------- |
| Azahar                               | Azahar team (successor to Citra / Lime3DS)              |
| Beetle PSX / Saturn / PCE / VB / NGP | Mednafen team (Ryphecha)                                |
| blueMSX                              | blueMSX team (Daniel Vik and contributors)              |
| bsnes                                | byuu / near and contributors                            |
| DeSmuME                              | DeSmuME team                                            |
| Dolphin                              | Dolphin team                                            |
| FBNeo (FinalBurn Neo)                | FBNeo team                                              |
| FCEUmm                               | FCEUmm team                                             |
| Flycast                              | flyinghead and contributors                             |
| Gambatte                             | Sindre Aamås (sinamas)                                  |
| Gearcoleco                           | Ignacio Sánchez (drhelius)                              |
| Genesis Plus GX                      | Eke-Eke                                                 |
| Geolith                              | R. Danbrook (rdanbrook)                                 |
| Kronos                               | Kronos team                                             |
| melonDS                              | Arisotura                                               |
| mGBA                                 | Vicki Pfau (endrift)                                    |
| Mupen64Plus-Next                     | libretro team                                           |
| Nestopia UE                          | Nestopia UE team                                        |
| Opera                                | libretro team (3DO)                                     |
| ParaLLEl-N64                         | libretro team (Themaister and contributors)             |
| Picodrive                            | notaz                                                   |
| PPSSPP                               | Henrik Rydgård and contributors                         |
| ProSystem                            | Greg Stanton (upstream) / libretro maintenance          |
| QuickNES                             | Shay Green (blargg)                                     |
| SAME CDi                             | CDi community (MAME derivative)                         |
| Snes9x                               | Snes9x team                                             |
| Stella                               | Stella team                                             |
| VecX                                 | Valavan Manohararajah (upstream) / libretro maintenance |
| Virtual Jaguar                       | Virtual Jaguar team                                     |
| Yabause                              | Yabause team                                            |
| Ymir                                 | StrikerX3 (high-accuracy Sega Saturn emulation core)    |

### Controller Illustrations

Artwork from [OpenEmuControllerArt](https://github.com/kodi-game/OpenEmuControllerArt) (BSD 3-Clause). Not affiliated with or endorsed by OpenEmu.

| Artist                                                              | Controllers                                                                          |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| **David McLeod** ([@Mucx](https://twitter.com/Mucx/))               | 32X, FDS, GB, GBA, Game Gear, SMS, NES, Sega CD, Genesis, SNES                       |
| **Ricky Romero** ([@RickyRomero](https://twitter.com/RickyRomero/)) | Atari 2600/5200, N64, NDS, Odyssey², PS1, PSP, Saturn, SG-1000, Vectrex, Virtual Boy |
| **Craig Erskine** ([@qrayg](https://twitter.com/qrayg/))            | GameCube, Neo Geo Pocket, PC Engine / TG16                                           |
| **Salvo Zummo** / **David Everly** / **Kate Schroeder**             | Atari 7800, 3DO, ColecoVision                                                        |

Inspired by [OpenEmu](https://openemu.org/) for macOS.

</details>

---

## License

[GNU General Public License v3.0](LICENSE.txt)
