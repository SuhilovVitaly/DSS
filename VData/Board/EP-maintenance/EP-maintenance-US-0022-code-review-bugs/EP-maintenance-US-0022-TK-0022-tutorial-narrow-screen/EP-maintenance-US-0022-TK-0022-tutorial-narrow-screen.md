---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0022-tutorial-narrow-screen
title: Обучение на узком экране: журнал залезает на правую колонку, реплики обрезаются
stage: draft
implementation_status: not_started
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

Не начат.
