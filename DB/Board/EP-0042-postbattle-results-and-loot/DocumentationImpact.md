# Влияние EP-0042 на документацию

| Область | Проверенное изменение | Документы |
|---|---|---|
| Действующие требования | Обязательный результат, авторитет Engine и стабильные стадии Save22 | [EngineRequirements](../../Documentation/01-Requirements/EngineRequirements.md), [FirstReleaseRequirements](../../Documentation/01-Requirements/FirstReleaseRequirements.md) |
| Игровая механика | Таймеры, вместимость, ExtractedWaiting, частичный сбор, остатки и каюты | [Добыча](../../Documentation/02-FirstRelease/Mechanics/DemoBountiesAndLoot.md), [трюм](../../Documentation/02-FirstRelease/Mechanics/CargoHold.md) |
| Интерфейс | Маршрут после боя, положение модали, карточки, футер, предупреждение, быстрые слоты | [Результаты боя](../../Documentation/02-FirstRelease/Screens/BattleResults.md), [тактический бой](../../Documentation/02-FirstRelease/Screens/TacticalBattle.md), [каталог](../../Documentation/02-FirstRelease/Screens/ScreenCatalog.md), [меню](../../Documentation/02-FirstRelease/Screens/GameMenu.md), [Save](../../Documentation/02-FirstRelease/Screens/Save.md), [Load](../../Documentation/02-FirstRelease/Screens/Load.md), [Tabletop](../../Documentation/03-Design/TabletopClient.md) |
| Архитектура и данные | Владение очередью, устойчивость команд, атомарная запись, legacy Open/Closed | [DemoRewards](../../Documentation/04-Engineering/DemoRewards.md), [SaveFormat](../../Documentation/04-Engineering/SaveFormat.md), [DemoCampaign](../../Documentation/04-Engineering/DemoCampaign.md), [архитектура](../../Documentation/00-Process/CLAUDE.md) |
| Навигация и история | Синхронизированы реестр и истории, помечен прежний контракт EP-0036, расширен входной корпус графа | [Документация](../../Documentation/README.md), [Board](../README.md), [EP-0036](../EP-0036-demo-bounties-and-loot/Documentation.md), [Graphify](../../Documentation/06-Tooling/Graphify.md), [тикеты](Tickets.md) |

Сверены README и Markdown рядом с `src`, `tests`, инструментами и ассетами. Операции используют прежний каталог `loot.json`, схему предметов, сценарии боя и существующий пул кают. Языковые реплики об исходе обновлены в English и Russian. Исторические карты EP-0027, EP-0032 и EP-0035 описывают свои поставки. Канонический актуальный маршрут закреплён в документах выше.

Порядок ручного осмотра находится в [эпике](Documentation.md#будущая-приёмка-владельцем). Нативное окно и игровой процесс ожидают осмотра владельцем.
