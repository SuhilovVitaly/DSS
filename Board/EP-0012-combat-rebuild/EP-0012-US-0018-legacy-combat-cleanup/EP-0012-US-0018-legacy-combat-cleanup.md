---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0018-legacy-combat-cleanup"
title: "Удаление старой боевой модели и регрессии"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0018 — Удаление старой боевой модели и регрессии

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)

**User story.** Как разработчик, я получаю согласованные runtime, forecasts и тесты после замены старой боевой модели.

**Acceptance criteria**
- **US18-AC1:** Старые hit/intercept/mode helpers и их tests удалены либо заменены геометрическими проверками; в production нет самонаведения, бросков, auto-PRO, burst/coast и breakdown.
- **US18-AC2:** Engine preview/forecast используют новую линейную модель и не рассчитывают скрытые чужие подрывы.
- **US18-AC3:** Regression tests с новыми правилами сохраняют ownership, determinism, module damage и idempotency; механическое удаление упавших assertions запрещено.


## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0003-point-aim-torpedoes, EP-0012-US-0004-point-aim-countermissiles, EP-0012-US-0006-threats-and-evasion, EP-0012-US-0008-encounter-and-time. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 10 / [EP-0012-US-0018-TK-0004-shared-point-forecast](EP-0012-US-0018-TK-0004-shared-point-forecast/EP-0012-US-0018-TK-0004-shared-point-forecast.md) | contracts-motion-engine | EP-0012-US-0006-TK-0001-blast-threat-math | US18-AC2 | 6 |
| 24 / [EP-0012-US-0018-TK-0003-point-combat-forecasts](EP-0012-US-0018-TK-0003-point-combat-forecasts/EP-0012-US-0018-TK-0003-point-combat-forecasts.md) | engine | EP-0012-US-0008-TK-0003-encounter-stats | US18-AC2 | 3 |
| 62 / [EP-0012-US-0018-TK-0001-remove-interception-helper](EP-0012-US-0018-TK-0001-remove-interception-helper/EP-0012-US-0018-TK-0001-remove-interception-helper.md) | engine | EP-0012-US-0004-TK-0003-remove-auto-pd | US18-AC1 | 4 |
| 63 / [EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests](EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests/EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests.md) | engine | EP-0012-US-0018-TK-0001-remove-interception-helper | US18-AC1, US18-AC3 | 4 |
| 65 / [EP-0012-US-0018-TK-0005-module-combat-regressions](EP-0012-US-0018-TK-0005-module-combat-regressions/EP-0012-US-0018-TK-0005-module-combat-regressions.md) | engine | EP-0012-US-0003-TK-0005-remove-homing-roll-modes | US18-AC3, US3-AC5, US4-AC3 | 4 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
