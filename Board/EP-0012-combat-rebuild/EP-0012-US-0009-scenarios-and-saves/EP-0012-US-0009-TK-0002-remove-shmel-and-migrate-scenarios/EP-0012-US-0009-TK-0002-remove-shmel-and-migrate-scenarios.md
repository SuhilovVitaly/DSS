---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0009-scenarios-and-saves"
ticket: "EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios"
title: "remove-shmel-and-migrate-scenarios"
stage: "implemented"
layer: "content-data"
depends_on: ["EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3"]
files_touched: 13
serves: ["US9-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios

## Why

[Сценарии и сохранения](../EP-0012-US-0009-scenarios-and-saves.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: remove-shmel-and-migrate-scenarios; доказать US9-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (13 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Scenarios/CombatShmel/scenario.json` | Существует: src/DeepSpaceSaga.Client/Scenarios/CombatShmel/scenario.json:1; { | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/Data/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json](../../../../src/DeepSpaceSaga.Client/Data/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json) | Существует: src/DeepSpaceSaga.Client/Data/Modules/CountermeasureLauncher/modules-countermeasurelauncher.json:1; { | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `Directory.Build.targets` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/DeepSpaceSaga.Client.Tests/CountermeasureContentTests.cs` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/DeepSpaceSaga.Client.Tests/ModuleStructureCatalogTests.cs` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/Support/LegacyCombatCatalog.json` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

| `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs` | Positive retired scenario/module references or obsolete output content | Remove only Shmel/single selections or unused single locale keys; replace active content assertions by absence guards; clean fixed obsolete deployed scenario file. Preserve other scenarios/classes/assertions. Full Engine point fixture migration stays US19-TK2. |

## Public API after the change

No public API change. CombatShmel and unused single launcher are retired from source/deployed content and positive test selectors; ship.shmel class and all eight surviving scenarios remain.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Удалить CombatShmel scenario и unused single launcher. Исправление Engine/Client test fixtures и остальных сценариев — US19; src/DeepSpaceSaga.CombatSim не менять, даже если в нём остаются исторические ссылки.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: content-data; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Shmel_scenario_is_absent_and_single_launcher_unreferenced**: scenario discovery не показывает Shmel; class shmel остаётся; single удалён только без ссылок.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0009TK0002Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0009TK0002Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include tests/DeepSpaceSaga.Client.Tests/PointCombatUS0009TK0002Tests.cs
git diff --check
```

## Definition of Done

- Served AC US9-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 2 named / 2358 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0009-tk0002-results.json).

Source/deployed scenario retirement and all eight surviving Engine loads PASS; ship.shmel class and single-Mukha preserved. No single-launcher data references; positive retired selectors removed. Engine discovery10/10 PASS. Active documentation cleanup remains final US15; complete US9-AC2 OPEN.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

Before implementation: retirement must remove positive data-driven selectors and single-module catalog expectations atomically. Expand matching checks to both Client and Engine fixture selectors; their broader old fire/roll behavior migration remains US19-TK2, not claimed by this ticket. Remove unused single locale labels and test-only unused single definition too. Keep ship.shmel class, one-Mukha scenario and all remaining cases. Directory.Build.targets removes only Scenarios/CombatShmel/scenario.json in build/publish outputs when its source is absent. Active documentation cleanup remains final US15, so US9-AC2 whole-story acceptance stays OPEN until then.
