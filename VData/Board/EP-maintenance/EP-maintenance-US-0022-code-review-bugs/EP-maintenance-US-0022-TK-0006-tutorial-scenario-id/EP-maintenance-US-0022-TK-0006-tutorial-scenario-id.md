---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0006-tutorial-scenario-id
title: Сценарий обучающего боя задан неявно
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC6]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Сценарий обучающего боя задан неявно

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Обучающий бой всегда стартует со сценарием `initial` при сиде `TutorialBattleSeed` независимо от порядка сценариев в каталоге.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0027-US-0001-TK-0001. Серьёзность: minor. Уверенность: CONFIRMED (код), последствия латентные.

**Дефект.** Решение пользователя фиксирует сценарий `initial`, но запрос передаёт `ScenarioId = null`, и движок берёт `catalog.Scenarios.Entries[0].Id`.

**Сценарий.** Сейчас в `scenarios.json` только `initial`. Добавление или перестановка боевого сценария молча меняет обучающий бой и ломает закреплённую выигрываемость при сиде 1, подсказки и якоря.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1107` | Запрос старта без ScenarioId. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:38` | Подстановка первого сценария каталога. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. Передавать `ScenarioId: "initial"` константой рядом с `TutorialBattleSeed`.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialFlowTests.NewGameWithoutSavesStartsTutorialBattleAndNeverShowsTheMap` проверяет `battle.Flow.State.ScenarioId == "initial"`.

## Definition of Done

- AC6 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
