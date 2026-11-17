# EP-0035 execution status

Branch `codex/ep-0035-demo-pirate-encounters`; origin `https://github.com/SuhilovVitaly/DSS`.
Base `0c06524b5e1c9fb631ff6c6dc49ca2da48717a23`. [66 ticket publication ledger](Tickets.md).
17 stories implemented; US0006 documentation is the final story. Full headless suite and final independent review follow its publication and are not claimed passed yet.
EP0034 prerequisite implementation was integrated (merge721e3a5e, UIc1c71929, fixturesc27c6e95); old dependency-blocked notes are [history](ExecutionHistory.md).

## Story evidence

Evidence root: `D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/` (earlier tickets also reference sibling `20261117-execution`). Each ticket preserves actual commands and failed attempts.

| Story | Completed focused checks |
|---|---|
| US1 | Engine36, Contracts7, Client5 |
| US2 | Engine15, Client8, Contracts4 |
| US3 | Engine3, Client25 |
| US4 | Engine96, Client25; us4-reviewed-tests.log / us4-Client-tests.log |
| US5 | Engine172, Client71, Contracts17; us5-outer-values-tests.log / us5-Client-tests.log / us5-Contracts-tests.log |
| US6 | Current contracts27-document link check: no missing paths; Board/graph check recorded after publication |
| US7 | Engine19, Contracts2 |
| US8 | Engine6, Contracts3, Client79 |
| US9 | Engine32, Contracts4, Client1 |
| US10 | Engine23, Client14, Contracts2 |
| US11 | Engine18, Contracts5 |
| US12 | Engine22, Client81, Contracts7 |
| US13 | Engine165, Client1, Contracts1 |
| US14 | Engine86; us14-reviewed-tests.log |
| US15 | Engine5, Client26; us15-accepted-tests.log |
| US16 | Engine5, Client21; us16-reviewed-tests.log / us16-client-tests.log |
| US17 | Engine185; us17-verified-tests.log |

These are separate focused historical runs, not additive full-suite totals. Current epic-wide results follow in FinalValidation.

## Review corrections

Independent preparatory review read all 150 Board/current-documentation Markdown documents, reviewed invariants and identified a P1 integration defect: all shipped stations were inside START sanctuary. New Game now retains the close mining introduction and places outer stations16–44k world units away, with matching travel thresholds; old saved map.Rules remain unchanged. Full-flight real passenger/cargo regression uses frozen seed320032, two time partitions, repeated pause and JSON Load; direct captures, detour completes. Other corrections include quarter-remainder pause preservation, observation boundary determinism, exact undock epoch and route ETA/cruise consistency. This preparatory review is not the final epic review.

## Owner-only checks

Game was not launched. Native/window/GPU/input/product acceptance: NOT RUN by explicit owner instruction. Owner checklist: [OwnerAcceptance](OwnerAcceptance.md). First close safe job, outer route risk, report batching/pause, Irina/script, pursuit escape/safe entry, squad battle, replay after close, failed-write retry, actual-loss settlement and retired group persistence require owner inspection.

Foreign EP0034 ticket edits remain excluded. No additional worktree was created. Cleanup and final remote SHA are recorded after the closing checks.
