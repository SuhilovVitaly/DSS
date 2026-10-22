# EP-0013 — карта покрытия

37 исходных AC из [revision2](SourceSpecification.md). Здесь PASS означает
автоматизированное покрытие и self-review реализации. Финальные фактические
прогоны/таймауты и нативные ограничения — в [ExecutionStatus](ExecutionStatus.md).
Число перечисленных параметров25 заменяет ошибочное29 в исходных AC.

| AC | Тикет | Проверяемое свидетельство |
|---|---|---|
| US1-AC1 | US1-TK1 | DebugTuningContractTests, сериализация optional payload |
| US1-AC2–AC3 | US1-TK2/TK3 | DebugTuningStateTests, DebugTuningCommandTests, DebugTuningTransportTests: immutable registry, индексы, atomic/revision/bounds/reasons, compact merge |
| US2-AC1–AC2 | US2-TK1 | DebugTuningShipTests: continuity/clamp, NPC turn, evasion/Approach, сохранение второй оси |
| US2-AC3–AC4 | US2-TK2 | DebugTuningProjectileTests: endpoint/deadline, radius/falloff, privacy, captured type/profile, CM-first order |
| US2-AC5–AC6 | US2-TK3 | DebugTuningReloadTests и общий Engine regression: readyAt, one cue, seeded partition equivalence |
| US3-AC1–AC2 | US3-TK1 | DebugTuningSaveTests: Save19 provenance, Save18 migration, preflight/rollback, deterministic continuation |
| US3-AC3–AC5 | US3-TK2 | DebugTuningStartupTests: shared schema1, defaults, invalid whole file, saved overlay then ordinary queued command |
| US4-AC1–AC2 | US4-TK1 | DebugTuningFileWriterTests:25 полей,250ms, atomic temp/move, retry/final flush, session barriers |
| US4-AC3 | US4-TK2 | DebugTuningDefaultsFileTests: original catalog reference export |
| US4-AC4 | US5-TK3 | DebugTuningPanelInputTests и writer integration: matching receipt, defaults confirmation, deletion, no delete after later edit |
| US5-AC1/AC3/AC5 | US5-TK2 | DebugTuningPanelInputTests: F12/Esc, buttons/fine wheel, command count, mouse capture; real OpenGL checks |
| US5-AC2/AC8 | US5-TK1/TK4 | DebugTuningPanelLayoutTests, DebugTuningLocalizationTests:25 rows, five sections, scale/scroll, RU/EN100 unchanged frames with0 managed bytes |
| US5-AC4/AC6 | US5-TK3 | Panel input/status tests: boundaries, localized rejections, StartupError, row reset, confirmed reset-all |
| US5-AC7 | US5-TK4 | Strict RU/EN Debug.* localization and actual value/status/render coverage |
| US6-AC1–AC2 | US6-TK2/TK3 | HoldMotionTests, SpeedHoldEngineTests: analytical arc,0/max splits, tolerance1e-6, partition invariance |
| US6-AC3 | US6-TK3 | Both axis orders; engine cycles/Approach/evasion cancellation and retained Cancelled receipt |
| US6-AC4–AC5 | US6-TK5 | ManualManeuverInputTests: ten seconds, exact edges, aliases, last opposite key and restoration |
| US6-AC6 | US6-TK3/TK5 | Shift latch until max/manual replacement; turn keeps acceleration; authoritative recovery |
| US6-AC7 | US6-TK5 | Real PlayerShipOnly and combat input path, removed old arrow step fallback |
| US6-AC8 | US6-TK4/TK6 | KeyboardEdgeTracker/CombatInputSchemaV2Tests, ManeuverHintsTests: S/Ctrl+S/Ctrl+C/I, E/Z/C and rebound localized hints |
| US6-AC9 | US6-TK4 | CombatInputSchemaV2Tests: scalar v1, default expansion, conflict warnings, strict schema2 arrays |
| US6-AC10 | US6-TK3/TK5 | Damage/disabled engine, recovery, focus/session/modal resync, saved sampled pose with released holds |
| US6-AC11 | US6-TK2/TK3 | SpeedHoldEngineTests.Engine_and_forecast_agree_across_speed_boundary_and_step_partitions: five-second predictor vs Engine within0.01km |
| US7 final1–6 | US7-TK1 | Impact inventory, normative/outside docs, dated history, synchronized Board/publication, final graph/hash/link/DAG audits |

Тесты находятся в `tests/DeepSpaceSaga.{Contracts,Motion,Engine,Client}.Tests/`;
имена выше соответствуют реальным файлам/классам. Нативный harness —
[DebugTuningNativeEvidence](../../tools/DeepSpaceSaga.Performance/DebugTuningNativeEvidence.cs).
Он даёт managed-input evidence в настоящем OpenGL окне. Hardware input, human
visual/audio/product acceptance и независимый review не выводятся из unit tests
или из семантического графа. Эти остаточные границы остаются открытыми.
