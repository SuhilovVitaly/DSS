---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0002-combat-catalog-v3"
title: "Каталог v3"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0002 — Каталог v3

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как дизайнер, я получаю в игре ровно те числа, что проверены симулятором v3.

**Acceptance criteria**
- **US2-AC1:** Значения в данных аппаратов:
  - `module.torpedolauncher.basic`: 70 км, перезарядка 10000 мс, урон 200, бронепробитие 20, разброс 2 %, профили D02;
  - новый `module.torpedolauncher.light` для Мухи: то же, перезарядка 7250 мс;
  - противоракетный аппарат: 30 км, 12500 мс, разброс 8 %, профили D07;
  - `hitDisableMs` = 8000.
- **US2-AC2:** Есть новый модуль `module.engine.tetrarch` со значениями D12, Стриж обновлён по D12, `module.engine.basic` не изменился. Тест на загрузку проверяет каждое число.
- **US2-AC3:** В данных нет `fire-modes.json`, команд `fire`, `setMode`, `selfDestruct`, `enable` и `disable`. Есть `commands.json` для новых команд.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0017-event-and-command-integration. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 25 / [EP-0012-US-0002-TK-0001-torpedo-launchers-v3](EP-0012-US-0002-TK-0001-torpedo-launchers-v3/EP-0012-US-0002-TK-0001-torpedo-launchers-v3.md) | content-data | EP-0012-US-0008-TK-0003-encounter-stats | US2-AC1 | 2 |
| 26 / [EP-0012-US-0002-TK-0002-countermissile-launcher-v3](EP-0012-US-0002-TK-0002-countermissile-launcher-v3/EP-0012-US-0002-TK-0002-countermissile-launcher-v3.md) | content-data | EP-0012-US-0002-TK-0001-torpedo-launchers-v3 | US2-AC1 | 2 |
| 27 / [EP-0012-US-0002-TK-0003-engines-v3](EP-0012-US-0002-TK-0003-engines-v3/EP-0012-US-0002-TK-0003-engines-v3.md) | content-data | EP-0012-US-0002-TK-0002-countermissile-launcher-v3 | US2-AC2 | 2 |
| 28 / [EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands](EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands/EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands.md) | content-data | EP-0012-US-0002-TK-0003-engines-v3 | US2-AC3 | 5 |
| 29 / [EP-0012-US-0002-TK-0005-combat-rules-v3](EP-0012-US-0002-TK-0005-combat-rules-v3/EP-0012-US-0002-TK-0005-combat-rules-v3.md) | content-data | EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands | US1-AC4, US2-AC1 | 2 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
