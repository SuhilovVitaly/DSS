---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0003-point-aim-torpedoes"
ticket: "EP-0012-US-0003-TK-0003-torpedo-area-detonation"
title: "torpedo-area-detonation"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0003-TK-0002-torpedo-point-launch"]
files_touched: 4
serves: ["US3-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0003-TK-0003-torpedo-area-detonation

## Why

[Торпеда в точку](../EP-0012-US-0003-point-aim-torpedoes.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: torpedo-area-detonation; доказать US3-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatJournal.cs:4; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.PlayerDefeat.cs:5; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0003TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

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

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
