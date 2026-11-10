---
epic: EP-0021-drone-combat-economy
story: EP-0021-US-0002-station-supply
title: Ближайшие поставщики и пополнение запасов
stage: draft
dependencies: [US-0001, US-0008, US-0009, US-0010]
created: 2026-11-09T11:34:51Z
source_request: "Оснастить пустой носитель эталонным крылом из четырёх дронов через ближайшие станции"
current_review: complete
revision: 2
---

# Ближайшие поставщики и пополнение запасов

## Входное техническое задание

Подготовить кампанию с пустым носителем, достаточным стартовым бюджетом и четырьмя дронами из действующего боя. Для каждого дрона нужен ангар, а для пилотов нужны каюты. Результат этой истории входит в путь закупки и боевого использования из [описания эпика](../Documentation.md).

## User story

Как командир, я нахожу всё для стартового крыла на двух ближайших пригодных человеческих станциях. Я вижу путь и назначение каждого поставщика. После стартовых покупок станции продолжают пополнять товары из местного сырья.

## Grounding

`SimulationEngine.MarketTime.cs:112–201` выполняет почасовой поток, `StationGeographyV2.cs:9–66` связывает стартовый кластер. Профили описаны в `Data/Markets/station-market-profiles.json:68–127,233–282`.

## Acceptance criteria

- **AC-01.** В каждом валидном старте станция A предлагает грузовой модуль, мостик, четыре ангара и четыре места для пилотов, станция B предлагает весь боевой набор и четырёх кандидатов. Маршрут A→B выбирается из реально доступных соседей.
- **AC-02.** Боевые партии потребляют сырьё, учитывают ёмкость и не останавливают выпуск гражданских товаров при дефиците или переполнении.
- **AC-03.** Навигация показывает план закупок и расстояние; дистанционная информация не используется как котировка.

## Non-goals

Ограничить работу результатами этой истории и allowlist её тикетов. Общий backlog, включая расширенный найм, трофеи и сохранение внутри боя, ведётся в эпике.

## Dependencies

US-0001, US-0008, US-0009, US-0010. Порядок реализации определяется зависимостями конкретных тикетов.

## Invariants

Торговля, размещение и учёт состояния авторитетны в Engine. Отказ и повтор операции сохраняют деньги и имущество в согласованном состоянии. Для действующих входов, старых сохранений и F8 проверяются существующие контракты в пределах этой истории.

## Approved ticket map

Карта сформирована по автоматической процедуре `epic-writer`. Статус карты разрешает подготовку планов, а продуктовый просмотр пользователь проведёт после получения всего набора.

| ID | Результат | Layer | Зависимости | Критерии |
|---|---|---|---|---|
| [US-0002-TK-0001](EP-0021-US-0002-TK-0001-military-supply-schema/EP-0021-US-0002-TK-0001-military-supply-schema.md) | Параллельные партии военного снабжения | engine | [US-0001-TK-0002](../EP-0021-US-0001-trade-combat-equipment/EP-0021-US-0001-TK-0002-equipment-binding/EP-0021-US-0001-TK-0002-equipment-binding.md) | AC-02 |
| [US-0002-TK-0002](EP-0021-US-0002-TK-0002-military-supply-tick/EP-0021-US-0002-TK-0002-military-supply-tick.md) | Выпуск оснащения из существующего сырья | engine | [US-0002-TK-0001](EP-0021-US-0002-TK-0001-military-supply-schema/EP-0021-US-0002-TK-0001-military-supply-schema.md) | AC-02 |
| [US-0002-TK-0003](EP-0021-US-0002-TK-0003-supply-content/EP-0021-US-0002-TK-0003-supply-content.md) | Стартовые объёмы и профильные партии | content-data | [US-0002-TK-0001](EP-0021-US-0002-TK-0001-military-supply-schema/EP-0021-US-0002-TK-0001-military-supply-schema.md), [US-0009-TK-0001](../EP-0021-US-0009-station-shipyard/EP-0021-US-0009-TK-0001-shipyard-content/EP-0021-US-0009-TK-0001-shipyard-content.md), [US-0001-TK-0001](../EP-0021-US-0001-trade-combat-equipment/EP-0021-US-0001-TK-0001-equipment-skus/EP-0021-US-0001-TK-0001-equipment-skus.md), [US-0011-TK-0001](../EP-0021-US-0011-carrier-campaign-continuity/EP-0021-US-0011-TK-0001-carrier-save-identity/EP-0021-US-0011-TK-0001-carrier-save-identity.md) | AC-01, AC-02 |
| [US-0002-TK-0004](EP-0021-US-0002-TK-0004-nearby-outfit-route/EP-0021-US-0002-TK-0004-nearby-outfit-route.md) | Размещение конечного стартового ассортимента | engine | [US-0002-TK-0003](EP-0021-US-0002-TK-0003-supply-content/EP-0021-US-0002-TK-0003-supply-content.md), [US-0008-TK-0002](../EP-0021-US-0008-engine-only-start/EP-0021-US-0008-TK-0002-docked-start-bootstrap/EP-0021-US-0008-TK-0002-docked-start-bootstrap.md), [US-0010-TK-0001](../EP-0021-US-0010-starter-pilot-quarters/EP-0021-US-0010-TK-0001-starter-pilot-offers/EP-0021-US-0010-TK-0001-starter-pilot-offers.md) | AC-01 |
| [US-0002-TK-0005](EP-0021-US-0002-TK-0005-outfit-route-guide/EP-0021-US-0002-TK-0005-outfit-route-guide.md) | Маршрут закупки и чеклист | client | [US-0002-TK-0004](EP-0021-US-0002-TK-0004-nearby-outfit-route/EP-0021-US-0002-TK-0004-nearby-outfit-route.md), [US-0009-TK-0005](../EP-0021-US-0009-station-shipyard/EP-0021-US-0009-TK-0005-shipyard-station-screen/EP-0021-US-0009-TK-0005-shipyard-station-screen.md), [US-0010-TK-0003](../EP-0021-US-0010-starter-pilot-quarters/EP-0021-US-0010-TK-0003-pilot-hire-screen/EP-0021-US-0010-TK-0003-pilot-hire-screen.md) | AC-03 |

## Decisions and review log

- 2026-11-09T11:46:33Z — Зафиксировано указание подготовить все истории и тикеты без промежуточных подтверждений. Полный текст и границы продуктовых решений приведены в [эпике](../Documentation.md).

## Gaps and backlog

Численные гипотезы и остаточные риски из [эпика](../Documentation.md) подтверждаются будущими реализацией и проверками. Проверки, указанные в тикетах, запланированы.
