# Выбор сценария

Показывает найденные scenario.json и их player-facing metadata/description. Выбор запускает соответствующий сценарий через session factory; невалидный файл не становится нормальной доступной сессией. Список не является перечнем сохранений.

Сценарий с `"hidden": true` в `scenarioMetadata` в список не попадает. Сейчас скрыт только Tutorial (`scenarioId: "tutorial"`): его запускает [главное меню](MainMenu.md) при отсутствии сохранений. Флаг необязателен; без него сценарий показывается, как раньше.

См. [каталог экранов](ScreenCatalog.md) и [основные требования](../../01-Requirements/EngineRequirements.md).

Источники: [ScenarioSelectScreen.cs](../../../../src/DeepSpaceSaga.Client/UI/Screens/ScenarioSelect/ScenarioSelectScreen.cs), [ScenarioRepository.cs](../../../../src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs).
