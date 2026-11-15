---
epic: EP-maintenance
story: EP-maintenance-US-0010-tactical-readability-and-results
ticket: EP-maintenance-US-0010-TK-0004-wreck-names
title: Имена рядом с крестами сбитых дронов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
depends_on: []
files_touched: 3
serves: [AC4]
created: "2026-11-10T14:03:31.830Z"
---

# Имена рядом с крестами сбитых дронов

## Why

На месте сбитого дрона или Тага остаются крест и серая подпись с именем. Подписи избегают перекрытия соседних маркеров и не раскрывают будущие уничтожения.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Подпись использует то же имя, что и живой юнит.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs`
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs`

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Изменить указанную точку отрисовки, ввода или генерации, затем проверить соответствующий критерий через существующую фикстуру.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

На месте сбитого дрона или Тага остаются крест и серая подпись с именем. Подписи избегают перекрытия соседних маркеров и не раскрывают будущие уничтожения.

## Tests

`BattleMapRenderTests` в Client.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

6/6 BattleMapRenderTests. Растровая проверка подтверждает появление серого имени и креста только после летального попадания. Лог: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/tk4/test.log.
