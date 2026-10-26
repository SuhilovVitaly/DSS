#requires -Version 7.0
param([Parameter(Mandatory)][string]$Repository, [Parameter(Mandatory)][string]$Results)
$ErrorActionPreference = 'Stop'
if (-not [IO.Path]::IsPathFullyQualified($Results)) { throw 'Results must be absolute.' }
[void][IO.Directory]::CreateDirectory($Results)
$env:DSS_DIAGNOSTICS_DIRECTORY = $Results
$env:DSS_INTERFACE_LOG_PATH = Join-Path $Results 'Interface.log'
$button = Join-Path $Repository 'src/DeepSpaceSaga.Client/Images/UI/GameSessionScreenUI/commands-panel/engine.accelerate.png'
Copy-Item -LiteralPath $button -Destination (Join-Path $Results 'master.png')
$manifest = Join-Path $Results 'manifest.json'
ConvertTo-Json -InputObject @([ordered]@{ master='master.png'; file='probe.png'; width=64; height=64; label='Portable export' }) | Set-Content -LiteralPath $manifest -Encoding utf8
& (Join-Path $Repository 'tools/Export-TacticalButtonAssets.ps1') -Manifest $manifest *> (Join-Path $Results 'export.log')
if (-not (Test-Path -LiteralPath (Join-Path $Results 'GameSessionScreenUI/probe.png'))) { throw 'Export PNG missing.' }
$prepare = Join-Path $Repository 'src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/character-generator/scripts/prepare.ps1'
$source = Join-Path $Repository 'src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/CHR-20260915-123931-52F226.png'
& $prepare -SourcePath $source -HeadWidth 370 -HeadHeight 490 -ChinY 555 -ProjectRoot $Repository -OutputDirectory (Join-Path $Results 'portraits') -PreviewDirectory (Join-Path $Results 'previews') *> (Join-Path $Results 'prepare.log')
$portrait = Get-ChildItem -LiteralPath (Join-Path $Results 'portraits') -Filter '*.png'
$preview = Get-ChildItem -LiteralPath (Join-Path $Results 'previews') -Filter '*.png'
if ($portrait.Count -ne 1 -or $preview.Count -ne 1) { throw 'Expected one portrait and one three-costume preview.' }
$files = @((Join-Path $Results 'GameSessionScreenUI/probe.png'), (Join-Path $Results 'tactical-buttons-contact-sheet.png'), $portrait.FullName, $preview.FullName)
$hashes = foreach ($file in $files) { [ordered]@{ file=$file; sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash } }
[ordered]@{ platform=[Runtime.InteropServices.RuntimeInformation]::OSDescription; powershell=$PSVersionTable.PSVersion.ToString(); passed=$true; files=@($hashes) } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Results 'summary.json') -Encoding utf8
