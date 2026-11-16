---
epic: EP-0030-debug-console
story: EP-0030-US-0003-kill-drone
ticket: EP-0030-US-0003-TK-0005-kill-command
title: Команда kill_drone
stage: approved
approval_kind: autonomous-execution
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0030-US-0003-TK-0004-handle-flow]
prerequisite_stories: [EP-0030-US-0001-global-console]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0005 — kill_drone

## Why

D-05 [эпика](../../Documentation.md): уничтожить в текущем бою дрона по имени. Тикет добавляет консольную команду, разрешение цели и отображение подтверждённого состояния. Покрывает AC-01..06 [истории](../EP-0030-US-0003-kill-drone.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».
- **G-04** (пользователь, 2026-11-16): «Id или подпись на карте». Совпадение — точное Ordinal по trimmed Unit Id ИЛИ по подписи `Build?.PilotName ?? Id`; несколько разных юнитов → ошибка неоднозначности; цели — `Kind=Drone` любой стороны; носители отклоняются.
- A-07/A-08 приняты как написано.

## Assumptions

- Disabled-дроны допустимы; только Destroyed → «Цель уже уничтожена».
- Пока показываются результаты залпа (`HasSalvoPresentation`), команда отвечает «занято».
- Принятое состояние показывается через новый `ShowConfirmedState()`, а не `OnBattleUpdated` (тот воспроизводит устаревшие шаги залпа).
- Строки команды — русский текст вне `UI/Screens/TacticalBattle`; `ShowConfirmedState` строк не добавляет.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/KillDroneDebugCommand.cs` | Новый | Команда и `Resolve` |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` | Каталог (US-0001/US-0002) | Одна строка `Register(new KillDroneDebugCommand())` |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | `OnBattleUpdated` :276, `HasSalvoPresentation` :63, `TurnBusy` :87, `_presentation` | `internal void ShowConfirmedState()` рядом с :276: при `Flow.PresentationState != null` — `Volatile.Write(ref _presentation, new(Flow.PresentationState!, null))` |
| `tests/DeepSpaceSaga.Client.Tests/DebugConsole/KillDroneDebugCommandTests.cs` | Новый | Тесты ниже |

Read-only: `GameSessionHandle.TacticalBattleRefusalText` (`GameSessionHandle.TacticalBattle.cs:79`), `TacticalBattleFlow.DebugDestroyAsync` (TK-0004), `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:51,151`.

## Public API after the change

- `KillDroneDebugCommand : IDebugCommand`, descriptor `("kill_drone", "kill_drone <имя>", "Уничтожить дрона любой стороны по Id или имени", "текущий бой")`.
- `internal static KillTargetResolution Resolve(TacticalBattleSnapshot state, string name)` — результат `Found / NotFound / Ambiguous / Carrier / AlreadyDestroyed` с найденным юнитом или списком Id.
- `TacticalBattleScreen.ShowConfirmedState()`.

## Implementation steps

1. Тесты ниже.
2. Пустые аргументы → `Error("Синтаксис: kill_drone <имя>")`.
3. Внутри `context.Host.RunExclusiveAsync`: `ActiveBattle` null / `IsSetup` / сессия не `context.Session` → «Нет активного боя»; `Flow.Pending || TurnBusy || HasSalvoPresentation` → `Busy()`; Phase Over → «Бой уже завершён».
4. `Resolve` (Ordinal по Id или `Build?.PilotName ?? Id`, distinct по Id): >1 → «Неоднозначное имя: X — совпадения: id, id»; NotFound → «Цель не найдена: X»; Carrier → «kill_drone не применяется к носителям»; Destroyed → «Цель уже уничтожена».
5. Запрос `new(NewIdentity("debug-kill"), BattleId, Revision, unitId)` через `Flow.DebugDestroyAsync`; `null` → `Busy()`; отказ → «Отказ: » + `TacticalBattleRefusalText(code)`; принят → `ShowConfirmedState()`, «Уничтожен: label (id)» и «Бой завершён», если Phase Over.
6. Регистрация в каталоге; build, scoped format, `git diff --check`.

## Out of scope

Уничтожение носителей, поиск по подстроке/маске, массовое уничтожение, изменение `OnBattleUpdated`.

## Invariants

- Команда не меняет скорость/паузу и не продвигает ход.
- Неоднозначность или неверная цель не отправляют запрос.
- Устаревший результат другой сессии отбрасывается контроллером (`context.IsCurrent`).

## Tests

`KillDroneDebugCommandTests`: `ResolvesExactIdOrPilotLabelOrdinal`, `NameWithSpacesIsSingleTarget`, `AmbiguousRejectedWithoutRequest`, `CarrierRejected`, `DestroyedReportsAlreadyDestroyed`, `DisabledDroneKillable`, `NoBattleOrSetupRejected`, `BusyDuringPendingTurnOrSalvoPresentation`, `KillsEnemyEndToEndAndPresentationShowsConfirmedState`, `LastEnemyKillEndsBattleThenExitBattleSettlesOnce`, `StaleSessionDiscarded`, `SpeedUnchanged`.

Последний тикет US-0003: после commit/push запустить все тесты истории.

```powershell
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/DebugConsole/ src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs tests/DeepSpaceSaga.Client.Tests/DebugConsole/
# конец истории US-0003:
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --filter FullyQualifiedName~TacticalBattleContractTests --results-directory $run --logger trx
dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --filter "FullyQualifiedName~DebugDestroyTests|FullyQualifiedName~LocalSessionBattleTests|FullyQualifiedName~ConnectionApiTests" --results-directory $run --logger trx
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DebugDestroyFlowTests|FullyQualifiedName~KillDroneDebugCommandTests|FullyQualifiedName~LocalizationCoverageTests" --results-directory $run --logger trx
```

Native: учебный и обычный бой, своя и вражеская цель — только на подтверждённом меньшем мониторе, иначе NOT RUN.

## Definition of Done

- Четыре файла; шаги 2-6 реализованы; тесты истории проходят.
- Build/format без ошибок; execution evidence, commit + push.

## Self-containment check

Правила сопоставления (G-04), порядок проверок, тексты и API заданы полностью.
