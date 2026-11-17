# EP-0035 final validation

Status: thematic EP35 checks and independent final review completed; owner stopped unrelated long corpus. Integration with updated main for the requested PR is being checked separately.

Artifact root: `D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/`.

## Complete headless inventory

- Contracts.Tests, Motion.Tests, Client.Tests, EconomyBalance.Tests, Performance.Tests: all tests.
- Engine.Tests: ordinary suite excluding Category=LongRunning/Balance, then every LongRunning/Balance test in bounded batches: SeriesParityTests Dogfight/Raid/Defence/Balanced/Commander (each seeds1..1000); AnomalyReportTests five tactics; DecoyReportTests; EvasionCalibrationTests all rows.
- Standalone HexWingSimulator: six `*.test.cjs` files via Node test runner.
- Legacy tactical content PowerShell harness: requires absent historical V4 tables; outcome must be reported separately, not treated as runtime acceptance.

Every test process has a 300000ms limit with process-tree termination. Full suite totals are recorded from actual runs; focused historical totals are not substituted.

Native game/window launches: NOT RUN, explicitly prohibited by owner. Inspected Client SkiaWindow test construction uses `createNativeWindow:false`; performance report tests use headless/raster or report CLI. No native launch is needed for these project suites. Owner checks are in OwnerAcceptance.md.

## Static gates

Full Release solution build: PASS, 0 warnings / 0 errors (`full-build.log`, subsequent `final-oracles-build.log`); final Client fixture correction also builds cleanly (`last-sensor-build.log`). New current-contract links:27 documents, 0 missing paths. New Board-ledger links:0 missing paths. Scoped integration-test whitespace normalized; diff check clean.

Graph source scope includes EP0034/EP0035 and current Documentation. Artifacts and exact limits recorded after final rebuild. Concurrent foreign EP0034 Board edit is present in source manifest but excluded from commits.

Closing graph rebuild PASS: graph-status-synced.log (following graph-closing-reviewed.log); graph/graph.json, graph/graph.html, graph/GRAPH_REPORT.md, graph/counts.json, graph/verification.json and graph/semantic-coverage.json. 1197 code files,1056 Markdown,32154 nodes,82540 edges,1064 communities. Known parser partial extraction: ModulePanelLocalizationTests.cs line25 (C# build passes). 1670 unresolved structural edges pruned,1199 canonical self-loops pruned,4202 unlocated reference nodes; selective host semantics7 concepts/6 source files. Earlier rebuild used explicit shrink approval for exclusion of the changing publication-receipt card; no active game-code scope was removed. Closing rebuild verifies all2253 input hashes. Navigation only, not runtime proof.

## Full runs and corrections (not acceptance)

full2: Contracts265/265; Motion1282/1283; Engine2540/3215; Economy57/61; Performance10/20. Client full2 exceeded300000ms, tree terminated; full3 partitions cover2408+629=3037 tests (2352+517 passed;56+112 failed). full3 Engine2560/3215. These runs are FAILED, not a green suite. Related sensor/disabled-survivor/relocated-fixture oracles were corrected in subsequent commits. Independent baseline/source classification is in ReviewFindings.md; no baseline execution is claimed.

Full standalone Node suite PASS73/73 (full-node.log). Legacy PowerShell harness FAILED because VData/Planning/V4.0/Data/README.md is absent, including at base (full-legacy-export.err). Series Dogfight full1000seeds PASS (full-series-dogfight-tests.log). Unsharded Evasion10 exceeded300000ms; process tree terminated. Sharded probe exactly1test PASS (calibration-list.log/calibration-probe-tests.log). Final long corpus matrix is corpus-final/plan.json:620 Evasion rows,10 Decoy rows,5 Series and5 Anomaly; results remain pending until every640 row is accounted for. Interrupted preliminary corpus directory is not evidence of completion.

Scoped format verification PASS after local EOL normalization (corpus-format-verified.log); Git semantic diff unaffected. Closing graph is refreshed; publication/review ledgers are excluded to avoid circular evidence hashes.

## Closing ordinary suites

These runs use the final compiled game and test fixtures; all processes completed within300000ms. Exact TRX and logs are under the artifact root, with prefixes below.

| Project / partition | Passed | Failed | Total | Evidence prefix |
|---|---:|---:|---:|---|
| Engine core, excluding long corpus |1816|642|2458|closing-EngineCore|
| Engine tactical/demo, excluding long corpus |746|11|757|closing-EngineBattleDemo|
| Client core |2353|55|2408|closing-ClientCore|
| Client battle excluding View |492|60|552|closing-ClientBattleNonView|
| Client View |25|52|77|closing-ClientView|
| Contracts |265|0|265|closing-Contracts|
| Motion |1282|1|1283|closing-Motion|
| EconomyBalance |57|4|61|closing-EconomyBalance|
| Performance headless/report |10|10|20|closing-Performance|

The835 failures remain failed; baseline/source comparisons and unresolved runtime attribution are in ReviewFindings.md. The148 tests explicitly named Ep0035 all pass in these closing runs. This subset is not substituted for the global suite.

`test-inventory/` records actual discovered cases: Engine3855 including640 corpus rows, Client3037, Contracts265, Motion1283, EconomyBalance61, Performance20; total8521. Engine's default project filter excludes LongRunning/Balance, so EngineAll inventory explicitly overrides it. `final-test-audit.json` compares display-name multisets with TRX results, preserving duplicate theory display names. Owner explicitly stopped unrelated long runs:500 corpus rows PASS,140 remaining/interrupted rows NOT RUN; no extra cases. Completed ordinary plus long cases:8381, of which7546 PASS and835 FAIL. This is not a completed global suite.

Route integration coverage is explicitly one frozen seed320032 and one outer destination, with two fixed route variants (direct and detour) at2500/5000 motion-ms partitions, real accepted passengers and owned cargo, pause and JSON Load. The US4-TK1 phrase "two fixed seed/routes" is interpreted as those two route variants with the two specified outcomes; two different destinations or two seeds are not claimed.

## Owner scope change and final review

Owner requested topic-related checks only and termination of remaining unrelated long runs. The coordinator and its child process trees were stopped at500/640 successful recorded cases. `corpus-audit.json` deliberately reports complete=false; remaining140 cases are not passed or silently excluded. Prior wording about waiting for all640 is historical execution policy, superseded by this explicit instruction and the updated EpicExecutionPrompt.md.

Independent final reviewer `/root/dependency_review` reviewed the aggregate base-to-2c308fbb diff and closing documentation/prompt edits: no new confirmed actionable EP35 production findings. Existing835 suite failures, unresolved baseline runtime attribution and native/product NOT RUN remain limitations. The newly requested integration with updated main and PR mergeability are a separate final publication check.
