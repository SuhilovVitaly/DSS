# Тактическая карта и маневрирование

Статус: частично реализовано.

Основные источники: `Documentation/01-Requirements/EngineRequirements.md`, `Documentation/03-Design/TacticalMapSpecification.md`, `GameSessionScreen`, `CommandPanels.md`.

## Цель

Игрок должен понимать положение корабля и объектов в системе, масштабировать карту и управлять движением корабля через командные панели Tetrarch Class.

## Уже существующая основа

- Координаты мира: `1 unit = 100 m`, `double`.
- Направление: `0°` вверх, `90°` вправо, по часовой стрелке.
- Клиент получает `AuthoritativeSnapshot` и рисует карту из `SnapshotBuffer`.
- Client-side prediction использует `DeepSpaceSaga.Motion`.
- Доступны команды двигателя: ускорение, торможение, повороты, удержание скорости/курса, синхронизация скорости/направления и orbit.
- Масштаб карты меняется колесом мыши и кнопками scale panel.

## Требования первого релиза

- Карта должна оставаться главным игровым экраном.
- Игрок должен уметь выбрать станцию, астероид, корабль или планету.
- Игрок должен уметь маневрировать к станции, астероиду или выбранному celestial object через командные панели.
- Игрок должен видеть текущую скорость симуляции и менять ее.
- Игрок должен видеть хотя бы минимальное состояние своего корабля: скорость, курс, груз, запас `Fuel`.
- Маневрирование должно проходить через `IGameSessionConnection` и `PlayerCommand`, без прямого обращения Client к Engine.
- `GameSessionScreen` должен группировать команды в панели Navigation, Maneuver, Engine и Space Control.

## Команды первого релиза

- Navigation: `navigation.dock`, `engine.orbit`, `engine.speedSynchronization`, `engine.directionSynchronization`, `navigation.approach`.
- Maneuver: `engine.maintainCourse`, `engine.turnLeftStep`, `engine.turnRightStep`, `engine.turnLeftUntilCancel`, `engine.turnRightUntilCancel`.
- Engine: `engine.accelerate`, `engine.brake`, `engine.maintainSpeed`.
- Space Control: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`, `navigation.stationsList`, `mining.extractIce`, `mining.stopExtraction`.

## Сближение Approach

Уточнение от 2026-09-20: [EngineRequirements, раздел 60](../../01-Requirements/EngineRequirements.md#approach-shortest-route) определяет кратчайший допустимый маршрут с минимальным временем прибытия при постоянной скорости. При достижимой встрече конечная точка — будущая задняя точка цели; при отсутствии перехвата — её задняя точка, зафиксированная в момент планирования. Конечный курс совпадает с курсом цели, ограничения поворота сохраняются. Равная или большая скорость цели сама по себе не исключает встречу.

Карта показывает подтверждённый `ApproachRoute`, одинаковый с исполнением в Engine. Запасной маршрут не обозначается гарантированным перехватом. Продолжение по конечному курсу к краю viewport не входит в конечный манёвр; режим «Весь маршрут» кадрирует манёвр. Красная пользовательская линия — пояснение желаемого направления полёта, не требование нового цвета UI. Детали: [ApproachRoutes](../../04-Engineering/ApproachRoutes.md).

## Границы MVP

- Полная орбитальная физика не требуется сверх уже описанной упрощенной модели.
- Бой, столкновения и урон не входят в минимальный игровой цикл первого релиза, если не будут специально добавлены позже.
