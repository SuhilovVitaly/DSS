---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
ticket: "EP-0012-US-0019-TK-0002-scenario-engine-fixtures"
title: "scenario-engine-fixtures"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios"]
files_touched: 4
serves: ["US19-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0019-TK-0002-scenario-engine-fixtures

## Why

[Совместимость сценариев и старых клиентских поверхностей](../EP-0012-US-0019-scenario-and-client-regressions.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: scenario-engine-fixtures; доказать US19-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs:7; public sealed class ScenarioGroupTranslationTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs:9; public sealed class ModuleCombatEndToEndTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs:5; public sealed class CombatBalanceReport | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0019TK0002Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

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

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
