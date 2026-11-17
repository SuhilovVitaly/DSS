# EP-0035 final validation

Status: full suite and independent final review pending after US6 publication.

Artifact root: `D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/`.

## Complete headless inventory

- Contracts.Tests, Motion.Tests, Client.Tests, EconomyBalance.Tests, Performance.Tests: all tests.
- Engine.Tests: ordinary suite excluding Category=LongRunning/Balance, then every LongRunning/Balance test in bounded batches: SeriesParityTests Dogfight/Raid/Defence/Balanced/Commander (each seeds1..1000); AnomalyReportTests five tactics; DecoyReportTests; EvasionCalibrationTests all rows.
- Standalone HexWingSimulator: six `*.test.cjs` files via Node test runner.
- Legacy tactical content PowerShell harness: requires absent historical V4 tables; outcome must be reported separately, not treated as runtime acceptance.

Every test process has a 300000ms limit with process-tree termination. Full suite totals are recorded from actual runs; focused historical totals are not substituted.

Native game/window launches: NOT RUN, explicitly prohibited by owner. Inspected Client SkiaWindow test construction uses `createNativeWindow:false`; performance report tests use headless/raster or report CLI. No native launch is needed for these project suites. Owner checks are in OwnerAcceptance.md.

## Static gates

Full Release solution build: PASS, 0 warnings / 0 errors (`full-build.log`). New current-contract links:27 documents, 0 missing paths. New Board-ledger links:0 missing paths. Scoped integration-test whitespace normalized; diff check clean.

Graph source scope includes EP0034/EP0035 and current Documentation. Artifacts and exact limits recorded after final rebuild. Concurrent foreign EP0034 Board edit is present in source manifest but excluded from commits.

Graph rebuild PASS: graph-final.log; graph/graph.json, graph/graph.html, graph/GRAPH_REPORT.md, graph/counts.json and graph/semantic-coverage.json. 1196 code files,1057 Markdown,32161 nodes,82543 edges,1048 communities. Known parser partial extraction: ModulePanelLocalizationTests.cs.1669 unresolved structural edges pruned,1198 canonical self-loops pruned,4200 unlocated reference nodes; selective host semantics7 concepts/6 source files. Navigation only, not runtime proof.

## Full runs and corrections (not acceptance)

full2: Contracts265/265; Motion1282/1283; Engine2540/3215; Economy57/61; Performance10/20. Client full2 exceeded300000ms, tree terminated; full3 partitions cover2408+629=3037 tests (2352+517 passed;56+112 failed). full3 Engine2560/3215. These runs are FAILED, not a green suite. Related sensor/disabled-survivor/relocated-fixture oracles were corrected in subsequent commits. Independent baseline/source classification is in ReviewFindings.md; no baseline execution is claimed.

Full standalone Node suite PASS73/73 (full-node.log). Legacy PowerShell harness FAILED because VData/Planning/V4.0/Data/README.md is absent, including at base (full-legacy-export.err). Series Dogfight full1000seeds PASS (full-series-dogfight-tests.log). Unsharded Evasion10 exceeded300000ms; process tree terminated. Sharded probe exactly1test PASS (calibration-list.log/calibration-probe-tests.log). Final long corpus matrix is corpus-final/plan.json:620 Evasion rows,10 Decoy rows,5 Series and5 Anomaly; results remain pending until every640 row is accounted for. Interrupted preliminary corpus directory is not evidence of completion.

Scoped format verification PASS after local EOL normalization (corpus-format-verified.log); Git semantic diff unaffected. Graph requires final refresh after these fixture/docs corrections.
