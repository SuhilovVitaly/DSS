---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0001-contracts-and-schema"
ticket: "EP-0012-US-0001-TK-0004-combat-rules-schema"
title: "combat-rules-schema"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0001-TK-0003-encounter-contracts"]
files_touched: 4
serves: ["US1-AC4"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0001-TK-0004-combat-rules-schema

## Why

[Контракты и схема данных](../EP-0012-US-0001-contracts-and-schema.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: combat-rules-schema; доказать US1-AC4 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs](../../../../src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs) | Существует: src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs:6; internal sealed record CombatRulesDefinition( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs](../../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs) | Существует: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8; public static class EngineContentLoader | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs](../../../../src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs) | Существует: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:9; internal sealed class GameDataRegistry | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

Proposed content schema: blast.coreFraction; scatter.skillPivot/skillSlope/minMultiplier; npc.reactionBaseMs/reactionPerSkillMs/reactionMinMs; combatStartRangeKm. Удалить обязательность chance/fireModes; сохранить armor/destruction. Production JSON меняется в US2-TK5.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Изменить схему и загрузчик combat rules по US1-AC4; отделить загрузку правил от удаляемых fire-modes. В этом тикете только Engine и его тесты, без правки Client/Data.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Rules_reject_invalid_blast_scatter_reaction_and_disable_fields**: coreFraction=0/1, отрицательные disable/scatter отклоняются с file+field.
- **Rules_load_without_fire_modes**: отсутствие fireModes не требует legacy chance section.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0001TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0001TK0004Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US1-AC4 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
