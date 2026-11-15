---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0008-rearm-between-battles
title: Перезарядка дронов из грузового трюма
stage: draft
dependencies: [US-0007]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Перезарядка дронов из грузового трюма

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Между боями игрок выбирает дрон и пополняет его магазин из реального груза корабля. Он видит количество до подтверждения, остаток в трюме и причину отказа. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Rearm принимает DroneId, quantity>0 и expected revision; переносит ровно quantity из доступных исправных cargo containers в магазин без превышения capacity.
- **AC-02**: При недостатке груза/места, активном бою, уничтоженном дроне/носителе, неработающем ангаре, stale revision операция атомарно отклоняется. Disabled drone не ремонтируется перезарядкой.
- **AC-03**: Повтор CommandId не списывает повторно, conflict ID с другим payload отклоняется; receipt сохраняется после Load.
- **AC-04**: Из 42 торпед пустое крыло 12+12+6+6 перезаряжается до полного и оставляет 6. Следующий новый бой не пополняет магазины автоматически.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0007 — Сохранение корабля, потерь и боезапаса](../EP-0029-US-0007-save-continuity/EP-0029-US-0007-save-continuity.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Запрос и результат перезарядки](EP-0029-US-0008-TK-0001-rearm-contract/EP-0029-US-0008-TK-0001-rearm-contract.md) | contracts | [US-0007-TK-0003](../EP-0029-US-0007-save-continuity/EP-0029-US-0007-TK-0003-catalog-compatibility/EP-0029-US-0007-TK-0003-catalog-compatibility.md) | AC-01, AC-02, AC-03 |
| [TK-0002 — Транзакция переноса торпед](EP-0029-US-0008-TK-0002-rearm-engine/EP-0029-US-0008-TK-0002-rearm-engine.md) | engine | [US-0008-TK-0001](EP-0029-US-0008-TK-0001-rearm-contract/EP-0029-US-0008-TK-0001-rearm-contract.md) | AC-01, AC-02, AC-03, AC-04 |
| [TK-0003 — Асинхронный transport перезарядки](EP-0029-US-0008-TK-0003-rearm-transport/EP-0029-US-0008-TK-0003-rearm-transport.md) | local-client | [US-0008-TK-0002](EP-0029-US-0008-TK-0002-rearm-engine/EP-0029-US-0008-TK-0002-rearm-engine.md) | AC-01, AC-02, AC-03 |
| [TK-0004 — Клиентское выполнение и pending](EP-0029-US-0008-TK-0004-rearm-client-handle/EP-0029-US-0008-TK-0004-rearm-client-handle.md) | client | [US-0008-TK-0003](EP-0029-US-0008-TK-0003-rearm-transport/EP-0029-US-0008-TK-0003-rearm-transport.md) | AC-01, AC-02, AC-03 |

## Completion evidence

- AC-01: TK-0001/0002/0003/0004 — `RequestAndReceiptRoundTrip`, `FortyTwoRefillsThirtySixLeavesSix`, `LocalRearmMatchesEngineReceipt` — PASS.
- AC-02: TK-0001/0002/0003/0004 — `InsufficientCargoIsAtomic`, `BattleAndDestroyedReject`, `CancelledBeforeSendDoesNotSpend` — PASS.
- AC-03: TK-0001/0002/0003/0004 — `DuplicateIdentityNoDoubleTransfer`, `RetryAfterLostReplyKeepsOneTransfer`, `LostResponseRetryKeepsIdentity` — PASS.
- AC-04: TK-0002 — `FortyTwoRefillsThirtySixLeavesSix`, `PartialReloadAndBasisConserved` — PASS.
- Artifacts: `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-025..028-*`.
- Implementation/automated: **PASS**. Native/product: **OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
