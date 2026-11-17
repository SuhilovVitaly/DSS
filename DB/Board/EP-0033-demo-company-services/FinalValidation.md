# Final validation — EP-0033

## Automated evidence

All paths below are rooted at `D:/DeepSpaceSaga/artifacts/validation/ep-0033/`.
Tests were run at each story boundary, as requested. Earlier failing attempts remain diagnostic history; only the explicitly named final runs are PASS.

| Boundary | Final logs | Result |
|---|---|---|
| US-0001 | 20261117-us0001/Engine4.log, Contracts4.log, Client4.log, client-regression.log | 4 / 3 / 8 / 37 PASS |
| US-0002 | 20261117-us0002/Engine5.log, Contracts.log, Client2.log | 7 / 6 / 43 PASS |
| US-0003 | 20261117-us0003/Engine6.log, Contracts.log, Client.log | 34 / 8 / 49 PASS |
| US-0004 | 20261117-us0004/Engine3.log, Contracts3.log, Client3.log | 14 / 10 / 71 PASS |
| US-0005 | 20261117-us0005/Engine3.log, Contracts2.log, Client2.log | 18 / 12 / 76 PASS |
| Final regression | 20261117-final/Engine.log, Contracts.log, Client3.log | 19 / 12 / 100 PASS (131 tests; overlapping earlier sets) |
| Updated rendered stock counter | 20261117-final/Client4.log | 1 PASS; rerun of the real service flow |

Release builds were part of these successful dotnet test invocations. Tests are focused, not the entire repository suite. Each process used a 300000 ms timeout with tree termination on timeout. No timeout is counted as a pass. Scoped whitespace formatting is recorded in 20261117-final/format.log and format2.log; final diff check accompanies publication.

Final filters: Engine `DemoEp0033Us000` plus relevant campaign replay and `Ep0029Us0005Tk0002Tests.TwoSurvivorsStartAsTwo` cases; Contracts `DemoEp0033`; Client `DemoEp0033`, `HireScreen`, `ShipScreen`, `FinanceScreen`, `Ep0029Us0009Tk000`. Exact passing case ownership remains in the source test classes. Reproduce from DSS with `dotnet test tests/<project>/<project>.csproj -c Release --artifacts-path <absolute validation run>/build --filter <filter> --results-directory <absolute validation run>/results`.

`DemoEp0033ServiceFlowTests.RealEngineServicesRenderAndCommitThroughScreens` loads actual demo content, restores a damaged/docked fixture, hires, repairs, buys a decoy charge and replacement drone, assigns the pilot and renders Finance. Actual balance is 5000 − 300 − 20 − 25 − 1500 = 3155. Engine callbacks, UI handlers and authoritative projections are real; the controlled damaged fixture is not a naturally played campaign. Retained headless PNGs: `20261117-final/screens/{hire,repair-quote,supply,shipyard,finance}.png`. Visual inspection found and fixed Ship text overlap before final capture.

## Native verification and its limits

The task-owned Release client ran from an isolated build directory; repository/user settings and saves were not used. `DSS_VALIDATION_SMALL_MONITOR=1` was set. Silk enumerated monitor 0 = 3440×1440 (largest), monitor 1 = 1920×1080, monitor 2 = 2560×1440. The initial index-0 attempt was rejected before showing the window; then explicit index 1 was used successfully. Evidence: `20261117-final/native.err`, `native-small.err`, `native-continue.err`, and `Interface.log`.

Observed through computer-use: menu, New demo campaign/start battle, restart, Continue from the generated isolated checkpoint to the world map, Ship opening/closing, repair refusal “Dock at a service station first”, Shipyard tab and Finance showing people 6, food 200, next payroll 24h and zero debt. Layout was readable at 1920×1079 with no observed service-panel overlap. Mouse interaction worked. Injected Escape / Ctrl+Alt+P / F10 did not produce an observed result, so keyboard shortcuts and native screenshot capture are not counted as verified. Both task-owned game processes were stopped.

Full native docked Hire/payment/replacement/debt/Save-Load flow: NOT RUN to completion. Native smoke above and headless integration do not establish full UI/input, GPU performance, audio, other monitor scales, independent player acceptance or economic balance. Those gates remain OPEN.

## Review and external dependency

Self-review corrected replacement battle-slot ancestry, readiness without pilot, actual mass after equipment changes, load-time payroll validation against saved time, permanent applied-command hashes beyond the 1024-response cache, pending interlocks, feedback labels and Ship alignment. Existing rearm regression assertions now use the current localized reason contract; inventory, money and pending assertions were retained. No independent review is claimed.

EP-0034-US-0002 passenger-contract runtime is absent. Real existing passengers are counted by cabins and ration consumption; this does not prove a passenger contract lifecycle. US-0005 integration acceptance stays OPEN until the owning epic exists and the real contract path is exercised. Full epic acceptance is therefore not claimed.

## Documentation, graph, publication and cleanup

Story-end checks cover changed/current Markdown links, six story IDs, 26 ticket IDs, declared dependency targets/cycles, statuses and whitespace. Results: `20261117-final/documentation-check.json`.
Offline graph evidence: `20261117-final/graph/verification.json`, `counts.json`, `GRAPH_REPORT.md`, `source-manifest.json`, `semantic-coverage.json`; fresh reviewed semantic input: `20261117-final/semantics/graph-semantic-ep0033.json`. Graph parser warnings are navigation limitations, not compiler failures. Publication receipts and cleanup verification are recorded after these checks in [Tickets](Tickets.md) and [ExecutionStatus](ExecutionStatus.md).

Graph result: 1058 code files and 935 Markdown inputs; 28643 nodes / 75780 edges. Verification checked 1993 source hashes plus 6 fresh semantic source hashes, with no missing sources, out-of-range locations, dangling final edges or self-loops. One AST parser warning in existing ModulePanelLocalizationTests.cs and selective semantic coverage are preserved in the report; this is not complete semantic understanding or gameplay acceptance.
Documentation checks PASS: 56 Markdown files, 432 relative links, 6 stories and 26 tickets; declared dependency targets exist and the selected dependency graph has no cycle. Cleanup removes task-owned builds, results, temporary scripts/directories and graph cache, retaining referenced logs, rendered screens, graph and its semantic inputs.

Cleanup verified: `20261117-final/cleanup.json`; no task-owned game process remains. Only evidence logs, screenshots, graph exports and semantic inputs remain. Root checks found no new task-specific directories in the workspace or repository and no DSS/artifacts directory. Existing unrelated root files were preserved.
