---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0006-module-salvage
title: Демонтаж целых продаваемых модулей
stage: approved
plan_status: executed
current_review: implementation-review
revision: 5
created: 2026-11-17T19:21:02Z
priority: P0
workstream: feature
dependencies: [EP-0036-US-0003-simple-loot]
implementation_status: implemented
planning_status: decomposed
---

# Демонтаж целых продаваемых модулей

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Связь с техническим заданием

[Эпик, точные решения и supersession](../Documentation.md). Актуальный груминг D-36-02…28 заменяет прежнюю простую пакетную добычу. Production не изменялся; stage draft сохраняет границу между готовыми planning artifacts и реализацией/приёмкой.

## User story

Как игрок, после победы я выбираю на тактической карте Disabled-дрон и вижу его сохранившиеся модули. Для каждого модуля я вижу название, сохранность, примерную цену продажи и шанс демонтажа. Я могу сделать одну попытку, а успех даёт исправный товар в трюм; отдельное повреждение трофея не моделируется. Предмет предназначен для продажи на станциях, установка трофейного оборудования отложена.

## Grounding

src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs:53–64 — реальная комплектация; src/DeepSpaceSaga.Engine/SimulationEngine.DroneReplacement.cs:43 — нынешний EquipDroneCommand покупает station offer, не cargo equipment. Поэтому этот эпик не обещает применение трофея через существующий EquipDroneCommand. Новые commodity mappings US6 TK2 предназначены только для продажи.

## Acceptance criteria / Completion evidence

- **AC-01:** Фактические установленные модули имеют стабильную identity по дрону/category/slot; каждый проходит один 50%-бросок уничтожения до показа. Уничтоженные предметы отсутствуют в таблице; одинаковые два двигателя остаются двумя экземплярами.
- **AC-02:** Одна попытка на module instance: p=(0.25+bonus)*multiplier, bonus0 либо0.10…0.15, weapon0.95,engine1.05,остальные1.0. Неудача терминальна, повтор/смена инженера/Load не дают попытку.
- **AC-03:** При успехе выдать исправный обычный cargo item; полная масса проверяется до броска, CargoFull не тратит попытку/RNG. Catalog mapping обеспечивает цену/массу/конечного покупателя; нет новой trophy durability, ремонта или установки.
- **AC-04:** Таблица показывает название, целый предмет/сохранность, примерную стоимость и authoritative шанс; pending/reject не имитируют успех. Интеграция с реальным map-click/exit US7 и сохранением US4 проверяема.

## Non-goals и ownership

US6 владеет модулями, mapping в продаваемые целые товары и module panel. Не создаёт вторую модель экипажа, денег, инвентаря или повреждений. D-36-11/17 о новой прочности/ремонте superseded D-36-20/21/23. Установка по просьбе пользователя отложена (D-36-28). Предмет не является frame целого дрона: A mapping учитывает actual installed weapon/armor/shield/engine/decoy, chassis не навешанный модуль.

## Dependencies и порядок

US3 shared contracts/runtime, US7 ранняя schema, US8 bonus API. US6 TK1 → TK2 → TK3 → TK4; TK5 (названия товаров) после TK2; schema/content/engine prerequisites точны в таблице.

## Approved ticket map

Planning map, не human approval implementation:
| ID | Layer | Dependencies | Criteria |
|---|---|---|---|
| [EP-0036-US-0006-TK-0001-contracts](EP-0036-US-0006-TK-0001-contracts/EP-0036-US-0006-TK-0001-contracts.md) | contracts | EP-0036-US-0003-TK-0001-contracts | AC-01…04, вклад owning layer |
| [EP-0036-US-0006-TK-0002-content](EP-0036-US-0006-TK-0002-content/EP-0036-US-0006-TK-0002-content.md) | content-data | EP-0036-US-0007-TK-0001-loot-schema, EP-0036-US-0003-TK-0002-content, EP-0036-US-0006-TK-0001-contracts | AC-01…04, вклад owning layer |
| [EP-0036-US-0006-TK-0003-engine](EP-0036-US-0006-TK-0003-engine/EP-0036-US-0006-TK-0003-engine.md) | engine | EP-0036-US-0006-TK-0002-content, EP-0036-US-0003-TK-0003-engine, EP-0036-US-0008-TK-0002-identity-and-skill | AC-01…04, вклад owning layer |
| [EP-0036-US-0006-TK-0004-client](EP-0036-US-0006-TK-0004-client/EP-0036-US-0006-TK-0004-client.md) | client | EP-0036-US-0006-TK-0003-engine, EP-0036-US-0003-TK-0005-localized-feedback | AC-01…04, вклад owning layer |
| [EP-0036-US-0006-TK-0005-trade-names](EP-0036-US-0006-TK-0005-trade-names/EP-0036-US-0006-TK-0005-trade-names.md) | client | EP-0036-US-0006-TK-0002-content, EP-0036-US-0003-TK-0002-content | AC-03, AC-04 (названия scrap/трофеев в торговле и трюме) |

## Decisions и Review log

Точные ответы пользователя с UTC временем записи хранятся в эпике D-36-01…28; не копировать несовместимые ранние A как активное правило. 2026-11-17T19:21:02Z — переработка карты после согласования intact trophy, fixed scrap yield, carrier scrap, common cabin pool и sale-only. Прежние US3 restrictions «без модулей/пленных» заменены распределением по US6/US9. Файлы/ID существующих US3 tickets сохранены; новые US6/7 IDs добавлены без переиспользования.

## Gaps and backlog

- Балансовые content A требуют плейтеста, не блокируют создание документов.
- Новая generic durability/ремонт отдельных trophies и установка трофеев вне текущего scope.
- Native/input/звук/product acceptance OPEN, новые named tests только запланированы.
- Нельзя закрывать общий эпик без engineer US8, custody US9 и интеграционного US4/документационного US5.
- Перед исполнением проверить drift исходников и завершение prerequisite tickets; расширение allowlist только явной Board правкой.

Planning DECOMPOSED / DRAFT; implementation NOT STARTED; automated/native/product NOT RUN.
