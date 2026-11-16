---
epic: EP-0030-debug-console
story: EP-0030-US-0003-kill-drone
ticket: EP-0030-US-0003-TK-0003-local-passthrough
title: LocalGameSessionConnection — проброс DebugDestroy
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
layer: local-client
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0030-US-0003-TK-0002-engine-force-destroy]
prerequisite_stories: []
files_touched: 3
serves: [AC-01, AC-06]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0003 — Локальный проброс запроса

## Why

Клиент общается с Engine через `IGameSessionConnection`; без переопределения локальное соединение вернёт `Unsupported` из default-метода TK-0001. Покрывает транспортную часть AC-01/06 [истории](../EP-0030-US-0003-kill-drone.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».

## Assumptions

- Интеграционные тесты local-client живут в Engine.Tests (`TacticalBattle/LocalSessionBattleTests.cs`, `ConnectionApiTests.cs`), как для существующих тактических методов.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` | Тактические методы через `TacticalCall` :124-133 | Добавить `DebugDestroyTacticalUnitAsync(...) => TacticalCall(() => _engine.DebugDestroyTacticalUnit(request), cancellationToken);` |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs` | Сценарии локальной сессии | `LocalConnectionForwardsDebugDestroy` |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs` | Список default-методов, возвращающих Unsupported | Добавить новый метод в проверку default-Unsupported |

## Public API after the change

`LocalGameSessionConnection.DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest, CancellationToken)` — публичное переопределение default-метода интерфейса.

## Implementation steps

1. Тесты: `LocalConnectionForwardsDebugDestroy` (Accepted, юнит Destroyed в последующем snapshot); default-реализация интерфейса возвращает `Unsupported`.
2. Однострочный метод в соединении.
3. Build Engine.Tests, scoped format, `git diff --check`.

## Out of scope

Клиентский `GameSessionHandle` (TK-0004), Engine-логика (TK-0002).

## Invariants

Соединение не содержит игровой логики; ошибки транспорта/отмена обрабатываются `TacticalCall` как для остальных методов.

## Tests

`LocalSessionBattleTests.LocalConnectionForwardsDebugDestroy`; расширенный default-Unsupported тест в `ConnectionApiTests`. Запуск — после TK-0005.

```powershell
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs
# конец истории:
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~LocalSessionBattleTests|FullyQualifiedName~ConnectionApiTests|FullyQualifiedName~DebugDestroyTests" --results-directory $run --logger trx
```

## Definition of Done

- Три файла; тесты проходят в прогоне конца истории; build/format без ошибок; execution evidence, commit + push.

## Self-containment check

Одна строка кода и два теста, все пути указаны.

## Execution evidence 2026-11-16

Изменённые файлы (3, в пределах Code context):

- `src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs` — `public ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default) => TacticalCall(() => _engine.DebugDestroyTacticalUnit(request), cancellationToken);` после `FireTacticalBattleAsync`. Игровой логики в соединении нет; отмена/dispose обрабатываются `TacticalCall`.
- `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs` — тест `LocalConnectionForwardsDebugDestroy`: Accepted, цель `Destroyed` в ответе и в последующем `GetTacticalBattleStateAsync`, ревизия +1, повтор того же CommandId возвращает тот же ответ, новая команда со старой ревизией → `StaleRevision`, отменённый токен → `OperationCanceledException` без мутации.
- `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs` — `LegacyImplementationsInheritTypedUnavailableDefaults` дополнен вызовом `DebugDestroyTacticalUnitAsync` (default-метод интерфейса → `Unsupported`). Рабочая копия файла была с LF (в отличие от остальных .cs) — переведена на CRLF, чтобы прошёл scoped `dotnet format`; при `core.autocrlf=true` содержимое в репозитории не меняется кроме самих строк теста.

Команды и результаты:

- `dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj` — до реализации: только CS1061 `LocalGameSessionConnection.DebugDestroyTacticalUnitAsync` отсутствует (red); после — 0 Warning(s), 0 Error(s).
- `dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs` — exit 0.
- `git diff --check` — exit 0.
- Тесты: NOT RUN — по инструкции оператора тесты запускаются в конце истории (после TK-0005).
