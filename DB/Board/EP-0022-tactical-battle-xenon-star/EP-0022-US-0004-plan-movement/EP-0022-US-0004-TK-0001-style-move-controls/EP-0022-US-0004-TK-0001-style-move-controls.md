---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0004-plan-movement
ticket: EP-0022-US-0004-TK-0001-style-move-controls
title: Оформить панель движения
stage: draft
layer: client
depends_on: [EP-0022-US-0001-TK-0001, EP-0022-US-0002-TK-0001]
files_touched: 3
serves: [AC-01]
created: 2026-11-10T16:52:26Z
revision: 1
---

# Оформить панель движения

[История](../EP-0022-US-0004-plan-movement.md) · [Эпик](../../Documentation.md)

## Why

Видны скорость, поворот и предупреждение пыли. Контроль движения и команда совпадают с Classic.

## Decisions

EP-0022 D-01/D-02 задают визуальное направление и геометрию. Запрос исполнения разрешает отдельный commit и push каждого тикета.

## Assumptions

Локально лицензированные срезы подключаются явным свойством сборки `XenonStarSource`. PNG из набора отсутствуют в Git. Общая поставка этих ресурсов остаётся G-01. Используется прежний загрузчик, сохранение выбора и шрифт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | Роли локального пакета | Добавить состояния движения и ползунки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | Slider | Настроить визуал трека и ручки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Move.cs` | DrawMove | Роли статуса и предупреждения движения. |

## Public API after the change

Схема сохранения, команды и публичные API прежние.

## Implementation steps

1. Добавить состояния движения и ползунки.
2. Настроить визуал трека и ручки.
3. Роли статуса и предупреждения движения.

## Out of scope

Другие экраны, правила боя, сохранение мира, замена шрифтов и лицензирование от имени владельца.

## Invariants

Макет, координаты ввода, расчёты и авторитет Engine остаются общими для всех скинов. Срезы применяются только к отрисовке.

## Tests

`dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --no-restore --results-directory <absolute-run-dir> --filter "Ep0022SkinScenesTests|PhaseMoveInputTests|MovementOrdersTests|MapMovementTests"` с ограничением дерева процессов 300000 ms. Сборка Client, scoped format и `git diff --check`.

## Definition of Done

AC-01 подтверждён целевыми проверками. Реальные результаты, ограничения и проверка публикации фиксируются в Board. Отдельный commit и немедленный push.

## Self-containment check

Production seam и проверка перечислены в Code context. Существующие команды и fixtures сохраняются.
