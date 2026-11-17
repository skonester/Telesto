# Contributing to Telesto

Telesto is an Emutastic fork focused on a usable Windows library frontend and Sega Saturn integration with Ymir. Please preserve upstream attribution and keep changes small enough to test independently.

## Useful work

- Reproduce and fix Saturn launch, BIOS discovery, disc-format, input, audio/video, and backup RAM problems.
- Improve first-run setup and make the distinction between libretro, embedded Ymir, and standalone Ymir clear.
- Test weekly packages on clean Windows installations and report the exact build version.
- Help add save states, disc swapping, and pause/reset to embedded Ymir in small changes with game boot tests.

See [README.md](README.md) for build/setup instructions and [docs/releases.md](docs/releases.md) for the release process and known version history.

## Reporting a problem

Include the Telesto version from **Preferences > About**, `build-info.json` from the package if available, Windows version, selected backend, reproduction steps, and relevant logs. For Saturn, include the game title/region, disc format, and whether embedded or standalone Ymir was selected. Do not attach games, BIOS files, account credentials, or personal library databases.

## Pull requests

Explain the user-visible problem and expected result. Build with `dotnet build Emutastic.sln -c Release`. For native changes, run the wrapper's CTest smoke test and manually check boot, input, audio/video, and backup RAM with legally obtained local test files. Check that libretro still launches when changing shared frontend code.

CI builds both Ymir paths, publishes the frontend, checks the package, and builds the installer. PRs never publish GitHub releases. Features that need deeper emulator integration should include clear limitations rather than implying parity with the libretro backend.
