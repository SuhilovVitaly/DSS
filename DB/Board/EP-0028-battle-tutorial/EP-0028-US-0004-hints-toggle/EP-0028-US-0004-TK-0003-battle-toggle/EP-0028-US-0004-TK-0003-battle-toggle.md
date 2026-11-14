---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0004-hints-toggle
ticket: EP-0028-US-0004-TK-0003-battle-toggle
title: Переключатель подсказок в бою
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0004-TK-0001-hints-preference, EP-0028-US-0002-TK-0002-comms-panel]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsToggleTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs
serves: [US-0004/AC-03]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Переключатель подсказок в бою

[История](../EP-0028-US-0004-hints-toggle.md) · [Эпик](../../Documentation.md)

## Цель

Связать кнопку панели «Подсказки: выкл»/значок «Подсказки: вкл» (US-0002-TK-0002, пока только в памяти) с настройкой: бой обучения стартует с сохранённым значением, а переключение в бою сохраняется в `Settings.json`.

## Decisions

D-04 — кнопка в бою и хранение в `gameSettings`; A-03 — значок «Подсказки: вкл» возвращает подсказки в том же бою ([эпик](../../Documentation.md#допущения)).

## Assumptions

- Ошибка записи не отменяет переключение в текущем бою (игрок хочет тишины сейчас); она пишется в InterfaceLog через `LogTutorialWarning`.
- При выключенных подсказках в начале боя панель сразу показывает значок «Подсказки: вкл»; директор продолжает учитывать выполненные шаги (US-0001-TK-0002, `Enabled = false`), журнал доступен (A-04).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | После US-0002: клики `hints-off`/`hints-on` меняют только `Director.Enabled` | Начальное `HintsEnabled`; колбэк `PersistHints` вызывается при каждом переключении |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs` | Значок и кнопка из US-0002-TK-0002 | Только при необходимости: состояние значка сразу после старта с выключенными подсказками |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Сессия создаётся в `CreateTacticalBattleView` (US-0001-TK-0004, `:1208-1209`); путь профиля — `SettingsFilePath` (`:111`) | `HintsEnabled = TutorialHintsPreference.Read(SettingsFilePath, LogTutorialWarning)`, `PersistHints = v => TutorialHintsPreference.TryWrite(SettingsFilePath, v, LogTutorialWarning)` |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialHintsToggleTests.cs` | Нет файла | Новые тесты |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs` | Фикстура пишет профиль `{"gameSettings":{"tacticalBattleSkinId":"classic"}}` (`:288-289`) | Параметр фикстуры для `tutorialHints`; сквозной тест «выкл в бою → сохранено → следующий старт выключен» |

## Public API after the change

```csharp
// TutorialSession
internal bool HintsEnabled { get; init; } = true;     // начальное значение → Director.Enabled
internal Func<bool, bool>? PersistHints { get; init; }
```

## Implementation steps

1. Сессия применяет `HintsEnabled` к директору при создании; на `hints-off`/`hints-on` — меняет состояние и вызывает `PersistHints`.
2. `SkiaWindow` передаёт значение и колбэк.
3. Тесты: юнит (сессия) и сквозной через `TutorialFlowTests`.

## Acceptance criteria

1. При `tutorialHints: false` бой обучения стартует без реплик, со значком «Подсказки: вкл».
2. Клик «Подсказки: выкл» записывает `false`, «Подсказки: вкл» — `true`; остальные ключи профиля сохраняются.
3. Ошибка записи не ломает бой и логируется.

## Verification signal

Первым красным — `TutorialHintsToggleTests.ToggleInBattleCallsPersistWithNewValue`; затем `StartsSilentWhenPreferenceIsOff`, `PersistFailureKeepsBattleRunning`, `TutorialFlowTests.HintsDisabledInBattleStayDisabledForNextTutorialStart`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0004-tk0003-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=toggle.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Строка Настроек (TK-0002); метрики «hints-disabled» (US-0006-TK-0001); правки `UI/Screens/TacticalBattle/`.

## Invariants

- Кнопки панели не перехватывают ввод вне своих границ (D-02).
- EP-0027-тесты `TutorialFlowTests` остаются зелёными без ослабления.

## Known fallout

`TutorialFlowTests` — расширение фикстуры (параметр по умолчанию сохраняет прежнее поведение).

## Definition of Done

- Шаги выполнены; named tests зелёные; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Точки подключения, API и поведение при ошибке заданы.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
