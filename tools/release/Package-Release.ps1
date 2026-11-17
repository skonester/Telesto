$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $root
$info = Get-Content .build/build-info.json -Raw | ConvertFrom-Json
$publish = Join-Path $root '.build/publish'
New-Item -ItemType Directory -Force dist, "$publish/Assets" | Out-Null
Copy-Item .build/build-info.json "$publish/build-info.json" -Force
Copy-Item Emutastic/README.txt, LICENSE.txt, LICENSE-CONTROLLER-ART.txt, AUTHORS.md $publish -Force
# Assets are WPF resources except this external database.
Copy-Item Emutastic/Assets/openvgdb.sqlite "$publish/Assets/openvgdb.sqlite" -Force
New-Item -ItemType Directory -Force "$publish/licenses/Ymir" | Out-Null
Copy-Item .build/ymir/LICENSE "$publish/licenses/Ymir/LICENSE.txt" -Force

foreach ($relative in @('Telesto.exe', 'telesto-ymir-core.dll', 'build-info.json', 'Assets/openvgdb.sqlite', 'licenses/Ymir/LICENSE.txt')) {
    if (-not (Test-Path "$publish/$relative" -PathType Leaf)) { throw "Package is missing $relative" }
}
$standalone = @(Get-ChildItem "$publish/ymircore" -Recurse -File | Where-Object { $_.Name -in @('ymir-sdl3.exe', 'ymir.sdl3.exe', 'ymir.exe') })
if ($standalone.Count -ne 1) { throw 'Standalone Ymir is missing or ambiguous.' }
$forbidden = @(Get-ChildItem $publish -Recurse -File | Where-Object { $_.Name -in @('ipl.bin', 'ymir-core.dll', 'zd.dll', 'library.db', 'portable.txt') })
if ($forbidden.Count) { throw "Unexpected firmware, legacy binaries or user data: $($forbidden.Name -join ', ')" }
$fileVersion = (Get-Item "$publish/Telesto.exe").VersionInfo.FileVersion
if ([version]$fileVersion -ne [version]$info.version) { throw "Executable version $fileVersion does not match $($info.version)." }

# Test the shipped DLL from the package directory so missing runtime DLLs fail here.
Copy-Item .build/native/telesto-ymir-smoke.exe "$publish/telesto-ymir-smoke.exe" -Force
try {
    & "$publish/telesto-ymir-smoke.exe"
    if ($LASTEXITCODE -ne 0) { throw 'Packaged Ymir DLL failed its lifecycle smoke test.' }
} finally {
    Remove-Item -LiteralPath "$publish/telesto-ymir-smoke.exe" -Force
}

$zipName = "Telesto-$($info.tag)-win-x64.zip"
Compress-Archive -Path "$publish/*" -DestinationPath "dist/$zipName" -Force
$isccCommand = Get-Command iscc -ErrorAction SilentlyContinue
$iscc = if ($isccCommand) { $isccCommand.Source } else { "${env:ProgramFiles(x86)}/Inno Setup 6/ISCC.exe" }
if (-not (Test-Path $iscc)) { throw 'Inno Setup 6 is required to build the installer.' }
& $iscc "/DMyAppVersion=$($info.version)" "/DMyPublishDir=$publish" "/O$root/dist" installer.iss
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }
$setupName = "Telesto-$($info.version)-win-x64-setup.exe"
if (-not (Test-Path "dist/$setupName")) { throw 'Installer output missing.' }
$info | ConvertTo-Json | Set-Content dist/build-info.json -Encoding utf8
$hashes = foreach ($name in @($zipName, $setupName, 'build-info.json')) {
    $hash = (Get-FileHash "dist/$name" -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $name"
}
$hashes | Set-Content dist/SHA256SUMS.txt -Encoding ascii
$notes = @"
Windows x64 build with embedded and standalone Ymir $($info.ymir_tag) (SSE2).

- Telesto version: **$($info.version)**
- Telesto source: https://github.com/skonester/Telesto/commit/$($info.telesto_commit)
- Ymir source: https://github.com/$($info.ymir_repository)/commit/$($info.ymir_commit)
- Build: $($info.workflow_run)

Download the ZIP to extract and run, or the setup EXE to install. Both include .NET 8.
Install the Microsoft Visual C++ x64 runtime: https://aka.ms/vs/17/release/vc_redist.x64.exe
BIOS files and games are supplied by the user. Embedded Ymir remains experimental.

Automated checks cover compilation, native DLL lifecycle, package contents and version identity.
Game boot and gameplay have not been tested by this workflow.
Build provenance is in build-info.json; verify downloads with SHA256SUMS.txt.
"@
$notes | Set-Content dist/release-notes.md -Encoding utf8
