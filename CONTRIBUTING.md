# Contributing to Telesto

Thanks for your interest in contributing. Telesto is a fork of Emutastic with a practical focus: improving Sega Saturn emulation on Windows, especially through Ymir integration.

This project still supports a broad libretro frontend, but pull requests to this fork should generally serve the Saturn effort or keep the existing app healthier for that work.

## Contribution Focus

The most useful contributions are:

- Sega Saturn launch, BIOS, disc, save, input, and controller fixes
- Ymir embedded or standalone integration work
- Saturn-specific library/import handling, artwork, metadata, and multi-disc behavior
- Stability fixes in emulator startup, shutdown, rendering, audio, or input paths
- Build, packaging, and portable-mode fixes that affect the Saturn/Ymir workflow
- Small frontend improvements that make Saturn testing or configuration easier

Broad multi-system changes are welcome when they are low-risk, well-tested, and do not distract from the Saturn path. Large unrelated rewrites should be discussed first.

## Getting Started

1. Fork the repository and clone it locally.
2. Open `Emutastic.sln` in Visual Studio 2022 or later.
3. Install the .NET 8 SDK.
4. Build the solution for Windows 10/11 x64.
5. Run the app and test with a local library.

The managed app should build without extra setup. Ymir standalone or embedded testing may require the optional payload under `portable/ymircore` or a locally built native wrapper.

## Saturn And Ymir Work

Before changing Saturn behavior, check the existing notes in `docs/ymir-integration-notes.md`.

Useful areas to understand:

- `Emutastic/Services/YmirLauncher.cs` handles standalone Ymir launch and profile setup.
- `Emutastic/Services/YmirNativeCore.cs` handles the embedded native wrapper path.
- `Emutastic/Services/CoreManager.cs` resolves core choices and fallback order.
- `Emutastic/Services/ConsoleHandlers/SaturnHandler.cs` contains Saturn-specific libretro handling.
- `native/ymir-telesto-core/` contains the native wrapper scaffold.

Please be careful with Saturn BIOS paths and filenames. Do not commit copyrighted BIOS files, game images, or bundled commercial content.

## Pull Requests

- Keep PRs focused: one feature, fix, or investigation result per PR.
- Explain which Saturn/Ymir path you tested: embedded, standalone, or libretro fallback.
- Test with at least one real Saturn disc image when touching launch, disc, BIOS, save, or input code.
- Keep migrations idempotent when touching SQLite schema code in `DatabaseService.InitializeDatabase()`.
- Match the existing WPF style and dark theme. The accent color is `#E03535`.
- Avoid broad formatting-only changes.

## Reporting Issues

Open an issue on GitHub with:

- What you were doing
- What you expected to happen
- What actually happened
- Your Windows version
- The Saturn core/path used, such as Ymir embedded, Ymir standalone, Mednafen Saturn, Kronos, or Yabause
- Relevant BIOS, disc format, controller, and log details

## Code Style

- C# with WPF / MVVM-lite patterns; avoid adding heavy frameworks.
- Match surrounding code style.
- Keep libretro callback paths allocation-free where possible because they run on the emulation thread.
- Prefer targeted fixes over abstractions unless the abstraction clearly reduces duplicated emulator-path logic.
