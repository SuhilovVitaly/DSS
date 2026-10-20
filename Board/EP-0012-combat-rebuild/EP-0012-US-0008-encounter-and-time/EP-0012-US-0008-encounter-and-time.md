---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0008-encounter-and-time"
title: "Боевое столкновение и время"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0008 — Боевое столкновение и время

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я вижу, когда бой начался и закончился, бой идёт на скорости x1, а после победы игра возвращается к прежней скорости.

**Acceptance criteria**
- **US8-AC1:** Состояние боя переходит `Idle → Active` по A01, `Active → Won` при гибели всех враждебных кораблей с ИИ, `Active → Lost` при гибели игрока. После `Won` состояние снова становится `Idle`, когда игрок подтвердит итоги (UI-11) или через 60 с физики без новых врагов.
- **US8-AC2:** Пока бой `Active`, сервер отвергает Speed2+ с кодом `combat_speed_locked`. Speed0 (пауза) и Speed1 разрешены. При переходе в `Active` со Speed2+ скорость становится Speed1, а прежняя запоминается; после боя она восстанавливается.
- **US8-AC3:** Статистика US1-TK3 считается движком. Длительность записывается в физических секундах, реальная получается × 4.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0007-mukha-ai-v3. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 22 / [EP-0012-US-0008-TK-0001-encounter-state](EP-0012-US-0008-TK-0001-encounter-state/EP-0012-US-0008-TK-0001-encounter-state.md) | engine | EP-0012-US-0007-TK-0003-npc-evade-and-reaction | US8-AC1 | 4 |
| 23 / [EP-0012-US-0008-TK-0002-combat-speed-lock](EP-0012-US-0008-TK-0002-combat-speed-lock/EP-0012-US-0008-TK-0002-combat-speed-lock.md) | engine | EP-0012-US-0008-TK-0001-encounter-state | US8-AC2 | 4 |
| 24 / [EP-0012-US-0008-TK-0003-encounter-stats](EP-0012-US-0008-TK-0003-encounter-stats/EP-0012-US-0008-TK-0003-encounter-stats.md) | engine | EP-0012-US-0008-TK-0002-combat-speed-lock | US8-AC3 | 5 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
