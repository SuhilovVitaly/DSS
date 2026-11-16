---
epic: EP-0030-debug-console
story: EP-0030-US-0002-exit-tutorial-battle
ticket: EP-0030-US-0002-TK-0001-exit-battle
title: Команда exit_battle — прерывание обучающего боя
stage: approved
approval_kind: autonomous-execution
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
prerequisite_stories: [EP-0030-US-0001-global-console]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0001 — exit_battle

## Why

D-03 [эпика](../../Documentation.md): `exit_battle` закрывает обучающий бой и открывает карту той же сессии. Engine уже умеет прерывать незавершённый бой с settlement (`SimulationEngine.TacticalBattle.cs:138`), клиент — закрывать бой через `TacticalBattleFlow.ExitAsync()` (`TacticalBattleFlow.cs:70`). Тикет добавляет консольную команду и хост-операцию. Покрывает AC-01..06 [истории](../EP-0030-US-0002-exit-tutorial-battle.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».
- **G-01** (пользователь, 2026-11-16): «Штатный экран гибели». При уничтоженном носителе `exit_battle` прерывает бой, а существующий guard `EnsureShipDestroyedScreen` (`SkiaWindow.cs:1281-1294`) показывает экран гибели, а не карту.
- A-04..A-06 эпика приняты как написано (решение сессии 2026-11-16).

## Assumptions

- Для незавершённого обучения вызывается `tutorial.RecordAbandoned()`, как в пути выхода в главное меню (`SkiaWindow.cs:914-921`).
- `TurnBusy` не блокирует команду: прерывание допустимо во время анимации; блокирует только `Flow.Pending`.
- Ошибка `ExitAsync` оставляет бой открытым; повтор использует штатный retry checkpoint и не повторяет settlement (`LocalGameSessionConnection._settledExits`).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs` | `IDebugConsoleHost` из US-0001 TK-0002 | Добавить `Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken ct)` |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `CloseTacticalBattle` :881-894, RecordAbandoned :914-921, guards :832-841, `EnsureShipDestroyedScreen` :1281-1294, `PopModalAsync` :1351-1362, `_transitionLock` :59 | Явная реализация `ExitTutorialBattleAsync` |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/ExitBattleDebugCommand.cs` | Новый | Команда: проверка аргументов, вызов хоста с `context.Session` |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` | Пустой реестр (US-0001 TK-0003) | Одна строка `Register(new ExitBattleDebugCommand())` |
| `tests/DeepSpaceSaga.Client.Tests/DebugConsole/ExitBattleDebugCommandTests.cs` | Новый | Тесты ниже |

Read-only: `TacticalBattleFlow.cs:34` (`IsTutorial`), `:70-117` (`ExitAsync`), `TacticalBattleScreen.cs:87` (`TurnBusy`); фикстуры `TacticalBattle/BattleNavigationTests.cs:326-501`, `Carrier/Ep0029Us0012Tk0003Tests.cs:178`; `GameSessionHandle` sealed — fake `IGameSessionConnection`.

## Public API after the change

- `IDebugConsoleHost.ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken ct)`.
- `ExitBattleDebugCommand : IDebugCommand`, descriptor `("exit_battle", "exit_battle", "Прервать обучающий бой и открыть карту", "обучающий бой")`.

## Implementation steps

1. Написать тесты ниже.
2. Команда: непустые аргументы → `Error("Синтаксис: exit_battle")`; иначе `context.Host.ExitTutorialBattleAsync(context.Session, ct)`.
3. Хост под `_transitionLock.WaitAsync(0)`, иначе `Busy()`:
   - `_closing`, нет сессии, `!ReferenceEquals(expectedSession, _session)` → «Нет активного обучающего боя»;
   - требовать `_screens.Current is TacticalBattleScreen { IsSetup: false, Flow.IsTutorial: true }`, иначе та же ошибка;
   - `Flow.Pending` → `Busy()`;
   - `unfinished = Flow.State is { Outcome: null }`;
   - `!await Flow.ExitAsync()` → `Error("Не удалось завершить бой: " + Flow.Error + ". Повторите exit_battle")`, бой остаётся;
   - если `unfinished` → `Tutorial?.RecordAbandoned()`; `await PopModalAsync()`; `destroyed = EnsureShipDestroyedScreen()`;
   - `Ok(destroyed ? "Бой прерван. Носитель уничтожен — экран гибели" : "Бой завершён, открыта карта")` с `CloseConsole: true`.
4. Зарегистрировать команду в каталоге.
5. Build Client + tests, scoped format, `git diff --check`.

## Out of scope

Изменение Esc/кнопок выхода и tutorial gate EP-0027; воскрешение носителя; Engine/Contracts; обычный (не обучающий) бой.

## Invariants

- Settlement применяется ровно один раз (Engine idempotency `ExecuteTactical`, `SimulationEngine.TacticalBattle.cs:164`).
- Уроки не отмечаются пройденными; прерывание не победа.
- Guard гибели не обходится (G-01).
- Обычный Esc и кнопки выхода работают как прежде.

## Tests

`ExitBattleDebugCommandTests`: `ExitsUnfinishedTutorialToMapOfSameSession`, `AbortedSettlementAppliedOnceAndAbandonRecorded`, `WorksDuringAnimationAndWithConfirmExitOpen`, `RejectsArguments`, `NoSessionAndNonTutorialBattleReturnNoTutorialBattle`, `PendingFlowOrHeldLockReturnsBusy`, `CheckpointFailureKeepsBattleThenRetryDoesNotResettle`, `FinishedTutorialClosesNormally`, `DestroyedCarrierRoutesToShipDestroyedScreen`, `StaleSessionRejected`, `EscAndExitButtonsUnchanged`.

Единственный тикет истории: после commit/push запустить тесты истории.

```powershell
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs src/DeepSpaceSaga.Client/UI/DebugConsole/ tests/DeepSpaceSaga.Client.Tests/DebugConsole/
# конец истории US-0002:
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~ExitBattleDebugCommandTests|FullyQualifiedName~DebugConsole|FullyQualifiedName~BattleNavigationTests" --results-directory $run --logger trx
```

Native: консоль → `exit_battle` → карта той же системы; только на подтверждённом меньшем мониторе, иначе NOT RUN.

## Definition of Done

- Пять файлов; шаги 2-4 реализованы; 11 тестов проходят в прогоне конца истории.
- Build/format без ошибок; execution evidence в тикете, commit + push.

## Self-containment check

Порядок проверок хоста, тексты, точки `SkiaWindow` и решение G-01 заданы; дополнительных решений не требуется.
