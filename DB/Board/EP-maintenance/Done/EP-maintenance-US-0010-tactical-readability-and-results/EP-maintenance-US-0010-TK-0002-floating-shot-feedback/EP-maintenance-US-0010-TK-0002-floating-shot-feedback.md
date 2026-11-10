---
epic: EP-maintenance
story: EP-maintenance-US-0010-tactical-readability-and-results
ticket: EP-maintenance-US-0010-TK-0002-floating-shot-feedback
title: Всплывающий результат выстрела
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
depends_on: []
files_touched: 4
serves: [AC2]
created: "2026-11-10T14:03:31.830Z"
---

# Всплывающий результат выстрела

## Why

Проверка уточнения: 34 целевых теста клиента прошли. Лог: D:/DeepSpaceSaga/artifacts/validation/tactical-map-polish/20261110144815363/client-recheck/test.log.

Уточнение от 2026-11-10. В итогах залпа временные выноски брони, ловушки и поглощения исчезают через 1,4 с, в том числе после пропуска. Таймер всплывающего урона по попаданию сохраняется.
Обновляются соответствующий рендер, ввод или генератор, связанные проверки и затронутые растровые эталоны.

Над целью всплывает фактически нанесённый урон в кадре попадания торпеды. Промах и поглощение используют момент завершения соответствующего полёта. Текст поднимается и затухает.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Подсказки отображают только выстрелы из допущенной ленты, без новых расчётов боя.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` — убрать отдельный таймер экрана результатов.
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapFireTests.cs`

Разрешено обновить затронутые PNG-эталоны полёта и результатов в `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots`.

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Отсчитывать время подсказки от прибытия каждой торпеды. Скрывать подсказки до прибытия и при пропуске анимации. Проверить реальный залп с фиксированными часами.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

Подсказка появляется одновременно с попаданием, в том числе если другие торпеды ещё в полёте. Переход к итогам сохраняет ход уже начатой подсказки.

## Tests

`MapFireTests|ResultsPhaseTests` в Client.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

20/20 MapFireTests и ResultsPhaseTests. Проверены подъём, истечение подсказок и сохранение существующей ленты результатов. Лог: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/tk2/test.log.

Уточнение от 2026-11-10: число появляется в кадре попадания каждой торпеды и продолжает подниматься без повторного запуска при переходе к итогам. При пропуске полёта новые всплывающие числа не создаются. Проверены кадры за 1 мс до попадания, в момент попадания и после затухания. Все 420 тестов тактического клиента, scoped format и `git diff --check` прошли. Лог: D:/DeepSpaceSaga/artifacts/validation/torpedo-impact-feedback/20261110142724628/verified/test.log.
