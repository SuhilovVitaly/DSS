# EP-0011 execution — 2026-10-18

Scope: requested US-0001 plus mandatory final documentation US-0002. Initial state: only untracked EP-0011 planning package, preserved and adopted into requested execution. Branch base-fight, upstream origin/base-fight; remote https://github.com/SuhilovVitaly/DSS. Initial local/remote ba1b3da258899b8ebe5892dc5f7da40f135c2127.

Execution authority: current user request and EpicExecutionPrompt override planning-only restrictions for implementation/Board/commit/push. Game launch prohibited by explicit story non-goal. Automated and native acceptance recorded separately. No subagents used; self-review is not independent review.

| Ticket | Automated implementation | Published SHA |
|---|---|---|
| US-0001-TK-0001 observation-history | PASS 15/15 unit cases | 0f2811430f47411b2be7aa321fb393f2de0a971f; origin/base-fight verified |
| US-0001-TK-0002 map-forecast-integration | PASS 50/50 combined with history and existing future tests | df6ffd4b43777b92a6fabf6096700ad17ba8fb13; origin/base-fight verified |
| US-0001-TK-0003 regression-coverage | PASS 2057/2057 Client; 47/47 Engine forecast | 246987bb7c904f4dc779512e8f5ff42b0ff51061; origin/base-fight verified |
| US-0002-TK-0001 documentation-and-graph | Documents/links/Graphify PASS; native OPEN | See final full ticket ID in Git; verify after commit |

Native/human gate OPEN / NOT RUN: game was not launched.

## Current validation commands and observed results

Run from D:/DeepSpaceSaga/DSS, .NET commands outside sandbox after initial named-pipe access failure; -p:UseSharedCompilation=false for build/test.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~NpcMotionObservationHistoryTests"
# PASS 15/15
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~NpcObservedTrajectoryTests|FullyQualifiedName~NpcMotionObservationHistoryTests|FullyQualifiedName~FutureTrajectoryTests"
# TK-0002 PASS 50/50
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~NpcObservedTrajectoryTests|FullyQualifiedName~FutureTrajectoryTests|FullyQualifiedName~CombatForecastCacheTests"
# TK-0003 PASS 62/62 before station-orbit addition; final version included in full suite below
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore -m:1 --filter "Category!=ForecastBudget" -p:UseSharedCompilation=false --logger "trx;LogFileName=ep0011-client.trx"
# PASS 2057/2057, 1m16s
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~CombatForecast" -p:UseSharedCompilation=false --logger "trx;LogFileName=ep0011-engine-forecast.trx"
# PASS 47/47, 4s
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore -m:1 -p:UseSharedCompilation=false
# PASS, 0 warnings/errors
dotnet format tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/NpcMotionObservationHistory.cs tests/DeepSpaceSaga.Client.Tests/NpcMotionObservationHistoryTests.cs
# TK-0001 PASS
dotnet format tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/SnapshotBuffer.cs src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/FutureTrajectoryProjector.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs
# TK-0002 PASS
dotnet format tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs tests/DeepSpaceSaga.Client.Tests/FutureTrajectoryTests.cs
# TK-0003 PASS
git diff --check
git diff --cached --check
```

[Durable test counters / local TRX hashes](evidence/automated-results.json). Initial invalid new fixture and EOF whitespace issue corrected before TK-0003 publication. Initial defect reproduced before TK-0002: hidden turn produced X=100 instead of straight X=2100; now passes. Automated tests do not certify GPU/native/user input acceptance.

## Story and final self-review

US-0001 production diff inspected against D-01/D-02 and A-01–A-08. All AC have named headless evidence in [coverage](../../Documentation/04-Engineering/NpcObservedTrajectories.md). No Engine/Contracts/Motion, AI/RNG/save/guidance or combat forecast implementation changes. No existing assertions weakened. Snapshot/history state remains bounded; pending frames cannot authorize observations; real draw seam passes the measured rate. Nothing was presented as independent reviewer approval. AC-08 native/human portion remains OPEN.

## Documentation impact inventory

| Document / area | Action and verification |
|---|---|
| Documentation/04-Engineering/NpcObservedTrajectories.md | New current contract, units/formulas, lifecycle, coverage, acceptance limits |
| Documentation/README.md | Navigation to current behavior and evidence |
| Documentation/00-Process/CLAUDE.md | Current developer architecture seam and limitations |
| Documentation/01-Requirements/EngineRequirements.md §22 | Explicit ordinary NPC future-line exception; precise current-pose contract preserved |
| Documentation/03-Design/TacticalMapSpecification.md | Observed future geometry and viewport behavior |
| Documentation/02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md | Player-visible rule and physical-second semantics |
| Documentation/02-FirstRelease/Screens/GameSession.md | Screen behavior, scope and native gate |
| Documentation/02-FirstRelease/TechnicalTasks/CombatTrajectoryForecasting.md §6.3 | Dated supersession note: ordinary NPC line differs; combat/launch consistency remains A-08 backlog |
| Documentation/06-Tooling/TradingSolarGraphify.md | Current structural corpus includes EP-0011; selective semantics remain EP-0010 only |
| Board EP-0011 | 2 stories / 4 tickets, current status, dependency map, evidence, full prior-ticket SHA; initial logs preserved |
| FutureTrajectoryProjector XML summaries | Distinguish observed NPC geometry from exact shared predictor |
| src/graphify-out/rebuild.py and generated output | EP-0011 added to Markdown corpus; execution report/evidence excluded from hashes; regenerate and inspect verification |
| Root README / AGENTS / CLAUDE, src/tests/tools adjacent docs | README.md absent; aliases and unaffected build/runtime docs need no edit; repository-wide symbol/trajectory search completed |
| Other epics and historical engineering reports | Dated evidence retained, no new acceptance inferred; no current ordinary-NPC contract needing additional edits found |

Final story scope includes required documentation only, no added feature work. Graph is navigation only and does not close native/FPS/balance acceptance. Generated graph pre-export HEAD and input hashes describe actual sources; current documentation ticket SHA is obtained from Git after committing rather than inserted into its own content.

## Final documentation / Graphify evidence — 2026-10-18

[Current contract](../../Documentation/04-Engineering/NpcObservedTrajectories.md), [ticket map](Tickets.md), [graph verification](evidence/graph-check.json). Documentation relative-link checker: 131 valid references, 0 missing files before tooling note. Final check repeated below. Board: 2 stories / 4 tickets, unique IDs, 3 implementation commits already verified remotely; documentation commit published last. Root aliases retained; dated historical logs preserved.

```powershell
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe src/graphify-out/rebuild.py
# PASS: 735 code / 143 Markdown, 13620 nodes, 47142 edges, 419 communities; no force/shrink override
& D:/DeepSpaceSaga/.graphify-venv/Scripts/python.exe src/graphify-out/rebuild.py --dry-run
# Current-source evidence validation; see graph-check.json for hash/endpoint checks
```

Graph verification: 878 source SHA256 and all recorded artifact hashes match; missing sources / invalid locations / dangling edges / self-loops = 0. Selective semantics remain EP-0010; Markdown for EP-0011 is structurally extracted, not full semantic analysis. 1027 unresolved AST edges explicitly pruned, 2606 reference-only symbols unlocated, token cost unmeasured. Navigation output is not native/FPS/gameplay acceptance. XML-comment-only production change in documentation ticket passed scoped format verification; functional implementation was validated before this ticket.

Final status: US-0001 implementation + automated regression delivered; US-0002 documentation + graph delivered. All ticket commits are published to origin/base-fight separately. Native/human acceptance remains OPEN / NOT RUN by explicit game-launch restriction; no claim of complete gameplay acceptance. A-08 combat/launch model alignment remains out of scope backlog.

Final observed checks: Graphify --dry-run PASS (13620 nodes / 47142 edges, 0 missing sources/invalid locations). Query NpcMotionObservationHistory resolves 33 related nodes including SnapshotBuffer.GetNpcObservedTurnRate and ObserveNpcScene. Final Markdown checker PASS: 20 changed/new Markdown files, 143 relative references, 0 missing files; 4 unique ticket IDs / 2 stories (evidence/documentation-check.json). Source and artifact hash checks PASS (evidence/graph-check.json). Unstaged and staged diff checks PASS. Temporary root graphify-out/cache was created by the offline extractor and removed after validation; tracked graph remains src/graphify-out. Final documentation commit is identified by its unique full ticket ID in Git; final response reports its verified SHA.
