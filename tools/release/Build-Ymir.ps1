$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $root
# Use VS for the Windows SDK, DXC, linker and CRT headers.
$vswhere = "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe"
$vsPath = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) { throw 'Visual Studio C++ build tools are required.' }
Import-Module "$vsPath/Common7/Tools/Microsoft.VisualStudio.DevShell.dll"
Enter-VsDevShell -VsInstallPath $vsPath -SkipAutomaticLocation -DevCmdArguments '-arch=x64 -host_arch=x64'
# The core has no vcpkg dependency; initialize only vendored libraries.
git -C .build/ymir submodule update --init --recursive --depth 1 -- vendor
if ($LASTEXITCODE -ne 0) { throw 'Could not initialize Ymir dependencies.' }
cmake -S native/ymir-telesto-core -B .build/native -G Ninja `
    -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=clang-cl -DCMAKE_CXX_COMPILER=clang-cl `
    "-DTELESTO_YMIR_SOURCE_DIR=$root/.build/ymir" -DBUILD_TESTING=ON
if ($LASTEXITCODE -ne 0) { throw 'Ymir configure failed.' }
cmake --build .build/native --parallel 4
if ($LASTEXITCODE -ne 0) { throw 'Ymir build failed.' }
ctest --test-dir .build/native --output-on-failure
if ($LASTEXITCODE -ne 0) { throw 'Ymir DLL smoke test failed.' }
