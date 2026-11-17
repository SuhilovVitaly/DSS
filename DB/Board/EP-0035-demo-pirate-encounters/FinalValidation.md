# EP-0035 final validation

Status: implementation, thematic checks and independent final review completed, including integration with main9cfd80af. Owner stopped unrelated long corpus. Native/product acceptance NOT RUN. PR publication/mergeability receipt is recorded separately after the final commit.

Artifact root: `D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/`.

## Historical global inventory (superseded by owner scope change)

- Contracts.Tests, Motion.Tests, Client.Tests, EconomyBalance.Tests, Performance.Tests: all tests.
- Engine.Tests: ordinary suite excluding Category=LongRunning/Balance, then every LongRunning/Balance test in bounded batches: SeriesParityTests Dogfight/Raid/Defence/Balanced/Commander (each seeds1..1000); AnomalyReportTests five tactics; DecoyReportTests; EvasionCalibrationTests all rows.
- Standalone HexWingSimulator: six `*.test.cjs` files via Node test runner.
- Legacy tactical content PowerShell harness: requires absent historical V4 tables; outcome must be reported separately, not treated as runtime acceptance.

Every test process has a 300000ms limit with process-tree termination. Full suite totals are recorded from actual runs; focused historical totals are not substituted.

Native game/window launches: NOT RUN, explicitly prohibited by owner. Inspected Client SkiaWindow test construction uses `createNativeWindow:false`; performance report tests use headless/raster or report CLI. No native launch is needed for these project suites. Owner checks are in OwnerAcceptance.md.

## Static gates

Full Release solution build: PASS, 0 warnings / 0 errors (`full-build.log`, subsequent `final-oracles-build.log`); final Client fixture correction also builds cleanly (`last-sensor-build.log`). New current-contract links:27 documents, 0 missing paths. New Board-ledger links:0 missing paths. Scoped integration-test whitespace normalized; diff check clean.

Graph source scope includes EP0034/EP0035 and current Documentation. Artifacts and exact limits recorded after final rebuild. Concurrent foreign EP0034 Board edit is present in source manifest but excluded from commits.

Final graph rebuild PASS: graph-main-integrated.log; graph/graph.json, graph/graph.html, graph/GRAPH_REPORT.md, graph/counts.json, graph/verification.json and graph/semantic-coverage.json.1199 code files,1059 Markdown,32218 nodes,82714 edges,1089 communities. Known parser partial extraction: ModulePanelLocalizationTests.cs line25 (C# build passes).1674 unresolved structural edges pruned,1201 canonical self-loops pruned,4210 unlocated reference nodes; selective host semantics7 concepts/6 source files. Earlier rebuild used explicit shrink approval for exclusion of the changing publication-receipt card; no active game-code scope was removed. Final SHA256 audit verifies2258 source files with0 drift. Navigation only, not runtime proof.

## Full runs and corrections (not acceptance)

full2: Contracts265/265; Motion1282/1283; Engine2540/3215; Economy57/61; Performance10/20. Client full2 exceeded300000ms, tree terminated; full3 partitions cover2408+629=3037 tests (2352+517 passed;56+112 failed). full3 Engine2560/3215. These runs are FAILED, not a green suite. Related sensor/disabled-survivor/relocated-fixture oracles were corrected in subsequent commits. Independent baseline/source classification is in ReviewFindings.md; no baseline execution is claimed.

Full standalone Node suite PASS73/73 (full-node.log). Legacy PowerShell harness FAILED because VData/Planning/V4.0/Data/README.md is absent, including at base (full-legacy-export.err). Series Dogfight full1000seeds PASS (full-series-dogfight-tests.log). Unsharded Evasion10 exceeded300000ms; process tree terminated. Sharded probe exactly1test PASS (calibration-list.log/calibration-probe-tests.log). The planned long matrix is corpus-final/plan.json:620 Evasion rows,10 Decoy rows,5 Series and5 Anomaly. Owner stopped it after500 successful Evasion rows;140 remaining/interrupted cases are NOT RUN. Interrupted preliminary corpus directory is not evidence of completion.

Scoped format verification PASS after local EOL normalization (corpus-format-verified.log); Git semantic diff unaffected. Closing graph is refreshed; publication/review ledgers are excluded to avoid circular evidence hashes.

## Ordinary suites before main integration

These runs use the pre-main-integration compiled game and test fixtures at2c308fbb; all processes completed within300000ms. They are retained historical regression evidence, not a global run of the later merge. Exact TRX and logs are under the artifact root, with prefixes below.

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

Independent final reviewer `/root/dependency_review` reviewed the aggregate base-to-2c308fbb diff and closing documentation/prompt edits, then the main integration and deadline correction through6f8eb85c: no new unresolved actionable EP35 production findings. Existing835 suite failures, unresolved baseline runtime attribution and native/product NOT RUN remain limitations.

## Final main integration and thematic verification

Merge6e0c2642 integrates main9cfd80af and resolves16 conflicts while preserving EP35 command guards, deferred commit, encounter contracts and Ship tabs/sensor/passenger UI. Main's new deadline formula exposed3 real integration failures on EP35's longer outer routes. Correction6f8eb85c changes cruisePermille3200 to5000; measured minimum for the preserved25% ordinary/cargo margin is approximately4696. Formula, maneuver allowance, type margins and frozen existing terms remain intact; assertions were not weakened.

- Release solution after correction: PASS0warnings/0errors (`main-deadline-build.log`).
- Engine194/194 PASS (`main-final-Engine-tests.log`): Demo/EP35/pirate/voyage-fuel integration, including all directed-route deadline feasibility, cargo delivery and mixed voyage round trips.
- Client143/143 PASS (`main-final-Client-tests.log`): Demo namespace, EP35 and passenger checks. Earlier broad name-filter probe152/154 included2 old StationMarketDemo failures already in the general-suite report; these remain failed, not repaired or reclassified as passed.
- Contracts51/51 PASS (`main-integration-Contracts-tests.log`), unchanged by the later content-only deadline correction.
- Final independent integration review: no new actionable findings. No global corpus was restarted.
- Current-document links:38 documents/430 links/0 missing (`final-doc-links.json`). Ticket publication audit:66 unique IDs, all referenced commits are ancestors (`publication-audit.json`).

## Retained evidence and cleanup

Artifact root contains historical and final `*.log`/`*.err`, raw test TRX in result directories, `corpus-final/` partial raw results and plan, `test-inventory/`, `graph/` exports/audits, `semantics/`, and named audit JSON/TSV. These are explicitly retained evidence; older counts are not added to final thematic totals. Disposable build output, scripts, intermediate raster fixtures and caches are removed. Cleanup details: `cleanup-summary.json`;2280 task-created legacy repro directories were separately removed (`legacy-cleanup.log`). Foreign EP34 ticket bytes were restored exactly after the integration stash; no task worktree was created. Final remote HEAD and PR state are recorded in `publication-final.json` after publication.
