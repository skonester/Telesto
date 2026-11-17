param([switch]$LatestYmir)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $root
New-Item -ItemType Directory -Force .build | Out-Null
$tag = if ($env:GITHUB_REF_TYPE -eq 'tag') { $env:GITHUB_REF_NAME } else { '' }
$identity = & "$PSScriptRoot/Get-ReleaseVersion.ps1" -Tag $tag -RunNumber ([int]$env:GITHUB_RUN_NUMBER)
$lock = Get-Content native/ymir-source.json -Raw | ConvertFrom-Json
$headers = @{ 'User-Agent' = 'Telesto-release'; Accept = 'application/vnd.github+json' }
if ($env:GH_TOKEN) { $headers.Authorization = "Bearer $env:GH_TOKEN" }
$api = "https://api.github.com/repos/$($lock.repository)"
$releasePath = if ($LatestYmir) { 'latest' } else { "tags/$($lock.tag)" }
$release = Invoke-RestMethod "$api/releases/$releasePath" -Headers $headers
if ($release.draft -or $release.prerelease) { throw 'Ymir must be a published stable release.' }
$commit = (Invoke-RestMethod "$api/commits/$($release.tag_name)" -Headers $headers).sha
if (-not $LatestYmir -and $commit -ne $lock.commit) { throw 'Pinned Ymir tag no longer matches its recorded commit.' }
$assets = @($release.assets | Where-Object { $_.name -cmatch '^ymir-windows-x86_64-SSE2-.*\.zip$' })
if ($assets.Count -ne 1) { throw 'Expected exactly one upstream Windows x64 SSE2 package.' }
$asset = $assets[0]
if ($asset.digest -notmatch '^sha256:[0-9a-f]{64}$') { throw 'Ymir asset has no SHA256 digest.' }
$sourceCommit = (git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Could not resolve Telesto source commit.' }
$dirty = @(git status --porcelain).Count -gt 0
$metadata = [ordered]@{
    version = $identity.version
    tag = $identity.tag
    built_at_utc = [datetime]::UtcNow.ToString('o')
    telesto_commit = $sourceCommit
    telesto_dirty = $dirty
    ymir_repository = $lock.repository
    ymir_tag = $release.tag_name
    ymir_commit = $commit
    ymir_asset = $asset.name
    ymir_asset_url = $asset.browser_download_url
    ymir_asset_sha256 = $asset.digest.Substring(7)
    workflow_run = "$env:GITHUB_SERVER_URL/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID"
}
$metadata | ConvertTo-Json | Set-Content .build/build-info.json -Encoding utf8
$outputs = @{
    version = $identity.version
    tag = $identity.tag
    ymir_repository = $lock.repository
    ymir_commit = $commit
}
$outputs.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" } | Add-Content $env:GITHUB_OUTPUT
Write-Output "Telesto $($identity.version), Ymir $($release.tag_name) ($commit)"
