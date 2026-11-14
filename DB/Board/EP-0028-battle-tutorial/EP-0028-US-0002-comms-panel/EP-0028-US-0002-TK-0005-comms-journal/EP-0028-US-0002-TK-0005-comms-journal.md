---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0002-comms-panel
ticket: EP-0028-US-0002-TK-0005-comms-journal
title: Журнал связи (H)
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0002-TK-0002-comms-panel]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialJournal.cs
  - src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs
  - src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialJournalTests.cs
  - tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs
serves: [US-0002/AC-05]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Журнал связи (H)

[История](../EP-0028-US-0002-comms-panel.md) · [Эпик](../../Documentation.md)

## Цель

Справка по требованию: по H открывается журнал связи со всеми репликами офицера, показанными в этом бою, по порядку. Пропущенную или свёрнутую подсказку можно перечитать, не прерывая бой.

## Decisions

D-02, D-06 — [эпик](../../Documentation.md#решения-пользователя-обязательные). A-04: журнал доступен и при выключенных подсказках; Esc закрывает открытый журнал первым.

## Assumptions

- Журнал хранит ID шагов (`Director.Heard`), текст берётся из Locale в момент отрисовки — всегда на текущем языке.
- Журнал — панель 460×300 над панелью связи (низ = верх панели − 6, левый край 206); прокрутка колесом; кнопка «Закрыть». Захватывает клики и колесо только в своих границах.
- H не используется ни одной привязкой по умолчанию (`Data/UI/maneuver-input.json`, `Data/UI/tactical-battle-input.json`, `git grep "Key.H"` в `src` пуст на базе).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialJournal.cs` | Нет файла. Перенос — `TutorialCommsPanel.Wrap` (TK-0002) | Новая панель журнала |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | После TK-0004: панель, подсветка, значок | `OnKeyDown`: H без модификаторов и не repeat → переключить журнал; Esc при открытом журнале → закрыть (true), иначе false. `OnMouseWheel`/`OnMouseDown` в границах журнала; `Bounds` включает журнал |
| `src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs` | Нативный опрос только `StandardKeys` (`:9-16`) и клавиш манёвра (`:26-35`); H нет (G-02) | Добавить `Key.H` в `StandardKeys` |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialJournalTests.cs` | Нет файла | Новые тесты |
| `tests/DeepSpaceSaga.Client.Tests/KeyboardEdgeTrackerTests.cs` | Тесты фронтов клавиш (`:8-30`) | Тест: H сообщается на фронте нажатия |

Хук `OnKeyDown` сессии вызывается экраном после `BlockWorldKey` и только без `ConfirmExit` (US-0001-TK-0004); Esc при закрытом журнале возвращает false и сохраняет прежний маршрут Esc (`TacticalBattleScreen.cs:190,238-239`).

## Public API after the change

```csharp
internal sealed class TutorialJournal
{
    internal bool IsOpen { get; }
    internal SKRect Bounds { get; }
    internal void Toggle(); internal void Close();
    internal void Draw(SKCanvas c, BattleLayout layout, IReadOnlyList<TutorialStep> heard);   // ключи tutorial:journal, tutorial:journal-close
    internal bool OnMouseDown(SKPoint p); internal bool OnMouseWheel(SKPoint p, float delta);
}
```

## Implementation steps

1. `TutorialJournal`: заголовок `Tutorial.Journal.Title`, строки реплик (`Wrap`), пустое состояние `Tutorial.Journal.Empty`, кнопка `Tutorial.Journal.Close`, прокрутка.
2. Подключение в сессии (см. Code context); панель связи в развёрнутом виде показывает подсказку `Tutorial.Panel.JournalHint` (уже есть в значке).
3. `KeyboardEdgeTracker.StandardKeys` += `Key.H`.

## Acceptance criteria

1. H открывает и закрывает журнал в любой фазе, кроме подтверждения выхода; Enter/Tab/стрелки при открытом журнале работают как раньше.
2. Журнал показывает реплики этого боя по порядку на текущем языке; при выключенных подсказках тоже открывается.
3. Esc закрывает открытый журнал и не открывает подтверждение выхода; при закрытом журнале Esc ведёт себя как в базе.
4. Нативный опрос сообщает H.

## Verification signal

Первым красным — `TutorialJournalTests.HTogglesJournalWithLinesHeardInOrder`; затем `EscClosesOpenJournalOnlyAndOtherwiseKeepsExitConfirmation`, `JournalCapturesOnlyItsOwnClicksAndWheel`, `JournalWorksWithHintsDisabled`, `KeyboardEdgeTrackerTests.H_is_reported_on_press_edge`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0002-tk0005-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~KeyboardEdgeTrackerTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~KeysDuringBattleTests|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=journal.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Исправление нативной E (G-01); журнал за пределами текущего боя; правки `UI/Screens/TacticalBattle/`.

## Invariants

- Код журнала в `UI/Screens/Tutorial/`; `KeyboardEdgeTracker.cs` вне `/TacticalBattle/`.
- Esc-маршрут EP-0027 (`TutorialFlowTests.TutorialEscapeConfirmationLeadsToMainMenu`) не меняется.

## Known fallout

`KeyboardRoutingTests`, `KeysDuringBattleTests`, `TutorialFlowTests` — регрессионный запуск без правок.

## Definition of Done

- Шаги выполнены; named tests зелёные; сборка Client без новых предупреждений.
- Отдельный коммит и `git push`.

## Self-containment check

Маршрут клавиш, геометрия, ключи текста и место добавления H указаны с `path:line`.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
