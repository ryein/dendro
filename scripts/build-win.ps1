<# 
  Usage:
    .\build-win.ps1 -Rhino R7 -Kind Dev
    .\build-win.ps1 -Rhino R8 -Kind Release [-Version 2.0.1]
#>

[CmdletBinding()]
param(
    # Rhino target: supports Rhino 7 (net48) or Rhino 8 (net8.0)
    [Parameter(Mandatory = $true)]
    [ValidateSet('R7', 'R8')]
    [string]$Rhino,

    # Build type: Dev (local debug packaging) or Release (versioned distribution)
    [Parameter(Mandatory = $true)]
    [ValidateSet('Dev', 'Release')]
    [string]$Kind,

    # Optional explicit version. If omitted, script tries git tag.
    [string]$Version
)

# Stop on errors and enforce strict variable use
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# --- Paths
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptRoot '..')
$ApiDir = Join-Path $RepoRoot 'DendroAPI'
$GhDir = Join-Path $RepoRoot 'DendroGH'
$DistRoot = Join-Path $RepoRoot 'dist'
$VcpkgDir = Join-Path $RepoRoot 'vcpkg'

# --- Enter Visual Studio dev shell so cmake/ninja/dotnet resolve
function Enter-BuildEnv {
    if ($env:VSCMD_VER) { return } # already inside dev shell
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found. Install VS Build Tools 2022 (C++ workload)." }
    $vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if (-not $vsPath) { throw "VS Build Tools with C++ workload not found." }
    Import-Module "$vsPath\Common7\Tools\Microsoft.VisualStudio.DevShell.dll" -ErrorAction Stop
    Enter-VsDevShell -VsInstallPath $vsPath -DevCmdArguments "-arch=x64 -host_arch=x64"
}

# --- Ensure a tool exists in PATH
function Assert-Tool($name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "Required tool '$name' not found in PATH."
    }
}

# --- Resolve a Ninja executable that can actually run
function Resolve-Ninja {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsNinja = & $vswhere -latest -products * -find '**\ninja.exe' |
            Select-Object -First 1
        if ($vsNinja -and (Test-Path $vsNinja)) {
            & $vsNinja --version *> $null
            if ($LASTEXITCODE -eq 0) { return $vsNinja }
        }
    }

    $pathNinja = Get-Command 'ninja' -ErrorAction SilentlyContinue
    if ($pathNinja) {
        try {
            & $pathNinja.Source --version *> $null
            if ($LASTEXITCODE -eq 0) { return $pathNinja.Source }
        }
        catch { }
    }

    throw "A working Ninja executable was not found. Repair the WinGet Ninja package or install the CMake tools included with Visual Studio."
}

# --- Ensure vcpkg exists and export vars for cmake
function Assert-Vcpkg {
    if (-not (Test-Path $VcpkgDir)) { throw "Missing vcpkg repo at $VcpkgDir" }
    $toolchain = Join-Path $VcpkgDir 'scripts\buildsystems\vcpkg.cmake'
    if (-not (Test-Path $toolchain)) { throw "vcpkg toolchain not found: $toolchain" }
    $env:VCPKG_ROOT = $VcpkgDir
    $env:VCPKG_TOOLCHAIN = $toolchain

    # Read baselines from both files (if present)
    $cfg = Get-Content (Join-Path $ApiDir 'vcpkg-configuration.json') -Raw | ConvertFrom-Json
    $baseline = $cfg.'default-registry'.baseline

    if ($baseline -and -not (git -c "safe.directory=$VcpkgDir" -C $VcpkgDir rev-parse --verify $baseline 2>$null)) {
        throw "vcpkg: baseline $baseline is not present in the local vcpkg checkout."
    }
    if ($baseline -and -not (git -c "safe.directory=$VcpkgDir" -C $VcpkgDir show "$baseline`:versions/baseline.json" 2>$null)) {
        throw "vcpkg: baseline $baseline is too old (missing versions/baseline.json). Update the baseline in vcpkg-configuration.json."
    }
}

# --- Compute release version from -Version or latest git tag
function Get-ReleaseVersion {
    param([string]$Version)
    $semver = '^(?<v>v)?(?<core>\d+\.\d+\.\d+)(?<pre>-[0-9A-Za-z\-\.]+)?(?<build>\+[0-9A-Za-z\-\.]+)?$'
    $legacyVersion = '^(?<v>v)?(?<major>\d+)\.(?<minor>\d+)$'

    # Explicit version
    if ($Version) {
        $v = $Version.Trim()
        if ($v -match $semver) { return "$($Matches['core'])$($Matches['pre'])$($Matches['build'])" }
        if ($v -match $legacyVersion) { return "$($Matches['major']).$($Matches['minor']).0" }
        throw "Version '$v' not valid semver. Use x.y.z or x.y.z-prerelease."
    }

    # Git tag
    $tag = $null
    try { $tag = (& git describe --tags --abbrev=0) 2>$null } catch { }
    if ($tag) {
        $v = $tag.Trim()
        if ($v -match $semver) { return "$($Matches['core'])$($Matches['pre'])$($Matches['build'])" }
        if ($v -match $legacyVersion) { return "$($Matches['major']).$($Matches['minor']).0" }
        throw "Git tag '$v' not valid semver. Use x.y.z or x.y.z-prerelease."
    }

    throw "No version provided and no git tag found. Use -Version x.y.z (e.g., 2.0.0 or 2.0.0-alpha)."
}

# --- Setup
Enter-BuildEnv
Assert-Tool 'cmake'
Assert-Tool 'dotnet'
Assert-Vcpkg
$NinjaPath = Resolve-Ninja

# --- Build native C++ API with cmake/ninja
Push-Location $ApiDir
try {
    & cmake --preset win-x64 "-DCMAKE_MAKE_PROGRAM=$NinjaPath"
    if ($LASTEXITCODE -ne 0) { throw "CMake configure failed with exit code $LASTEXITCODE." }

    & cmake --build --preset win-x64-release
    if ($LASTEXITCODE -ne 0) { throw "Native build failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }

# Verify DLL produced
$NativeDll = Join-Path $ApiDir 'build\win-x64\out\Release\DendroAPI.dll'
if (-not (Test-Path $NativeDll)) { throw "Missing native DLL: $NativeDll" }

# --- Build .NET Grasshopper plugin (depends on Rhino version)
if ($Rhino -eq 'R7') {
    $Proj = Join-Path $GhDir 'DendroGH.R7.csproj'
    $tfm = 'net48'
}
else {
    $Proj = Join-Path $GhDir 'DendroGH.R8.csproj'
    $tfm = 'net8.0'
}

dotnet restore $Proj
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }

dotnet build $Proj -c Release -v minimal /p:Platform=x64
if ($LASTEXITCODE -ne 0) { throw "Grasshopper build failed with exit code $LASTEXITCODE." }

# Locate .gha assembly
$ghaPrimary = Join-Path $GhDir ("bin\x64\Release\{0}\DendroGH.gha" -f $tfm)
$GhaPath = if (Test-Path $ghaPrimary) { $ghaPrimary } else { $null }
if (-not $GhaPath) { throw "DendroGH.gha not found. Checked:`n$($ghaPrimary -join "`n")" }

# --- Dist directory layout
if ($Kind -eq 'Dev') {
    $KindDir = Join-Path $DistRoot 'dev'
    $Dest = Join-Path $KindDir 'win-x64'
}
else {
    $KindDir = Join-Path $DistRoot 'releases'
    $ver = Get-ReleaseVersion -Version $Version
    $Dest = Join-Path $KindDir $ver
}
New-Item -ItemType Directory -Force -Path $Dest | Out-Null

# --- Copy artifacts into dist
Copy-Item -LiteralPath $GhaPath    -Destination (Join-Path $Dest 'DendroGH.gha') -Force
Copy-Item -LiteralPath $NativeDll  -Destination (Join-Path $Dest 'DendroAPI.dll') -Force

# --- Done
if ($Kind -eq 'Release') {
    Write-Host "OK → $Kind build ($Rhino) v$ver"
}
else {
    Write-Host "OK → $Kind build ($Rhino)"
}
Write-Host "Staged:"
Write-Host "  $($Dest)\DendroGH.gha"
Write-Host "  $($Dest)\DendroAPI.dll"
