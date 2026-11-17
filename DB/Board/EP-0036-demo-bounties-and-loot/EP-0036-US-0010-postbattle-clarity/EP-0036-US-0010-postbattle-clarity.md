---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0010-postbattle-clarity
title: Понятная пост-боевая фаза и выброс груза
stage: approved
plan_status: executed
approval_kind: workflow
current_review: implementation-review
revision: 1
created: 2026-11-17T19:41:16Z
priority: P0
workstream: feature
dependencies: [EP-0036-US-0007-postbattle-loot-lifecycle, EP-0036-US-0003-simple-loot]
implementation_status: implemented
planning_status: decomposed
source_request: "прочитай нарезанные тикеты эпика и реши что мы забыли и что нужно добавить"
---

# Понятная пост-боевая фаза и выброс груза

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Связь с техническим заданием

[Эпик и решения D-36-34…37](../Documentation.md). История добавлена при сверке декомпозиции 2026-11-17: US7 TK2 самоуничтожение реализовано только в engine и переиспользует обычную гибель носителя, а у игрока нет подсказки, что после победы надо кликать по объектам. D-36-35 добавляет выброс груза в окне лута, потому что в D-36-18 при нехватке места попытка недоступна, а US7 TK3 запрещает посторонние действия во время сбора.

## User story

Как игрок, я вижу в журнале и на карте, что вражеский носитель самоуничтожился, и понимаю, что после победы можно кликнуть по дронам и врекам. Если трюм полон, я могу прямо в окне лута выбросить часть груза и освободить место для трофеев.

## Grounding

- `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:11` — TacticalBattleLogKind без отдельного самоуничтожения; `BattleLogFormatter.cs:41` — Destroyed → Log.CarrierDestroyed.
- `src/DeepSpaceSaga.Engine/SimulationEngine.CargoCost.cs:42` — RemoveCargoCost; механики выброса груза в коде нет (поиск jettison/drop/discard пуст).
- `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs` — OverlayClick, сюда US7 TK4 добавляет ветку LootOpen.

## Acceptance criteria

- **AC-01:** Самоуничтожение носителя пишется в журнал боя отдельной записью и строкой RU/EN, отличной от гибели носителя под огнём; повторный tick или Load не добавляют вторую запись.
- **AC-02:** В LootOpen на карте видна подсказка о сборе и кнопка «Завершить сбор», вреки дронов и носителя подписаны; кнопка использует единый confirm flow US7, в бою подсказки нет.
- **AC-03:** В окне лута игрок выбрасывает выбранное количество cargo stack. Выброшенное исчезает безвозвратно и без оплаты, cost basis уменьшается существующим путём, повтор CommandId и Load не списывают дважды.
- **AC-04:** Выброс доступен только в LootOpen этого BattleId и только для товаров трюма. Пассажиров, пленных, экипаж, топливо и установленное оборудование выбросить нельзя. Освобождённая масса сразу учитывается проверкой CargoFull US3/US6.

## Approved ticket map

Planning map, не human approval implementation:

| ID | Layer | Dependencies | Criteria |
|---|---|---|---|
| [EP-0036-US-0010-TK-0001-selfdestruct-log](EP-0036-US-0010-TK-0001-selfdestruct-log/EP-0036-US-0010-TK-0001-selfdestruct-log.md) | engine | EP-0036-US-0007-TK-0002-carrier-selfdestruct | AC-01 |
| [EP-0036-US-0010-TK-0002-postbattle-presentation](EP-0036-US-0010-TK-0002-postbattle-presentation/EP-0036-US-0010-TK-0002-postbattle-presentation.md) | client | EP-0036-US-0010-TK-0001-selfdestruct-log, EP-0036-US-0007-TK-0004-postbattle-map | AC-01, AC-02 |
| [EP-0036-US-0010-TK-0003-jettison-engine](EP-0036-US-0010-TK-0003-jettison-engine/EP-0036-US-0010-TK-0003-jettison-engine.md) | engine | EP-0036-US-0007-TK-0003-postbattle-lifecycle, EP-0036-US-0003-TK-0003-engine | AC-03, AC-04 |
| [EP-0036-US-0010-TK-0004-jettison-ui](EP-0036-US-0010-TK-0004-jettison-ui/EP-0036-US-0010-TK-0004-jettison-ui.md) | client | EP-0036-US-0010-TK-0003-jettison-engine, EP-0036-US-0007-TK-0004-postbattle-map | AC-03, AC-04 |

## Non-goals

Звук и VFX самоуничтожения, выброс вне фазы лута, мировые контейнеры с выброшенным грузом, продажа груза во время сбора, высадка пленных (D-36-34).

## Gaps and backlog

Тексты подсказки — baseline, приёмку даёт плейтест. Отдельная анимация взрыва — backlog. Native/product acceptance OPEN.

Planning DECOMPOSED / DRAFT; implementation NOT STARTED; automated/native/product NOT RUN.
