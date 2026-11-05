# US-0001 TK-0001 — content validation, 2026-11-05

Scope: 29 JSON files exported from the independent Markdown tables in
`VData/Planning/V4.0/Data`; source Markdown included in Git for reproducibility.
No reference Python implementation is required (D-USER-14/15).

`tools/tactical-battle/Export-Content.ps1` and `-Check` both PASS. The check compares
values, key order, filenames and file count against a fresh table export. The
existing recursive Client content rule ships all files; new tests verify the
actual build output, scenario order and equipment references. Carrier layout
has an empty engine array; a single-engine drone retains an array rather than a
PowerShell scalar. The strict Engine Settings schema admits `tacticalBattle`;
loading the catalog itself belongs to TK-0003.

## Automated results

| Project | Passed | Failed |
|---|---:|---:|
| Contracts.Tests | 273 | 0 |
| Motion.Tests | 251 | 0 |
| Client.Tests (including 2 new ContentDataTests) | 2324 | 0 |
| Engine.Tests | 2484 | 0 |
| EconomyBalance.Tests | 61 | 0 |
| Performance.Tests | 9 | 0 |
| Total | 5402 | 0 |

Solution build: `dotnet build DeepSpaceSaga.sln --no-restore --nologo -v:minimal -m:1`
PASS, zero warnings/errors. Scoped `dotnet format ... whitespace --verify-no-changes
--no-restore --include` for EngineContentLoader and ContentDataTests PASS;
`git diff --check` PASS. Commands ran from `D:/DeepSpaceSaga/DSS`.

Each test process had a 300000 ms process-tree cap. Initial monolithic Engine
and EconomyBalance invocations were interrupted or timed out and are **not**
counted as successful full runs. Final coverage is their complete partition:

- Engine: 2329 non-corpus/non-Balance cases; 114 corpus cases in explicit
  DisplayName batches of at most 8; 20 dodge + 20 static + 1 smoke Balance case.
  All 2443 non-Balance discovered IDs are present, with no missing IDs.
- EconomyBalance: 42 evaluator/report cases, 12 exact BalanceRun test IDs,
  1 exact 240-hour case and 6 exact LongVoyageRunner test IDs. All 61 discovered
  IDs are present. Empty exploratory filter runs are excluded.

The exact DisplayName filters escape parentheses and run directly through
`dotnet vstest ... /TestCaseFilter:... /ResultsDirectory:<absolute> /Logger:trx`.
Ordinary project runs used `dotnet test <project> --no-build --no-restore
--filter Category!=LongRunning --results-directory <absolute> --logger trx`.
No assertions, tests or categories were weakened or removed.

## Evidence

All paths below are under
`D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/`:

- `20261105-content-02/build.log`, `format.log`, project logs/TRX for
  Contracts, Motion, Client and Performance.
- `20261105-content-03/test-inventory.txt`, Balance logs/TRX under
  `balance-dodge`, `balance-static`, `balance-smoke`.
- `20261105-content-04/engine-general.log`, `corpus-*.log` and their TRX;
  `economy-inventory.txt`, `economy-1.log` and its TRX (240-hour case).
- `20261105-content-06/voyage-*.log` and their TRX.
- `20261105-content-07/economy-simple.log`, `economy-case-*.log` and their TRX;
  `coverage-summary.json` records counts and zero coverage gaps.

The first regression reproduced two integration defects (single-engine array
unwrapping and strict Settings rejection); both were corrected before the final
passing runs. A legacy Balance test initially wrote to its default build-output
folder; task-written files were moved into the designated interrupted-run
evidence folder, then the series reran with an absolute `DSS_BALANCE_OUTPUT`.

Review: self-review only; independent review is not claimed. Native/input,
Linux parity and battle acceptance: NOT RUN for this content ticket. It does
not implement the new battle runtime or its screen.
