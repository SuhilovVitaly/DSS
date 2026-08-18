# Тактическая карта и маневрирование

Статус: частично реализовано.

Основные источники: `deep_space_saga_engine_requirements.md`, `Docs/TacticalMapSpecification.md`, `GameSessionScreen`, `CommandPanels.md`.

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

- Navigation: `navigation.dock`, `engine.orbit`, `engine.speedSynchronization`, `engine.directionSynchronization`.
- Maneuver: `engine.maintainCourse`, `engine.turnLeftStep`, `engine.turnRightStep`, `engine.turnLeftUntilCancel`, `engine.turnRightUntilCancel`.
- Engine: `engine.accelerate`, `engine.brake`, `engine.maintainSpeed`.
- Space Control: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`, `navigation.stationsList`, `mining.extractIce`, `mining.stopExtraction`.

## Границы MVP

- Полная орбитальная физика не требуется сверх уже описанной упрощенной модели.
- Бой, столкновения и урон не входят в минимальный игровой цикл первого релиза, если не будут специально добавлены позже.