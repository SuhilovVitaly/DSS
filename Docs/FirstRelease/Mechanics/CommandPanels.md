# Командные панели корабля

Статус: смысловая группировка команд первого релиза обновлена под Tetrarch Class.

Связанные документы: `TetrarchClass.md`, `TacticalMapAndManeuvering.md`, `Docking.md`, `Fuel.md`, `ElectricityAndEnergyCells.md`, `IceMining.md`.

## Цель

Командные панели на `GameSessionScreen` группируют module-addressed команды корабля по смыслу, а не строго по физическому модулю. Команда может быть технически командой Engine, но отображаться в панели Navigation, если игрок воспринимает ее как навигационное действие.

## Navigation

| Команда | Технический модуль | Условие |
| --- | --- | --- |
| `navigation.dock` | Navigation Computer | Выбрана станция; дистанция `< 200 km`; скорость и направление синхронизированы со станцией |
| `engine.orbit` | Engine | Выбран celestial object |
| `engine.speedSynchronization` | Engine | Выбран celestial object |
| `engine.directionSynchronization` | Engine | Выбран celestial object |
| `navigation.approach` | Engine | Выбран celestial object; прокладывает курс в точку позади цели по ходу её движения (пересчитывается каждый цикл), завершается точным совпадением скорости и направления с целью |

## Maneuver

| Команда | Технический модуль | Параметр |
| --- | --- | --- |
| `engine.maintainCourse` | Engine | Курс `0..360` |
| `engine.turnLeftStep` | Engine | - |
| `engine.turnRightStep` | Engine | - |
| `engine.turnLeftUntilCancel` | Engine | Останавливается отменой/следующей командой |
| `engine.turnRightUntilCancel` | Engine | Останавливается отменой/следующей командой |

## Engine

| Команда | Технический модуль | Параметр |
| --- | --- | --- |
| `engine.accelerate` | Engine | - |
| `engine.brake` | Engine | - |
| `engine.maintainSpeed` | Engine | Скорость `0..max` |

## Space Control

| Команда | Технический модуль | Условие |
| --- | --- | --- |
| `scanner.generalScan` | Scanner | Выбран celestial object |
| `scanner.structuralScan` | Scanner | Выбран celestial object |
| `scanner.nearbySignatures` | Scanner | Без цели |
| `navigation.stationsList` | Navigation Computer | Без цели |
| `mining.extractIce` | Drilling Unit | Выбран asteroid; `scanner.structuralScan` подтвердил лед; дистанция `<= 100 km`; скорость и направление синхронизированы; есть место в cargo |
| `mining.stopExtraction` | Drilling Unit | Активна добыча льда |

## Требования первого релиза

- `GameSessionScreen` должен показывать команды через четыре панели: Navigation, Maneuver, Engine, Space Control.
- Панели должны работать поверх существующей module-addressed command model.
- Команды Navigation Computer: `navigation.dock`, `navigation.stationsList`.
- Команды Engine: `engine.accelerate`, `engine.brake`, `engine.maintainCourse`, `engine.maintainSpeed`, `engine.turnLeftStep`, `engine.turnRightStep`, `engine.turnLeftUntilCancel`, `engine.turnRightUntilCancel`, `engine.speedSynchronization`, `engine.directionSynchronization`, `engine.orbit`, `navigation.approach` (физически команда Engine, отображается в панели Navigation).
- Команды Scanner: `scanner.generalScan`, `scanner.structuralScan`, `scanner.nearbySignatures`.
- Команды Drilling Unit: `mining.extractIce`, `mining.stopExtraction`.
- UI должен показывать недоступность команды через понятную причину: нет цели, цель неверного типа, не выполнена синхронизация, нет топлива, нет `Energy Cells`, нет mining module, нет места в cargo, корабль не в нужном состоянии.