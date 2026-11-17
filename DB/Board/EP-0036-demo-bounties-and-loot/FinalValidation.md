# Final validation — EP-0036

Implementation: 12 stories / 48 tickets, including US5 documentation last. Publication mapping: [Tickets](Tickets.md). Current contracts: [DemoRewards](../../Documentation/04-Engineering/DemoRewards.md), [gameplay](../../Documentation/02-FirstRelease/Mechanics/DemoBountiesAndLoot.md). Full walkthrough/AC scope: [AcceptanceCoverage](AcceptanceCoverage.md).

## Automated evidence

Evidence root: `D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/`.
Retained logs and matching `*-results/*.trx` are exact run evidence, including failures and corrected repeats. Build/test artifacts and helper scripts are disposable and removed after verification.

| Boundary | Actual result | Evidence files in root |
|---|---|---|
| Final Client thematic set | 50 distinct cases covered successfully across initial 44/50 and corrected capacity repeat 11/11 (5 overlap). Six initial failures were predecessor expectations for crew/reserve and locale placeholders; preserved assertions for old identities/modules/no legacy engineer. | final-client.log, final-capacity-fixed.log and matching TRX |
| Final Contracts | 3/3 passed | final-contracts.log and matching TRX |
| Final Engine continuation | 3/3 passed, following affected neighboring validation; no later semantic Engine changes | us4-engine-final.log and matching TRX |
| US12 | Engine31/31; Client77/78 then existing journal-sharing race isolated repeat1/1 | us12-engine.log, us12-client.log, us12-client-repeat.log |
| Other stories | Story-specific results, failures, corrections and independent review recorded as executed | ExecutionStatus.md, retained us1…us11 logs/TRX |
| Scoped format | dotnet format whitespace --verify-no-changes on changed C# succeeded | format-final.log, format-final.err |
| Current docs | 31 changed documents, all checked relative links resolve; later graph navigation docs also checked | documentation-links.json |
| Board | 12 stories, 48 tickets; no missing links/dependencies or ticket cycles | board-validation.json |

Commands ran from repository root with absolute artifact paths and a 300000ms process-tree cap. Tests ran only at completed story boundaries/final thematic boundary (one early US3 scheduling deviation is explicitly recorded in ExecutionStatus). No full solution test suite, long seed corpus or balance benchmark was run. Headless SkiaWindow tests explicitly use createNativeWindow:false; no game process/window was launched.

## Independent review

Separate Engine/Contracts and Client/documentation reviewers examined the whole epic and corrections. Closed findings include source pilot provenance, packaged kneeling metadata/cache, pending retry intent, Open loot vs next pending encounter Load atomicity, latest-battle report selection, manual F5/F9 loot routing and prisoner cabin summary. Final reviews reported no open actionable findings. Current docs corrected passenger offer sizing and passenger-only daily-food acceptance gate.

Auto-review initially rejected a push due to remote trust evidence; verified existing origin/upstream and explicit EpicExecutionPrompt publication authorization resolved it. All subsequent publication completed. No approval blocker remains.

## Remaining acceptance and scope

Game/native/product, real monitor/input/GPU and artwork acceptance: NOT RUN — owner launch only. Owner walkthrough is in AcceptanceCoverage. Four prepared exact female portrait IDs support kneeling outfits; missing/male identities use the same source portrait or neutral card, never another face. Loot modules are sale-only; captive interactions and full profitability/repair balance remain outside this epic.

Unrelated owner changes to EP31 DemoPlan, EP34 station-job ticket and EP41 TicketPlan remain uncommitted and intact. The EP36 portion of shared Board README was synchronized; the DemoPlan execution paragraph was separately staged without absorbing owner grooming edits. No worktree was created.

## Graph and final static checks

Offline Graphify final build: 33,869 nodes, 86,013 edges, 1,136 communities; 1,275 code and 1,121 Markdown files. All 2,396 input hashes and 6 selected semantic input hashes verified; zero missing sources/out-of-range locations/dangling edges/self-loops. Post-export drift check is empty. One existing parser limitation: ModulePanelLocalizationTests.cs line25 may be partially extracted (not a compiler error). Semantics are selective, token use unmeasured; no image interpretation or external API. HTML aggregates communities instead of rendering 33k individual nodes.

Retained graph evidence: graph/verification.json, graph/input-evidence.json, graph/semantic-coverage.json, graph/ast-warnings.json, graph/GRAPH_REPORT.md, graph/graph.json and graph/graph.html; source hashes include the preserved owner working-tree docs. The graph is navigation, not runtime/native proof. Final static evidence final-static-checks.json verifies current Markdown links and publication ancestry.

## Cleanup and publication verification

Task-owned build outputs, Graphify cache, helper scripts, temporary capacity fixtures, metrics and raster/screenshot outputs were removed. Retained only the referenced log/TRX and graph evidence under D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/. Root inspection found no task-created temporary directory in workspace/repository; existing unrelated directories were preserved. No task worktree or running game exists. Final receipt is committed and pushed to the same verified upstream; exact remote HEAD is checked after its commit and reported to the owner.
