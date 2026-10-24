# Production combat analyzer

This CLI runs the shipped `CombatMukha` scenario through the production Engine and the same public-command policy used by `CombatBalanceReport`. Its shared policy source is linked from `tests/DeepSpaceSaga.Engine.Tests/ProductionCombatBalanceHarness.cs`; xUnit is not a dependency. `global.json` pins the installed SDK 10.0.400. The tool is outside `DeepSpaceSaga.sln`.

From the repository root, create a unique output directory under the workspace's
canonical validation artifacts directory:

```powershell
$analysisRun = Join-Path 'D:/DeepSpaceSaga/artifacts/validation/combat-analyzer' ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $analysisRun -Force | Out-Null
dotnet build tools/DeepSpaceSaga.CombatAnalyzer
dotnet run --no-build --project tools/DeepSpaceSaga.CombatAnalyzer -- --self-test
dotnet run --no-build --project tools/DeepSpaceSaga.CombatAnalyzer -- --seeds 3001:4000 --policy dodge --output "$analysisRun/dodge.csv"
dotnet run --no-build --project tools/DeepSpaceSaga.CombatAnalyzer -- --seeds 3001:4000 --policy static --output "$analysisRun/static.csv"
```

Each output creates CSV (one row per seed), `.summary.json`, and `.jsonl.gz` containing every outcome and actual command trace. Use `--root <repo>` when running outside the checkout. Each seed uses a fresh Engine and named RNG state; the registry/scenario are reused unchanged. All shipped operators are checked at skill20.

To inspect the published 2000-battle artifact without rerunning battles, use the
`$analysisRun` directory created above:

```powershell
dotnet run --no-build --project tools/DeepSpaceSaga.CombatAnalyzer -- --input DB/Board/EP-0012-combat-rebuild/evidence/us0014-tk0001-production-balance.jsonl.gz --seeds 3001:4000 --policy dodge --output "$analysisRun/recorded-dodge.csv"
```

Input is the current balance JSONL `RunResult` protocol (`PolicyVersion=public-observation-v1`), plain or gzip. The reader validates identities, exact selected seed coverage, consistent catalog/scenario hashes, finite durations, current point-launch/evade commands, receipts and absence of legacy chance facts. Input mode checks recorded data; its summary records that battles were not rerun. Preserve provenance hashes with exported results. Output paths that would overwrite the input artifact are rejected.

Exit codes: **0** valid accepted full corpus, or a partial range explicitly marked `NOT_EVALUATED_PARTIAL_CORPUS`; **2** complete seeds3001–4000 outside a win/duration/completion corridor; **1** invalid arguments, malformed records, missing/duplicate seeds or I/O failure. A partial range never establishes balance acceptance. The self-test uses clearly synthetic boundary checks plus one actual production integrity probe.

Policy v1 observes every100ms physical time, reacts after500ms, aims along two confirmed one-second observed turns (otherwise current-course straight), uses the fast torpedo/wide counter profiles, and executes evasion through `maneuver.evade`. It reads public projectile vectors, profiles and radii; foreign endpoints/deadlines and private AI controls are excluded. Static retains fire/PRO and never evades. The fixed horizon is600physical seconds. Real x1 duration is physical×4; nearest-rank P90 includes all outcomes and victory sampling has100ms physical resolution. The full report requires all1000 battles to finish, dodge wins72–88%, static wins≤10%, and P90≤5.5real minutes.

The measured [EP-0012 report](../../DB/Board/EP-0012-combat-rebuild/evidence/balance.md) leaves dodge balance acceptance open (96.6% wins, P907.493333real min); static passes (5.9%, P902.296505min). No catalog/scenario/AI/policy tuning is performed by this tool. A reference simulator command tape was not supplied; input-policy equivalence is unproven.

Validation processes must retain the project-wide five-minute process-tree cap. For a slower host, run explicit seed batches, preserve all rows, and validate the complete1000-seed artifact per policy using `--input`; keep the full requested cohort.
