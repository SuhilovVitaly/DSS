---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0007-mukha-ai-v3"
ticket: "EP-0012-US-0007-TK-0003-npc-evade-and-reaction"
title: "npc-evade-and-reaction"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0007-TK-0002-npc-aim-and-fire"]
files_touched: 4
serves: ["US7-AC3"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0007-TK-0003-npc-evade-and-reaction

## Why

[ИИ Мухи v3](../EP-0012-US-0007-mukha-ai-v3.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: npc-evade-and-reaction; доказать US7-AC3 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs:6; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs](../../../../src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs) | Существует: src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs:9; public sealed partial class SimulationEngine | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs](../../../../src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs) | Существует: src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs:9; internal static class RngStreamNames | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; внутренние детали ограничены перечисленным scope.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. `SimulationEngine.HostileCombat.cs` и `.CombatReactions.cs`. Реакция `1400 − 30 × pilot` мс.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Npc_reaction_is_skill_based_and_rolls_once_per_new_threat**: skill0/20/30 =>1400/800/500мс; успех .1 из npc_dodge.
- **Npc_evade_uses_shared_executor_and_pause_stops_deadline**: нет второго RNG roll при дроблении тика/повторе snapshot.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0007TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0007TK0003Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/SimulationEngine.HostileCombat.cs src/DeepSpaceSaga.Engine/SimulationEngine.CombatReactions.cs src/DeepSpaceSaga.Engine/Rng/RngStreamNames.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0007TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US7-AC3 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
