---
epic: EP-maintenance
story: EP-maintenance-US-0029-tetrarch-deck
ticket: EP-maintenance-US-0029-TK-0003-desktop-delivery
title: Запускаемое приложение для Windows
stage: approved
implementation_status: implemented
layer: tools/DeepSpaceSaga.TetrarchDeck
depends_on: [EP-maintenance-US-0029-TK-0001-compact-interior, EP-maintenance-US-0029-TK-0002-crew-on-deck]
---

# Запускаемое приложение для Windows

[История](../EP-maintenance-US-0029-tetrarch-deck.md)

Сборка открывает сценарии Тетрарха штатным загрузчиком игры. Оконная проверка выбора и приказа заканчивается прибытием. Проверка каждого сценария охватывает доступность отсеков и размеры модулей.

Исходный контракт — `ScenarioLoader.LoadFromFile`, корабль игрока, `ShipModuleData.OccupiedCells`, `ShipCrewMemberData` и `CarrierState.OwnedDrones`. Размеры дрона относительно роста человека составляют 5 в длину и 1,5 в высоту. В старых сценариях часть полей внешности опущена.

Изменяемые файлы в пределах приложения и решения — DeepSpaceSaga.TetrarchDeck.csproj, Program.cs, DeckChecks.cs, README.md, DeepSpaceSaga.sln.

Приёмка выполняется после `dotnet publish` запуском приложения в режиме `--verify` по всем сценариям. Отчёт хранит фактически полученный результат.

## Проверка

Собрана Release-поставка .NET 8. Тот же EXE запущен в изолированном профиле со скрытым окном на меньшем дисплее DISPLAY2 и штатно закрыт. Проверены упакованный контент и рабочие файлы игры. [Сборка](D:/DeepSpaceSaga/artifacts/validation/tetrarch-deck/20261124-120641-600/TetrarchDeck/DeepSpaceSaga.TetrarchDeck.exe) и [кадр палубы](D:/DeepSpaceSaga/artifacts/validation/tetrarch-deck/20261124-120641-600/Deck.png).
