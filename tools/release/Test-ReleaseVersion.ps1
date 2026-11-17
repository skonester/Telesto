$ErrorActionPreference = 'Stop'
$script = "$PSScriptRoot/Get-ReleaseVersion.ps1"
$date = [datetime]'2026-11-17T09:23:00Z'
$weekly = & $script -RunNumber 42 -Timestamp $date
if ($weekly.version -ne '2026.11.17.42' -or $weekly.tag -ne 'v2026.11.17.42') { throw 'Weekly identity failed.' }
foreach ($tag in @('v1.8.0', 'v2026.11.17.42')) {
    $actual = & $script -Tag $tag
    if ($actual.tag -ne $tag) { throw "Tag round trip failed: $tag" }
}
foreach ($tag in @('telesto2', 'v1.8.0-beta', 'v1.8', 'v1.8.0;echo bad', 'v65535.0.0')) {
    $rejected = $false
    try { $null = & $script -Tag $tag } catch { $rejected = $true }
    if (-not $rejected) { throw "Invalid tag accepted: $tag" }
}
$next = & $script -RunNumber 43 -Timestamp $date
if ([version]$next.version -le [version]$weekly.version) { throw 'Same-day releases must increase.' }
Write-Output 'Release version checks passed.'
