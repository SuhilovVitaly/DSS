#requires -Version 7.0
<#
Exports manifest-sized tactical PNGs and a contact sheet; optionally installs runtime assets.
Requires PowerShell 7+ and .NET SDK 8+. Uses cross-platform Skia, including on Linux.
Linux: pwsh ./tools/Export-TacticalButtonAssets.ps1 -Manifest /absolute/assets/manifest.json
#>
param(
    [Parameter(Mandatory=$true)][string]$Manifest,
    [switch]$Install
)
$ErrorActionPreference = 'Stop'
$manifestPath = (Resolve-Path -LiteralPath $Manifest).Path
$projectRoot = Split-Path -Parent $PSScriptRoot
$tool = Join-Path $projectRoot 'tools/DeepSpaceSaga.PortraitAssets/DeepSpaceSaga.PortraitAssets.csproj'
& dotnet run --project $tool -c Release --verbosity quiet -- export-tactical $manifestPath $projectRoot $Install.IsPresent.ToString().ToLowerInvariant()
if ($LASTEXITCODE -ne 0) { throw "Tactical export failed ($LASTEXITCODE)." }
