---
epic: EP-0022-tactical-battle-xenon-star
story: EP-0022-US-0001-select-xenon-skin
ticket: EP-0022-US-0001-TK-0001-package-hud
title: Подключить пакет Xenon Star
stage: implemented
layer: client
depends_on: []
files_touched: 4
serves: [AC-01]
created: 2026-11-10T16:52:26Z
revision: 1
---

# Подключить пакет Xenon Star

[История](../EP-0022-US-0001-select-xenon-skin.md) · [Эпик](../../Documentation.md)

## Why

Настройки видят Xenon Star при сборке с указанным набором. Пакет работает из каталога сборки и исчезает из доступных при отсутствии или порче ресурсов. Стандартная сборка без набора сохраняет Classic и Contrast.

## Decisions

EP-0022 D-01/D-02 задают визуальное направление и геометрию. Запрос исполнения разрешает отдельный commit и push каждого тикета.

## Assumptions

Локально лицензированные срезы подключаются явным свойством сборки `XenonStarSource`. PNG из набора отсутствуют в Git. Общая поставка этих ресурсов остаётся G-01. Используется прежний загрузчик, сохранение выбора и шрифт.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | Правило копирования Contrast | Подключить манифест и только перечисленные PNG в локальной сборке. |
| `src/DeepSpaceSaga.Client/Skins/xenon-star/skin.json` | Новый файл | Добавить ID xenon-star и роли общих панелей, заголовков и кнопок. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleDesign.cs` | Round и Button | Наложить текстуру внутри рамки, сохранить контур и расположение текста. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ShippedSkinTests.cs` | Тест поставки Contrast | Проверить фактическую загрузку и fallback Xenon Star. |

## Public API after the change

Опциональное свойство MSBuild `XenonStarSource`, ID `xenon-star`, манифест v1.

## Implementation steps

1. Подключить манифест и только перечисленные PNG в локальной сборке.
2. Добавить ID xenon-star и роли общих панелей, заголовков и кнопок.
3. Наложить текстуру внутри рамки, сохранить контур и расположение текста.
4. Проверить фактическую загрузку и fallback Xenon Star.

## Out of scope

Другие экраны, правила боя, сохранение мира, замена шрифтов и лицензирование от имени владельца.

## Invariants

Макет, координаты ввода, расчёты и авторитет Engine остаются общими для всех скинов. Срезы применяются только к отрисовке.

## Tests

`dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --no-restore --results-directory <absolute-run-dir> --filter "ShippedSkinTests|SkinBattleIntegrationTests|BattleDesignTests"` с ограничением дерева процессов 300000 ms. Сборка Client, scoped format и `git diff --check`.

## Definition of Done

AC-01 подтверждён целевыми проверками. Реальные результаты, ограничения и проверка публикации фиксируются в Board. Отдельный commit и немедленный push.

## Self-containment check

Production seam и проверка перечислены в Code context. Существующие команды и fixtures сохраняются.

## Execution

- Release с `-p:XenonStarSource="D:/DeepSpaceSaga/DSS-Images/Xenon Star"`. `ShippedSkinTests`, `SkinBattleIntegrationTests` и `BattleDesignTests` — 9/9. Реальный каталог загружает пакет, а повреждённый PNG приводит к Classic.
- `dotnet publish` с тем же свойством прошёл. SHA-256 манифеста и трёх PNG совпали в build и publish. Исходный каталог требуется сборщику при включении пакета, клиент читает только установленный пакет.
- Scoped `dotnet format whitespace --verify-no-changes` и `git diff --check` прошли. Formatter сообщил предупреждение загрузки workspace без ошибок.
- В Git включены манифест и отображение. Публичное распространение сборки с графикой Kodiak Graphics остаётся G-01.
