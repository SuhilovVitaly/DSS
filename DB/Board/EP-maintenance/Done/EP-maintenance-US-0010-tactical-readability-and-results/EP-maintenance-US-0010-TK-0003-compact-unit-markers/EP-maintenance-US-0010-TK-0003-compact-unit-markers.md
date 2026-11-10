---
epic: EP-maintenance
story: EP-maintenance-US-0010-tactical-readability-and-results
ticket: EP-maintenance-US-0010-TK-0003-compact-unit-markers
title: Компактные маркеры дронов и Тагов
stage: approved
approval_kind: workflow
implementation_status: implemented
layer: client
depends_on: []
files_touched: 3
serves: [AC3]
created: "2026-11-10T14:03:31.830Z"
---

# Компактные маркеры дронов и Тагов

## Why

Уменьшить силуэты, подписи и обе шкалы на карте примерно на 20%. Размер карточек и область клика сохраняются.

## Decisions

Отдельное исправление по запросу пользователя с коммитом и отправкой в текущую ветку.

## Assumptions

Подписи носителей остаются прежнего размера.

## Code context

- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs`
- `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0019Acceptance.cs`

## Public API after the change

Форматы команд и сохранений прежние.

## Implementation steps

Изменить указанную точку отрисовки, ввода или генерации, затем проверить соответствующий критерий через существующую фикстуру.

## Out of scope

Изменения боевой формулы, камеры и правил сохранений.

## Invariants

Уменьшить силуэты, подписи и обе шкалы на карте примерно на 20%. Размер карточек и область клика сохраняются.

## Tests

`BattleMapRenderTests|Ep0019ViewTests` в Client.Tests. Результаты под абсолютным каталогом `D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/<run>`. Лимит процесса 300000 мс.

## Definition of Done

Критерий подтверждён целевыми проверками, `git diff --check` проходит, статус и результаты записаны перед публикацией.

## Self-containment check

В контексте указаны файлы реализации и тестов.

## Статус

13/13 BattleMapRenderTests и Scene_layout_matches_design_spec. Проверены компактные пиксельные границы силуэтов, ориентация, подписи и отсутствие наложения. Лог: D:/DeepSpaceSaga/artifacts/validation/maintenance-us-0010-0011/20261110140406919/tk3-final/test.log.
