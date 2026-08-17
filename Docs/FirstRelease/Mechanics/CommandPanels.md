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
| `engine.speed-synchronization` | Engine | Выбран celestial object |
| `engine.direction-synchronization` | Engine | Выбран celestial object |

## Maneuver

| Команда | Технический модуль | Параметр |
| --- | --- | --- |
| `engine.maintain-course` | Engine | Курс `0..360` |
| `engine.turn-left-step` | Engine | - |
| `engine.turn-right-step` | Engine | - |
| `engine.turn-left-until-cancel` | Engine | Останавливается отменой/следующей командой |
| `engine.turn-right-until-cancel` | Engine | Останавливается отменой/следующей командой |

## Engine

| Команда | Технический модуль | Параметр |
| --- | --- | --- |
| `engine.accelerate` | Engine | - |
| `engine.brake` | Engine | - |
| `engine.maintain-speed` | Engine | Скорость `0..max` |

## Space Control

| Команда | Технический модуль | Условие |
| --- | --- | --- |
| `scanner.general-scan` | Scanner | Выбран celestial object |
| `scanner.structural-scan` | Scanner | Выбран celestial object |
| `scanner.nearby-signatures` | Scanner | Без цели |
| `navigation.stations-list` | Navigation Computer | Без цели |
| `mining.extract-ice` | Drilling Unit | Выбран asteroid; `scanner.structural-scan` подтвердил лед; дистанция `<= 100 km`; скорость и направление синхронизированы; есть место в cargo |
| `mining.stop-extraction` | Drilling Unit | Активна добыча льда |

## Требования первого релиза

- `GameSessionScreen` должен показывать команды через четыре панели: Navigation, Maneuver, Engine, Space Control.
- Панели должны работать поверх существующей module-addressed command model.
- Команды Navigation Computer: `navigation.dock`, `navigation.stations-list`.
- Команды Engine: `engine.accelerate`, `engine.brake`, `engine.maintain-course`, `engine.maintain-speed`, `engine.turn-left-step`, `engine.turn-right-step`, `engine.turn-left-until-cancel`, `engine.turn-right-until-cancel`, `engine.speed-synchronization`, `engine.direction-synchronization`, `engine.orbit`.
- Команды Scanner: `scanner.general-scan`, `scanner.structural-scan`, `scanner.nearby-signatures`.
- Команды Drilling Unit: `mining.extract-ice`, `mining.stop-extraction`.
- UI должен показывать недоступность команды через понятную причину: нет цели, цель неверного типа, не выполнена синхронизация, нет топлива, нет `Energy Cells`, нет mining module, нет места в cargo, корабль не в нужном состоянии.