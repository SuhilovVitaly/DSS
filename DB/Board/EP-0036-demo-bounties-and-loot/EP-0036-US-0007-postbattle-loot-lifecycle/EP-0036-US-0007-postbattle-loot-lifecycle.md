---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0007-postbattle-loot-lifecycle
title: Самоуничтожение и фаза лута на тактической карте
stage: draft
plan_status: draft
current_review: plan-review
revision: 1
created: 2026-11-17T19:21:02Z
priority: P0
workstream: feature
dependencies: []
implementation_status: complete
planning_status: decomposed
---

# Самоуничтожение и фаза лута на тактической карте

## Связь с техническим заданием

[Эпик, точные решения и supersession](../Documentation.md). Актуальный груминг D-36-02…28 заменяет прежнюю простую пакетную добычу. Production не изменялся; stage draft сохраняет границу между готовыми planning artifacts и реализацией/приёмкой.

## User story

Как игрок, я вижу, что вражеский носитель самоуничтожается после потери всех собственных дронов и оставляет врек. После подтверждённой победы я остаюсь на тактической карте и выбираю остатки для сбора. Мои боевые потери уже учтены, а повтор выхода не применяет их ещё раз. При выходе я получаю предупреждение об оставленном; сохранение во время сбора возвращает меня к тому же состоянию лута.

## Grounding

src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs:150–175 — текущий exit применяет settlement и очищает session; SortieSettlement.cs:17,63 — idempotent guard/encounter settlement; DemoCampaign.cs:40 — текущий BattleActive gate; LocalGameSessionConnection.cs:207,234 — checkpoint/commands; TacticalBattleScreen.cs:210,349 — exit/input. Новая LootOpen фаза проектируется здесь, а не утверждается существующей.

## Acceptance criteria / Completion evidence

- **AC-01:** Каждый enemy carrier проверяет собственные OriginCarrierId дроны, Destroyed/Disabled всех => selfdestruct wreck; соседний carrier с active wing не гибнет. Disabled пилоты/дроны не уничтожаются, AoE не вводится.
- **AC-02:** Confirmed victory атомарно commits sortie/encounter и материализует frozen source IDs/poses/loot results до открытия LootOpen. Exit/Load не повторяют settlement или генерацию; defeat/abort не создают лут.
- **AC-03:** На тактической карте клики по sources без подлёта открывают единое окно US3/US6/US9; все пути выхода проходят предупреждение/подтверждение, discard closes навсегда.
- **AC-04:** Checkpoint сохраняет frozen battlefield, попытки/остатки/receipt; Load возобновляет LootOpen без активного боя и без prebattle replay. Ошибка storage не выдаётся за успешное durable сохранение.

## Non-goals и ownership

US7 владеет schema загрузки loot.json, самоуничтожением, фазой победа→loot→exit, map input и checkpoint transport. Подробные loot правила US3/US6, crew US8, prisoner US9 — отдельные владельцы. Story dependency aggregation не является all-story барьером: TK1 schema исполняется до US3/6 content, TK3/4 — после их runtime/panels. Ticket graph ниже — единственный порядок интеграции; циклической зависимости по завершению story не вводить.

## Dependencies и порядок

Фазы schema → content/runtime US3/6/8 → lifecycle → UI/continuation. Конкретные ticket prerequisites перечислены ниже; внешняя мировая battle model уже принадлежит EP-0035.

## Approved ticket map

Planning map, не human approval implementation:
| ID | Layer | Dependencies | Criteria |
|---|---|---|---|
| [EP-0036-US-0007-TK-0001-loot-schema](EP-0036-US-0007-TK-0001-loot-schema/EP-0036-US-0007-TK-0001-loot-schema.md) | engine | EP-0036-US-0003-TK-0001-contracts | AC-01…04, вклад owning layer |
| [EP-0036-US-0007-TK-0002-carrier-selfdestruct](EP-0036-US-0007-TK-0002-carrier-selfdestruct/EP-0036-US-0007-TK-0002-carrier-selfdestruct.md) | engine | none | AC-01…04, вклад owning layer |
| [EP-0036-US-0007-TK-0003-postbattle-lifecycle](EP-0036-US-0007-TK-0003-postbattle-lifecycle/EP-0036-US-0007-TK-0003-postbattle-lifecycle.md) | engine | EP-0036-US-0003-TK-0003-engine, EP-0036-US-0006-TK-0003-engine, EP-0036-US-0007-TK-0002-carrier-selfdestruct | AC-01…04, вклад owning layer |
| [EP-0036-US-0007-TK-0004-postbattle-map](EP-0036-US-0007-TK-0004-postbattle-map/EP-0036-US-0007-TK-0004-postbattle-map.md) | client | EP-0036-US-0007-TK-0003-postbattle-lifecycle, EP-0036-US-0003-TK-0004-client, EP-0036-US-0006-TK-0004-client | AC-01…04, вклад owning layer |
| [EP-0036-US-0007-TK-0005-postbattle-continuation](EP-0036-US-0007-TK-0005-postbattle-continuation/EP-0036-US-0007-TK-0005-postbattle-continuation.md) | local-client | EP-0036-US-0007-TK-0003-postbattle-lifecycle | AC-01…04, вклад owning layer |

## Decisions и Review log

Точные ответы пользователя с UTC временем записи хранятся в эпике D-36-01…28; не копировать несовместимые ранние A как активное правило. 2026-11-17T19:21:02Z — переработка карты после согласования intact trophy, fixed scrap yield, carrier scrap, common cabin pool и sale-only. Прежние US3 restrictions «без модулей/пленных» заменены распределением по US6/US9. Файлы/ID существующих US3 tickets сохранены; новые US6/7 IDs добавлены без переиспользования.

## Gaps and backlog

- Балансовые content A требуют плейтеста, не блокируют создание документов.
- Новая generic durability/ремонт отдельных trophies и установка трофеев вне текущего scope.
- Native/input/звук/product acceptance OPEN, новые named tests только запланированы.
- Нельзя закрывать общий эпик без engineer US8, custody US9 и интеграционного US4/документационного US5.
- Перед исполнением проверить drift исходников и завершение prerequisite tickets; расширение allowlist только явной Board правкой.

Planning DECOMPOSED / DRAFT; implementation NOT STARTED; automated/native/product NOT RUN.
