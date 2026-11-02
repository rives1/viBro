<#
.SYNOPSIS
  Packages artifacts\publish\viBro.exe into dist\viBro-<version>-win-x64.zip (+ .sha256).
  Run build\build.ps1 first.

.PARAMETER ExpectedTag
  Optional git tag (e.g. v0.1.0). The script fails if it does not match the project version.
#>
[CmdletBinding()]
param([string]$ExpectedTag = '')

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $Root 'Directory.Build.props') -Raw
$version = $props.SelectSingleNode('/Project/PropertyGroup/Version').InnerText.Trim()

if ($ExpectedTag -and $ExpectedTag -ne "v$version") {
    throw "Tag '$ExpectedTag' does not match project version 'v$version' (Directory.Build.props)."
}

$exe = Join-Path $Root 'artifacts\publish\viBro.exe'
if (-not (Test-Path $exe)) { throw 'artifacts\publish\viBro.exe not found. Run build\build.ps1 first.' }

$dist = Join-Path $Root 'dist'
$stage = Join-Path $dist "viBro-$version-win-x64"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Copy-Item $exe $stage
foreach ($f in 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'README.md', 'CHANGELOG.md') {
    Copy-Item (Join-Path $Root $f) $stage
}

$zip = "$stage.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item $stage -Recurse -Force

$hash = (Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant()
Set-Content -Path "$zip.sha256" -Value "$hash  $(Split-Path -Leaf $zip)" -Encoding ASCII

Write-Host "==> $zip" -ForegroundColor Cyan
Write-Host "    sha256 $hash"
