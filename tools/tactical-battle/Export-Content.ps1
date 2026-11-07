#!/usr/bin/env pwsh
# V4 Data Markdown tables are the independent source; this tool contains no combat rules.
[CmdletBinding()]
param([switch]$Check, [string]$OutputDirectory)

$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = Join-Path $repo 'VData/Planning/V4.0/Data'
$destination = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $repo 'src/DeepSpaceSaga.Client/Data/TacticalBattle' }
$culture = [Globalization.CultureInfo]::InvariantCulture
$documents = [ordered]@{}

function Read-Table([string]$File, [string]$Name) {
    $body = [IO.File]::ReadAllText((Join-Path $source $File))
    $match = [regex]::Match($body, "(?s)<!-- table:$Name -->(.*?)<!-- /table -->")
    if (-not $match.Success) { throw "Missing table $Name in $File" }
    $lines = @($match.Groups[1].Value -split '\r?\n' | Where-Object { $_.StartsWith('|') })
    $headers = @($lines[0].Trim('|').Split('|') | ForEach-Object { $_.Trim().Replace('`', '').Replace(' (±)', '') })
    foreach ($line in $lines | Select-Object -Skip 2) {
        $cells = @($line.Trim('|').Split('|') | ForEach-Object { $_.Trim() })
        if ($cells.Count -ne $headers.Count) { throw "Invalid row in $File/$Name : $line" }
        $row = [ordered]@{}
        for ($i = 0; $i -lt $headers.Count; $i++) { $row[$headers[$i]] = $cells[$i] }
        $row
    }
}

function Parse-Cell([string]$Value) {
    $original = $Value
    $value = $Value.Replace('−', '-')
    if ($value -eq 'true') { return $true }
    if ($value -eq 'false') { return $false }
    if ($value.StartsWith('[') -and $value -notlike '[[]effect:*') {
        return ,@($value | ConvertFrom-Json)
    }
    $integer = 0L
    if ([long]::TryParse($value, [Globalization.NumberStyles]::Integer, $culture, [ref]$integer)) { return $integer }
    $real = 0.0
    if ([double]::TryParse($value, [Globalization.NumberStyles]::Float, $culture, [ref]$real)) { return $real }
    return $original
}

function Set-Field($Object, [string]$Path, $Value) {
    $parts = $Path.Split('.')
    $current = $Object
    for ($i = 0; $i -lt $parts.Count - 1; $i++) {
        if (-not $current.Contains($parts[$i])) { $current[$parts[$i]] = [ordered]@{} }
        $current = $current[$parts[$i]]
    }
    $current[$parts[-1]] = $Value
}

foreach ($name in @('rules', 'ai')) {
    $content = [ordered]@{}
    foreach ($row in Read-Table 'README.md' $name) {
        $field = $row['поле'].Replace('`', '')
        $value = Parse-Cell $row['значение']
        if ($field -eq 'version') { $value = $row['значение'] }
        if ($field -eq 'systems.table') {
            $matches = [regex]::Matches($row['значение'], 'effect: (\w+), weight: (\d+)')
            if ($matches.Count -eq 0) { throw 'Invalid systems.table' }
            $value = @($matches | ForEach-Object { [ordered]@{ effect = $_.Groups[1].Value; weight = [int]$_.Groups[2].Value } })
        }
        Set-Field $content $field $value
    }
    $documents["$name.json"] = $content
}

foreach ($category in @(
    @('Frames', 'frames'), @('Modules/Weapons', 'weapons'), @('Modules/Engines', 'engines'),
    @('Modules/Shields', 'shields'), @('Modules/Armor', 'armor'))) {
    foreach ($row in Read-Table "$($category[0])/README.md" $category[1]) {
        $content = [ordered]@{}
        foreach ($field in $row.Keys) {
            if ($row[$field] -ne '—') { $content[$field] = Parse-Cell $row[$field] }
        }
        $documents["$($category[0])/$($row['id']).json"] = $content
    }
}

foreach ($name in @('pilots', 'layouts')) {
    $content = [ordered]@{}
    foreach ($row in Read-Table 'README.md' $name) {
        $entry = [ordered]@{}
        foreach ($field in $row.Keys) {
            if ($field -eq 'id' -or $row[$field] -eq '—') { continue }
            if ($field -eq 'engines') {
                $entry[$field] = @($row[$field].Split(',') | ForEach-Object { $_.Trim() })
            } else {
                $entry[$field] = Parse-Cell $row[$field]
            }
        }
        if ($name -eq 'layouts') {
            if (-not $entry.Contains('weapon')) { $entry['weapon'] = $null }
            if (-not $entry.Contains('shield')) { $entry['shield'] = $null }
            if (-not $entry.Contains('engines')) { $entry['engines'] = @() }
        }
        $content[$row['id']] = $entry
    }
    $documents["$name.json"] = $content
}

$scenarios = [ordered]@{}
foreach ($row in Read-Table 'README.md' 'goals') {
    $goal = [ordered]@{}
    foreach ($field in @('playerSide', 'winMaxOneLoss', 'winNoLoss', 'tolerance', 'seeds')) { $goal[$field] = Parse-Cell $row[$field] }
    # No separate description in the Markdown table; retain the supplied scenario name.
    $scenarios[$row['сценарий']] = [ordered]@{ name = $row['name']; description = $row['name']; goal = $goal; sides = @() }
}
foreach ($row in Read-Table 'README.md' 'scenarios') {
    $scenario = $scenarios[$row['сценарий']]
    $sideId = $row['сторона']
    $side = @($scenario.sides | Where-Object { $_.id -eq $sideId }) | Select-Object -First 1
    if ($null -eq $side) {
        $side = [ordered]@{ id = $sideId; tactic = $row['тактика']; carrier = $null; drones = @() }
        $scenario.sides += $side
    }
    $unit = [ordered]@{}
    foreach ($field in @('id', 'pilot', 'layout', 'x', 'y', 'course', 'speed')) {
        if ($row[$field] -ne '—') { $unit[$field] = Parse-Cell $row[$field] }
    }
    if ($row['что'] -eq 'носитель') { $side.carrier = $unit } else { $side.drones += $unit }
}
$documents['scenarios.json'] = $scenarios

foreach ($path in $documents.Keys) {
    $target = Join-Path $destination $path
    $json = ($documents[$path] | ConvertTo-Json -Depth 50) + "`n"
    if ($Check) {
        if (-not [IO.File]::Exists($target) -or [IO.File]::ReadAllText($target).Replace("`r`n", "`n") -cne $json.Replace("`r`n", "`n")) {
            throw "Content differs from Markdown: $target"
        }
    } else {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        [IO.File]::WriteAllText($target, $json, [Text.UTF8Encoding]::new($false))
    }
}
$actual = @(Get-ChildItem -LiteralPath $destination -Recurse -File -Filter '*.json')
if ($actual.Count -ne $documents.Count) { throw "Unexpected JSON files in $destination" }
Write-Output "$($documents.Count) JSON files $(if ($Check) { 'verified' } else { 'exported' }) from V4 Markdown tables."
