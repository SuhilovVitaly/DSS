# Phase Three Combat Acceptance — EP-0008

## Current EP-0012 contract — 2026-10-21

This dated EP8 result remains historical; it does not establish EP12 acceptance. Current production balance fails the dodge win-rate/P90 corridor. Native24+1 actual GPU runs establish only their enumerated checks; physical/human/audio/accessibility remain OPEN. [Point-aim combat contract](PointAimCombat.md).

## Historical record (superseded combat rules)

All combat formulas, saves, commands, conclusions and permissions below describe their original dated stage. They are not the current writer/runtime or fresh acceptance. Non-combat invariants remain applicable.

Execution date:2026-10-12 (Asia/Jerusalem). This is implementation/self-review evidence, not user APPROVED or product/balance acceptance.

## Automated evidence

| Layer / scenario | Evidence |
|---|---|
| Full solution before acceptance-only additions |4302/4302, skipped0; Contracts185, Motion170, Engine2030 including all correctness corpus, Client1849, Performance9, EconomyBalance59; `D:/DeepSpaceSaga/ep8-us12tk1-results` |
| Generated PlayerShipOnly actual battle | `PhaseThreeCombatEndToEndTests`: production registry/full generated maps, real LocalSession, Brake command, NPC pursuit/shots, auto/manual PR, actual pre-death file, uninterrupted/restored comparison, three lethal hits, frozen terminal world, rejected overwrite with unchanged bytes, recovery through fresh session |
| Physical/calendar/real domains | `PhaseThreeDeterminismTests`: seeds1/42/99, one30-minute physical interval vs irregular steps, pause and x100; objects/journal/RNG facts/outcome equal |
| Multi-enemy and extremes | `PhaseThreeMultiHostileTests`: two independent NPC shots, no forged NPC user commands; two manual PR,0/100% draws, custom speed9/turn45/range111/TTL8000/reload7000; large/split equality |
| Engine acceptance regression |1922/1922 non-corpus; new battle matrix6/6; `D:/DeepSpaceSaga/ep8-us12tk2-engine.log` |
| Client viewport matrix | `PhaseThreeCombatUiFlowTests`: six size/scale combinations through actual LocalSession and screen mouse handlers; distinct paused manual launches, preview, duplicate-click suppression, module identity, visible button bounds, footer/defense non-overlap and PR labels outside HUD |
| Defeat/current-file UI | `DefeatNavigationTests`, `PhaseThreeSessionRoundtripTests`: actual window router/Load/F9, rejected old versions, cancel/retry, saved two-PR and AI continuation, no replayed transient effects; separate from native pixels |

Full generated-world tests do not relocate objects or alter HP during a running battle. The multi-enemy and native UI fixtures configure initial positions before NewGame; that separate fixture is not described as the default generated world.

## Native method and limits

Runner: [CombatNativeEvidence.cs](https://github.com/SuhilovVitaly/DSS/blob/c894925d187c90a658d1e25958feed69a45d7e41/tools/DeepSpaceSaga.Performance/CombatNativeEvidence.cs), reproduction: [performance README](../../tools/DeepSpaceSaga.Performance/README.md#ep-0008-combat-native-evidence).

Actual Skia/OpenGL window, production content, real Engine/LocalSession/ScreenStack and mouse/keyboard handlers. Custom starting fixture has8 Enemy NPCs; all launch automatically toward the player. The script toggles one defense independently, hovers/selects/fires both manual modules, compares frozen preview route to actual launch, changes zoom/speed/pause and scrolls journal. Recovery waits for the actual rendered Defeat screen, uses Load/cancel/reopen/selected pre-death slot, and checks the fresh running session with both PR. Separate no-solution cases use0 accuracy,0.01km/s and1000ms TTL to prove the enabled manual button and honest absent encounter marker.

The controlled engine clock and client prediction timestamp are synchronized. Normal measured frames advance5 physical milliseconds per render; recovery frames are paused after loading. Native presentation timing uses the real monotonic clock independently. This measures this reproducible multi-projectile workload, not full-map real-time AI throughput. The full generated-world actual-session battle is proven separately above.

The collector excludes120 warmup frames and records600 completed-swap intervals, CPU submit and swap wait. It reports actual device/context and the80FPS/p99<=12.5ms target separately from interaction completion. No GPU-execution timestamp query or physical scanout measurement is claimed. Human manual playthrough and subjective balance remain NOT RUN/not-assessed.

## Findings repaired during native review

1.1280x720/UI150: map toolbar and other bottom controls crossed defense details. Bottom controls now move beside the command panels when they fit; toolbar avoidance works without AiMap too. Six real click/session cases assert non-overlapping geometry.
2.PR lifetime labels could be covered by HUD controls. Labels now wrap to available map width and search placements avoiding both HUD and other PR labels. Six cases assert visible non-overlap for simultaneous launches.
3.Harness-only: reading a swapped default framebuffer returned the previous back buffer. An internal optional post-flush/pre-swap callback captures the actually rendered screen; normal gameplay has no callback.
4.Harness-only: independently advancing wall-clock prediction while a fixture engine clock stood still produced false x100 visual drift. Internal SnapshotBuffer injection lets the harness share its deterministic timestamp with Engine and Screen; the public GameSessionHandle constructor still uses the normal real clock.

## Native matrix

Previously collected artifacts, inspected without relaunching the game on 2026-10-12. User explicitly deferred all further game/native runs. 13/14 interaction scenarios completed; 1920x1080/UI150 combat stopped at step11 with no exception and without a timing sample. Its acceptance remains OPEN; no inferred PASS. Earlier exploratory reports under `D:/DeepSpaceSaga/ep8-native*` are not final samples.

| Case | Interaction | Swap interval p99, ms | 80 FPS target |
|---|---|---:|---|
| 1280x720-1-combat | PASS | 14.88 | failed |
| 1280x720-1-recovery | PASS | 15.75 | failed |
| 1280x720-1.2-combat | PASS | 12.70 | failed |
| 1280x720-1.2-recovery | PASS | 13.40 | failed |
| 1280x720-1.5-combat | PASS | 19.55 | failed |
| 1280x720-1.5-no-solution | PASS | 11.34 | passed |
| 1280x720-1.5-recovery | PASS | 36.76 | failed |
| 1920x1080-1-combat | PASS | 27.61 | failed |
| 1920x1080-1-no-solution | PASS | 15.01 | failed |
| 1920x1080-1-recovery | PASS | 20.16 | failed |
| 1920x1080-1.2-combat | PASS | 23.96 | failed |
| 1920x1080-1.2-recovery | PASS | 39.64 | failed |
| 1920x1080-1.5-combat | INCOMPLETE (step 11/22) | not measured | OPEN |
| 1920x1080-1.5-recovery | PASS | 91.40 | failed |

Artifacts: `D:/DeepSpaceSaga/ep8-acceptance-native/<case>.json`, adjacent `.log` and screenshots. Intel Arc140V, OpenGL3.3, driver32.0.101.8860, VSync100Hz. Completed timing reports have120 warmup/600 measured frames. The 80FPS target is not accepted. GPU execution, scanout and human playthrough remain unmeasured.

Native samples predate the later9ab6eed terminal-clock correction; native verification after that correction is NOT RUN by user instruction. The JSON commit field records base817a856; measurements included this ticket's then-uncommitted sources. Source SHA256 below identifies those sources; generated JSON hashes identify the exact evidence. Native completion is explicitly deferred and does not block the user's authorized code/documentation/publication chain.

| Evidence / source | SHA256 |
|---|---|

| `1280x720-1-combat.json` | `c57e80d53eed90267db6e49ff249b8becccb7ff8081426865db153a0643e4210` |
| `1280x720-1-recovery.json` | `e40734c0590a641365d147ddfe356460a15cf41ce13c66f4ab6efc649c0540f4` |
| `1280x720-1.2-combat.json` | `10edd72154f4b1f30353e921b3dc452f63022b95660d54f9275d57bc107fb6c7` |
| `1280x720-1.2-recovery.json` | `3acd1e3edbe7f2fc63e2c6a7b5f886f8956957ae6c39988184675eaefc325f7a` |
| `1280x720-1.5-combat.json` | `af1174ab841899ab31726f5328b28ef96c2aa555bedd0fa17c97ce795f8840ec` |
| `1280x720-1.5-no-solution.json` | `ccaa0c2bcb3c7a7158ccf857ffe4962562461b7f3a2febd5647b1e00aed00a9b` |
| `1280x720-1.5-recovery.json` | `4e83992ec660443798655ddb88185f7eec39d7392708b25a75ead8ffb056e06c` |
| `1920x1080-1-combat.json` | `5cf3d7d5cf58acd827c8f10ec7cba4e0817c6f9c6fcb7b8db3f58af2fd8f4f84` |
| `1920x1080-1-no-solution.json` | `3ef8f8c654a61b5175f2cc3deee2cca794e7258b5cc1d8df27b8112eb78ec310` |
| `1920x1080-1-recovery.json` | `797c99033c35f443738fb61db030ffc5eba99693e9223cb704f4adc184508e91` |
| `1920x1080-1.2-combat.json` | `8fb83fe6c4f0fa767957446fb6d971b83c45796b56f8a920557481c9532fe212` |
| `1920x1080-1.2-recovery.json` | `baf9669a2cb1c45219118fa7d65de1d26a01fd802daa8035f6ec221c5893fed2` |
| `1920x1080-1.5-combat.json` | `0e0e200924a1e81df86d3e5ef1c231a85b1e40277b95b9a65e5b8a75365a8c6f` |
| `1920x1080-1.5-recovery.json` | `bc87f830c68182c78750ee95e6d9cfc57132e9a13d4df9967b90cd8dd492a3e3` |
| `CombatNativeEvidence.cs` | `52d13e8bbea618e4a1004be780939cf2059427109b3b6bd47bc58534c99678da` |
| `SkiaWindow.cs` | `0d1af8addf2e239ff6944bc63fe2ef7fdbaa6417487fce48475b55e613b25ca6` |
| `GameSessionHandle.cs` | `d8faf20d8bc97ec231ed63ca4da0ab712d4a8653a945b8ebb51c0efad9e9ec01` |
| `GameSessionScreen.cs` | `2a512351470d7ae688c4e9c3cdf41f4341dd4ecd217f2641416570f0e823931a` |
| `GameSessionScreen.MapView.cs` | `148f4bead5179c4b93d9ed8fcca3f4cc27a4c5df36daba1cda7c1fc856e41526` |
| `GameSessionScreen.Countermeasures.cs` | `81e5071245444b31f8224d3fd429a6dd281087c05c874a2d479ad5b080d2c3c2` |

Final automated ticket validation: Client1855/1855, skipped0 (`D:/DeepSpaceSaga/ep8-us12tk3-client-final.log`); solution build0 warnings/errors (`D:/DeepSpaceSaga/ep8-us12tk3-final-build.log`); scoped whitespace verification and `git diff --check` PASS. No game/native rerun after the user restriction.


## Final self-review — US-0012-TK-0004

Scope: EP-0008 production changes through9ab6eed, Contracts/Engine/Motion/LocalClient/Client/content and actual-file continuation. This is author self-review, not independent review or user approval. Graphify14935-node baseline was used for navigation only; primary sources and executable tests determine behavior.

Confirmed finding P2: A09 required ceil(exact death), while DefeatedClock used floor. Real fractional-contact regression failed5001 vs expected5002. Correction9ab6eedcf51aea3515dbf2bab1ea3f4d58786dea published and remote verified; affected196 tests PASS. Exact event/calendar cutoff and no subsequent gameplay events remain unchanged. Footer/label findings were fixed and published by402c375.

### Decisions to executable evidence

Test names below are actual classes in `tests/DeepSpaceSaga.{Contracts,Motion,Engine,Client}.Tests`, not proposed stubs. Story/ticket scopes below map their AC to these checks. Native and human evidence remains separate.

| Decisions | Stories / primary implementation | Executable evidence |
|---|---|---|
| Q01,Q02,Q03,Q04,Q41,Q44 | US3; SimulationEngine.Combat.cs / HostileFire.cs shared guards/internal actor | HostileTorpedoFireTests (range entry, ready/operator, all enemies, no player receipts, next physical ms, partitions/file resume); TorpedoLaunchTests |
| Q05,Q06,Q07,Q08,Q09,Q10,Q11,Q16,Q26,Q31,Q35 | US5/6; ManualDefense.cs, Countermeasures.cs, DefensePreview.cs | ManualCountermeasureFireTests (real pause, duplicate identity, range, orphan/current hostility, non-player target, no-solution0%, repeat after reload); DualDefenseArbitrationTests; DefensePreviewProjectionTests |
| Q12,Q13,Q14,Q15,Q37,Q48 | US6; absolute expiry, contact ordering and CountermeasureRng | CountermeasureLifetimeTests (contact before/equal/after expiry, hull tie,60 physical seconds, capped coast, file resume); CountermeasureResolutionTests; PhaseThreeJournalTests |
| Q17,Q20,Q21,Q22,Q23,Q24,Q25 | US1; module catalog, InterceptionMath, WeaponOperators.cs | InterceptionChanceTests; WeaponOperatorRuntimeTests; CountermeasureContentLoaderTests; TorpedoLauncherContentTests; PhaseThreeMultiHostileTests (custom parameters,0/100 draws) |
| Q18,Q19,Q39,Q40,Q42,Q43,Q51 | US4; HostileOrbitMath, RuntimeMotion, HostileCombat.cs | HostileOrbitMathTests (independent integration, bounded settling plus3 orbits,5% band/10% hysteresis, engine limits); HostileOrbitRuntimeTests; HostileCombatPathTests; PhaseThreeDeterminismTests |
| Q27,Q28 | US9/10; PlayerDefeat.cs, SimulationClock, EconomyTime.cs, LocalGameSessionConnection.SaveAsync, DefeatScreen/SkiaWindow | PlayerDefeatTests; DefeatTimeBoundaryTests including fractional correction; DefeatedSessionSaveTests; DefeatNavigationTests; PhaseThreeCombatEndToEndTests |
| Q29,Q30,Q36,Q38,Q45,Q46,Q50 | US7/8; per-module panels, ManualDefense.cs, map annotations, selected info/journal | DualDefensePanelLayoutTests; ManualDefenseUiFlowTests; ManualDefensePreviewTests; DefenseMapAnnotationsTests; HostileSelectedInfoTests; PhaseThreeCombatInfoTests; PhaseThreeJournalUiTests; PhaseThreeCombatUiFlowTests; partial native matrix above |
| Q32,Q33,Q34 | US2; PlayerShipOnly and three-cabin type only | CountermeasureBootstrapTests; TetrarchLoadoutTests; content catalog tests; real generated battle with two modules |
| Q47 | US6; CompareDefenseLaunch and shared attempt mutation | DualDefenseArbitrationTests (earliest encounter, chance tie, module ordinal, threat priority, rejected/no-solution candidates) |
| Q49 | US11; explicit save ingress16 / defense2 / hostile1, staged restore | PhaseThreeSaveVersionTests; PhaseThreeSaveSchemaTests; PhaseThreeSaveContinuationTests; PhaseThreeSessionRoundtripTests |

Review checked public ownership before launch, exact module/command identity, no RNG in preview/rejection/expiry,0/100 contact draws, shared attempt, physical vs calendar vs real UI time, all terminal capture entrypoints, staging before world replacement, normalized IDs, saved captured values and real UI routing. No additional confirmed defect remains from this review; this does not convert deferred acceptance into PASS.

### Story AC and ticket coverage

All13 story cards and45 ticket IDs/dependencies/relative links were checked; every declared AC is served. US13 documentation/graph work follows this review and is not claimed complete here.

| Story | Declared AC | Serving ticket numbers |
|---|---|---|
| EP-0008-US-0001 | AC-0001, AC-0002, AC-0003 | 0001, 0002, 0003, 0004, 0005 |
| EP-0008-US-0002 | AC-0001, AC-0002, AC-0003 | 0001, 0002 |
| EP-0008-US-0003 | AC-0001, AC-0002, AC-0003 | 0001, 0002, 0003 |
| EP-0008-US-0004 | AC-0001, AC-0002, AC-0003, AC-0004 | 0001, 0002, 0003, 0004, 0005 |
| EP-0008-US-0005 | AC-0001, AC-0002, AC-0003, AC-0004 | 0001, 0002, 0003, 0004 |
| EP-0008-US-0006 | AC-0001, AC-0002, AC-0003, AC-0004 | 0001, 0002, 0003 |
| EP-0008-US-0007 | AC-0001, AC-0002, AC-0003, AC-0004 | 0001, 0002, 0003, 0004 |
| EP-0008-US-0008 | AC-0001, AC-0002, AC-0003 | 0001, 0002, 0003 |
| EP-0008-US-0009 | AC-0001, AC-0002, AC-0003 | 0001, 0002, 0003 |
| EP-0008-US-0010 | AC-0001, AC-0002, AC-0003 | 0001, 0002, 0003 |
| EP-0008-US-0011 | AC-0001, AC-0002, AC-0003, AC-0004 | 0001, 0002, 0003, 0004 |
| EP-0008-US-0012 | AC-0001, AC-0002, AC-0003, AC-0004 | 0001, 0002, 0003, 0004 |
| EP-0008-US-0013 | AC-0001, AC-0002, AC-0003 | 0001, 0002 |

### Acceptance disposition

US12 AC1 has real generated-session/file evidence; AC2 depends on the final test results recorded below. AC3 remains PARTIAL/OPEN:13/14 native interactions and deferred remainder. AC4 self-review and measured cadence are recorded; the80FPS target is not accepted. User explicitly authorized continuing review/docs and EP9 without further game/native launches. Human playthrough, subjective balance and physical scanout remain OPEN. Prior dated reports retain their original results.


Documentation discrepancies identified for mandatory US13: current summaries still call Save15/11 current; EP7 formula/skill multipliers and no offensive AI/manual PR are superseded; six fixed panels do not describe two defense instances; selected-pirate range ring is superseded by module hover; default two-cabin/two-crew descriptions need a PlayerShipOnly-only exception. Historical EP6/7 reports will receive explicit supersession links, retaining their dated evidence. The frontmatter ticket_count44 conflicts with actual45 and must be reconciled. These are tracked documentation tasks, not accepted current gameplay descriptions.


### Final automated validation

After correction9ab6eed: `dotnet test DeepSpaceSaga.sln -c Release --no-restore --filter "FullyQualifiedName!~CorrectnessCorpus" --logger trx --results-directory D:/DeepSpaceSaga/ep8-us12tk4-final-results` PASS4201/4201, skipped0 (Contracts185, Motion170, Engine1923, Client1855, Performance9, EconomyBalance59). Log: `D:/DeepSpaceSaga/ep8-us12tk4-final-solution.log`. Release solution build succeeded with0 warnings/errors (`D:/DeepSpaceSaga/ep8-us12tk4-build.log`). The correction's new regression was red before the fix and196 affected checks green after it. Scope format/diff verification passed.

The full Debug solution run started before correction9ab6eed and completed on2026-10-12: PASS4314/4314, skipped0 (Contracts185, Motion170, Engine2036, Client1855, Performance9, EconomyBalance59). Command: `dotnet test DeepSpaceSaga.sln --no-restore --logger trx --results-directory D:/DeepSpaceSaga/ep8-us12tk4-results`; log `D:/DeepSpaceSaga/ep8-us12tk4-solution.log`. Engine duration25m50s includes114 correctness-corpus cases. Those generation/layout cases are unaffected by the one-line terminal-clock correction; the fresh Release run above covers all remaining tests and the new fractional-death regression. This is two distinct runs, not a claim that4315 tests were executed together on the final revision. No game or native window was launched for these checks.
