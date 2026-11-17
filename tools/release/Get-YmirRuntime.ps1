$ErrorActionPreference = 'Stop'
Set-Location (Resolve-Path "$PSScriptRoot/../..").Path
$info = Get-Content .build/build-info.json -Raw | ConvertFrom-Json
Invoke-WebRequest $info.ymir_asset_url -OutFile .build/ymir-runtime.zip
$hash = (Get-FileHash .build/ymir-runtime.zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $info.ymir_asset_sha256) { throw 'Ymir download checksum mismatch.' }
Expand-Archive .build/ymir-runtime.zip .build/ymir-runtime -Force
$executables = @(Get-ChildItem .build/ymir-runtime -Recurse -File | Where-Object { $_.Name -in @('ymir-sdl3.exe', 'ymir.sdl3.exe', 'ymir.exe') })
if ($executables.Count -ne 1) { throw 'Expected one standalone Ymir executable.' }
"runtime_dir=$($executables[0].Directory.FullName)" | Add-Content $env:GITHUB_OUTPUT
