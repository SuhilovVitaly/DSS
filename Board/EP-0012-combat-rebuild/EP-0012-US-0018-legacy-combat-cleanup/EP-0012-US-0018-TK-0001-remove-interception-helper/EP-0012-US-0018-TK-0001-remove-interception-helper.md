---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0001-remove-interception-helper"
title: "remove-interception-helper"
stage: "draft"
layer: "engine"
depends_on: ["EP-0012-US-0009-TK-0005-save18-defense-validation"]
files_touched: 4
serves: ["US18-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0018-TK-0001-remove-interception-helper

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: remove-interception-helper; доказать US18-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (4 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs](../../../../src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs) | Существует: src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs:3; public static class InterceptionMath | Только изменения для scope этой карточки и served AC. |
| [tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs:7; public class InterceptionChanceTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs:9; public class WeaponOperatorRuntimeTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. После save18 validation удалить устаревший InterceptionMath и InterceptionChanceTests; перенести сохраняющиеся operator invariants в геометрические regression tests.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: engine; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Legacy_interception_api_has_no_consumers**: rg по production и tests, build owning project; operator failures по-прежнему не расходуют RNG.

Matching project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`; proposed test class `PointCombatUS0018TK0001Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0018TK0001Tests"
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0001Tests.cs
git diff --check
```

## Definition of Done

- Served AC US18-AC1 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
