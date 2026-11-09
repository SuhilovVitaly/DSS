# Tactical battle content

Run from the DSS repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1
pwsh -NoProfile -File tools/tactical-battle/Export-Content.ps1 -Check
```

Base fields come from the marked Markdown tables in `VData/Planning/V4.0/Data`.
Initiative alone comes from runtime pilots.json, joined by exact ordinal pilot ID.
`-PilotInitiativeFile <absolute-path>` selects an isolated initiative source.
All initiative entries and ID coverage are strictly validated before any write.
Run `tools/tactical-battle/tests/Export-Content.Tests.ps1` for malformed source,
read-only Check, corrupt output, ID coverage and unchanged base-field cases.
The tool exports only JSON to `src/DeepSpaceSaga.Client/Data/TacticalBattle`.
`-Check` never writes files and fails for missing, extra or changed JSON.
`-OutputDirectory <absolute-path>` supports isolated content fixtures.
Catalog keys, scenario sides, drone lists and equipment order follow the tables.
Absent optional fields stay absent; carrier weapon/shield are null and engines empty.
The scenario name supplies its description because the source has no description column.

`ContentSyncTests` in Engine.Tests independently parses the Markdown and compares
every base value, field presence, catalog order, scenario unit order and the
runtime initiative extension. It also verifies
that the exporter detects a corrupted fixture without overwriting it.

Windows validation example (create the destination first):

```powershell
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/<unique-run>'
New-Item -ItemType Directory -Force $run
dotnet test tests/DeepSpaceSaga.Engine.Tests --filter 'FullyQualifiedName~TacticalBattle&Category!=LongRunning' --results-directory $run --logger trx
```

Set `DSS_VALIDATION_ROOT` to an absolute diagnostic directory to choose fixture
locations on another host. Fixtures are removed after each test. Process output,
TRX and other diagnostics belong in the workspace validation root.

Reference Markdown is historical independent evidence of the original simultaneous implementation.
It is not the oracle for current initiative firing or EP-0020 evasion, decoys and anomalies. Explicit manual cases and a
limited independent oracle check the new rules; seeded direct/API parity checks
determinism. Each test process needs a 300000 ms process-tree cap.
The exporter does not generate or alter battle, series or trace expectations.

For independent reference checks and single-tactic LongRunning commands, see [V4 sources and validation](../../DB/Documentation/06-Tooling/CarrierWingSim.md).

Ordinary Engine runs exclude both Category=LongRunning and Category=Balance. Run EvasionCalibrationTests/DecoyReportTests/AnomalyReportTests explicitly for balance evidence; their CSV/JSON outputs must stay under the absolute validation root. SeriesParityTests compares complete current outcomes, units, slots and RNG; the optional DSS_EP0020_NORMALIZE_SLOTS flag changes only exported cross-version signature hashes, never the full direct/explicit assertion. Legacy no-module/no-anomaly fixtures preserve historical M1/M2 evidence and are explicitly named; current M3/M4 views use runtime content. [Current execution evidence](../../DB/Board/EP-0020-tactical-battle-mechanics/ExecutionStatus.md).
