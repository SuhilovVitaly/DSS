---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0017-event-and-command-integration"
title: "Контракты событий, серверные доклады и доставка"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 2
---

# US-0017 — Контракты событий, серверные доклады и доставка

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)

**User story.** Как игрок, я получаю новые боевые события и результаты по существующей публичной границе Engine → LocalClient → Client.

**Acceptance criteria**
- **US17-AC1:** DTO journal/reports представляют blast и shotdown, скрывают чужие module/damage/endpoint/deadline.
- **US17-AC2:** Engine производит доклады EP-0010 по фактическим событиям, сохраняет sequence, captured speaker и once marks.
- **US17-AC3:** LocalClient доставляет encounter/projectile/event payload без изменения порядка, receipts обходят scene clock, compact removal остаётся явным.
- **US17-AC4:** Контракты continuous turn и подтверждения итогов доступны до серверного/UI использования.


## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

При исполнении prerequisite contract/schema US1 уже опубликован в пяти тикетах; его downstream legacy cleanup не является prerequisite добавления event DTO. Порядок сохраняется по точному ticket DAG. Closure всей US1 и всей US17 остаётся OPEN до интеграции, а не выводится из готовности отдельных contracts.

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0001-contracts-and-schema, EP-0012-US-0005-snapshot-privacy, EP-0012-US-0018-legacy-combat-cleanup. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 6 / [EP-0012-US-0017-TK-0001-combat-event-contracts](EP-0012-US-0017-TK-0001-combat-event-contracts/EP-0012-US-0017-TK-0001-combat-event-contracts.md) | contracts-and-localization | EP-0012-US-0001-TK-0005-launcher-profile-schema | US17-AC1 | 6 |
| 7 / [EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts](EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts/EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts.md) | contracts | EP-0012-US-0017-TK-0001-combat-event-contracts | US17-AC4 | 6 |
| 25 / [EP-0012-US-0017-TK-0002-engine-report-producers](EP-0012-US-0017-TK-0002-engine-report-producers/EP-0012-US-0017-TK-0002-engine-report-producers.md) | engine | EP-0012-US-0018-TK-0003-point-combat-forecasts | US17-AC2, US13-AC4 | 5 |
| 28 / [EP-0012-US-0017-TK-0003-local-client-delivery](EP-0012-US-0017-TK-0003-local-client-delivery/EP-0012-US-0017-TK-0003-local-client-delivery.md) | local-client | EP-0012-US-0005-TK-0002-hidden-fields-leak-test | US17-AC3 | 3 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
