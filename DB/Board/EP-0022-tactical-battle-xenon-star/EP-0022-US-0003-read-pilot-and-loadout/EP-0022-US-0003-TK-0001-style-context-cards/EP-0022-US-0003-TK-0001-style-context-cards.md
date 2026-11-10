---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0003-read-pilot-and-loadout
ticket: EP-0022-US-0003-TK-0001-style-context-cards
title: Оформить пилотов и контекст
stage: implemented
layer: client
depends_on: [EP-0022-US-0001-TK-0001]
files_touched: 6
serves: [AC-01]
created: 2026-11-10T16:52:26Z
revision: 1
---

# Оформить пилотов и контекст

[История](../EP-0022-US-0003-read-pilot-and-loadout.md) · [Эпик](../../Documentation.md)

## Why

Числовые значения, подписи, портреты и размеры карточек совпадают. Сегменты обрезаются по фактической доле.

## Decisions

EP-0022 D-01/D-02 задают визуальное направление и геометрию. Запрос исполнения разрешает отдельный commit и push каждого тикета.

## Assumptions

Локально лицензированные срезы подключаются явным свойством сборки `XenonStarSource`. PNG из набора отсутствуют в Git. Общая поставка этих ресурсов остаётся G-01. Используется прежний загрузчик, сохранение выбора и шрифт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | Роли локального пакета | Добавить карточки и шкалы. |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | Список срезов | Добавить графику статуса. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PilotCardColumn.cs` | Draw | Отрисовать выбранное состояние, сегменты и портрет без изменения hit-test. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BuildCard.cs` | Draw | Оформить контекст сборки. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | Bar | Сегментация в границах заполнения. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | Парный рендер одинаковых кадров | Добавить повреждённую сборку и проверку заполнения шкалы. |

Scope расширен на тест, который проверяет новую графическую маску в реальном пакете и паритет показателей при повреждении.

## Public API after the change

Схема сохранения, команды и публичные API прежние.

## Implementation steps

1. Добавить карточки и шкалы.
2. Добавить графику статуса.
3. Отрисовать выбранное состояние, сегменты и портрет без изменения hit-test.
4. Оформить контекст сборки.
5. Сегментация в границах заполнения.

## Out of scope

Другие экраны, правила боя, сохранение мира, замена шрифтов и лицензирование от имени владельца.

## Invariants

Макет, координаты ввода, расчёты и авторитет Engine остаются общими для всех скинов. Срезы применяются только к отрисовке.

## Tests

`dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --no-restore --results-directory <absolute-run-dir> --filter "Ep0022SkinScenesTests|PilotCardColumnTests|BuildCardRenderTests|OverlayLocalizationTests|HoverCardTests"` с ограничением дерева процессов 300000 ms. Сборка Client, scoped format и `git diff --check`.

## Definition of Done

AC-01 подтверждён целевыми проверками. Реальные результаты, ограничения и проверка публикации фиксируются в Board. Отдельный commit и немедленный push.

## Self-containment check

Production seam и проверка перечислены в Code context. Существующие команды и fixtures сохраняются.

## Execution

- Release Client — 19/19 целевых тестов. Парный кадр включает повреждённую сборку и совпадающие подписи и зоны. Растровая проверка фиксирует нулевую, частичную и полную доли шкалы.
- Scoped форматирование и git diff --check прошли. Статус и приоритет контекста используют прежнюю проекцию. Добавлены два локальных среза из Cockpit HUD.
