---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0002-read-tactical-map
ticket: EP-0022-US-0002-TK-0001-style-battlefield
title: Оформить слои карты и прицелы
stage: implemented
layer: client
depends_on: [EP-0022-US-0001-TK-0001]
files_touched: 5
serves: [AC-01]
created: 2026-11-10T16:52:26Z
revision: 1
---

# Оформить слои карты и прицелы

[История](../EP-0022-US-0002-read-tactical-map.md) · [Эпик](../../Documentation.md)

## Why

На совпадающем кадре оформление различается, положения проекций и геометрия ввода равны. Рамка под тактическими слоями.

## Decisions

EP-0022 D-01/D-02 задают визуальное направление и геометрию. Запрос исполнения разрешает отдельный commit и push каждого тикета.

## Assumptions

Локально лицензированные срезы подключаются явным свойством сборки `XenonStarSource`. PNG из набора отсутствуют в Git. Общая поставка этих ресурсов остаётся G-01. Используется прежний загрузчик, сохранение выбора и шрифт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | Роли локального пакета | Палитра и изображения поля, рамки, прицелов. |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | Список срезов | Добавить фон, рамку и прицелы. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.cs` | Draw, MarkerPoint | Разместить декор под геометрией и скорректировать тона карты. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleMapRenderer.Fire.cs` | DrawPhaseOverlays | Только рисование прицела и контраст линий. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | Новый файл | Сравнить композицию и данные карты. |

## Public API after the change

Схема сохранения, команды и публичные API прежние.

## Implementation steps

1. Палитра и изображения поля, рамки, прицелов.
2. Добавить фон, рамку и прицелы.
3. Разместить декор под геометрией и скорректировать тона карты.
4. Только рисование прицела и контраст линий.
5. Сравнить композицию и данные карты.

## Out of scope

Другие экраны, правила боя, сохранение мира, замена шрифтов и лицензирование от имени владельца.

## Invariants

Макет, координаты ввода, расчёты и авторитет Engine остаются общими для всех скинов. Срезы применяются только к отрисовке.

## Tests

`dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --no-restore --results-directory <absolute-run-dir> --filter "Ep0022SkinScenesTests|BattleMapRenderTests|MapDesignTests|MapMovementTests|MapFireTests|OverlayCameraInteractionTests"` с ограничением дерева процессов 300000 ms. Сборка Client, scoped format и `git diff --check`.

## Definition of Done

AC-01 подтверждён целевыми проверками. Реальные результаты, ограничения и проверка публикации фиксируются в Board. Отдельный commit и немедленный push.

## Self-containment check

Production seam и проверка перечислены в Code context. Существующие команды и fixtures сохраняются.

## Execution

- Release Client — 25/25 целевых тестов. Парный рендер двух фаз сравнил карту, прямоугольники и текст одного кадра. В обоих кадрах видны спокойная рамка, разметка и различимые аномалии.
- Scoped formatter после приведения нового файла к CRLF и git diff --check прошли. Добавлены пять локальных срезов с исходными путями в csproj.
- Нативная камера и приёмка владельцем проверяются после функциональных тикетов.
