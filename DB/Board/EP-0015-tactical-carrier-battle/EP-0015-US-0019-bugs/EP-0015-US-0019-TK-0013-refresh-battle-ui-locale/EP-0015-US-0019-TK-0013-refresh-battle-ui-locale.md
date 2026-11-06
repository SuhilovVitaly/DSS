---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0019-bugs
ticket: EP-0015-US-0019-TK-0013-refresh-battle-ui-locale
stage: approved
layer: client
depends_on: [EP-0015-US-0019-TK-0007-follow-game-language]
files_touched:
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs
---

# Восстановить язык правой панели боя

## Контракт AC-13

Настройка языка из Settings.json фактически запущенной сборки применяется при F8. Через настройки и реальные входы открываются русские и английские кнопки, фазы, названия и содержимое всех четырёх вкладок.

## Code context и реализация

Перечитать сохранённый язык перед созданием экрана подготовки. Проверить на production router после выбора в Settings. У текущих DSS и Dss-Main раздельные Settings.json; перенести только уже выбранный русский в локальный Dss-Main, если язык там ещё не задан.

## Проверка

В Dss-Main запустить проверки DeepSpaceSaga.Client.Tests `LocalizationCoverageTests` и `git diff --check`. Каждый тикет опубликовать отдельным коммитом. Ручную приёмку фиксировать отдельно.
