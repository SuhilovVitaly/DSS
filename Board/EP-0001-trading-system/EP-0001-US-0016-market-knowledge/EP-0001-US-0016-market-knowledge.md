---
epic: EP-0001-trading-system
story: EP-0001-US-0016-market-knowledge
title: Устаревающее знание удалённых рынков
stage: approved
dependencies: [EP-0001-US-0004-seeded-trading-map, EP-0001-US-0015-authoritative-market-quotes]
created: 2026-09-21T15:11:56Z
source_request: "создай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0016-market-knowledge\\EP-0001-US-0016-market-knowledge.md"
current_review: complete
revision: 1
---

# Устаревающее знание удалённых рынков

## Входное техническое задание

Точное сообщение пользователя: «создай тикеты D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0016-market-knowledge\EP-0001-US-0016-market-knowledge.md». Время фиксации запроса: 2026-09-21T15:11:56Z (UTC).

Исходный объём истории: Concept:173, 203; MVP:288, 419–429; эпик `../Documentation.md:101, 125, 153–154, 188, 885–895`. Игрок получает coarse knowledge об известных удалённых станциях, но не бесплатный точный прайс-лист. Точные authoritative котировки остаются локальным результатом US-0015 и публикуются через существующий путь `Station -> Trade` только при стыковке. Все пути и ссылки `path:line` ниже относятся к `D:/DeepSpaceSaga/DSS`; строки описывают снимок grounding 2026-09-21, а новые API явно обозначены как планируемые.

## User story

Как игрок, я хочу выбирать торговое направление по доступной информации, не получая бесплатный глобальный прайс-лист. Для каждой уже известной станции я вижу её экономическую роль, известную доступность, coarse stock bands и игровое время последнего наблюдения. После изменения authoritative рынка сохранённое наблюдение не обновляется удалённо, а помечается `stale`. При стыковке наблюдение обновляется, а точная цена, доступный максимум и последовательная котировка доступны только в локальном `Trade`. После ухода точная локальная проекция исчезает, но последнее coarse observation остаётся доступным и впоследствии может устареть.

## Acceptance criteria

- AC-01: Для пяти известных станций из US-0004 authoritative snapshot содержит по одному market-knowledge observation: station id, экономическую роль, известную доступность, `ObservedAtGameTimeMs`, `ObservedMarketRevision` и coarse stock band каждого управляемого market item. Массив не содержит точных цен, station budget, quote curve или `QuoteId`.
- AC-02: Изменение `MarketRevision` после производства, потребления, события или сделки не заменяет удалённое observation; оно публикуется с `IsStale = true`. Повторный snapshot без изменения рынка не меняет timestamp/observed revision, а повторная команда не создаёт второе observation.
- AC-03: При фактической стыковке с известной станцией Engine обновляет её observation до текущих role/availability/bands/revision/time и одновременно оставляет точную котировку только в `DockedStationTrade`. После Undock `DockedStationTrade == null`, а observation не получает точных локальных полей.
- AC-04: При выборе или наведении на известную Station существующая панель Object Info показывает role, availability, stock bands, время наблюдения и явный `STALE/FRESH`; для неизвестного объекта или станции без observation этих строк нет. Отдельный экран разведки не создаётся.
- AC-05: Save/Load сохраняет наблюдённые значения, timestamp и observed revision без удалённого refresh; после загрузки stale вычисляется против восстановленной текущей `MarketRevision`, а несовместимые station/item ссылки отклоняются до замены текущего мира.

## Non-goals

- Платная разведка, прогноз цены, price trend, отдельный intelligence screen и расширение scanner-команд.
- Автоматическое открытие карты, изменение физической видимости/`IsKnown`, генерация пяти станций и их маршрутов — это US-0004.
- Формула котировки, quote curve, `QuoteId`, инкремент `MarketRevision` и отклонение stale quote — это US-0015; исполнение сделки — US-0003.
- Публикация station budget, точных удалённых stock quantities, удалённых buy/sell prices или клиентский пересчёт band/price.
- Маршрутная доступность и риск US-0007/US-0008. Поле availability этой истории означает известную возможность обратиться к рынку станции (`not destroyed` и `station access not denied`), а не гарантию безопасного маршрута.

## Dependencies

- `EP-0001-US-0004-seeded-trading-map`: материализует пять известных станций и их стабильные object ids; approved artifact `../EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-seeded-trading-map.md:28–34, 61–62`.
- `EP-0001-US-0015-authoritative-market-quotes`: вводит монотонную station-level `MarketRevision` и точную локальную котировку; текущая draft-история находится в `../EP-0001-US-0015-authoritative-market-quotes/EP-0001-US-0015-authoritative-market-quotes.md:15–27`. Её ticket map на момент grounding отсутствует, поэтому тикеты US-0016 не угадывают номера внешних тикетов; перед реализацией требуется завершённая story dependency.
- Текущая рабочая ветка уже содержит частично подготовленные bounded-market поля `TargetStock`, `MaxStock`, `FreeStockCapacity`, `StockState` в `src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:48–93`, но Engine projection ещё не заполняет их (`src/DeepSpaceSaga.Engine/SimulationEngine.cs:573–597`). Это незавершённая параллельная работа US-0002, а не готовая зависимость.

## Grounding

- Engine является владельцем состояния; Client только читает публичные snapshots и отправляет команды через session boundary: `Documentation/01-Requirements/EngineRequirements.md:362–430`, `Documentation/00-Process/CLAUDE.md:22–56`.
- Текущий `AuthoritativeSnapshot` публикует `Objects` и точный `DockedStationTrade`, причём документирует его как non-null только при фактической стыковке: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11–34`.
- `StationTradeSnapshot` сейчас раскрывает точный stock и цену только локальной станции, скрывает station Credits и уже имеет authoritative `Shortage|Normal|Surplus`: `src/DeepSpaceSaga.Contracts/StationTradeSnapshot.cs:6–34, 48–93`.
- Engine строит `DockedStationTrade` только для `ship.IsDocked`/`DockedStationObjectId` и возвращает null вне этого состояния: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:551–597`. Undock очищает docking fields: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:1883–1903`.
- Текущий snapshot не содержит массива remote market knowledge, timestamp наблюдения или `MarketRevision`; `rg` по `src` подтвердил отсутствие `MarketRevision`, а `AuthoritativeSnapshot.cs:11–53` заканчивается другими trailing fields. Это gap, закрываемый TK-0001/TK-0002.
- Station market profile уже имеет `TypeId`, `DisplayName`, supply/demand sets и inventory/economy data: `src/DeepSpaceSaga.Engine/Content/StationMarketProfileDefinition.cs:68–82`. Runtime station хранит `MarketProfileId`: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:3290–3299`.
- Save формируется из runtime objects и `GameStateData`; текущий `GameStateData` не хранит knowledge: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:760–828`, `src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs:54–80`.
- Object Info уже показывает выбранный/hovered object через `GameSessionScreen.ToObjectInfoPanelData`: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:2083–2109`. Его formatter сейчас выводит только Name/Speed/Direction: `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:160–179`.
- Epic требует remote role/availability/last-known stock band/timestamp, stale после изменения рынка и отсутствие отдельного intelligence screen: `../Documentation.md:101, 125, 153–154, 188, 885–895`.

## Invariants and assumptions

- A-01: План принят автоматическим workflow; запрос на создание тикетов не требует отдельного подтверждения ticket map.
- A-02: Источник observation для MVP — уже известные станции US-0004. New Game создаёт первое coarse observation для каждой `Station` с `IsKnown == true`; неизвестные физические объекты не раскрываются. Новая scanner-команда не вводится.
- A-03: Role — player-facing `StationMarketProfileDefinition.DisplayName`. Availability — наблюдённый boolean `!station.IsDestroyed && !StationAccessState.AccessDenied`; это не route availability/risk.
- A-04: Stock knowledge — только per-item enum `StationMarketStockState`; точное `StockQuantity`, `TargetStock`, `MaxStock` и цены не копируются в remote DTO. Порядок bands детерминирован по `ItemTypeId`.
- A-05: Engine хранит последнее observation отдельно от текущего рынка. `IsStale` вычисляется при projection как несовпадение current/observed `MarketRevision` либо текущей/observed availability; это не обновляет observation или timestamp.
- A-06: Пока корабль пристыкован, observation станции обновляется до current revision перед публикацией snapshot. После Undock оно остаётся последним известным; exact `DockedStationTrade` исчезает по существующему правилу.
- A-07: Knowledge timestamps используют календарный `GameTimeMs`, не физический `MotionTimeMs`; observation отражает экономическое время и не меняет скорость/движение.
- A-08: Добавление optional `marketKnowledge` в save требует additive schema handling. Конкретный номер `SaveFormatVersion` выбирается только после реализации предыдущих stories: нельзя резервировать число поверх текущего `9` (`ScenarioData.cs:6–26`) и незавершённых dependencies.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0016-TK-0001-market-knowledge-contract | Контракт coarse knowledge без удалённых цен | contracts | story dependencies | AC-01, AC-03 |
| EP-0001-US-0016-TK-0002-authoritative-market-observations | Authoritative observations, stale и Save/Load | engine | TK-0001; US-0004; US-0015 | AC-01, AC-02, AC-03, AC-05 |
| EP-0001-US-0016-TK-0003-market-knowledge-presentation | Market knowledge в существующей Object Info | client | TK-0002 | AC-04 |

Dependency order: TK-0001 -> TK-0002 -> TK-0003. Лимиты implementation files: 3, 4, 3. Каждый тикет имеет один production layer и matching test project.

## Gaps and backlog

- G-01: US-0015 ещё draft и не имеет ticket map. TK-0002 фиксирует требуемую семантику `MarketRevision`, но не реализует quote formula или revision increments; при несовпадении будущего approved API вернуть US-0016 в review, не создавать адаптер молча.
- G-02: US-0004 имеет approved план, но его пять станций/`tradingMap` ещё не подтверждены реализованными. Тесты TK-0002 должны использовать fixture из реализованной зависимости, а не дублировать генератор карты.
- G-03: Текущая частичная работа US-0002 меняет market profile/schema и принадлежит пользователю. Тикеты US-0016 не расширяют и не перезаписывают её scope.
- G-04: Отдельная платная разведка, сканирование рынка, тренды и история нескольких observations — backlog эпика (`../Documentation.md:210`).
- G-05: `requirements-engineer`, упомянутый `Documentation/00-Process/AGENTS.md:3–14`, отсутствует в доступном каталоге skills. Применён специализированный обязательный DSS-StoryBuilder workflow.
- Нерешённых блокирующих вопросов нет.

## Decision and review log

- 2026-09-21T15:11:56Z — точное сообщение пользователя сохранено в `source_request`; запрошено создание тикетов для однозначно указанной US-0016.
- Grounding: проверены canonical process files, релевантные требования, эпик, история, dependencies, текущие Contracts/Engine/Client/test surfaces и dirty worktree. Идентификаторы path/frontmatter согласованы; unrelated изменения не затронуты.
- Plan-review: выбран contracts -> engine -> client, существующий Object Info вместо нового экрана, coarse bands вместо удалённых чисел. План принят автоматическим workflow без вопроса.
- Созданы TK-0001, TK-0002 и TK-0003 в dependency order. Story переведена в `approved/complete`.
