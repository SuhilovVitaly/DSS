# EP-0009 US11 combat balance evidence — 2026-10-12

Source: `tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs` and `CombatBalanceReport.cs`, executed against the current production content in `src/DeepSpaceSaga.Client/Scenarios/CombatMukha` and `CombatShmel`. Command: `dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter Category=Balance --logger "console;verbosity=detailed"`. Result: 2/2 PASS, 14m01s.

For each scenario, the test runs seeds 1..100 with the shipped loadout, standard launcher mode and active production hostile AI. Every five seconds of physical time, the test selects the first surviving hostile in scenario order and sends a real player `Fire` command whenever the player launcher reports ready. It stops on Engine defeat, destruction of all three hostiles, or 600 seconds of physical time. A timed-out fight is counted as no victory. The 400-run historical simulator reference in epic §7 is retained as historical evidence; its source is absent from this checkout and its player policy is not reproducible here.

| Scenario | Completed within 600s | Tetrarch victories | Historical reference | Delta | ±10 pp | Player hits / run | Enemy torpedoes / run | Player PRO launches / run | Lower median duration |
|---|---:|---:|---:|---:|---|---:|---:|---:|---:|
| CombatMukha | 99/100 | 67% | 81.2% | −14.2 pp | outside | 6.21 | 20.87 | 24.42 | 155 s |
| CombatShmel | 99/100 | 51% | 49.5% | +1.5 pp | within | 6.57 | 22.79 | 26.42 | 170 s |

The Mukha delta is an observed discrepancy, not a failed ticket gate under US11-AC2. Exact attribution is not proven: the engine uses the EP-0008-derived orbit controller with discrete turns, torpedoes start on the carrier heading, and the player decision policy here may differ from the missing `AttackAI` simulator. The two timeouts make the stated victory rate conservative under this 600-second horizon. No rules or content were retuned on the basis of this small cross-model comparison.

The focused end-to-end test also passed 2/2 for seed 1: both scenarios produced hostile launches, executed player shots, hit rolls, module damage and a terminal outcome. Native/game runs were NOT RUN by user instruction; this evidence is headless Engine evidence only.
