---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0005-snapshot-privacy"
title: "Сокрытие информации"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0018-legacy-combat-cleanup"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0005 — Сокрытие информации

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я не могу узнать то, чего не знает мой корабль: где взорвётся чужая торпеда и как устроен вражеский корабль.

**Acceptance criteria**
- **US5-AC1:** О чужих снарядах клиент получает только `ObservedProjectileSnapshot`. Точки и времени подрыва в снапшоте нет.
- **US5-AC2:** О вражеских кораблях клиент получает `EnemyShipStatusSnapshot` вместо `ModuleHealth`. Состояние ИИ (`HostileCombat`) передаётся как сейчас (O12).
- **US5-AC3:** Тест сериализует снапшот боя с активными чужими торпедами и врагами и проверяет, что в JSON нет точки подрыва, времени подрыва и модулей врага.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0017-event-and-command-integration. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 37 / [EP-0012-US-0005-TK-0001-snapshot-masking](EP-0012-US-0005-TK-0001-snapshot-masking/EP-0012-US-0005-TK-0001-snapshot-masking.md) | engine | EP-0012-US-0017-TK-0002-engine-report-producers | US5-AC1, US5-AC2 | 5 |
| 38 / [EP-0012-US-0005-TK-0002-hidden-fields-leak-test](EP-0012-US-0005-TK-0002-hidden-fields-leak-test/EP-0012-US-0005-TK-0002-hidden-fields-leak-test.md) | engine | EP-0012-US-0005-TK-0001-snapshot-masking | US5-AC3 | 2 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
