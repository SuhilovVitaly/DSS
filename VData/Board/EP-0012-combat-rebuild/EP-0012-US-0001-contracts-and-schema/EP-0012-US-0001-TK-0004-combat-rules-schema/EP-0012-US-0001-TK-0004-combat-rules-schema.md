---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0001-contracts-and-schema"
ticket: "EP-0012-US-0001-TK-0004-combat-rules-schema"
title: "combat-rules-schema"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0001-TK-0003-encounter-contracts"]
files_touched: 9
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

Актуальный allowlist (9 файлов, включая tests); расширение обосновано ниже. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs](../../../../../src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs) | Существует: src/DeepSpaceSaga.Engine/Content/CombatRulesDefinition.cs:6; internal sealed record CombatRulesDefinition( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs](../../../../../src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs) | Существует: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:8; public static class EngineContentLoader | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs](../../../../../src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs) | Существует: src/DeepSpaceSaga.Engine/Content/GameDataRegistry.cs:9; internal sealed class GameDataRegistry | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0001TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |
| `tests/DeepSpaceSaga.Engine.Tests/CombatRulesContentTests.cs` | Existing legacy content assertions | Nullable Chance assertion; revise missing-path expectation for optional fireModes. |
| `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs` | Existing legacy fixture | Assert legacy Chance exists before with-expression. |
| `tests/DeepSpaceSaga.Engine.Tests/AutomaticDefenseTests.cs` | Existing legacy fixture | Assert legacy Chance exists before with-expression. |
| `tests/DeepSpaceSaga.Engine.Tests/PhaseThreeJournalTests.cs` | Existing legacy fixture | Assert legacy Chance exists before with-expression. |
| `tests/DeepSpaceSaga.Engine.Tests/DefeatTimeBoundaryTests.cs` | Existing legacy fixture | Assert legacy Chance exists before with-expression. |

## Public API after the change

Proposed content schema: blast.coreFraction; scatter.skillPivot/skillSlope/minMultiplier; npc.reactionBaseMs/reactionPerSkillMs/reactionMinMs; combatStartRangeKm. Удалить обязательность chance/fireModes; сохранить armor/destruction. Production JSON меняется в US2-TK5.

### Уточнение исполнения — 2026-10-20

Dependency TK-0003 опубликован: `c3978c9cb3dda2c030bbe10e42d6c3839827387c`, remote SHA проверен. Scope расширен до 9 файлов по полномочиям EpicExecutionPrompt: nullable Chance затрагивает ровно пять существующих test consumers, перечисленных выше. Добавляются assert присутствия legacy Chance, а не ослабление проверок попаданий/исходов.

На переходном этапе loader различает полный новый набор blast/scatter/npc/combatStartRangeKm и прежний набор chance: новые поля обязательны совместно и проверяются строго; legacy остаётся только для ещё не мигрировавших fixtures/runtime до предусмотренного cleanup. Новый набор не принимает chance/fireModes. Отсутствующий fireModes допустим только для нового набора; объявленный пустой/нечитаемый путь остаётся ошибкой. Missing combatRules file определяется до проверки optional fireModes — соответствующее старое ожидание обновляется, сохраняя диагностику filename. Здесь не меняются production JSON, damage/armor/destruction или runtime алгоритмы.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Изменить схему и загрузчик combat rules по US1-AC4; отделить загрузку правил от удаляемых fire-modes. В этом тикете только Engine и его тесты, без правки Client/Data.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

План проверок; фактические результаты исполнения приведены в Status / Evidence.

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

2026-10-20: schema implementation и self-review выполнены. Named checks PASS 24/24; регрессия всех изменённых fixture consumers и content loader PASS 65/65 (0 failed/skipped); owning build 0 warnings/errors; scoped whitespace/full formatter и diff check PASS. Первое неверное сравнение ImmutableArray в новом тесте заменено последовательным сравнением элементов, затем 24/24 PASS. Старые assertions исходов не удалялись.

Дополнительный полный Engine test process **не завершился**: hard timeout 300000 ms, дерево завершено; полный PASS не заявляется. Общий Engine gate OPEN и должен быть выполнен bounded пакетами на интегрированной реализации. В regression не включён незавершённый полный результат как будто он прошёл. [Фактические команды, stdout, timeout metadata](../../evidence/tk0004-results.json).

Self-review проверил полный новый набор обязательных полей, finite/range guards, запрет смешанного chance/modes, filename+field диагностику, прежние armor/destruction, сохранение старого формата только на время migration. Это review исполнителя, не независимый review. Production JSON, runtime point combat, native/product/balance OPEN. Отдельный commit/push разрешён текущим поручением; SHA проверяется после коммита.
