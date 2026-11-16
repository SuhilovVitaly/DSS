---
epic: EP-0030-debug-console
story: EP-0030-US-0003-kill-drone
ticket: EP-0030-US-0003-TK-0004-handle-flow
title: GameSessionHandle и TacticalBattleFlow — DebugDestroy
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0030-US-0003-TK-0003-local-passthrough]
prerequisite_stories: []
files_touched: 3
serves: [AC-03, AC-04, AC-06]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0004 — Клиентская отправка запроса

## Why

Консольная команда должна отправлять запрос через сессию и flow боя, чтобы подтверждённое состояние принималось тем же путём, что ход, а ошибки транспорта превращались в штатные отказы. Покрывает AC-03/04/06 [истории](../EP-0030-US-0003-kill-drone.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».

## Assumptions

- `Submit` возвращает `null`, если flow уже занят (`Pending`); вызывающий интерпретирует `null` как «занято».
- Операция логируется как `"debug-destroy"` в `SendTactical`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs` | `ExitTacticalBattleAsync` через `SendTactical("exit", ...)` :52-53, `SendTactical` :55 | Добавить `DebugDestroyTacticalUnitAsync` через `SendTactical("debug-destroy", ...)` |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` | `MoveAsync`/`FireAsync` через `Submit` :67-68, `Submit` :119 | `public Task<TacticalBattleResponse?> DebugDestroyAsync(DebugDestroyTacticalUnitRequest request) => Submit(() => Session.DebugDestroyTacticalUnitAsync(request));` |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/DebugDestroyFlowTests.cs` | Новый | Тесты ниже; fake `IGameSessionConnection` (`GameSessionHandle` sealed) |

## Public API after the change

- `GameSessionHandle.DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default): ValueTask<TacticalBattleResponse>`.
- `TacticalBattleFlow.DebugDestroyAsync(DebugDestroyTacticalUnitRequest request): Task<TacticalBattleResponse?>`.

## Implementation steps

1. Тесты ниже.
2. Метод handle по образцу Exit.
3. Метод flow через `Submit`.
4. Build Client + tests, scoped format, `git diff --check`.

## Out of scope

Консольная команда и обновление презентации (TK-0005); новые строки в `UI/Screens/TacticalBattle` (локализационные проверки).

## Invariants

- Ошибка транспорта → `Unavailable` (как в существующих методах `SendTactical`).
- Flow принимает подтверждённый snapshot только при Accepted; одна операция одновременно.

## Tests

`DebugDestroyFlowTests`: `HandleForwardsAndMapsTransportFailureToUnavailable`, `FlowAdoptsAcceptedStateAndReturnsNullWhilePending`. Запуск — после TK-0005.

```powershell
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/DebugDestroyFlowTests.cs
# конец истории:
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter FullyQualifiedName~DebugDestroyFlowTests --results-directory $run --logger trx
```

## Definition of Done

- Три файла; два теста проходят в прогоне конца истории; build/format без ошибок; execution evidence, commit + push.

## Self-containment check

Сигнатуры и образцы (`ExitTacticalBattleAsync`, `MoveAsync`) указаны строками.

## Execution evidence 2026-11-16

Изменённые файлы (3, в пределах Code context):

- `src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs` — `public ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default)` через `SendTactical("debug-destroy", ...)` сразу после `ExitTacticalBattleAsync`; ошибка транспорта → `Unavailable` существующим путём `SendTactical`.
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs` — `public Task<TacticalBattleResponse?> DebugDestroyAsync(DebugDestroyTacticalUnitRequest request) => Submit(() => Session.DebugDestroyTacticalUnitAsync(request));` после `FireAsync`. Новых строк локализации нет.
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/DebugDestroyFlowTests.cs` — новый; fake `IGameSessionConnection` (синтетический snapshot `TutorialTestBattle.Battle()`), тесты `HandleForwardsAndMapsTransportFailureToUnavailable` (проброс исходного запроса, лог `TACTICAL send/accepted debug-destroy`, исключение транспорта → `Unavailable` без `Failure`) и `FlowAdoptsAcceptedStateAndReturnsNullWhilePending` (пока запрос в полёте — второй kill и move возвращают `null` без обращения к соединению; принятый ответ становится `State`/`PresentationState` с ревизией +1; отказ сохраняет подтверждённое состояние и выставляет текст отказа).

Команды и результаты:

- `dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj` — 0 Warning(s), 0 Error(s) (собирает и `DeepSpaceSaga.Client`).
- `dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs tests/DeepSpaceSaga.Client.Tests/TacticalBattle/DebugDestroyFlowTests.cs` — exit 0.
- `git diff --check` — exit 0.
- Тесты: NOT RUN — по инструкции оператора тесты запускаются в конце истории (после TK-0005).
