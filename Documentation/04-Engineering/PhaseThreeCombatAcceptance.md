# Phase Three Combat Acceptance — EP-0008

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

Runner: [CombatNativeEvidence.cs](../../tools/DeepSpaceSaga.Performance/CombatNativeEvidence.cs), reproduction: [performance README](../../tools/DeepSpaceSaga.Performance/README.md#ep-0008-combat-native-evidence).

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

The JSON commit field records base817a856; measurements included this ticket's then-uncommitted sources. Source SHA256 below identifies those sources; generated JSON hashes identify the exact evidence. Native completion is explicitly deferred and does not block the user's authorized code/documentation/publication chain.

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
