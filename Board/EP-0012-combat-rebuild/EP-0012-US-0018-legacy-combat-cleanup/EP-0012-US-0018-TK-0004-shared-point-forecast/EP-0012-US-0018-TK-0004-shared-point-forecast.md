---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
ticket: "EP-0012-US-0018-TK-0004-shared-point-forecast"
title: "shared-point-forecast"
stage: "draft"
layer: "motion"
depends_on: ["EP-0012-US-0006-TK-0001-blast-threat-math"]
files_touched: 3
serves: ["US18-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 1
---

# EP-0012-US-0018-TK-0004-shared-point-forecast

## Why

[Удаление старой боевой модели и регрессии](../EP-0012-US-0018-legacy-combat-cleanup.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: shared-point-forecast; доказать US18-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (3 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Motion/CombatForecast.cs](../../../../src/DeepSpaceSaga.Motion/CombatForecast.cs) | Существует: src/DeepSpaceSaga.Motion/CombatForecast.cs:6; public enum CombatForecastStatus { Recalculating, Contact, NoContact, CalculationLimited, InsufficientData, TargetLost, Expired, MissedCoast, TargetTerminated } | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs](../../../../src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs) | Существует: src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs:6; public static class CurrentManeuverForecast | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0018TK0004Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перевести shared CombatForecast/CurrentManeuverForecast на новые DTO и чистую геометрию, сохранив cancellation/budget semantics; guidance helpers удалять только при отсутствии потребителей.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: motion; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Shared_forecast_has_no_hidden_enemy_point_and_matches_own_deadline**: equality на прямом участке, expiry/cancellation/CalculationLimited сохранены.

Matching project: `tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj`; proposed test class `PointCombatUS0018TK0004Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0018TK0004Tests"
dotnet build tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Motion/CombatForecast.cs src/DeepSpaceSaga.Motion/CurrentManeuverForecast.cs tests/DeepSpaceSaga.Motion.Tests/PointCombatUS0018TK0004Tests.cs
git diff --check
```

## Definition of Done

- Served AC US18-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.
