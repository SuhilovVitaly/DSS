---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0004-point-aim-countermissiles"
ticket: "EP-0012-US-0004-TK-0003-remove-auto-pd"
title: "remove-auto-pd"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0016-TK-0001-combat-localization"]
files_touched: 4
serves: ["US4-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0004-TK-0003-remove-auto-pd

## Why

[Противоракеты](../EP-0012-US-0004-point-aim-countermissiles.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: remove-auto-pd; доказать US4-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs:8; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs:7; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Удалить auto-intercept arbitration, burst, intercept roll и coasting из runtime; старые defense fire/enable/disable не принимаются. Остаточные helpers и legacy test migration — US18.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **No_automatic_defense_burst_or_intercept_roll**: без defense.launch не появляется ПРО; две установки перезаряжаются независимо.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0004TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0004TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.Countermeasures.cs src/DeepSpaceSaga.Engine/SimulationEngine.ManualDefense.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0004TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US4-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.
