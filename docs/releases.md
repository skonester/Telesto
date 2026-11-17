# Building and maintaining Telesto releases

## What the old versions mean

- The checked-in frontend project declares version **1.5.3**. This was also the value at the `telesto2` source tag, commit `c639234` (June 2, 2026).
- The old standalone `portable/ymircore/ymir-sdl3.exe` reports **Ymir 0.3.1**. Its exact source commit was not recorded. The historical uploaded ZIP cannot be assigned a more precise upstream revision from that alone.
- `1f566b5` in the stability notes is a **Telesto commit**, not a Ymir version.
- Other `v1.x` tags exist in inherited Emutastic history; they do not identify the Ymir version bundled with Telesto.
- The new pinned development baseline is [Ymir v0.4.1](https://github.com/ymir-emu/Ymir/releases/tag/v0.4.1), commit `d49c0868cf9998d279394498ad0cd0efbd102ccb`, recorded in `native/ymir-source.json`.

## Automatic releases

`.github/workflows/release.yml` builds on pull requests, pushes to `main`, numeric version tags, manual dispatch, and Mondays at **09:23 UTC** (03:23 CST / 04:23 CDT).

Only scheduled runs, manual runs on `main` with publishing enabled, and numeric version tags publish releases in `skonester/Telesto`. Pull requests and ordinary pushes upload downloadable workflow artifacts instead.

Weekly/manual versions use `YEAR.MONTH.DAY.RUN_NUMBER`, for example `v2026.11.17.42`. Every new run gets a unique fourth component. Rerunning the same workflow run retains its version; an already published release is never overwritten. Old three-part numeric tags remain supported. Use calendar versions going forward: a new `v1.x` tag will compare older than calendar versions in the app.

Weekly runs resolve the latest **stable** Ymir release once, build the embedded wrapper from that release's exact commit, and download its official Windows x64 SSE2 standalone archive after verifying the upstream SHA256 digest. Both backends therefore follow the same upstream release. A source/API/build failure prevents publication and leaves the previous release available.

PRs, pushes, and numeric tags use the pinned Ymir version in `native/ymir-source.json`; manual runs can choose either the pin or the latest stable upstream release. To move the development pin, update both its tag and full commit hash and submit a PR. Never replace upstream files with stub CMake files.

Every package includes both Ymir backends, .NET, licenses, and the OpenVGDB database. Games, BIOS, personal libraries, and legacy debug binaries are excluded. Release assets include the ZIP, installer, `build-info.json`, and `SHA256SUMS.txt`. The manifest records Telesto/Ymir commits, the upstream archive digest, build time, and workflow URL. The executable also carries the source commits in its informational version.

The automated tests cover compilation, native DLL create/reset/destroy and missing-disc error handling, package contents, and executable version. They do **not** establish game compatibility. Before promoting a build to users, check Saturn boot, input, audio/video, backup saves, and an existing libretro game. Embedded Ymir remains experimental.

## Activate or run immediately

1. Put the workflow and supporting changes on the repository's default branch, `main`.
2. Open **Actions > Build and release**. If GitHub disabled workflows for this fork, choose **Enable workflows**. Scheduled workflows run only from the default branch and GitHub can disable schedules after 60 days without repository activity.
3. Choose **Run workflow**, select `main`, and leave **latest_ymir** and **publish_release** enabled to build and publish immediately. The normal `GITHUB_TOKEN` is sufficient; no personal token or additional secret is needed.
4. Confirm the Windows build and release jobs succeed and both downloadable packages are attached. The build job is read-only; only the final release job gets `contents: write`.

GitHub schedules are best effort and can be delayed. Failures appear in Actions; repository maintainers should enable notifications for failed workflows. A failed upload leaves a draft that the same run can finish on retry.

## Local equivalent

Use Windows, .NET 8 SDK, Visual Studio 2022 C++ Build Tools with a Windows SDK containing DXC, Clang, CMake 3.28+, Ninja, and Inno Setup 6. CMake 3.31.10 is a known working version.

```powershell
$env:GITHUB_RUN_NUMBER = '1'
$env:GITHUB_SERVER_URL = 'https://github.com'
$env:GITHUB_REPOSITORY = 'skonester/Telesto'
$env:GITHUB_RUN_ID = 'local'
New-Item -ItemType Directory -Force .build | Out-Null
$env:GITHUB_OUTPUT = "$PWD/.build/outputs.txt"
./tools/release/Test-ReleaseVersion.ps1
./tools/release/Prepare-Release.ps1
$info = Get-Content .build/build-info.json -Raw | ConvertFrom-Json
git clone https://github.com/ymir-emu/Ymir.git .build/ymir
git -C .build/ymir checkout $info.ymir_commit
./tools/release/Build-Ymir.ps1
./tools/release/Get-YmirRuntime.ps1
```

Then download/extract OpenVGDB as shown in the workflow, publish with its version/runtime directory properties and `PublishDir` set to `.build/publish/`, and run `./tools/release/Package-Release.ps1`. Outputs are in `dist/`. Local builds from uncommitted edits are development artifacts; only committed CI builds have complete source provenance.
