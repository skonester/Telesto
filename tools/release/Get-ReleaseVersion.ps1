param(
    [string]$Tag = '',
    [int]$RunNumber = 0,
    [datetime]$Timestamp = [datetime]::UtcNow
)
$ErrorActionPreference = 'Stop'
if ($Tag) {
    if ($Tag -cnotmatch '^v\d+\.\d+\.\d+(\.\d+)?$') {
        throw "Release tags must be vMAJOR.MINOR.PATCH or vMAJOR.MINOR.PATCH.REVISION: $Tag"
    }
    $version = $Tag.Substring(1)
} else {
    if ($RunNumber -lt 1) { throw 'A positive workflow run number is required.' }
    $version = '{0}.{1}.{2}.{3}' -f $Timestamp.Year, $Timestamp.Month, $Timestamp.Day, $RunNumber
}
$parts = $version.Split('.') | ForEach-Object { [int]$_ }
if (@($parts | Where-Object { $_ -gt 65534 }).Count) {
    throw "Version components must fit Windows assembly version fields: $version"
}
[pscustomobject]@{ version = $version; tag = "v$version" }
