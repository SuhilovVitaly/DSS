---
epic: EP-maintenance
story: EP-maintenance-US-0029-tetrarch-deck
ticket: EP-maintenance-US-0029-TK-0001-compact-interior
title: План палубы по сценарию
stage: approved
implementation_status: implemented
layer: tools/DeepSpaceSaga.TetrarchDeck
depends_on: []
---

# План палубы по сценарию

[История](../EP-maintenance-US-0029-tetrarch-deck.md)

Каждый установленный модуль занимает ровно 144 тайла в квадрате 12×12. Количество отдельных кают и приписанных дронов совпадает с манифестом. Оборудование оставляет доступ к дверям и узким проходам.

Исходный контракт — `ScenarioLoader.LoadFromFile`, корабль игрока, `ShipModuleData.OccupiedCells`, `ShipCrewMemberData` и `CarrierState.OwnedDrones`. Размеры дрона относительно роста человека составляют 5 в длину и 1,5 в высоту. В старых сценариях часть полей внешности опущена.

Изменяемые файлы в пределах приложения и решения — Deck.cs, DeckPainter.cs, Art/drone-utility.png, Art/desk.png.

Приёмка выполняется после `dotnet publish` запуском приложения в режиме `--verify` по всем сценариям. Отчёт хранит фактически полученный результат.

## Проверка

Все семь конфигураций прошли проверку квадратов 12×12, размеров дронов, количества кают и связности пола. В Demo доступны 1270 свободных клеток. Проверены площади всех предметов обстановки. [Протокол](D:/DeepSpaceSaga/artifacts/validation/tetrarch-deck/20261124-120641-600/checks.txt).

## Однотонная плитка

По уточнению пользователя каюты, коридоры, ангары и остальные модули используют одинаковую серую плитку с тонким швом. Убраны полосы, решётки, коврики и варианты оттенков. Проверка оконного кадра сравнивает девять точек внутри плитки во всех модулях и в переходах. [Общий вид](D:/DeepSpaceSaga/artifacts/validation/tetrarch-deck/20261124-102747-554Z/Deck.png).
