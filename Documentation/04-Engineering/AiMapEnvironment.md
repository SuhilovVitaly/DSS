# Базы ИИ, поля и точки интереса

Solar-system content задаёт базы Planetary/Orbital, движущиеся территории,
Radiation/Dust/Debris и POI templates. Результат сохраняется как materialized
AiMap: anchors, geometry и тексты восстанавливаются без повторной генерации.
Неверные версии, ID, ссылки и геометрия отклоняются до замены мира.

Базы ИИ исключены из человеческих quote/trade/refuel и Dock путей; Engine
проверяет `station_access_denied`, UI не является единственной защитой.
Поля — информационные: не меняют автоматически движение, сенсоры, цену,
топливо или damage. POI не обещают исследование, награду или производство.
Patrol radius описывает территорию; не является доказательством патрульного AI.

Клиентские слои Orbits/Territories/Fields/PointsOfInterest меняют отрисовку и
hit-testing, не мир. Выбор descriptor отделён от Engine SelectedObjectId;
модульная команда не должна отправляться на поле как на реальный корабль.
Декоративный рисунок не расходует Engine RNG и не создаёт игровые entities.
Границы генерации и размещения заданы в конфигурации и bounded validators;
конечный горизонт проверки не доказывает безопасность на бесконечном времени.

Источники: [solar-system.json](../../src/DeepSpaceSaga.Client/Data/Maps/solar-system.json), [AiTradePlacementValidator.cs](../../src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs), [AiMapPresentation.cs](../../src/DeepSpaceSaga.Client/UI/Screens/GameSession/AiMapPresentation.cs).
