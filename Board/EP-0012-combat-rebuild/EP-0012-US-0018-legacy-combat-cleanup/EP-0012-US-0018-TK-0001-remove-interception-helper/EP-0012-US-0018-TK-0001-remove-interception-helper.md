---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0001-remove-interception-helper"
title: "remove-interception-helper"
stage: "implemented"
layer: "engine"
depends_on: ["EP-0012-US-0004-TK-0003-remove-auto-pd"]
files_touched: 5
serves: ["US18-AC1"]
created: "2026-10-20T07:48:46.640Z"
revision: 3
---

# EP-0012-US-0018-TK-0001-remove-interception-helper

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: remove-interception-helper; доказать US18-AC1 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs` (deleted) | Существует: src/DeepSpaceSaga.Engine/Combat/InterceptionMath.cs:3; public static class InterceptionMath | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs` (deleted) | Существует: tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs:7; public class InterceptionChanceTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| [tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs](../../../../tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs) | Существует: tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorRuntimeTests.cs:9; public class WeaponOperatorRuntimeTests | Перенос релевантных legacy assertions/fixtures без ослабления остающихся инвариантов. |
| `tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0001Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

| `src/DeepSpaceSaga.Engine/Scenario/CountermeasureSaveData.cs` | Last production consumer found by rg | Remove historical base-zero interception probability fallback and reject retired interception rating facts; current point journal is already strictly probability-free. Retain unrelated legacy torpedo journal validation until US3-TK5. |

## Public API after the change

Retired public `InterceptionMath` is removed without a replacement probability API. Current point commands, live operator/scatter projection and captured flight contracts remain authoritative.

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

2026-10-21: implementation и self-review выполнены. PASS 4 named / 1273 bounded regression (0 failed/skipped); owning build/scoped whitespace/full formatter/diff check PASS. Все процессы hard cap 300000 ms. [Команды и фактические результаты](../../evidence/us0018-tk0001-results.json).

Deleted InterceptionMath and InterceptionChanceTests; the last production consumer now rejects retired interception rating facts rather than duplicating the old formula. Actual point operator migration18PASS preserves live independent skills, absent-operator no side effects, captured geometry/reporter versus crew edits, real source Settings file load, future catalog changes, old-save atomic rejection and privacy. Rebuilt Client bounded2326PASS excluding five documented legacy defense consumers and ForecastBudget. Full Engine/Client legacy integration remains OPEN for US3-TK5 and US18-TK5; no full-suite/native/balance PASS claim.

Review исполнителя, не независимое. Downstream integration/native/product/balance остаются OPEN до соответствующих тикетов. SHA проверяется после commit/push.

2026-10-20 execution prerequisite correction: legacy removal follows Save18, forecast and Client migrations; see [current DAG](../../Tickets.md#migration-prerequisite-correction). AC/deletion requirements remain unchanged.

2026-10-21 source audit: the only remaining InterceptionMath caller is historical defense-save journal validation; reject this retired continuation instead of recreating its formula elsewhere. Migrate every WeaponOperatorRuntimeTests case to actual source point catalog and Save18, preserving operator identity/absence, live skill projection versus captured geometry, catalog edits, public privacy and atomic old-save rejection. Delete InterceptionChanceTests because probability rules are retired; current launch-scatter stream continuation and legal operator bounds remain covered directly.
