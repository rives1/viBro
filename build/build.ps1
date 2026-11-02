<#
.SYNOPSIS
  Builds the portable viBro.exe (single file, self-contained, win-x64).

.DESCRIPTION
  1. Finds the .NET 10 SDK (or installs it per-user into .dotnet\ with -InstallDotnet, no admin).
  2. Downloads the pinned libmpv build (build\dependencies.json), verifies SHA-256, extracts libmpv-2.dll
     into deps\libmpv\.
  3. Publishes src\viBro to artifacts\publish\viBro.exe and checks that it is a single file.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build\build.ps1 -InstallDotnet
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$InstallDotnet,     # install the .NET 10 SDK into .dotnet\ if missing (per-user, no admin)
    [switch]$Compress,          # enable single-file compression (smaller EXE, slower start; for comparison)
    [switch]$DepsOnly           # only fetch libmpv
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

$Root = Split-Path -Parent $PSScriptRoot
$Deps = Get-Content (Join-Path $PSScriptRoot 'dependencies.json') -Raw | ConvertFrom-Json

function Write-Step([string]$Text) { Write-Host "==> $Text" -ForegroundColor Cyan }

function Get-ProjectVersion {
    [xml]$props = Get-Content (Join-Path $Root 'Directory.Build.props') -Raw
    $node = $props.SelectSingleNode('/Project/PropertyGroup/Version')
    if (-not $node) { throw 'Version not found in Directory.Build.props' }
    return $node.InnerText.Trim()
}

function Get-FileFromUrls([string[]]$Urls, [string]$OutFile) {
    foreach ($url in $Urls) {
        try {
            Write-Host "    downloading $url"
            Invoke-WebRequest -Uri $url -OutFile $OutFile -UseBasicParsing -MaximumRedirection 10
            if ((Get-Item $OutFile).Length -gt 0) { return }
        } catch {
            Write-Warning "    failed: $($_.Exception.Message)"
        }
    }
    throw "Download failed from all sources: $($Urls -join ', ')"
}

# SHA-256 via .NET: Get-FileHash lives in a script module that some locked-down machines block.
function Get-Sha256([string]$Path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $fs = [System.IO.File]::OpenRead($Path)
    try { return ([System.BitConverter]::ToString($sha.ComputeHash($fs)) -replace '-', '').ToLowerInvariant() }
    finally { $fs.Dispose(); $sha.Dispose() }
}

# Runs a native tool without letting its stderr abort the script (Windows PowerShell 5.1 behaviour).
function Invoke-Quiet([scriptblock]$Block) {
    $saved = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { & $Block 2>$null } finally { $ErrorActionPreference = $saved }
}

# ------------------------------------------------------------------ .NET SDK
function Resolve-Dotnet {
    $local = Join-Path $Root '.dotnet\dotnet.exe'
    $candidates = @()
    if (Test-Path $local) { $candidates += $local }
    $global = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($global) { $candidates += $global.Source }

    foreach ($exe in $candidates) {
        $sdks = Invoke-Quiet { & $exe --list-sdks }
        if ($sdks -match '^10\.') { return $exe }
    }

    if (-not $InstallDotnet) {
        throw ".NET 10 SDK not found. Re-run with -InstallDotnet (per-user install into .dotnet\, no admin) " +
              "or install it from https://dotnet.microsoft.com/download/dotnet/10.0"
    }

    Write-Step 'Installing .NET 10 SDK into .dotnet\ (per-user, no admin)'
    $dir = Join-Path $Root '.dotnet'
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $installer = Join-Path $dir 'dotnet-install.ps1'
    Invoke-WebRequest -Uri $Deps.dotnet.installScript -OutFile $installer -UseBasicParsing
    & $installer -Channel $Deps.dotnet.channel -InstallDir $dir -NoPath
    if (-not (Test-Path $local)) { throw '.NET SDK installation failed.' }
    return $local
}

# ------------------------------------------------------------------ libmpv
function Expand-7z([string]$Archive, [string]$Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    # 1) Windows' bundled tar (libarchive). Recent Windows 11 builds read .7z; older ones may not.
    $tar = Join-Path $env:SystemRoot 'System32\tar.exe'
    if (Test-Path $tar) {
        Invoke-Quiet { & $tar -xf $Archive -C $Destination } | Out-Null
        if ($LASTEXITCODE -eq 0 -and (Get-ChildItem $Destination -Recurse -Filter $Deps.libmpv.dll)) { return }
        Write-Host '    tar.exe could not extract the archive, using 7zr.exe'
    }

    # 2) Official standalone 7zr.exe (no installation).
    $tools = Join-Path $Root 'deps\tools'
    New-Item -ItemType Directory -Force -Path $tools | Out-Null
    $sevenZip = Join-Path $tools '7zr.exe'
    if (-not (Test-Path $sevenZip)) {
        Get-FileFromUrls @($Deps.sevenZip.url) $sevenZip
    }
    $hash = Get-Sha256 $sevenZip
    if ($Deps.sevenZip.sha256) {
        if ($hash -ne $Deps.sevenZip.sha256) { throw "7zr.exe SHA-256 mismatch: $hash" }
    } else {
        Write-Warning "    7zr.exe SHA-256 not pinned. Downloaded file: $hash (pin it in build\dependencies.json)"
    }
    & $sevenZip x $Archive "-o$Destination" -y | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "7zr.exe failed with exit code $LASTEXITCODE" }
}

function Install-LibMpv {
    $m = $Deps.libmpv
    $target = Join-Path $Root 'deps\libmpv'
    $dll = Join-Path $target $m.dll
    $stamp = Join-Path $target 'VERSION.txt'

    if ((Test-Path $dll) -and (Test-Path $stamp) -and ((Get-Content $stamp -Raw).Trim() -eq $m.version)) {
        Write-Step "libmpv $($m.version) already present"
        return
    }

    Write-Step "Fetching libmpv $($m.version)"
    $download = Join-Path $Root 'deps\download'
    New-Item -ItemType Directory -Force -Path $download | Out-Null
    $archive = Join-Path $download $m.archive

    if (-not (Test-Path $archive) -or (Get-Sha256 $archive) -ne $m.sha256) {
        Get-FileFromUrls $m.urls $archive
    }
    $hash = Get-Sha256 $archive
    if ($hash -ne $m.sha256) {
        throw "libmpv archive SHA-256 mismatch.`n  expected $($m.sha256)`n  actual   $hash`n" +
              "Do not continue with an unverified binary. Check build\dependencies.json."
    }
    Write-Host '    SHA-256 verified'

    $extract = Join-Path $download 'extract'
    if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
    Expand-7z $archive $extract

    $found = Get-ChildItem $extract -Recurse -Filter $m.dll | Select-Object -First 1
    if (-not $found) { throw "$($m.dll) not found in $($m.archive)" }

    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item $found.FullName $dll -Force
    Set-Content -Path $stamp -Value $m.version -Encoding ASCII
    Remove-Item $extract -Recurse -Force
    Write-Host "    $($m.dll): $([math]::Round((Get-Item $dll).Length / 1MB, 1)) MB"
}

# ------------------------------------------------------------------ main
$version = Get-ProjectVersion
Write-Step "viBro $version ($Configuration)"

Install-LibMpv
if ($DepsOnly) { return }

$dotnet = Resolve-Dotnet
Write-Step "Using $dotnet ($(& $dotnet --version))"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

# Vulkan loader embedded in the EXE (third_party\vulkan-loader, see SOURCE.md): check it was not altered.
$vulkan = Join-Path $Root 'third_party\vulkan-loader\vulkan-1.dll'
$vulkanSha = '8878cdc6e7656fd8c81135eaf546ce97a4dab8e75b2023bc28fb97a33c2d96f8'
if ((Get-Sha256 $vulkan) -ne $vulkanSha) { throw "third_party\vulkan-loader\vulkan-1.dll SHA-256 mismatch" }

# The compressed variant goes to its own folder so both EXEs can be compared side by side.
$publishDir = Join-Path $Root $(if ($Compress) { 'artifacts\publish-compressed' } else { 'artifacts\publish' })
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

Write-Step 'Publishing single-file EXE'
$compressValue = if ($Compress) { 'true' } else { 'false' }
& $dotnet publish (Join-Path $Root 'src\viBro\viBro.csproj') `
    -c $Configuration -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=$compressValue `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }

# The EXE must be the only file: anything else means a dependency was not embedded.
$exe = Join-Path $publishDir 'viBro.exe'
if (-not (Test-Path $exe)) { throw 'viBro.exe not produced' }
$extra = @(Get-ChildItem $publishDir -File | Where-Object { $_.Name -ne 'viBro.exe' })
if ($extra.Count -gt 0) {
    $names = ($extra | ForEach-Object { $_.Name }) -join ', '
    if ($extra.Name -contains $Deps.libmpv.dll -or $extra.Name -contains 'vulkan-1.dll') {
        throw "Native libraries were not embedded in the EXE (found next to it). Files: $names"
    }
    Write-Warning "Unexpected files next to viBro.exe: $names"
}

$info = (Get-Item $exe).VersionInfo
Write-Step "Done: $exe"
Write-Host "    size            $([math]::Round((Get-Item $exe).Length / 1MB, 1)) MB"
Write-Host "    file version    $($info.FileVersion)"
Write-Host "    product version $($info.ProductVersion)"
