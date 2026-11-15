---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0002-comms-panel
ticket: EP-0028-US-0002-TK-0005-comms-journal
title: Журнал связи (H)
stage: implemented
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

## Результат

Новый `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialJournal.cs`: панель 460×300 (левый край 206, низ = верх развёрнутой панели связи − 6 = 608; положение фиксированное), заголовок `Tutorial.Journal.Title`, кнопка `Tutorial.Journal.Close` (`tutorial:journal-close`), реплики `Director.Heard` по порядку (текст из Locale в момент отрисовки, перенос `TutorialCommsPanel.Wrap(text, TextWidth = 422)`), пустое состояние `Tutorial.Journal.Empty`, прокрутка колесом (вверх — к ранним, шаг 3 строки), открывается на последних репликах; ключ `tutorial:journal`. `TutorialSession`: `Journal`, `HeardSteps`; `OnKeyDown` — H без модификаторов и не repeat переключает журнал (и при выключенных подсказках), Esc при открытом журнале закрывает его (true), иначе false; `OnMouseDown`/`OnMouseWheel` в границах открытого журнала захватываются; `Bounds` включает журнал. Сценарий без шагов журнала не имеет (H → false). `KeyboardEdgeTracker.StandardKeys` += `Key.E, Key.D, Key.H`.

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0002-tk0005-20261114T180230/`):

- Красный старт: тесты журнала не компилируются (`TutorialJournal`, `TutorialSession.Journal` нет) — `../us0002-tk0005-red/build-red.log`; после реализации журнала, до правки `StandardKeys`, `KeyboardEdgeTrackerTests` — 3 красных (H, E, D без привязки манёвра) из 28 — `../us0002-tk0005-red/tracker-red.log`.
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~KeyboardEdgeTrackerTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~KeysDuringBattleTests|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=journal.trx"` (`DSS_VALIDATION_ROOT` = папка прогона) — **207/207** — `journal.trx`, `journal.log`. Новые: `TutorialJournalTests.HTogglesJournalWithLinesHeardInOrder` (RU, EN), `EmptyJournalSaysSoAndAScriptWithoutStepsHasNoJournal`, `JournalCapturesOnlyItsOwnClicksAndWheel`, `LongJournalScrollsAndOpensAtTheLatestLine`, `JournalWorksWithHintsDisabled`; `TutorialJournalScreenTests.EscClosesOpenJournalOnlyAndOtherwiseKeepsExitConfirmation` (живой Tabletop: H, Tab при открытом журнале, Esc закрывает журнал без подтверждения выхода, следующий Esc — подтверждение, H во время подтверждения игнорируется); `KeyboardEdgeTrackerTests.H_is_reported_on_press_edge`, `Battle_evade_and_decoy_keys_are_polled_regardless_of_maneuver_bindings` (E, D).
- Регрессия клавиш: `--filter "FullyQualifiedName~Keyboard|FullyQualifiedName~Maneuver|FullyQualifiedName~Keys|FullyQualifiedName~ScreenEventTests|FullyQualifiedName~FirePanel|FullyQualifiedName~Evade|FullyQualifiedName~Decoy"` — **257/257** — `keys-regression.trx`, `keys-regression.log`.
- `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` — 0 предупреждений, 0 ошибок — `build.log`.
- Растровая проверка (временный тест, не закоммичен): `snap/journal-1280x720.png` (RU: журнал над панелью, подсветка `pilot:{own}`); `snap/capacity.txt` — ёмкость строки панели (13 pt, 352 px): RU ≈ 41–47 символов, EN ≈ 49–54 символа.
- ForbiddenCalls не затрагивается: правок в `UI/Screens/TacticalBattle/` нет (`KeyboardEdgeTracker.cs` вне `/TacticalBattle/`).

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие решения исполнителя: (1) журнал стоит на фиксированной высоте над развёрнутой панелью (не прыгает при сворачивании в значок); (2) открывается прокрученным к последним репликам; (3) сценарий без шагов (нет `battle-tutorial.json`) журнала не имеет — H возвращает false, как и прежде (`EmptySessionCapturesNoInput` без изменений); (4) строка `Tutorial.Panel.JournalHint` остаётся в значке связи; в развёрнутую панель не добавлена — `TutorialCommsPanel.cs` не входит в `files_touched` этого тикета; (5) проверка Esc с открытым журналом — в отдельном классе `TutorialJournalScreenTests` (коллекция `InterfaceLog`) того же файла.

2026-11-14 — Расширение объёма без остановки по указанию пользователя 2026-11-14 («сделай без остановок»), согласовано оркестратором по G-01: в `KeyboardEdgeTracker.StandardKeys` вместе с H добавлены `Key.E` и `Key.D` (экран боя обрабатывает E = уклонение и D = ловушка в фазе Fire, `TacticalBattleScreen.cs:231,236`, но E нативно не опрашивалась, а D приходила только через привязку `turnRight`). Проверено, что E/D/H не меняют поведение других экранов: в `src` их обрабатывает только `TacticalBattleScreen` (E/D в Fire) и сессия обучения (H); прочие экраны (`GameSessionScreen`, `Settings`, `Save` — ввод текста через KeyChar, `Trade`, `Dialogue`, меню и т. п.) эти клавиши не используют; D уже опрашивалась по умолчанию через `maneuver-input.json`. Тест — `KeyboardEdgeTrackerTests.Battle_evade_and_decoy_keys_are_polled_regardless_of_maneuver_bindings`. Файлы уже в `files_touched`. Gap G-01 в `Documentation.md` эпика не обновлён — файл вне `files_touched`; синхронизация — US-0006-TK-0003 (нативное подтверждение E/D — US-0006-TK-0002).
