---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen
title: Обучение на узком экране: журнал залезает на правую колонку, реплики обрезаются
stage: approved
approval_kind: workflow
implementation_status: implemented
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC22]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Обучение на узком экране: журнал залезает на правую колонку, реплики обрезаются

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

На поддерживаемых соотношениях вплоть до 5:4 журнал связи не перекрывает правую колонку боя, а каждая реплика помещается в панель связи без многоточия на обоих языках.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0028-US-0002-TK-0005, EP-0028-US-0002-TK-0002. Серьёзность: minor. Уверенность: CONFIRMED (журнал), PLAUSIBLE (реплики).

**Дефект.** Панель связи ужимается по `designWidth - 246 - Left`, а журнал всегда `206 + 460 = 666` по x. Правая колонка начинается с `Wd - 240`. При `Wd < 912` и панель уже 460: текстовая колонка меньше 352, а `TutorialContentTests.EveryLineFitsTwoPanelLinesInBothLanguages` проверяет только ширину 352.

**Сценарий.** Окно 1280×1024 → `Wd = 900`, правая колонка с x 660. Открытый журнал (H) занимает x 660…666 при y 308…608, и `TutorialSession.OnMouseDown` съедает клики по журналу боя и краю панели фаз. Реплики, едва помещавшиеся в две строки по 352, переносятся на третью и обрезаются многоточием (нарушение D-06).

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialJournal.cs:16, 25` | Фиксированная ширина журнала. |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialCommsPanel.cs:70-86` | Ширина панели и текста. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs:121` | Начало правой колонки. |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs` | Добавлено при исполнении: передаёт `designWidth` в `Journal.Draw`. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | Добавлено при исполнении: сокращены `Tutorial.Line.Intro` и `Tutorial.Line.OverVictory`. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Добавлено при исполнении: сокращена `Tutorial.Line.OverVictory`. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Передавать `designWidth` в `Journal.Draw` и `Bounds`, ширину журнала считать как у панели; перенос строк по фактической ширине. Тест содержания проверять на минимально поддерживаемой ширине (Wd 900 → 340) или не давать панели быть уже 460.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialScreenIntegrationTests` на 1280×1024: журнал не пересекает `phase` и `battle-log`. `TutorialContentTests` на минимальной ширине.

## Definition of Done

- AC22 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Реализован 2026-11-16 на ветке `maintenance/US-0022-code-review-bugs`. Дефект на текущем main подтверждён: журнал связи всегда рисовался шириной 460 (x 206…666) и при 1280×1024 (Wd 900) заходил на правую колонку (с x 660); при ширине текста 340 три реплики требовали трёх строк и получали многоточие.

- `TutorialCommsPanel.WidthFor`/`TextWidthFor`: прежнее правило ширины панели вынесено в общие помощники (на полной ширине числа те же).
- `TutorialJournal`: ширина как у панели связи для текущей `designWidth`, перенос по фактической ширине, кэш строк пересобирается при её смене. `TutorialSession` передаёт `designWidth`.
- Реплики (смысл сохранён, слова, которые проверяет `Ep0029Us0011Tk0003Tests`, на месте):
  - EN `Tutorial.Line.Intro`: «…Click a pilot card on the left.» → «…Select a pilot on the left.»
  - EN `Tutorial.Line.OverVictory`: «Rearm from the hold — the ship screen holds 42 torpedoes.» → «Rearm on the ship screen — the hold has 42 torpedoes.»
  - RU `Tutorial.Line.OverVictory`: «…на экране корабля. Жмите «На карту».» → «…на экране корабля, затем «На карту».»

Тесты: `TutorialScreenIntegrationTests.OpenJournalOnNarrowWindowStaysClearOfTheRightColumn`, `TutorialJournalTests.NarrowWindowShrinksTheJournalLikeThePanelAndRewrapsItsRows`; `TutorialContentTests.EveryLineFitsTwoPanelLinesInBothLanguages` проверяет ширину текста 340 (Wd 900).

Проверки: `dotnet build tests/DeepSpaceSaga.Client.Tests` — 0 предупреждений, 0 ошибок. Тесты запускаются один раз после завершения истории; результат записан в документе истории.
