---
epic: EP-0030-debug-console
story: EP-0030-US-0003-kill-drone
ticket: EP-0030-US-0003-TK-0001-destroy-contract
title: Контракт DebugDestroyTacticalUnitRequest
stage: approved
approval_kind: autonomous-execution
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: []
prerequisite_stories: [EP-0030-US-0001-global-console]
files_touched: 3
serves: [AC-04, AC-06]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0001 — Контракт принудительного уничтожения

## Why

A-08 [эпика](../../Documentation.md): уничтожение авторитетно выполняет Engine. Клиенту нужен транспортный запрос с идентичностью команды, боя и ревизии, чтобы повтор и устаревшие запросы отклонялись. Покрывает AC-04/06 [истории](../EP-0030-US-0003-kill-drone.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».
- A-07/A-08 приняты как написано (решение сессии 2026-11-16).

## Assumptions

- Новые коды отказа не добавляются (существующие `WrongBattle`, `StaleRevision`, `WrongPhase`, `InvalidTarget`, `NoBattle`, `InvalidRequest` достаточны); это не трогает `LocalizationCoverageTests`.
- Метод интерфейса — default-реализация, возвращающая `Unsupported`, как остальные тактические методы.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs` | `ExitTacticalBattleRequest` :22 | Добавить рядом новый record |
| `src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs` | Тактические default-методы :12-28, `UnsupportedTactical` :26 | Добавить default-метод после `ExitTacticalBattleAsync` (:24-25) |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | Существующие тесты контрактов | Тест `DebugDestroyRequestRoundTrips` |

## Public API after the change

```csharp
public sealed record DebugDestroyTacticalUnitRequest(string CommandId, string BattleId, ulong ExpectedRevision, string UnitId);
ValueTask<TacticalBattleResponse> DebugDestroyTacticalUnitAsync(DebugDestroyTacticalUnitRequest request, CancellationToken cancellationToken = default) =>
    UnsupportedTactical(request?.CommandId, cancellationToken);
```

## Implementation steps

1. Тест `DebugDestroyRequestRoundTrips` (JSON round-trip / равенство record по образцу соседних тестов файла).
2. Record в `TacticalBattleRequests.cs`.
3. Default-метод в `IGameSessionConnection`.
4. Build Contracts + Contracts.Tests, scoped format, `git diff --check`.

## Out of scope

Engine-логика (TK-0002), LocalClient (TK-0003), клиентский handle (TK-0004), новые коды отказа.

## Invariants

- Существующие реализации `IGameSessionConnection` компилируются без изменений (default-метод).
- Ответ использует существующий `TacticalBattleResponse` (`TacticalBattleRequests.cs:33`).

## Tests

`TacticalBattleContractTests.DebugDestroyRequestRoundTrips`. Запуск — после последнего тикета истории (TK-0005) по инструкции оператора.

```powershell
dotnet build tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs
# конец истории:
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --filter FullyQualifiedName~TacticalBattleContractTests --results-directory $run --logger trx
```

## Definition of Done

- Record и default-метод добавлены; тест компилируется и проходит в прогоне конца истории.
- Build/format без ошибок; execution evidence, commit + push.

## Self-containment check

Точные сигнатуры и места вставки заданы.
