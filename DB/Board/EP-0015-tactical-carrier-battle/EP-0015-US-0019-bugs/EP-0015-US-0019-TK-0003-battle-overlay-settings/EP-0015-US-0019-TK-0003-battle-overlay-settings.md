---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0003-battle-overlay-settings
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0002-enemy-straight-forecast]
files_touched:
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs
---

# Настройки видимости карты боя

## Контракт и критерий AC-03

Правая панель содержит три независимых флажка RU/EN. По умолчанию показываются все зелёные сектора, кормовые зоны и прогноз. Отключение всех секторов оставляет сектор выбранного дрона игрока. Кормовой сектор и прогноз переключаются независимо в планировании движения и стрельбы.

## Code context и реализация

Заменить прежние два переключателя группой настроек правой панели. Хранить значения на экране боя. Брать выбранный дрон из плана соответствующей фазы.

## Tests

Фильтры `BattleMapRenderTests|LocalizationCoverageTests|FirePlanningTests` в соответствующих DeepSpaceSaga.Client.Tests. Проверить изменённые файлы через `git diff --check`.

## Definition of Done

Критерий покрыт проверкой в Dss-Main. Результат проверок записан отдельно от ручной приёмки. Отдельный коммит опубликован в ветке эпика.

## Исполнение

Коммит `adb3e8f2` опубликован в ветке эпика. 18/18 в BattleMapRenderTests, FirePlanningTests, PanelHeaderTests и MouseRoutingTests. Четыре ключа подтверждены в собранных RU/EN JSON. Три теста LocalizationCoverageTests зависят от стандартного относительного пути к исходникам, который меняется с --artifacts-path. Ручная приёмка открыта.
