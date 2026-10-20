---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0010-tactical-map"
title: "Тактическая карта"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0005-snapshot-privacy", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0017-event-and-command-integration", "EP-0012-US-0018-legacy-combat-cleanup", "EP-0012-US-0019-scenario-and-client-regressions"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0010 — Тактическая карта

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я вижу на карте всё, что нужно для решения: свои торпеды с кругом поражения, трассы чужих, кольца дальности и путь своего корабля. Все обязательные требования перечислены ниже.

**Acceptance criteria**
- **US10-AC1:** Отрисовка по UI-02-R3…R6:
  - своя торпеда: трасса, круг R с ядром, отсчёт до подрыва;
  - чужая: продлённая трасса и подпись профиля;
  - противоракета: малый круг;
  - взрывы торпеды и противоракеты, эффект «сбита».
  - Физический круг R точен до1px; minimum6px — отдельный marker при малом масштабе.
- **US10-AC2:** Кольца дальности торпеды (70 км) и ПРО (30 км) в бою видны постоянно. Каждое включается и выключается переключателем в панели своего модуля, состояние хранится в сессии (UI-02-R2, UI-08-R6).
- **US10-AC3:**
  - Вектор скорости и прогноз пути своего корабля на 5 с физики (дуга по текущей команде поворота).
  - Клавиша F («бой в кадре») подгоняет зум.
- **US10-AC4:** Цвета и стили боя — только из `Data/UI/combat-visuals.json`.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0019-scenario-and-client-regressions. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 30 / [EP-0012-US-0010-TK-0001-combat-visuals-v3](EP-0012-US-0010-TK-0001-combat-visuals-v3/EP-0012-US-0010-TK-0001-combat-visuals-v3.md) | client | EP-0012-US-0019-TK-0003-client-forecast-cache-migration | US10-AC4 | 4 |
| 31 / [EP-0012-US-0010-TK-0002-projectiles-and-blasts-render](EP-0012-US-0010-TK-0002-projectiles-and-blasts-render/EP-0012-US-0010-TK-0002-projectiles-and-blasts-render.md) | client | EP-0012-US-0010-TK-0001-combat-visuals-v3 | US10-AC1 | 4 |
| 32 / [EP-0012-US-0010-TK-0003-persistent-range-rings](EP-0012-US-0010-TK-0003-persistent-range-rings/EP-0012-US-0010-TK-0003-persistent-range-rings.md) | client | EP-0012-US-0010-TK-0002-projectiles-and-blasts-render | US10-AC2 | 4 |
| 33 / [EP-0012-US-0010-TK-0004-own-path-forecast-and-fit](EP-0012-US-0010-TK-0004-own-path-forecast-and-fit/EP-0012-US-0010-TK-0004-own-path-forecast-and-fit.md) | client | EP-0012-US-0010-TK-0003-persistent-range-rings | US10-AC3 | 4 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
