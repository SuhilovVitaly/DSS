---
epic: EP-maintenance
story: EP-maintenance-US-0022-code-review-bugs
ticket: EP-maintenance-US-0022-TK-0024-tutorial-metrics-io
title: Журнал обучения пишется синхронно в кадре и повторяет сбойную запись
stage: draft
implementation_status: not_started
severity: minor
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
serves: [AC24]
created: "2026-11-15T12:00:00Z"
revision: 1
---

# Журнал обучения пишется синхронно в кадре и повторяет сбойную запись

[История](../EP-maintenance-US-0022-code-review-bugs.md)

## Ожидаемый результат

Запись метрик обучения не выполняет файловый ввод-вывод в кадре отрисовки и после первой ошибки записи больше не пытается писать.

## Why

Найдено код-ревью 2026-11-15 на main `113b8506`. Источник: EP-0028-US-0006-TK-0001. Серьёзность: minor. Уверенность: CONFIRMED.

**Дефект.** Каждое событие шага внутри `RenderFrame` делает `Directory.CreateDirectory`, `File.WriteAllText` всего файла и `File.Move`. При недоступном каталоге исключение бросается и ловится на каждом событии; предупреждение пишется один раз, а ввод-вывод не отключается.

**Сценарий.** Недоступный `LocalAppData/DeepSpaceSaga/BattleMetrics` (только чтение, антивирус, сетевой профиль): на каждом показе или закрытии реплики кадр подвисает на время ввода-вывода и исключения; на медленном диске микрофризы и без ошибки.

## Code context

Строки указаны по `113b8506`.

| Файл | Состояние |
|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialSession.cs:99-100` | `Observe` → `RecordEvents`. |
| `src/DeepSpaceSaga.Client/UI/Screens/Tutorial/TutorialMetrics.cs:39-71` | `Add` с записью файла. |

## Implementation steps

1. Воспроизвести сценарий тестом из раздела Tests до исправления.
2. После первой ошибки отключать запись; сбрасывать файл при смене фазы или в фоне, а не в кадре.
3. Прогнать тест и соседние тесты затронутых классов.

## Tests

`TutorialMetricsTests`: после первой ошибки записи новые события не пишутся на диск.

## Definition of Done

- AC24 выполнен, тест из раздела Tests проходит и падает без исправления.
- В разделе «Статус» записаны команды и результаты проверок.

## Статус

Не начат.
