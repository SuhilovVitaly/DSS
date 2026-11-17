---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0003-simple-loot
title: Пообъектный сбор расходников и металлолома
stage: draft
plan_status: draft
current_review: plan-review
revision: 5
created: 2026-11-17T19:21:02Z
priority: P0
workstream: feature
dependencies: [EP-0035-US-0003-world-battle]
implementation_status: implemented-tested
planning_status: decomposed
---

# Пообъектный сбор расходников и металлолома

## Связь с техническим заданием

[Эпик, точные решения и supersession](../Documentation.md). Актуальный груминг D-36-02…28 заменяет прежнюю простую пакетную добычу. Production не изменялся; stage draft сохраняет границу между готовыми planning artifacts и реализацией/приёмкой.

## User story

Как игрок, после победы я кликаю по отдельному дрону или вреку на тактической карте и забираю его добычу без подлёта. С выведенного из строя дрона я могу забрать сохранившиеся ракеты и ловушки отдельными действиями. С каждого уничтоженного дрона и самоуничтожившегося носителя я один раз пытаюсь собрать фиксированное количество металлолома. Выход с предупреждением завершает сбор; история не создаёт отдельный inventory или второй денежный баланс.

## Grounding

src/DeepSpaceSaga.Engine/SimulationEngine.CargoCost.cs:22 — AddCargoCost сохраняет неизвестный basis; src/DeepSpaceSaga.Contracts/CargoStackSnapshot.cs:11 — quantity и масса через item type; src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs:64,70 — DecoyCharges/AmmoCurrent. Начальная модель общего loot offer по battleId из старых тикетов superseded.

## Acceptance criteria / Completion evidence

- **AC-01:** Каждый уничтоженный drone wreck и selfdestruct carrier wreck предоставляет одну попытку лома: 25% без инженера, 35–40% с инженером. Выход фиксирован по типу в content; носитель даёт только лом.
- **AC-02:** Disabled-дрон даёт реальные остатки расходников; один 50%-бросок на весь запас каждого типа решает уничтожение. Уничтоженное скрыто, оставшееся собирается отдельными кнопками; масса проверяется атомарно.
- **AC-03:** Результаты/источники/остатки/attempt flags сохраняются и не reroll при повторе/Load. Cargo provenance и unknown basis сохраняются; scrap имеет конечного покупателя.
- **AC-04:** Общий panel показывает выбранный объект и подтверждённые результаты; реальный map/exit/restore путь подключается US7, модули US6, пилот US9. Выход с подтверждением оставляет остаток.

## Non-goals и ownership

US3 владеет scrap commodity, остатками расходников, общим persisted loot ledger и shared DTO. Не владеет шансом/предметами демонтажа US6, ролью инженера US8, пленными US9 или input lifecycle US7. Прежняя пакетная случайная добыча на группу заменена правилами на каждый объект. Количество лома фиксировано в content, A baseline указан в TK2; цифры — для плейтеста.

## Dependencies и порядок

Мировой бой EP-0035-US-0003 предоставляет фактические состояния. Schema US7 TK1 нужна content TK2; Engineer US8 TK2 нужен runtime TK3. US7 поздняя интеграция зависит от готовых US3 panels, поэтому не трактовать US7 целиком как prerequisite US3.

## Approved ticket map

Planning map, не human approval implementation:
| ID | Layer | Dependencies | Criteria |
|---|---|---|---|
| [EP-0036-US-0003-TK-0001-contracts](EP-0036-US-0003-TK-0001-contracts/EP-0036-US-0003-TK-0001-contracts.md) | contracts | none | AC-01…04, вклад owning layer |
| [EP-0036-US-0003-TK-0002-content](EP-0036-US-0003-TK-0002-content/EP-0036-US-0003-TK-0002-content.md) | content-data | EP-0036-US-0007-TK-0001-loot-schema | AC-01…04, вклад owning layer |
| [EP-0036-US-0003-TK-0003-engine](EP-0036-US-0003-TK-0003-engine/EP-0036-US-0003-TK-0003-engine.md) | engine | EP-0036-US-0003-TK-0002-content, EP-0036-US-0008-TK-0002-identity-and-skill | AC-01…04, вклад owning layer |
| [EP-0036-US-0003-TK-0005-localized-feedback](EP-0036-US-0003-TK-0005-localized-feedback/EP-0036-US-0003-TK-0005-localized-feedback.md) | content-data | EP-0036-US-0003-TK-0001-contracts | AC-01…04, вклад owning layer |
| [EP-0036-US-0003-TK-0004-client](EP-0036-US-0003-TK-0004-client/EP-0036-US-0003-TK-0004-client.md) | client | EP-0036-US-0003-TK-0005-localized-feedback, EP-0036-US-0003-TK-0003-engine | AC-01…04, вклад owning layer |

## Decisions и Review log

Точные ответы пользователя с UTC временем записи хранятся в эпике D-36-01…28; не копировать несовместимые ранние A как активное правило. 2026-11-17T19:21:02Z — переработка карты после согласования intact trophy, fixed scrap yield, carrier scrap, common cabin pool и sale-only. Прежние US3 restrictions «без модулей/пленных» заменены распределением по US6/US9. Файлы/ID существующих US3 tickets сохранены; новые US6/7 IDs добавлены без переиспользования.

## Gaps and backlog

- Балансовые content A требуют плейтеста, не блокируют создание документов.
- Новая generic durability/ремонт отдельных trophies и установка трофеев вне текущего scope.
- Native/input/звук/product acceptance OPEN, новые named tests только запланированы.
- Нельзя закрывать общий эпик без engineer US8, custody US9 и интеграционного US4/документационного US5.
- Перед исполнением проверить drift исходников и завершение prerequisite tickets; расширение allowlist только явной Board правкой.

Planning DECOMPOSED / DRAFT; implementation NOT STARTED; automated/native/product NOT RUN.
