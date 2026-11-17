# Выбор сценария

Показывает найденные scenario.json и их player-facing metadata/description. Выбор запускает соответствующий сценарий через session factory; невалидный файл не становится нормальной доступной сессией. Список не является перечнем сохранений.

Сценарий с `"hidden": true` в metadata не попадает в обычный список. В публичном demo-профиле список дополнительно ограничен `scenarioId: "demo"`; главное меню запускает его напрямую. Development сохраняет обычный список и скрытый Tutorial. См. [главное меню](MainMenu.md).

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Источники: [ScenarioSelectScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/ScenarioSelect/ScenarioSelectScreen.cs), [ScenarioRepository.cs](../../../../src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs).
