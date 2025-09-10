<# 
  setup-win.ps1
  Installs build prerequisites and bootstraps vcpkg for Dendro.

  Usage:
    .\setup-win.ps1 -RepoRoot "C:\path\to\repo" -PkgMgr winget|choco
#>

[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..')),
    [ValidateSet('winget', 'choco')]
    [string]$PkgMgr = 'winget'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
function Get-VSWithCppPath {
    if (-not (Test-Path $vswhere)) { return $null }
    try {
        & $vswhere -latest -products * `
            -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
            -property installationPath 2>$null
    }
    catch { $null }
}

function Test-NetFx48DevPack {
    $ref = 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades'
    Test-Path $ref
}


# --- Logging helpers (PowerShell 5/7 compatible)
function Say([string]$msg, [ConsoleColor]$color = [ConsoleColor]::Gray) {
    Write-Host $msg -ForegroundColor $color
}
function Step([string]$msg) { Say "==> $msg" Cyan }
function Ok([string]$msg) { Say "✔ $msg" Green }
function Warn([string]$msg) { Say "⚠ $msg" Yellow }
function Fail([string]$msg) { Say "✖ $msg" Red }
function Dot() { Write-Host "." -NoNewline -ForegroundColor DarkGray }

function Get-ToolVersion([string]$name) {
    try {
        $cmd = Get-Command $name -ErrorAction Stop
        if ($cmd.FileVersionInfo -and $cmd.FileVersionInfo.FileVersion) { return $cmd.FileVersionInfo.FileVersion }
    }
    catch { }
    try {
        $line = & $name --version 2>$null | Select-Object -First 1
        if ($line) { return ($line -replace '\s+', ' ') }
    }
    catch { }
    return $null
}

function Have([string]$name) { [bool](Get-Command $name -ErrorAction SilentlyContinue) }

function Assert-Admin {
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    ).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
    if (-not $isAdmin) { throw "Run this script in an elevated (Administrator) PowerShell." }
    Ok "Running elevated."
}

function Assert-Tool([string]$name) {
    $v = Get-ToolVersion $name
    if ($v) { Ok "Found $name ($v)" }
    else { throw "Missing tool: $name (install may have failed)." }
}

function Install-WithWinget {
    param([string]$Id, [string]$Override = $null, [string]$Display)
    Step "Installing $Display via winget (Id: $Id)..."
    $args = @('install', '--id', $Id, '--silent', '--accept-package-agreements', '--accept-source-agreements', '--scope', 'machine')
    if ($Override) { $args += @('--override', $Override) }
    $out = & winget @args 2>&1
    $code = $LASTEXITCODE

    if ($code -ne 0) {
        if ($out -match 'already installed' -or $out -match 'No available upgrade') {
            Ok "$Display already installed (winget)."; return
        }
        if ($out -match 'No package found matching input criteria') {
            Warn "winget couldn’t find $Display (Id: $Id). Will try Chocolatey if available."; throw "WINGET_NOT_FOUND"
        }
        Fail "winget failed for $Display (code $code)."; throw "WINGET_FAILED"
    }
    Ok "$Display installed."
}

function Install-WithChoco {
    param([string]$Pkg, [string[]]$Params = @(), [string]$Display)
    Step "Installing $Display via Chocolatey (Pkg: $Pkg)..."
    $args = @('install', $Pkg, '-y', '--no-progress')
    if ($Params) { $args += $Params }
    & choco @args
    if ($LASTEXITCODE -ne 0) { throw "choco install failed for $Display ($Pkg) with exit code $LASTEXITCODE." }
    Ok "$Display installed."
}

function Install {
    param(
        [string]$WingetId,
        [string]$ChocoPkg,
        [string]$Name,                 # human-friendly name shown in logs
        [string]$Override = $null,
        [string[]]$ChocoParams = @()
    )

    if ($PkgMgr -eq 'winget' -and -not (Have 'winget')) {
        Warn "winget not found → falling back to Chocolatey."
        $script:PkgMgr = 'choco'
    }
    if ($PkgMgr -eq 'choco' -and -not (Have 'choco')) {
        throw "Neither winget nor choco available. Install one (winget recommended on Win 10/11)."
    }

    if ($PkgMgr -eq 'winget') { Install-WithWinget -Id $WingetId -Override $Override -Display $Name }
    else { Install-WithChoco  -Pkg $ChocoPkg -Params $ChocoParams -Display $Name }
}

# ---- Start
Say "Dendro Windows setup • PowerShell $($PSVersionTable.PSVersion) • PkgMgr: $PkgMgr" DarkCyan
Assert-Admin

# --- Visual Studio Build Tools 2022 + C++ workload
$vsPath = Get-VSWithCppPath
if ($vsPath) {
    Ok "VS Build Tools + C++ workload already present: $vsPath"
}
else {
    Install -WingetId 'Microsoft.VisualStudio.2022.BuildTools' `
        -ChocoPkg 'visualstudio2022buildtools' `
        -Name 'Visual Studio 2022 Build Tools + C++ workload' `
        -Override '--add Microsoft.VisualStudio.Workload.VCTools --includeRecommended --quiet --wait --norestart'
    # Re-check to confirm workload landed
    $vsPath = Get-VSWithCppPath
    if (-not $vsPath) { throw "VS Build Tools install finished but C++ workload not detected." }
    Ok "VS Build Tools with C++ workload detected: $vsPath"
}
# --- CMake
if (Have 'cmake') { Ok "CMake already present ($(Get-ToolVersion cmake))" }
else { Install -WingetId 'Kitware.CMake' -ChocoPkg 'cmake' -Name 'CMake' }

# --- Ninja
if (Have 'ninja') { Ok "Ninja already present ($(Get-ToolVersion ninja))" }
else { Install -WingetId 'Ninja-build.Ninja' -ChocoPkg 'ninja' -Name 'Ninja' }

# --- Git
if (Have 'git') { Ok "Git already present ($(Get-ToolVersion git))" }
else { Install -WingetId 'Git.Git' -ChocoPkg 'git' -Name 'Git' }

# --- .NET SDK 8.x (for R8)
Install -WingetId 'Microsoft.DotNet.SDK.8' -ChocoPkg 'dotnet-sdk' -Name '.NET SDK 8.x'

# --- .NET Framework 4.8 Dev Pack (for R7)
if (Test-NetFx48DevPack) {
    Ok ".NET Framework 4.8 Developer Pack already present"
}
else {
    try {
        # correct ID:
        Install -WingetId 'Microsoft.DotNet.Framework.DeveloperPack_4' `
            -ChocoPkg 'netfx-4.8-devpack' `
            -Name '.NET Framework 4.8 Developer Pack'
    }
    catch {
        if ($_.Exception.Message -eq 'WINGET_NOT_FOUND' -or $_.Exception.Message -eq 'WINGET_FAILED') {
            if (Have 'choco') {
                Install-WithChoco -Pkg 'netfx-4.8-devpack' -Display '.NET Framework 4.8 Developer Pack'
            }
            else {
                throw ".NET Framework 4.8 Developer Pack: winget failed and Chocolatey not available."
            }
        }
        else { throw }
    }

    if (-not (Test-NetFx48DevPack)) { throw ".NET Framework 4.8 Developer Pack install did not materialize." }
    Ok ".NET Framework 4.8 Developer Pack ready"
}

# --- Confirm core tools
foreach ($t in 'cmake', 'ninja', 'git', 'dotnet') { Assert-Tool $t }

# --- Bootstrap vcpkg
$VcpkgDir = Join-Path $RepoRoot 'vcpkg'
if (-not (Test-Path $VcpkgDir)) {
  Write-Host "vcpkg not found at $VcpkgDir → cloning..."
  git --version *> $null
  if ($LASTEXITCODE -ne 0) { throw "Git not found; install Git first." }
  & git clone --depth 1 https://github.com/microsoft/vcpkg $VcpkgDir
  if ($LASTEXITCODE -ne 0) { throw "Failed to clone vcpkg into $VcpkgDir." }
}

# If the repo is shallow, make it a full clone so pinned baselines exist
$gitDir = Join-Path $VcpkgDir '.git'
$shallowFile = Join-Path $gitDir 'shallow'
if (Test-Path $gitDir) {
  if (Test-Path $shallowFile) {
    Step "Converting vcpkg to a full clone (needed for builtin baselines)"
    & git -C $VcpkgDir fetch origin --prune --tags --unshallow
    if ($LASTEXITCODE -ne 0) { throw "Failed to unshallow vcpkg repository." }
    Ok "vcpkg repository is now a full clone."
  } else {
    # Still refresh tags/remote state to ensure baseline/tag availability
    & git -C $VcpkgDir fetch origin --prune --tags
    if ($LASTEXITCODE -ne 0) { throw "Failed to fetch tags for vcpkg repository." }
  }
}

$bootstrapBat = Join-Path $VcpkgDir 'bootstrap-vcpkg.bat'
if (-not (Test-Path $bootstrapBat)) { throw "Missing $bootstrapBat" }

Step "Bootstrapping vcpkg at $VcpkgDir"
Push-Location $VcpkgDir
try {
  & $bootstrapBat -disableMetrics | ForEach-Object { Write-Host $_ }
  if ($LASTEXITCODE -ne 0) { throw "vcpkg bootstrap failed with exit code $LASTEXITCODE." }
  Ok "vcpkg bootstrapped."
}
finally { Pop-Location }

# --- Export env vars (session + machine)
$toolchain = Join-Path $VcpkgDir 'scripts\buildsystems\vcpkg.cmake'
if (-not (Test-Path $toolchain)) { throw "Missing vcpkg toolchain at $toolchain" }

$env:VCPKG_ROOT = $VcpkgDir
$env:VCPKG_TOOLCHAIN = $toolchain
[Environment]::SetEnvironmentVariable('VCPKG_ROOT', $VcpkgDir, 'Machine')
[Environment]::SetEnvironmentVariable('VCPKG_TOOLCHAIN', $toolchain, 'Machine')
Ok "Environment set: VCPKG_ROOT, VCPKG_TOOLCHAIN"

# --- Verify VS Dev Shell
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found; VS Build Tools install may have failed." }
$vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $vsPath) { throw "VS Build Tools with C++ workload not detected." }
Ok "VS Build Tools detected at: $vsPath"

Say ""
Say "All set. Open a NEW elevated shell to pick up env vars, then run:" DarkCyan
Say "  scripts\build-win.ps1 -Rhino R7 -Kind Dev" DarkCyan
Say "  scripts\build-win.ps1 -Rhino R8 -Kind Release -Version 2.0.0" DarkCyan
