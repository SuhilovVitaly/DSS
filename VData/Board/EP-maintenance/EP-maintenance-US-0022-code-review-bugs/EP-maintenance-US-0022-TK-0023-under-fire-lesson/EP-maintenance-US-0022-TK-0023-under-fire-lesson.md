---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0023-under-fire-lesson
title: Урок «под огнём» закрывается защитой чужого дрона
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC23]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Урок «под огнём» закрывается защитой чужого дрона

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Шаг `ctx.under-fire` засчитывается, только когда уклонение (E) или ловушку (D) получил тот дрон, на которого указывает реплика.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0028-US-0003-TK-0004, EP-0028-US-0001-TK-0002. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** `EvadeOrDecoy => observation.FirePlan?.Rows.Any(r => r.Evade || r.Decoy)` не учитывает `TutorialLine.SignalUnitId`, хотя реплика подсвечивает конкретного дрона (`anchor: pilot:{signal}`).

**Сценарий.** Фаза огня, ход ≥ 2, под огнём Вера. Игрок ставит ловушку на Олега, Вера без защиты. Шаг записан как `completed` в директоре и метриках.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialDirector.cs:159` | Предикат `EvadeOrDecoy`. |
| `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` | Шаг `ctx.under-fire`. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Для `EvadeOrDecoy` при заданном `SignalUnitId` проверять строку с `r.Drone.Id == line.SignalUnitId`.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialDirectorTests`: ловушка на другом дроне не закрывает шаг, на указанном — закрывает.

## Definition of Done

- AC23 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
