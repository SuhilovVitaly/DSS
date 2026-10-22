---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
ticket: "EP-0012-US-0019-TK-0002-scenario-engine-fixtures"
title: "scenario-engine-fixtures"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios"]
files_touched: 17
serves: ["US19-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0019-TK-0002-scenario-engine-fixtures

## Why

[Совместимость сценариев и старых клиентских поверхностей](../EP-0012-US-0019-scenario-and-client-regressions.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: scenario-engine-fixtures; доказать US19-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (17 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs:7; public sealed class ScenarioGroupTranslationTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs:9; public sealed class ModuleCombatEndToEndTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs:5; public sealed class CombatBalanceReport | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0019TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `tests/DeepSpaceSaga.Engine.Tests/CombatReportsScenarioFixtures.cs` | Real scenario report fixture still sends removed fire command | Use default point command/profile and current public positions; retain deterministic report sequence, scanner/outcome and input semantics. |

| `src/DeepSpaceSaga.Engine/SimulationEngine.Encounter.cs` | Fast point clock walks every pending real millisecond even with no encounter-capable enemy | Consume peaceful gaps in one bounded interval when no enemy can enter combat; preserve per-millisecond boundary handling whenever a candidate exists. |

| `tests/DeepSpaceSaga.Engine.Tests/QuotedTradeExecutionTests.cs` | Full-suite failures: current combat continuation copied into fresh/retired economic fixtures | Clear combat continuation when intentionally constructing a fresh scenario or retired-version internal fixture; preserve all Save18 envelopes for current saves and keep economic assertions intact. |

| `tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs` | Full-suite failures: current combat continuation copied into fresh/retired economic fixtures | Clear combat continuation when intentionally constructing a fresh scenario or retired-version internal fixture; preserve all Save18 envelopes for current saves and keep economic assertions intact. |

| `tests/DeepSpaceSaga.Engine.Tests/TradingEconomySaveSchemaTests.cs` | Full-suite failures: current combat continuation copied into fresh/retired economic fixtures | Clear combat continuation when intentionally constructing a fresh scenario or retired-version internal fixture; preserve all Save18 envelopes for current saves and keep economic assertions intact. |

| `tests/DeepSpaceSaga.Engine.Tests/ScenarioEngineTests.cs` | Full-suite assertions still assume old module indices, active types/commands and Strizh limits | Assert independent current type IDs/complete active set, new launch metadata and approved1000/24Strizh limits; keep Basic values and real dispatch checks. |

| `tests/DeepSpaceSaga.Engine.Tests/EngineCategoryDispatchTests.cs` | Full-suite assertions still assume old module indices, active types/commands and Strizh limits | Assert independent current type IDs/complete active set, new launch metadata and approved1000/24Strizh limits; keep Basic values and real dispatch checks. |

| `tests/DeepSpaceSaga.Engine.Tests/CatalogCompatibilityTests.cs` | Economic cargo fixtures retain incompatible point continuation | Clear point continuation for fresh/historical economic fixtures, preserve current-save envelopes. |

| `tests/DeepSpaceSaga.Engine.Tests/GeneratedWorldPersistenceTests.cs` | Legacy world fixture assumes retired save remains loadable | Keep rejection and prove current-save geography is not regenerated. |

| `tests/DeepSpaceSaga.Engine.Tests/MarketKnowledgeTests.cs` | Full-suite historical/fresh economic scenario retains current combat state | Use coherent fresh state or an explicit no-combat economic fixture for historical normalization; retain retired-save rejection and economic assertions. |

| `tests/DeepSpaceSaga.Engine.Tests/MarketRevisionTests.cs` | Full-suite historical/fresh economic scenario retains current combat state | Use coherent fresh state or an explicit no-combat economic fixture for historical normalization; retain retired-save rejection and economic assertions. |

| `tests/DeepSpaceSaga.Engine.Tests/VoyageSaveLoadContinuityTests.cs` | Full-suite historical/fresh economic scenario retains current combat state | Use coherent fresh state or an explicit no-combat economic fixture for historical normalization; retain retired-save rejection and economic assertions. |

| `tests/DeepSpaceSaga.Engine.Tests/StationEconomyGenerationTests.cs` | Full-suite historical/fresh economic scenario retains current combat state | Use coherent fresh state or an explicit no-combat economic fixture for historical normalization; retain retired-save rejection and economic assertions. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Убрать ожидания CombatShmel из актуальных tests; group translation и end-to-end используют Mukha v3 с world-unit конверсией.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Scenario_engine_fixtures_do_not_reference_removed_shmel_file**: выборки и translations работают на оставшихся fixtures.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0019TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0019TK0002Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0019TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US19-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: implementation и self-review выполнены. PASS 4 named / 1265 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0019-tk0002-results.json).

Actual source v3 Main/Single/Pirate translation, pose/outcome/RNG parity;18scenario checks include a100-seed1..100 integration smoke without balance inference. Peaceful large-gap clock regression reproduced under20second process cap before fix and passes afterward; enemy-bearing interval/speed semantics remain covered by existing US8 tests. Economic fresh/historical fixtures are coherent and retain financial, atomicity and real Save18 rejection/roundtrip assertions. PASS2360/2360 Client non-budget,33/33 final schema checks; source Engine format PASS. Two full non-balance Engine attempts timed out; failed diagnostics preserved in evidence/us0019-tk0002-diagnostics.json. Full Engine remains OPEN for legacy cleanup and partitioned generation corpus checks; native/product/balance NOT RUN/OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Before implementation: selected production-scenario fixtures3failed/14passed; full Engine inventory2555cases/190classes. Convert ModuleCombat run and report production run to actual canonical v3 registry and typed point commands. Keep real hostile/player launch, damage, terminal and determinism invariants; replace positive hit-roll assertion with zero legacy rolls/chance fields and positive detonation. Hundred-seed Engine report is a bounded integration smoke on1..100, not US14 acceptance; full out-of-sample1000-seed policies remain US14. No content values change; the initial no-runtime-change plan is superseded by the bounded peaceful-clock correction below.

Smoke seed scope: keep1..100 for the integration report, separate from the fixed3001..4000 US14 balance corpus. No acceptance percentage is asserted or inferred from the smoke report.

Full non-balance Engine reproduction exceeded300000ms (tree terminated at300133ms), with additional old fresh-scenario/envelope fixtures failing. Source audit identifies unconditional1ms real slicing at any high speed. Add an independent large peaceful gap regression and reproduce it under a shorter20second external process cap before the minimal no-enemy optimization. Do not relax encounter timing/speed gates or unrelated validations.

Isolated clock reproduction:20second hard cap reached at20122ms before fix; same target PASS after no-enemy optimization. Full-suite economic failures share fresh SaveFormat0 templates retaining a Save18 point envelope, and retired-version helper retaining modern launcher envelopes. Correct only fixture construction, never relax production continuation validation.
