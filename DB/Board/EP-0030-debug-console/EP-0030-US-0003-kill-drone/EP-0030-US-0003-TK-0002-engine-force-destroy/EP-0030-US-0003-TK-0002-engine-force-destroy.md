---
epic: EP-0030-debug-console
story: EP-0030-US-0003-kill-drone
ticket: EP-0030-US-0003-TK-0002-engine-force-destroy
title: Engine — авторитетное принудительное уничтожение юнита
stage: approved
approval_kind: autonomous-execution
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0030-US-0003-TK-0001-destroy-contract]
prerequisite_stories: []
files_touched: 3
serves: [AC-01, AC-03, AC-04, AC-05, AC-06]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0002 — DebugDestroyTacticalUnit в Engine

## Why

A-08 [эпика](../../Documentation.md): уничтожение обходит попадание/броню/щиты, не расходует выстрел, торпеду и RNG, не продвигает ход, но вызывает штатные последствия гибели и проверку исхода. Покрывает AC-01, AC-03..06 [истории](../EP-0030-US-0003-kill-drone.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».
- **G-04** (пользователь, 2026-11-16): «Id или подпись на карте». Цели — юниты `Kind=Drone` любой стороны; носители отклоняются.
- A-08 принято исполнителем как техническое допущение (не решение пользователя).

## Assumptions

- Отключённые (Disabled) дроны допустимые цели; только Destroyed → `InvalidTarget`.
- Поле `FirstLoss` фиксируется только если цель была активной (как в залповом пути `TacticalBattleSalvo.cs:263`).
- Не трогать `_paths`, `_shots`, `_fireSteps` и RNG — запланированные действия убитого дрона отбрасываются штатной проверкой статуса при исполнении фазы.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs` | `ExitTacticalBattle` :138, `ExecuteTactical` :164 (idempotency по CommandId) | Добавить `public TacticalBattleResponse DebugDestroyTacticalUnit(DebugDestroyTacticalUnitRequest request)` около :160 |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | `Check` (валидация BattleId/ревизии), журнал Destroyed :171-177, `Battle.CheckVictory()`, `AddOutcome` | Добавить `DebugDestroy(request)` |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DebugDestroyTests.cs` | Новый | Тесты ниже |

Read-only: путь уничтожения залпом `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs:236`, FirstLoss :263; tombstone settlement `src/DeepSpaceSaga.Engine/SimulationEngine.SortieSettlement.cs:35`.

## Public API after the change

- `SimulationEngine.DebugDestroyTacticalUnit(DebugDestroyTacticalUnitRequest request): TacticalBattleResponse` — через `ExecuteTactical`; `null` → `InvalidRequest`; нет сессии → `NoBattle`.
- `TacticalBattleSession.DebugDestroy(DebugDestroyTacticalUnitRequest request): TacticalBattleResponse` (internal/public по соседним методам).

## Implementation steps

1. Тесты ниже.
2. Отказы без мутации: BattleId → `WrongBattle`; ревизия → `StaleRevision`; Phase Over → `WrongPhase`; юнит отсутствует / Carrier / Destroyed → `InvalidTarget`.
3. Применение: `wasActive`; `Hull=0`, `Status=Destroyed`, `Speed=0`; если `wasActive` — `FirstLoss ??= side`; событие Destroyed в журнале (id цели, `Describe` юнита); `CheckVictory()`; если `Result != null` — `AddOutcome(phaseBefore)`; `Revision++`; `Accepted`.
4. Метод engine через `ExecuteTactical`.
5. Build Engine + Engine.Tests, scoped format, `git diff --check`.

## Out of scope

LocalClient и клиент (TK-0003..0005), новые коды отказа, восстановление юнитов, уничтожение носителей.

## Invariants

- Engine — единственный владелец исхода (`DB/Documentation/01-Requirements/EngineRequirements.md:11`); повтор CommandId возвращает кэшированный ответ (`:45`).
- RNG, боезапас и номер хода не меняются.
- Потеря своего дрона попадает в settlement, включая прерванный бой (tombstone).

## Tests

`DebugDestroyTests`: `DestroysEnemyDroneWithoutRngAmmoOrTurnAdvance`, `DestroysOwnDisabledDrone`, `LogsDestroyedEvent`, `RejectsCarrierUnknownAndDestroyedUnchanged`, `RejectsWrongBattleStaleRevisionOverPhase`, `SameCommandIdReturnsCachedReceipt`, `LastEnemyDroneEndsBattleWithOutcome`, `KilledDroneNeitherMovesNorFires`, `FirePhaseKillRemovesItFromTargets`, `AbortedOwnedSortieTombstonesKilledDrone`.

Запуск — после последнего тикета истории (TK-0005).

```powershell
dotnet build tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DebugDestroyTests.cs
# конец истории:
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter FullyQualifiedName~DebugDestroyTests --results-directory $run --logger trx
```

## Definition of Done

- Три файла; правила отказа и применения реализованы; 10 тестов проходят в прогоне конца истории.
- Build/format без ошибок; execution evidence, commit + push.

## Self-containment check

Порядок проверок, мутации, коды и точки кода заданы; детерминизм защищён запретом трогать RNG/пути/выстрелы.
