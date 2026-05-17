#  Telesto

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![GitHub Contributors](https://img.shields.io/github/contributors/skonester/telesto.svg)](https://github.com/skonester/telesto/graphs/contributors)

**Telesto** is a modern multi-system libretro emulator frontend for Windows, inspired originally by OpenEmu on macOS.

 Named after the planet Saturn's real moon and also the name I have affecionately given the character from the iconic Sega Saturn commercials, Telesto itself orbits around interesting projects in 2026 emulation while using Emutastic as home planet.
 
 Telesto provides a sleek, unified interface for all your retro gaming needs under one easy to understand house. Built for people just want to pick up and start gaming easily.
 
 Telesto would not exist without the strong work of Emustastic bringing OpenEmu over to Windows. 



![Telesto Banner](Emutastic/Assets/banners%20and%20icons/emutastic-banner-scaled.png)

## Features at a Glance

- **34+ emulated systems** across 11 manufacturers with automatic core selection.

- **Integrated Sega Saturn emulation from fork** via the Ymir core (embedded or standalone). We want to bring more eyes to the ymir project.

## Requirements

- **Windows 10/11** (x64)
- **.NET 8 Desktop Runtime** - [Download here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- **Visual C++ Redistributable 2015–2022 (x64)** - [Download here](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- **libretro core `.dll` files** (downloadable in-app)
- **`SDL3.dll`** (x64) for controller name detection (downloadable in-app)
- **`ffmpeg.exe`** for video recording (downloadable in-app)
- **DAT files** for ROM identification (downloadable in-app)

> **Windows SmartScreen:** Telesto is not code-signed. Click **"More info"** then **"Run anyway"** on first launch.

## Supported Systems

Telesto supports **34 systems across 11 manufacturers** with automatic core selection and intelligent fallback.

| System | Tag | Core (priority order) | BIOS Required |
|--------|-----|----------------------|---------------|
| NES | NES | nestopia → quicknes → fceumm | No |
| Famicom Disk System | FDS | nestopia | `disksys.rom` |
| SNES | SNES | snes9x → bsnes | No |
| Nintendo 64 | N64 | parallel_n64 → mupen64plus_next | No |
| GameCube | GameCube | dolphin | No |
| Game Boy | GB | mgba → gambatte → sameboy | No |
| Game Boy Color | GBC | mgba → gambatte → sameboy | No |
| Game Boy Advance | GBA | mgba | Optional |
| Nintendo 3DS | 3DS | azahar | No |
| Nintendo DS | NDS | desmume → melonds | No |
| Virtual Boy | VirtualBoy | mednafen_vb | No |
| Genesis / Mega Drive | Genesis | genesis_plus_gx → picodrive | No |
| Sega CD / Mega CD | SegaCD | genesis_plus_gx | Region BIOS |
| Sega 32X | Sega32X | picodrive | No |
| Sega Saturn | Saturn | ymir (embedded) → ymir (standalone) → mednafen_saturn → kronos → yabause | Region BIOS |
| Master System | SMS | genesis_plus_gx → picodrive | No |
| Game Gear | GameGear | genesis_plus_gx | No |
| SG-1000 | SG1000 | genesis_plus_gx | No |
| Dreamcast | Dreamcast | flycast | No |
| PlayStation | PS1 | mednafen_psx_hw → mednafen_psx | Region BIOS |
| PSP | PSP | ppsspp | No |
| TurboGrafx-16 | TG16 | mednafen_pce → mednafen_pce_fast | No |
| TurboGrafx-CD | TGCD | mednafen_pce → mednafen_pce_fast | `syscard3.pce` |
| Neo Geo Pocket | NGP | mednafen_ngp | No |
| Neo Geo Pocket Color | NGPC | mednafen_ngp | No |
| Neo Geo | NeoGeo | geolith | `neogeo.zip` + `aes.zip` |
| Arcade | Arcade | fbneo | No |
| Atari 2600 | Atari2600 | stella | No |
| Atari 7800 | Atari7800 | prosystem | No |
| Atari Jaguar | Jaguar | virtualjaguar | No |
| ColecoVision | ColecoVision | gearcoleco → bluemsx | No |
| Vectrex | Vectrex | vecx | No |
| 3DO | 3DO | opera | `panafz10.bin` |
| Philips CD-i | CDi | same_cdi | No |

## 📁 BIOS Files

Place BIOS files in `%AppData%\Telesto\System\` (or `PortableData\System\` next to the .exe in portable mode). The app also checks each system's ROM folder.

### System-Specific BIOS Requirements

**Sega CD** — `bios_CD_U.bin` (USA), `bios_CD_E.bin` (Europe), `bios_CD_J.bin` (Japan)

**Sega Saturn** — Ymir automatically detects and copies your Saturn IPL BIOS files (`sega_101.bin`, `mpr-17933.bin`, `mpr-17941.bin`) from Telesto's central `System` directory directly into the emulator profile's `roms/ipl` subdirectory on launch.

**PlayStation** — USA: `scph5501.bin`, `scph1001.bin`, `scph7001.bin`. Europe: `scph5502.bin`. Japan: `scph5500.bin`

**TurboGrafx-CD** — Any of: `syscard3.pce`, `syscard2.pce`, `syscard1.pce`

**3DO** — Any of: `panafz10.bin` (Panasonic), `panafz1j.bin` (Japan), `goldstar.bin` (GoldStar)

**Famicom Disk System** — `disksys.rom`

## ROM Import

Telesto makes importing your ROM collection simple and intelligent:

- **Drag & drop** ROMs directly onto the library
- **Automatic detection** via file extension and SHA1 lookup against DAT files
- **Smart multi-disc bundling** (Final Fantasy VII, Metal Gear Solid, etc.) automatically creates single library entries
- **Hand-authored `.m3u` playlists** are honored as-is
- **Ambiguous formats** (`.chd`, `.iso`, `.cue`, `.bin`) show a console picker if no DAT match is found

**Important:** Download DAT files in **Preferences → Cores / Extras** before importing for best results.

## 🎮 Sega Saturn Emulation: Ymir Integration

Telesto features deep integration with the Ymir Saturn emulator core:

### 🪐 Embedded/In-Process Core (`ymir_embedded`)
- **Direct Rendering:** Software-rendered frames captured via native callbacks
- **Integrated Audio:** Stereo sample callbacks stream directly into Telesto's audio player
- **Unified Controls:** Native button mapping to Ymir's Saturn digital pad button masks
- **Saves & Backups:** Automatic creation and formatting of backup RAM
- **No Open Trays:** Automatically handles virtual disc tray after boot

### Standalone Fallback (`ymir_standalone`)
- **Profile Seeding:** Clean local Ymir profile with updates disabled
- **BIOS Synchronization:** Automatic copying of IPL BIOS files to standalone profile


## Features

### Themes
- Four built-in themes: **Dark** (default), **Light**, **OLED Black**, **Midnight Blue**
- Full visual editor with 44 color tokens and live preview
- Custom background images with zoom, pan, and tile controls
- Export/import themes as `.emutheme` files

### Controllers
- XInput button polling during gameplay
- SDL3 device name detection for hundreds of controllers
- Per-controller button mapping in **Preferences → Input**
- Falls back to generic names if `SDL3.dll` is absent

### RetroAchievements
- Earn achievements while playing via [RetroAchievements](https://retroachievements.org/)
- Enable in **Preferences → Achievements** with your RA username and password
- Achievements appear as toast notifications during gameplay

### About & Updates
- **Preferences → About** shows version, build date, and credits
- Automatic GitHub release checking with manual download option
- Notification-only — no auto-installer, no telemetry

### Core Options
- Per-core settings (internal resolution, graphics plugins, etc.)
- Access in **Preferences → Core Options**

### Disk Swapping (FDS, PS1, Saturn, Sega CD)
- Press **L3 + Start** in-game to flip between discs/sides
- Rebindable to any two-button chord in **Preferences → Controls → Disk Swap**
- Status bar shows new disc number on each swap
- Multi-disc games auto-bundled at import time

## Folder Layout

```
Telesto.exe / rcheevos.dll / .NET runtime DLLs
```

```
%AppData%\Telesto\          (or your custom data folder)
    library.db
    Native\                   (SDL3.dll, ffmpeg.exe)
    DATs\                     (No-Intro / Redump DATs)
    Cores\                    (libretro core DLLs)
    System\                   (BIOS files)
    Save States\ / BatterySaves\ / Screenshots\ / Recordings\ / Artwork\ / ...
```

### Portable mode

Drag and drop ROMs onto the library or use **Import ROMs**. The app detects the console from file extension, cleans the title, and hashes the ROM. For ambiguous formats (`.chd`, `.iso`, `.cue`, `.bin`), a SHA1 lookup against DAT files is attempted first — if no match, a console picker is shown.

**Multi-disc games** (Final Fantasy VII, Metal Gear Solid, etc.) are auto-bundled into a single library entry — drop a folder containing the disc files (`.cue`/`.bin` or `.chd`) and Telesto writes an `.m3u` playlist alongside them so the game shows up once, not three times. Hand-authored `.m3u` files in the folder are honored as-is.

**Important:** Download DAT files in **Preferences → Cores / Extras** before importing. Without them, disc images and some cartridge ROMs may be assigned to the wrong system during import.

---

## Sega Saturn Emulation: Ymir Integration

Telesto supports Sega Saturn emulation through the **Ymir** core (developed by StrikerX3). We offer two integration paths:

### Embedded/In-Process Core (`ymir_embedded`)
Telesto hosts the Ymir emulator in-process via a native C++ wrapper (`telesto-ymir-core.dll`) and a managed P/Invoke layer (`YmirNativeCore`).
*   **Direct Rendering:** Software-rendered XRGB8888 frames are captured via native callbacks and presented directly onto Telesto's `WriteableBitmap` rendering surface.
*   **Integrated Audio:** Stereo sample callbacks stream audio directly into Telesto's `AudioPlayer` queue.
*   **Unified Controls:** Native button mapping maps Telesto's user-configured input profiles directly to Ymir's internal Saturn digital pad button masks.
*   **Saves & Backups:** Automatic creation, formatting, and seeding of internal backup RAM as well as a 32 Mbit backup RAM cartridge image.
*   **No Open Trays:** Automatically handles closing the virtual disc tray after boot to bypass standard CD player BIOS screens.

### Standalone Fallback (`ymir_standalone`)
Launches the standalone `ymir-sdl3.exe` external emulator while keeping it aligned with Telesto.
*   **Profile Seeding:** Telesto automatically provisions and maintains a clean local Ymir profile directory under `YmirProfiles/default`, disabling automated update checks and enabling per-game internal backup RAM.
*   **BIOS Synchronization:** On launch, Telesto checks for your Saturn IPL BIOS files (`sega_101.bin`, `mpr-17933.bin`, `mpr-17941.bin`) in your central `System` directory and copies them directly into the standalone profile's `roms/ipl` subdirectory.

---
## Features

<details>
<summary><strong>Themes</strong></summary>

Four built-in themes: **Dark** (default), **Light**, **OLED Black**, **Midnight Blue**. Full visual editor with 44 color tokens and live preview. Set custom background images with zoom, pan, and tile controls. Export/import themes as `.emutheme` files.

</details>

<details>
<summary><strong>Controllers</strong></summary>

XInput button polling during gameplay with SDL3 device name detection. Xbox, DualSense/DualShock, and hundreds of other controllers are identified by product name. Button mappings configurable per-controller in **Preferences → Input**. Falls back to generic names if `SDL3.dll` is absent.

</details>

<details>
<summary><strong>RetroAchievements</strong></summary>

Earn achievements while playing via [RetroAchievements](https://retroachievements.org/). Enable in **Preferences → Achievements** with your RA username and password. Achievements appear as toast notifications during gameplay.

</details>

<details>
<summary><strong>About & Updates</strong></summary>

**Preferences → About** shows the current version, build date, and credits. On open, it checks GitHub for the latest release and surfaces a download link if a newer version is available. Notification-only — no auto-installer, no telemetry.

</details>

- **Core Options** — Per-core settings (internal resolution, graphics plugins, etc.) in **Preferences → Core Options**

<details>
<summary><strong>Disk Swapping (FDS, PS1, Saturn, Sega CD)</strong></summary>

Press **L3 + Start** in-game to flip between discs/sides on systems that need it. Rebindable to any two-button chord (controller or keyboard) in **Preferences → Controls → Disk Swap**. The status bar shows the new disc number on each swap.

Multi-disc games are auto-bundled at import time — see the [ROM Import](#rom-import) section. See the [wiki page](https://github.com/skonester/telesto/wiki/Disk-Swapping) for per-console specifics and troubleshooting.

</details>

---

## Folder Layout

```
Telesto.exe / rcheevos.dll / .NET runtime DLLs
```

```
%AppData%\Telesto\          (or your custom data folder)
    library.db
    Native\                   (SDL3.dll, ffmpeg.exe — downloadable in-app)
    DATs\                     (No-Intro / Redump DATs — downloadable in-app)
    Cores\                    (libretro core DLLs — downloadable in-app)
    System\                   (BIOS files)
    Save States\ / BatterySaves\ / Screenshots\ / Recordings\ / Artwork\ / ...
```

### Portable mode

Drop an empty `portable.txt` next to `Telesto.exe` **or** launch with the `--portable` command-line flag, and **everything** lives in `PortableData\` beside the .exe — config, library database, save states, battery saves, screenshots, recordings, artwork, BIOS files, libretro cores, and any ROMs you import. Move the install folder to a USB stick and run it on any Windows PC; library paths are stored relative to `PortableData\` so drive-letter changes (E:→F:) don't break anything. ROM imports are auto-copied into `PortableData\Roms\<Console>\` so they travel with the USB without setting up a separate library folder. See **[Portable Mode](https://github.com/skonester/telesto/wiki/Portable-Mode)** in the wiki for the full on-disk layout, caveats, and how to revert.

---

## Building

Requires Visual Studio 2022+ with **.NET desktop development** workload.

```
git clone <repo>
cd Emutastic
dotnet build .\Emutastic.sln -c Release
```

---

<details>
<summary><strong>Credits</strong></summary>

### Libretro Cores

Emulation is handled by libretro cores maintained by their upstream authors. Telesto bundles none of them — the in-app core manager downloads from the libretro build servers on demand. Please support these projects directly.

| Core | Upstream author(s) |
|---|---|
| Azahar | Azahar team (successor to Citra / Lime3DS) |
| Beetle PSX / Saturn / PCE / VB / NGP | Mednafen team (Ryphecha) |
| blueMSX | blueMSX team (Daniel Vik and contributors) |
| bsnes | byuu / near and contributors |
| DeSmuME | DeSmuME team |
| Dolphin | Dolphin team |
| FBNeo (FinalBurn Neo) | FBNeo team |
| FCEUmm | FCEUmm team |
| Flycast | flyinghead and contributors |
| Gambatte | Sindre Aamås (sinamas) |
| Gearcoleco | Ignacio Sánchez (drhelius) |
| Genesis Plus GX | Eke-Eke |
| Geolith | R. Danbrook (rdanbrook) |
| Kronos | Kronos team |
| melonDS | Arisotura |
| mGBA | Vicki Pfau (endrift) |
| Mupen64Plus-Next | libretro team |
| Nestopia UE | Nestopia UE team |
| Opera | libretro team (3DO) |
| ParaLLEl-N64 | libretro team (Themaister and contributors) |
| Picodrive | notaz |
| PPSSPP | Henrik Rydgård and contributors |
| ProSystem | Greg Stanton (upstream) / libretro maintenance |
| QuickNES | Shay Green (blargg) |
| SAME CDi | CDi community (MAME derivative) |
| Snes9x | Snes9x team |
| Stella | Stella team |
| VecX | Valavan Manohararajah (upstream) / libretro maintenance |
| Virtual Jaguar | Virtual Jaguar team |
| Yabause | Yabause team |
| Ymir | StrikerX3 (high-accuracy Sega Saturn emulation core) |

### Controller Illustrations
Artwork from [OpenEmuControllerArt](https://github.com/kodi-game/OpenEmuControllerArt) (BSD 3-Clause). Not affiliated with or endorsed by OpenEmu.

| Artist | Controllers |
|---|---|
| **David McLeod** ([@Mucx](https://twitter.com/Mucx/)) | 32X, FDS, GB, GBA, Game Gear, SMS, NES, Sega CD, Genesis, SNES |
| **Ricky Romero** ([@RickyRomero](https://twitter.com/RickyRomero/)) | Atari 2600/5200, N64, NDS, Odyssey², PS1, PSP, Saturn, SG-1000, Vectrex, Virtual Boy |
| **Craig Erskine** ([@qrayg](https://twitter.com/qrayg/)) | GameCube, Neo Geo Pocket, PC Engine / TG16 |
| **Salvo Zummo** / **David Everly** / **Kate Schroeder** | Atari 7800, 3DO, ColecoVision |

Inspired by [OpenEmu](https://openemu.org/) for macOS.

</details>

---

## License

[GNU General Public License v3.0](LICENSE)
