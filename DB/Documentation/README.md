# Deep Space Saga: документация

> **Контракт EP-0013.** Действующие контракты EP-0013: [панель и файлы отладки](04-Engineering/DebugTuningPanel.md), [независимые ручные оси и schema2](04-Engineering/ManualManeuver.md). Реализация и приёмка учитываются раздельно в [Board](../Board/EP-0013-debug-tuning-panel/ExecutionStatus.md).

Документация описывает игровые контракты и текущий код. Незавершённые
функции явно отделены от реализованных. Дата ревизии: 22 октября 2026.
Writer Save19 и миграция Save18 проверены по ScenarioData/ScenarioLoader;
Панели point combat описаны в [PointAimCombat](04-Engineering/PointAimCombat.md#панели-модулей),
а публикация и отдельные acceptance gates — в [Board EP-0014](../Board/EP-0014-module-panels/ExecutionStatus.md).
Состояние рабочего дерева может изменяться параллельно,
поэтому publication/приёмку нужно смотреть в Board, а не выводить из этой страницы.

## Начать здесь

- [Требования движка](01-Requirements/EngineRequirements.md).
- [Текущая игра и границы первого релиза](01-Requirements/FirstReleaseRequirements.md).
- [Архитектура](00-Process/CLAUDE.md).
- [Правила документации](00-Process/DocumentationSystem.md).
- [Эпики и задачи](../Board).

## Темы

### 02-FirstRelease/Mechanics

- [Грузовой отсек](02-FirstRelease/Mechanics/CargoHold.md).
- [Команды корабля](02-FirstRelease/Mechanics/CommandPanels.md).
- [Экипаж и жилые модули](02-FirstRelease/Mechanics/CrewAndHabitation.md).
- [Стыковка](02-FirstRelease/Mechanics/Docking.md).
- [Energy Cells и электричество](02-FirstRelease/Mechanics/ElectricityAndEnergyCells.md).
- [Топливо двигателя](02-FirstRelease/Mechanics/Fuel.md).
- [Добыча и обследование](02-FirstRelease/Mechanics/IceMining.md).
- [Валюта и финансы](02-FirstRelease/Mechanics/Money.md).
- [Солнечная система и кластеры](02-FirstRelease/Mechanics/SolarSystemMapConcept.md).
- [Склад и рынок станции](02-FirstRelease/Mechanics/StationInventory.md).
- [Тактическая карта и маневрирование](02-FirstRelease/Mechanics/TacticalMapAndManeuvering.md).
- [Корабль Tetrarch](02-FirstRelease/Mechanics/TetrarchClass.md).
- [Торговля](02-FirstRelease/Mechanics/Trading.md).

### 02-FirstRelease/Screens

- [Пассажирские контракты](02-FirstRelease/Screens/Contracts.md).
- [Поражение](02-FirstRelease/Screens/Defeat.md).
- [Диалог](02-FirstRelease/Screens/Dialog.md).
- [Финансы](02-FirstRelease/Screens/Finance.md).
- [Меню игровой сессии](02-FirstRelease/Screens/GameMenu.md).
- [Тактический экран](02-FirstRelease/Screens/GameSession.md).
- [Найм экипажа](02-FirstRelease/Screens/Hire.md).
- [Загрузка игры](02-FirstRelease/Screens/Load.md).
- [Главное меню](02-FirstRelease/Screens/MainMenu.md).
- [Сохранение игры](02-FirstRelease/Screens/Save.md).
- [Выбор сценария](02-FirstRelease/Screens/ScenarioSelect.md).
- [Экраны и переходы](02-FirstRelease/Screens/ScreenCatalog.md).
- [Настройки](02-FirstRelease/Screens/Settings.md).
- [Обзор корабля](02-FirstRelease/Screens/Ship.md).
- [Окно станции](02-FirstRelease/Screens/Station.md).
- [Торговля](02-FirstRelease/Screens/Trade.md).

### 03-Design

- [Портреты в диалогах](03-Design/DialoguePortraits.md).
- [Портретные наборы и композиция](03-Design/PortraitGenerator.md).
- [Мужской набор M4](03-Design/PortraitGeneratorM4.md).
- [Женский набор W4](03-Design/PortraitGeneratorW4.md).
- [Стиль тактических кнопок](03-Design/TacticalButtons/STYLE.md).
- [Дизайн тактической карты](03-Design/TacticalMapSpecification.md).

### 04-Engineering

- [Базы ИИ, поля и точки интереса](04-Engineering/AiMapEnvironment.md).
- [Approach: маршрут сближения](04-Engineering/ApproachRoutes.md).
- [Боевые доклады и журнал](04-Engineering/CombatReports.md).
- [Отладочные параметры: текущий backend](04-Engineering/DebugTuning.md).
- [Каталог товаров и совместимость](04-Engineering/ItemCatalogSchema.md).
- [Прогноз движения NPC по наблюдениям](04-Engineering/NpcObservedTrajectories.md).
- [Точечный бой](04-Engineering/PointAimCombat.md).
- [Сохранения и восстановление](04-Engineering/SaveFormat.md).
- [Экономика и процедурный мир](04-Engineering/TradingAndSolarSystem.md).

### 06-Tooling

- [Ubuntu: сборка, запуск и пользовательские файлы](06-Tooling/Ubuntu.md).

- [Инструмент подготовки портретов](06-Tooling/DeepSpaceSaga.PortraitAssets.md).
- [Ассеты и данные тактического UI](06-Tooling/GameSessionScreenUI.md).
- [Навигационный граф](06-Tooling/Graphify.md).

Здесь хранятся только `.md`. Результаты тестов, дампы, скрипты,
архивы и старые журналы запусков не являются документацией игры. Исторические
версии документов доступны через Git; планы и execution evidence находятся в Board.
Эта ревизия не утверждает, что выполнены новые gameplay, native или balance проверки.
