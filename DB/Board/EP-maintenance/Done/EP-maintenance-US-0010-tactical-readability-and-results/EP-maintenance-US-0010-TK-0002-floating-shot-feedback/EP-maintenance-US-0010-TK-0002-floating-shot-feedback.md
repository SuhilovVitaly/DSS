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

В начале результатов залпа над целью всплывает фактически нанесённый урон или локализованное «Промах». Текст поднимается и затухает. Поглощение отличается от обычного промаха.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Подсказки отображают только выстрелы из допущенной ленты, без новых расчётов боя.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` — таймер при пропуске анимации и повторе.
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/MapFireTests.cs`

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Изменить указанную точку отрисовки, ввода или генерации, затем проверить соответствующий критерий через существующую фикстуру.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

В начале результатов залпа над целью всплывает фактически нанесённый урон или локализованное «Промах». Текст поднимается и затухает. Поглощение отличается от обычного промаха.

## Tests

`MapFireTests|ResultsPhaseTests` в Client.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

20/20 MapFireTests и ResultsPhaseTests. Проверены подъём, истечение подсказок и сохранение существующей ленты результатов. Лог: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/tk2/test.log.
