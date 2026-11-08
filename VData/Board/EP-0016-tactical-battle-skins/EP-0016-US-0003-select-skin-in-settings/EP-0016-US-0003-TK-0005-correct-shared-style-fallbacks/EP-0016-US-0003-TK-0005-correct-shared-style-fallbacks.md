---
epic: EP-0016-tactical-battle-skins
story: EP-0016-US-0003-select-skin-in-settings
ticket: EP-0016-US-0003-TK-0005-correct-shared-style-fallbacks
title: Исправить наследование общего стиля и выбор шрифта многострочного текста
stage: done
layer: client
depends_on: [EP-0016-US-0003-TK-0004-wire-settings-to-battle]
serves: [US2-AC1, US2-AC2, US2-AC4, US3-AC4]
files_touched: 11
---

# Исправить наследование общего стиля и выбор шрифта

Финальная самопроверка [общих виджетов](../../EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0001-theme-shared-primitives/EP-0016-US-0002-TK-0001-theme-shared-primitives.md) и [вкладок](../../EP-0016-US-0002-migrate-battle-rendering/EP-0016-US-0002-TK-0005-theme-information-tabs/EP-0016-US-0002-TK-0005-theme-information-tabs.md) выявила два риска для авторских пакетов. Переносы строк могут быть приняты за отсутствующие глифы, а явный адрес рамки может отрезать fallback на общий стиль.

- Воспроизвести на реальном TTF/OTF из каталога и на растровой рамке.
- В `TacticalBattleSkinRenderScope.cs` отличить разделители строк от печатаемых глифов, сохранить штатный fallback для недоступных символов и сократить обход пустого Classic.
- В `TabStrip.cs` (`ControlVisuals`) сохранить общий стиль рамки и подписи при неполном локальном переопределении.
- Полная регрессия обнаружила нарушения существующего guard Motion в коде оформления, включая унаследованные выражения в изменённом рендере карты. Дополнительно разрешены `TacticalBattleMapRenderer.cs`, `TacticalBattleScreen.cs`, `TacticalBattleMovePanel.cs`, `TacticalBattleFirePanel.cs`, `TacticalBattlePanelHeader.cs`, `TacticalBattleSidesTab.cs` и `TacticalBattleSkinCatalog.cs`. Заменить визуальные clamp/min/max и деления эквивалентными разрешёнными операциями. Сохранить геометрию, сверить пиксели Classic и оставить guard без изменений.
- Проверить в `SkinCatalogTests.cs` и `WidgetsListsTests.cs`, повторить тактические растры и нативный маршрут. Скин не меняет входные области и правила боя.

## Исполнение

До правки два целевых теста воспроизвели потерю общего цвета рамки и замену Humaroid на Verdana из-за CR/LF. После правки локальная роль дополняется общей, а пробелы разметки не участвуют в проверке глифов. Для реально отсутствующего глифа действует штатный шрифт. Classic пропускает поиск переопределений.

Полный запуск обнаружил guard Motion в изменённых участках рендера, в том числе на выражениях карты, существовавших на 81e6d871. В затронутом Client применены эквивалентные разрешённые операции. Тест guard сохранён и прошёл 19/19.

- Тактические тесты и Settings на итоговом коде: 252/252 PASS. Растры Classic 23/23 побайтно совпали. Scoped whitespace и staged diff check PASS. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk5/focused.trx`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk5/motion-guard.trx`, `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/us3-tk5/classic-parity.json`.
- После сборки Release повторены два нативных EN маршрута. Выбор пакета 38/38, fallback 37/37. Итог и 224 RNG calls совпали. Подготовка побайтно одинакова. `D:/DeepSpaceSaga/artifacts/validation/ep-0016/20261106-execution-43bf71/native/reviewed-comparison.json`.
- Review проверил изоляцию Client и сохранение актуальных ветвей ввода, команд, палитры и файлового каталога.
