<#
.SYNOPSIS
  Builds Menew for one architecture and packs it with Velopack: installer, portable zip, full and delta
  packages and the update feed. Used by CI (every PR) and by the Release workflow, and runs on any Windows
  machine with the .NET SDK (and, for Native AOT, the "Desktop development with C++" tools).

.EXAMPLE
  ./scripts/package.ps1 -Rid win-x64
  ./scripts/package.ps1 -Rid win-arm64 -NotesFile notes.md -PreviousFrom https://github.com/Quost/menew-windows
#>
param(
  [Parameter(Mandatory)] [ValidateSet('win-x64', 'win-arm64')] [string] $Rid,
  # Markdown shown by the updater; the changelog section of the version in a release
  [string] $NotesFile,
  # Download the latest release of this repo first, so Velopack can make a delta package from it
  [string] $PreviousFrom,
  [switch] $Prerelease,
  # Azure Trusted Signing metadata.json; signs Menew.exe, its DLLs, Update.exe and Setup.exe when given
  [string] $AzureTrustedSignFile,
  [string] $OutputDir = 'artifacts/releases'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Invoke-Native([string] $exe, [string[]] $arguments) {
  & $exe @arguments
  if ($LASTEXITCODE -ne 0) { throw "$exe exited with code $LASTEXITCODE" }
}

$version = ([xml](Get-Content Directory.Build.props)).SelectSingleNode('/Project/PropertyGroup/Version').InnerText
$velopack = ([xml](Get-Content src/Menew.App/Menew.App.csproj)).SelectSingleNode("//PackageReference[@Include='Velopack']").Version
if (-not $version) { throw 'No <Version> in Directory.Build.props' }
if (-not $velopack) { throw 'No Velopack PackageReference in src/Menew.App/Menew.App.csproj' }
$platform = @{ 'win-x64' = 'x64'; 'win-arm64' = 'ARM64' }[$Rid]
$publish = "artifacts/publish/$Rid"
Write-Host "Menew $version for $Rid (Velopack $velopack)"

# 1. Native AOT, self-contained, unpackaged
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
Invoke-Native dotnet @('publish', 'src/Menew.App', '-c', 'Release', '-r', $Rid, "-p:Platform=$platform", '-o', $publish)
if (-not (Get-ChildItem $publish -Recurse -Filter *.xbf)) { throw 'The published app is missing compiled XAML (.xbf) resources.' }
if (-not (Test-Path "$publish/Menew.pri")) { throw 'The published app is missing Menew.pri.' }
if (-not (Test-Path "$publish/Menew.exe")) { throw 'The published app is missing Menew.exe.' }

# 2. vpk, the same version as the Velopack library in the app (Dependabot bumps the csproj, vpk follows)
$tools = 'artifacts/tools'
$vpk = Join-Path $tools 'vpk.exe'
$installed = if (Test-Path $vpk) { (& $vpk --version 2>$null | Select-Object -First 1) } else { '' }
if ("$installed" -notlike "$velopack*") {
  if (Test-Path $vpk) { Invoke-Native dotnet @('tool', 'uninstall', 'vpk', '--tool-path', $tools) }
  Invoke-Native dotnet @('tool', 'install', 'vpk', '--version', $velopack, '--tool-path', $tools)
}

# 3. the previous release, for the delta package (none yet is fine: then only the full package is made)
New-Item -ItemType Directory -Force $OutputDir | Out-Null
if ($PreviousFrom) {
  $download = @('download', 'github', '--repoUrl', $PreviousFrom, '--channel', $Rid, '--outputDir', $OutputDir)
  if ($env:GITHUB_TOKEN) { $download += @('--token', $env:GITHUB_TOKEN) }
  if ($Prerelease) { $download += '--pre' }
  & $vpk @download
  if ($LASTEXITCODE -ne 0) { Write-Host "::notice::No previous release for $Rid could be downloaded: this release has no delta package." }
}

# 4. pack. The channel is the RID, so the asset names are stable and an install only follows its own
#    architecture: Menew-win-x64-Setup.exe, releases.win-x64.json... (README "Release assets")
$pack = @(
  'pack', '--packId', 'Menew', '--packVersion', $version, '--packDir', $publish, '--mainExe', 'Menew.exe',
  '--packTitle', 'Menew', '--packAuthors', 'Matheus Quost', '--icon', 'src/Menew.App/Assets/Menew.ico',
  '--channel', $Rid, '--runtime', $Rid, '--shortcuts', 'StartMenuRoot', '--outputDir', $OutputDir
)
if ($NotesFile) { $pack += @('--releaseNotes', $NotesFile) }
if ($AzureTrustedSignFile) { $pack += @('--azureTrustedSignFile', $AzureTrustedSignFile) }
Invoke-Native $vpk $pack

foreach ($expected in @("Menew-$Rid-Setup.exe", "Menew-$Rid-Portable.zip", "releases.$Rid.json", "Menew-$version-$Rid-full.nupkg")) {
  if (-not (Test-Path (Join-Path $OutputDir $expected))) {
    Get-ChildItem $OutputDir | Format-Table Name, Length
    throw "Velopack did not produce $expected (the asset names are documented in the README; update both)."
  }
}
Get-ChildItem $OutputDir | Format-Table Name, Length
