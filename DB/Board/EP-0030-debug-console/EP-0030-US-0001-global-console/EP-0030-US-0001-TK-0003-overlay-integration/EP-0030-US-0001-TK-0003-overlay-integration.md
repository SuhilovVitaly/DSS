---
epic: EP-0030-debug-console
story: EP-0030-US-0001-global-console
ticket: EP-0030-US-0001-TK-0003-overlay-integration
title: Overlay консоли и подключение к SkiaWindow
stage: approved
approval_kind: autonomous-execution
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0030-US-0001-TK-0002-console-core]
prerequisite_stories: []
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04, AC-05]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0003 — Overlay и глобальная маршрутизация ввода

## Why

Консоль должна открываться на любом экране и поверх модальных окон, забирать ввод и рисоваться над стеком экранов (D-01/D-02 [эпика](../../Documentation.md)). Тикет подключает контроллер TK-0002 к `SkiaWindow` и добавляет точку регистрации команд. Покрывает AC-01..05 [истории](../EP-0030-US-0001-global-console.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».
- A-01, A-02, A-06 эпика приняты как написано (решение сессии 2026-11-16): консоль доступна в обычном запуске без флага сборки.

## Assumptions

- Консоль недоступна в краткое окно `StationOverlayIsInvalid()` и при зависшем UI-потоке; это не нарушает A-06.
- F10 (скриншот) продолжает работать при открытой консоли.
- Overlay занимает верхние ~45% окна; масштаб — `(float)GetUiScale()`.
- `DebugCommandCatalog.CreateDefault()` в US-0001 пуст; каждая следующая история добавляет одну строку `Register`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugConsoleOverlay.cs` | Новый | `Render(SKCanvas, DebugConsoleController, int width, int height, float uiScale)`, `OnMouseDown/OnMouseMove/OnMouseUp/OnMouseWheel`; `ScrollTextPanel` для вывода + `TextInputBox` (оба в `UI/Controls`) |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` | Новый | `internal static class DebugCommandCatalog { static DebugCommandRegistry CreateDefault(); }` — пока пустой реестр |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | `PollKeyboard` :690-729, `HandleKeyboardEvent` :732 (F10 :734, pending transitions :740-749, `Current.OnKeyDown` :751), `OnKeyChar` :678-686, мышь :609/:637/:646/:663, рендер стека :378-392, ConnectionLost :393-395, `canvas.Restore` :400, `OnFocusChanged` :554-588, `_transitionLock` :59, `_session` :54 | Реализовать `IDebugConsoleHost` явно, поле `internal DebugConsoleController DebugConsole`, маршрутизация шагов 1-8 |
| `tests/DeepSpaceSaga.Client.Tests/DebugConsole/DebugConsoleWindowTests.cs` | Новый | Window-less тесты ниже |

Read-only: window-less конструктор `new SkiaWindow(screen, factory, null, createNativeWindow:false, settingsPath)` (`SkiaWindow.cs:102`); фикстуры `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs:326-501`, `TacticalBattle/KeyboardRoutingTests.cs:55-73`, `Carrier/Ep0029Us0012Tk0003Tests.cs` (private LifecycleFixture :178 — копировать паттерн, не выносить). Коллекция `[Collection("InterfaceLog")]`.

## Public API after the change

- `SkiaWindow : IDebugConsoleHost` (явная реализация): `Session => _session`, `IsTransitionBusy => _transitionLock.CurrentCount == 0`, `ActiveBattle => _screens.Current as TacticalBattleScreen`, `RunExclusiveAsync` — `_transitionLock.WaitAsync(0)`, при неудаче `DebugCommandResult.Busy()`, иначе выполнить и `Release` в finally.
- `internal DebugConsoleController DebugConsole { get; }`.
- `DebugConsoleOverlay` и `DebugCommandCatalog` — по таблице.

## Implementation steps

1. Написать тесты ниже по образцу window-less фикстур.
2. `HandleKeyboardEvent`, до F10: `IsToggleChord(key, mods) && !repeat` → `Toggle`, return. Если открыта: F10 делает скриншот как раньше, иначе `DebugConsole.OnKeyDown`, return.
3. `PollKeyboard`: пока консоль открыта, не вызывать `SynchronizeModifiers` и не пересылать `OnKeyUp` экрану.
4. `OnKeyChar`: при открытой консоли — в консоль; символы при зажатых Ctrl+Alt отбрасывать (иначе «p»/«з» попадёт во ввод).
5. Мышь: при открытой консоли — только overlay, курсор по умолчанию, без `HandleScreenEvent`.
6. При открытии повторить потерю фокуса (`OnFocusChanged` :577-587): `ResetRepeats`, `Current.OnInputReset()`, `GameSessionScreen.OnDeactivated()`. При закрытии, если окно в фокусе: `ResyncToCurrentState(_keyboard, Current is TacticalBattleModalScreen)` и `OnActivated()`.
7. `OnRender`: рисовать overlay после ConnectionLost (:395) и до `canvas.Restore` (:400).
8. `RunExclusiveAsync` через `_transitionLock.WaitAsync(0)`; `Dispose` окна освобождает контроллер.
9. Build Client + tests, scoped format, `git diff --check`.

## Out of scope

Конкретные команды; изменение скорости/паузы; новые экраны; системные горячие клавиши вне окна приложения.

## Invariants

- Открытие/закрытие не меняет паузу и скорость (A-02, AC-05).
- Ни клавиши, ни символы, ни клики не доходят до нижележащего экрана, пока консоль открыта (AC-03); Esc закрывает только консоль.
- Удержание сочетания переключает один раз; после закрытия удерживаемые клавиши не воспроизводятся.
- Переходы экранов по-прежнему сериализуются `_transitionLock` (`SkiaWindow.cs:59`); консоль не захватывает его блокирующе.

## Tests

`DebugConsoleWindowTests`: `ChordOpensConsoleOnMainMenuWithoutSession`, `ChordOpensOverMapAndTutorialBattleWithoutSpeedChange`, `KeysCharsAndClicksDoNotReachScreenWhileOpen`, `EscapeClosesOnlyConsole`, `HeldChordTogglesOnce`, `CloseDoesNotReplayHeldKeys`, `RunExclusiveReportsBusyWhileTransitionLockHeld`, `OverlayRendersAboveStack`.

Последний тикет US-0001: после commit/push выполнить тесты всей истории.

```powershell
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/SkiaWindow.cs src/DeepSpaceSaga.Client/UI/DebugConsole/ tests/DeepSpaceSaga.Client.Tests/DebugConsole/
# конец истории US-0001:
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~KeyboardEdgeTrackerTests|FullyQualifiedName~DebugConsoleControllerTests|FullyQualifiedName~DebugConsoleWindowTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~BattleNavigationTests" --results-directory $run --logger trx
```

Native-проверка (главное меню, карта, бой, модальные окна, RU/EN) — только на подтверждённом меньшем мониторе по AGENTS.md; иначе NOT RUN.

## Definition of Done

- Четыре файла изменены/созданы; шаги 2-8 реализованы.
- Тесты истории проходят; исправления — отдельный commit `... TK-0003: fix ...`.
- Native evidence записан отдельно или помечен NOT RUN; execution evidence в тикете, commit + push.

## Self-containment check

Все точки вставки в `SkiaWindow` указаны строками, контракт хоста задан, фикстуры названы.
