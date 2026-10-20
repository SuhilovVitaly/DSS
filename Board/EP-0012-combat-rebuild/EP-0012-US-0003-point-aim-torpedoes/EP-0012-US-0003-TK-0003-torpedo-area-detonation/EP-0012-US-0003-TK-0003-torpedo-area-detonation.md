---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
ticket: "EP-0012-US-0003-TK-0003-torpedo-area-detonation"
title: "torpedo-area-detonation"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0003-TK-0002-torpedo-point-launch"]
files_touched: 4
serves: ["US3-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0003-TK-0003-torpedo-area-detonation

## Why

[Торпеда в точку](../EP-0012-US-0003-point-aim-torpedoes.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: torpedo-area-detonation; доказать US3-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Актуальный allowlist 4 файлов. После выделения point executor фактическая реализация подрыва живёт в тематическом partial. Нужны scheduler/общий damage helper и ранняя маскировка новых enemy damage facts; существующий PlayerDefeat используется без изменения.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PointCombat.cs) | Point projectile executor | Area resolution, ordinal same-time batch, private damage projection. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.cs) | Snapshot boundary | Point-mode foreign ModuleHealth/Defense masking and boolean EnemyStatus, required before applying real damage. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

Execution refinement: scheduler выбирает captured дробный deadline и обрабатывает все равновременные point-подрывы по ordinal ID, затем смерти. Нулевой rounded damage не вызывает RNG/disable. Общий ApplyModuleHit получает damage/AP/armorDamage параметрами, без поддельного legacy flight. Публичные numeric/module damage details разрешены только для player ship; enemy status — boolean facts. Legacy path сохраняется до cleanup. Counter blasts подключаются своим тикетом перед torpedo batch, без изменения этих правил.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. 1. В `AdvanceCombatTo` (`:232-339`) торпеды взрываются в порядке §3.4.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Area_blast_hits_all_ships_and_friends_once**: два enemy и свой в R получают один mass-weighted module hit каждый.
- **Blast_core_edge_and_zero_damage**: 0/.3R/.65R/R/>R дают200/200/100/0/0 до брони; нулевой урон не отключает модуль.
- **Blast_order_and_partition_are_deterministic**: разное дробление advance сохраняет итог и RNG counters.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0003TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0003TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US3-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-20: implementation и self-review выполнены. PASS 13 named / 273 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0003-tk0003-results.json).

Проверены два enemy и владелец в R, корабль с modules без shipClassId, R-boundary/rounded-zero без hit-location draw, unchanged armor/destruction helper, midpoint AwayFromZero, physical disable8000ms, дробный deadline101.5625ms и движущийся корабль damage88. 12 simultaneous flights: ordinal IDs1,10,11,12,2..9; deaths follow all hits, terminal PlayerDefeat сохраняется. Same-time ship positions captured до damage/engine interruption. Enemy numeric/module/critical-cause details скрыты для point mode. Shared mass-weighted damage применяется без fake legacy flight. Full Engine и общий Client performance gate остаются OPEN по предыдущему evidence, не заменены bounded273.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.
