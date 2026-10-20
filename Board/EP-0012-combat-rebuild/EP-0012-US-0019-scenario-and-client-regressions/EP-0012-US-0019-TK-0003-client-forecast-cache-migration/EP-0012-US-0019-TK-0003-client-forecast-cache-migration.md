---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0019-scenario-and-client-regressions"
ticket: "EP-0012-US-0019-TK-0003-client-forecast-cache-migration"
title: "client-forecast-cache-migration"
stage: "draft"
layer: "client"
depends_on: ["EP-0012-US-0017-TK-0003-local-client-delivery"]
files_touched: 5
serves: ["US19-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0019-TK-0003-client-forecast-cache-migration

## Why

[Совместимость сценариев и старых клиентских поверхностей](../EP-0012-US-0019-scenario-and-client-regressions.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: client-forecast-cache-migration; доказать US19-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (5 файлов, включая tests); всё вне списка запрещено. Baseline c894925d187c90a658d1e25958feed69a45d7e41.

| File | Current state | Allowed change |
|---|---|---|
| [src/DeepSpaceSaga.Client/SnapshotBuffer.cs](../../../../src/DeepSpaceSaga.Client/SnapshotBuffer.cs) | Существует: src/DeepSpaceSaga.Client/SnapshotBuffer.cs:8; public sealed record SnapshotPrediction( | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs](../../../../src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs) | Существует: src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs:8; public sealed partial class SnapshotBuffer | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatForecastCache.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatForecastCache.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatForecastCache.cs:9; internal sealed class CombatForecastCache | Только изменения для scope этой карточки и served AC. |
| [src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs) | Существует: src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs:8; internal static class CombatTrajectoryProjector | Только изменения для scope этой карточки и served AC. |
| `tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0003Tests.cs` | **NEW — proposed**, сейчас отсутствует | Named cases ниже; независимые expected значения. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. Перевести client snapshot buffer, forecast cache и projector на новые own/observed/projectile kinds; explicit removal и display-admitted data сохраняются.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: client; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Все ниже — **план**, не выполненные тесты.

- **Client_prediction_uses_admitted_observations_only**: future packets/HostileCombat не уточняют enemy point; duplicate removal/load reset корректны.

Matching project: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`; proposed test class `PointCombatUS0019TK0003Tests`. Test process hard timeout5минут; баланс1000seed — batches с полным суммарным корпусом.

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~PointCombatUS0019TK0003Tests"
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore
dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/SnapshotBuffer.cs src/DeepSpaceSaga.Client/SnapshotBuffer.Presentation.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatForecastCache.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/CombatTrajectoryProjector.cs tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0003Tests.cs
git diff --check
```

## Definition of Done

- Served AC US19-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

Planning-only / draft. Implementation NOT STARTED; commands NOT RUN; native/product OPEN.

2026-10-20 consumer prerequisite correction: point consumers precede production data activation; actual integration gates remain OPEN until activation/Save18. See [DAG](../../Tickets.md#consumer-prerequisite-correction).
