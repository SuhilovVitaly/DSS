---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0007-follow-game-language
stage: done
layer: client
depends_on: [EP-0015-US-0019-TK-0006-remove-salvo-map-summary]
files_touched:
  - src/DeepSpaceSaga.Client/Localization.cs
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs
---

# Применять язык игры в тактическом бою

## Контракт AC-07

Вновь открытый экран подготовки и боя после смены языка в настройках использует выбранную локаль для журнала, фаз, кнопок, подписей и справочной информации. Имена и идентификаторы объектов берутся из каталога.

## Code context и реализация

Localization.Get использует словарь, загруженный единожды. После успешной записи Settings.json опубликовать словарь выбранного языка. Проверить переключение EN → RU → EN на подготовке и на карте, в фазе и журнале.

## Tests и Definition of Done

В Dss-Main проходят целевые проверки DeepSpaceSaga.Client.Tests с фильтром `LocalizationCoverageTests` и `git diff --check`. Опубликовать отдельный коммит ветки эпика. Ручную приёмку записать отдельно.

## Исполнение

Коммит `ae1df209` опубликован. 18/18 в LocalizationCoverageTests и базовом LocalizationTests. Проверено переключение EN → RU → EN на подготовке, фазе, вкладке журнала, тексте событий и подписи состояния. Четыре проверки локализации других панелей в широком фильтре упали на жёстко заданных путях к исходникам при --artifacts-path. Ручная приёмка открыта.
