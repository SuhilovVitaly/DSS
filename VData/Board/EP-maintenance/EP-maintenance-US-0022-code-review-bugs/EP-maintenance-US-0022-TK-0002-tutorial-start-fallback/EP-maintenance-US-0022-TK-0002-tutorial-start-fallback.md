---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0002-tutorial-start-fallback
title: «Новая игра» без сохранений не должна застревать при сбое обучения
stage: draft
implementation_status: not_started
severity: major
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC2]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# «Новая игра» без сохранений не должна застревать при сбое обучения

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Если обучающий бой не стартовал, игрок без сохранений всё равно может начать игру: «Новая игра» открывает выбор сценария (или карту), причина сбоя пишется в InterfaceLog.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0027-US-0001-TK-0001 (AC2, AC6). Серьёзность: major. Уверенность: CONFIRMED.

**Дефект.** Без сохранений «Новая игра» всегда ведёт в `StartTutorialAsync`. При любой ошибке код пишет InterfaceLog и возвращает главное меню. Запасного пути нет, а сохранение без игры не создать. Комментарий `IGameSessionFactory.GetTutorialScenarioPath` («default null keeps factories without tutorial support unchanged») неверен: такие фабрики теряют «Новую игру».

**Сценарий.** Чистая установка, каталог тактического контента повреждён (`TacticalBattle.Catalog == null`), мир загружается. `StartTacticalBattle` возвращает `Unavailable`, `StartTutorialAsync` бросает «Tutorial battle was rejected», меню остаётся. Каждое следующее нажатие повторяет то же самое. То же при отсутствии папки Tutorial.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:868-872` | Выбор между `StartTutorialAsync` и `OpenScenarioSelect` по числу сохранений. |
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1081-1141` | `StartTutorialAsync`, catch на :1126 возвращает главное меню. |
| `src/DeepSpaceSaga.Client/IGameSessionFactory.cs:18-22` | Контракт `GetTutorialScenarioPath` и неверный комментарий. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. При `GetTutorialScenarioPath() == null` или отказе боя (`Unavailable`) переходить в `OpenScenarioSelect()`. Пользовательское решение «сбой → меню и запись в лог» сохранить для остальных ошибок, но не оставлять игрока без способа начать игру. Исправить комментарий контракта.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialFlowTests`: фабрика без обучения и бой `Unavailable` ведут в выбор сценария; существующий тест сбоя дополнительно проверяет строку InterfaceLog по AC6.

## Definition of Done

- AC2 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
