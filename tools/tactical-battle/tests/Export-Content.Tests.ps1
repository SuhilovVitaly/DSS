[CmdletBinding()]
param([Parameter(Mandatory)][string]$ValidationRoot)
$ErrorActionPreference = 'Stop'
$validation = [IO.Path]::GetFullPath($ValidationRoot)
$fixture = Join-Path $validation ('export-fixture-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($fixture) | Out-Null
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$exporter = Join-Path $repo 'tools/tactical-battle/Export-Content.ps1'
$runtime = Join-Path $repo 'src/DeepSpaceSaga.Client/Data/TacticalBattle/pilots.json'
$runtimeHash = (Get-FileHash -LiteralPath $runtime).Hash
$original = [IO.File]::ReadAllText($runtime)
$output = Join-Path $fixture 'output'
$pwsh = (Get-Process -Id $PID).Path
function Export([string]$Target, [bool]$Check, [string]$Source, [bool]$Success) {
    $arguments = @('-NoProfile', '-File', $exporter, '-OutputDirectory', $Target)
    if ($Check) { $arguments += '-Check' }
    if ($Source) { $arguments += @('-PilotInitiativeFile', $Source) }
    $result = & $pwsh @arguments 2>&1
    if (($LASTEXITCODE -eq 0) -ne $Success) { throw "Unexpected export result: $result" }
}
try {
    Export $output $false '' $true
    Export $output $true '' $true
    $pilotsPath = Join-Path $output 'pilots.json'
    $pilots = [IO.File]::ReadAllText($pilotsPath) | ConvertFrom-Json -AsHashtable
    foreach ($id in @('vera', 'yun', 'marta', 'oleg')) {
        $expected = @{ vera = 55; yun = 53; marta = 59; oleg = 50 }[$id]
        if ($pilots[$id].initiative -ne $expected) { throw "Wrong initiative: $id" }
    }
    foreach ($i in 1..6) {
        if (($pilots["thug$i"].initiative -join ',') -cne '40,55') { throw 'Wrong thug range.' }
    }
    Write-Output 'PASS ExportPreservesApprovedInitiativeAndV4BaseFields'
    $pilots.vera.initiative = 99
    [IO.File]::WriteAllText($pilotsPath, ($pilots | ConvertTo-Json -Depth 50))
    $corruptHash = (Get-FileHash -LiteralPath $pilotsPath).Hash
    Export $output $true '' $false
    if ((Get-FileHash -LiteralPath $pilotsPath).Hash -ne $corruptHash) { throw 'Check overwrote corrupt content.' }
    Export $output $false '' $true
    Export $output $true '' $true
    Write-Output 'PASS CheckRejectsCorruptOutputWithoutWriting'
    $source = Join-Path $fixture 'source.json'
    foreach ($invalid in @('<missing>', 'null', '1.5', '"50"', 'true', '-1', '101', '[55,40]', '[40,55,60]', '[40.0,55]')) {
        $node = [Text.Json.Nodes.JsonNode]::Parse($original)
        if ($invalid -eq '<missing>') { $node['vera'].AsObject().Remove('initiative') | Out-Null }
        else { $node['vera']['initiative'] = [Text.Json.Nodes.JsonNode]::Parse($invalid) }
        [IO.File]::WriteAllText($source, $node.ToJsonString())
        Export (Join-Path $fixture 'must-not-exist') $false $source $false
        if (Test-Path -LiteralPath (Join-Path $fixture 'must-not-exist')) { throw 'Invalid source wrote output.' }
    }
    foreach ($invalid in @($original.Replace('"vera":', '"Vera":'), $original.Replace('"vera":', '"unknown":'),
        $original.Replace('"yun":', '"vera":'), '{}', '[]', $original.Replace('"initiative": 55', '"initiative": 55, "initiative": 55'))) {
        [IO.File]::WriteAllText($source, $invalid)
        Export (Join-Path $fixture 'must-not-exist') $false $source $false
        if (Test-Path -LiteralPath (Join-Path $fixture 'must-not-exist')) { throw 'Invalid IDs wrote output.' }
    }
    # Only the declared extension may come from runtime; base fields come from V4.
    $node = [Text.Json.Nodes.JsonNode]::Parse($original)
    $node['vera']['name'] = [Text.Json.Nodes.JsonValue]::Create('not a V4 name')
    $node['vera']['extra'] = [Text.Json.Nodes.JsonValue]::Create(123)
    [IO.File]::WriteAllText($source, $node.ToJsonString())
    Export $output $false $source $true
    $pilots = [IO.File]::ReadAllText($pilotsPath) | ConvertFrom-Json -AsHashtable
    if ($pilots.vera.name -cne 'Вера' -or $pilots.vera.Contains('extra')) { throw 'Copied runtime base/unknown field.' }
    if ((Get-FileHash -LiteralPath $runtime).Hash -ne $runtimeHash) { throw 'Runtime source changed.' }
    Write-Output 'PASS InvalidSourceOrPilotIdentityFailsBeforeAnyWrite'
} finally {
    if (-not [IO.Path]::GetFullPath($fixture).StartsWith($validation + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
        throw 'Fixture cleanup escaped validation root.'
    }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
