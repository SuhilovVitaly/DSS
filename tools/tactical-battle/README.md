# Tactical battle content

Run from the DSS repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1 -Check
```

The source is the marked Markdown tables in `VData/Planning/V4.0/Data`.
The tool exports only JSON to `src/DeepSpaceSaga.Client/Data/TacticalBattle`.
`-Check` never writes files and fails for missing, extra or changed JSON.
`-OutputDirectory <absolute-path>` supports isolated content fixtures.
Catalog keys, scenario sides, drone lists and equipment order follow the tables.
Absent optional fields stay absent; carrier weapon/shield are null and engines empty.
The scenario name supplies its description because the source has no description column.

`ContentSyncTests` in Engine.Tests independently parses the Markdown and compares
every value, field presence, catalog order and scenario unit order. It also verifies
that the exporter detects a corrupted fixture without overwriting it.

Windows validation example (create the destination first):

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/<unique-run>'
New-Item -ItemType Directory -Force $run
dotnet test tests/DeepSpaceSaga.Engine.Tests --filter FullyQualifiedName~TacticalBattle --results-directory $run --logger trx
```

Set `DSS_VALIDATION_ROOT` to an absolute diagnostic directory to choose fixture
locations on another host. Fixtures are removed after each test. Process output,
TRX and other diagnostics belong in the workspace validation root.

Reference Markdown is independent evidence from the original implementation.
The exporter does not generate or alter battle, series or trace expectations.
