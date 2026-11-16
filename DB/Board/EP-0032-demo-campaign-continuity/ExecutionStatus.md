# EP-0032 execution — 2026-11-17

**Implementation: 7 stories, 28 tickets. Manual/product acceptance: OPEN, deferred to the owner.**
Branch: `codex/ep-0032-demo-campaign-continuity`. Remote: `https://github.com/SuhilovVitaly/DSS`.
Base: `eaac17c7569668c47b29836dd89d90efe535a7ef`.
Prerequisite EP-0031-US-0005 was completed and published separately (`bdfb09e1`, `c5e4610c`).
Order: US0007 → US0001 → US0002 → US0003 → US0004 → US0005 → US0006.
Each ticket has its own immediately pushed commit; corrections have additional commits, no history rewriting.
[Full publication ledger](Tickets.md), [coverage and manual checklist](AcceptanceCoverage.md), [documentation inventory](DocumentationImpact.md).

## Result

Save22 persists campaign metadata and pilot memorial alongside the existing carrier/wing and settlement. Real sorties retain hull/ammunition/losses, destroyed pilots leave the roster permanently, and disabled pilots survive. Public New Game starts the five-station Demo world and owned-wing tutorial. Autosave uses the existing atomic continuation writer; Continue selects the newest fully compatible save. Terminal and recoverable outcomes are distinct. Engine remains authoritative; no second inventory, roster or settlement was introduced.

## Verification

All commands ran from `D:/DeepSpaceSaga/DSS`, with absolute artifacts/results paths and process-tree timeout 300000 ms. Tests were collected at story boundaries as requested. These are focused corpora, not a claim that the entire test suite passed.

Evidence root: `D:/DeepSpaceSaga/artifacts/validation/ep-0032/`.

| Run | Logs retained | Result |
|---|---|---|
| 20261117-prerequisite | prerequisite-review.md | prerequisite documentation review |
| 20261117-us0007-retry | engine3.log, contracts.log, client2.log, format logs | 10 Engine / 3 Contracts / 4 Client PASS |
| 20261117-us0001 | engine2.log, contracts2.log, client.log, regression.log | 3 Engine / 1 Contracts / 4 Client + 6 ShipScreen PASS |
| 20261117-us0002 | engine.log, contracts.log, client2.log | cumulative 15 Engine / 5 Contracts / 10 Client PASS |
| 20261117-us0003 | engine.log, contracts.log, client.log | cumulative 20 Engine / 6 Contracts / 17 Client PASS |
| 20261117-us0004 | engine.log, contracts.log, client.log | cumulative 22 Engine / 7 Contracts / 14 Client PASS |
| 20261117-us0005 | engine.log, client3.log, format.log, format.err | cumulative 23 Engine / 126 Client PASS; scoped whitespace verification clean |
| 20261117-regression | after2.log, format2.log, format2.err | 105 Engine regression checks PASS (Save18–22, catalogs, campaign); format clean |
| 20261117-final | build.log, build.err, documentation-check.log | solution Release build PASS, 0 warnings, 0 errors; documentation checks |

Earlier failed attempts remain in the evidence directory: fixture/assertion compile issues, a missing UI import, stale Credits expectation and an overly broad test replacement were corrected before final publication. No failing attempt is counted as PASS. Local test builds include the owning production projects. Final solution build covers all projects; no new run of the entire test corpus is claimed.

## Review and assumptions

Self-review covered transport ownership, command replay, revisions, settlement idempotency, atomic save failure, memorial/live-roster conflicts, fixed content identity, stale session cancellation, load adoption, status only after durable acknowledgement and manual-slot preservation. This is not independent review.

The fixed demo uses the existing five-node TradingMap generator; full SolarSystem generation is not used. Existing starter-carrier content is shared. Far-trade station retains the existing scientific-military economy profile. The display of calendar loss time uses the authoritative millisecond value. Services/recruitment and downstream campaign actions remain owned by EP-0033 onward, not inferred from reserved DTO sections.

No guarantee of saving uncommitted changes across force-kill or failed storage: last durable file remains valid. A terminal save can legitimately be the latest compatible file. Manual checkpoint selection is available; Continue does not resurrect a destroyed carrier.

## Native and remaining gates

Before the user's later instruction, an isolated compiled profile ran on the verified 1920x1080 monitor, guarded by `DSS_VALIDATION_SMALL_MONITOR=1`, rendering at 1920x1079. Main menu and empty Continue feedback were observed only. User then explicitly requested no game launches and reserved manual checks. The task-owned process was stopped; remaining native, physical input sequences, audio, balance and product acceptance are OPEN. See the owner checklist.

## Graph and cleanup

Offline graph rebuilt using fresh campaign semantic fragments and the maintained AST/Markdown tool. Final output: `D:/DeepSpaceSaga/artifacts/validation/ep-0032/20261117-final/graph/GRAPH_REPORT.md`; `graph.log` / `graph.err` record the first build, `graph-final.log` / `graph-final.err` record refresh after the last code/document changes. Parser warning: ModulePanelLocalizationTests may be partially extracted; graph navigation does not establish runtime correctness. Unresolved AST edges and self-loops are audited by the tool, not treated as facts.

Disposable build directories, test results and graph AST cache were removed after checks. Logs, reviewed semantic input and graph/report evidence are retained. No managed worktree was created. Task-owned native process stopped; no new root temporary folders created. Cleanup/root verification is recorded in final/cleanup.log.

Late Continue adoption-error correction: `024e7609`, Client126 PASS in final/client.log; it does not alter persistence or canonical contracts. Final documentation SHA is resolved after commit by exact ticket ID and checked against origin; see Tickets.md. Generated files remain outside Documentation; source code and current contracts are the primary evidence.

Late fixture regression correction: `d8b44073` preserves direct Save20→21 tests, updates current writer assertions to22, removes impossible campaign metadata from historical test projections, and resolves source paths from compile-time file location. Failed attempts are retained as before.log/after.log; after2.log is the passing 105-test result. No production behavior changed.
