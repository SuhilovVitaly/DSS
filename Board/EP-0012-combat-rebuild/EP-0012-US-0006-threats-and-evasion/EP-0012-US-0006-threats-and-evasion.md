---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0006-threats-and-evasion"
title: "Угрозы и уклонение"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0006 — Угрозы и уклонение

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я одной командой увожу корабль из зоны взрыва вражеской торпеды, а потом корабль сам возвращается на курс.

**Acceptance criteria**
- **US6-AC1:** `BlastThreatMath` по позиции и скорости корабля, трассе, скорости и R торпеды возвращает:
  - есть ли угроза (A02);
  - время входа в R + 0,5;
  - t_star;
  - вектор наибольшего сближения;
  - опасный участок трассы относительно прогнозного пути корабля.
  - Проверяется аналитическими fixtures из тикета blast-threat-math.
- **US6-AC2:** `maneuver.evade` выполняет D11. Отказы: `unknown_threat` (снаряд не чужой или уже не существует), `engine_disabled`. Базовый курс по A04. Счётчик уклонений растёт один раз на эпизод (как `dodges` в симуляторе).
- **US6-AC3:** В бою поворот работает непрерывно с максимальной скоростью двигателя, пока зажата команда (UI-05-R1). Вне боя шаговый поворот работает как раньше.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0003-point-aim-torpedoes, EP-0012-US-0004-point-aim-countermissiles. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 9 / [EP-0012-US-0006-TK-0001-blast-threat-math](EP-0012-US-0006-TK-0001-blast-threat-math/EP-0012-US-0006-TK-0001-blast-threat-math.md) | motion | EP-0012-US-0003-TK-0001-blast-math | US6-AC1 | 2 |
| 16 / [EP-0012-US-0006-TK-0002-evade-command](EP-0012-US-0006-TK-0002-evade-command/EP-0012-US-0006-TK-0002-evade-command.md) | engine-contracts-motion | EP-0012-US-0004-TK-0002-countermissile-detonation | US6-AC2 | 12 |
| 17 / [EP-0012-US-0006-TK-0003-continuous-turn-in-combat](EP-0012-US-0006-TK-0003-continuous-turn-in-combat/EP-0012-US-0006-TK-0003-continuous-turn-in-combat.md) | contracts-motion-engine | EP-0012-US-0006-TK-0002-evade-command | US6-AC3 | 12 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
