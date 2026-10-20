---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0007-mukha-ai-v3"
title: "ИИ Мухи v3"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0006-threats-and-evasion"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0007 — ИИ Мухи v3

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я сражаюсь с Мухами, которые ведут себя как в симуляторе v3: держат орбиту, стреляют с упреждением и иногда уклоняются.

**Acceptance criteria**
- **US7-AC1:** Эксплойт «полный вперёд» исправлен: при игроке на максимальной скорости Мухи выходят на дистанцию пуска не позже чем за 60 с физики (тест на сценарии). Параметры орбиты берутся из сценария (`standoffKm` 25, `bandKm` 8, `flankOffsetDeg` 35).
- **US7-AC2:** NPC стреляет по D13: для каждого профиля считает линейное упреждение, время подлёта τ, ожидаемый промах `0,25 × 0,5 × a_цели × max(0, τ − 1)² + σ` и накрытие по D03. Выбирает лучший профиль; стреляет, если накрытие ≥ 0,3 и рядом с точкой нет своих.
- **US7-AC3:** Уклонение NPC по D14: реакция по формуле из правил, бросок 10 % из потока `npc_dodge` один раз на угрозу, манёвр через исполнитель US6-TK2. Случайная задержка `CombatReactions` (500–1250 мс) больше не используется для боя.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0006-threats-and-evasion. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 18 / [EP-0012-US-0007-TK-0001-orbit-lead-fix](EP-0012-US-0007-TK-0001-orbit-lead-fix/EP-0012-US-0007-TK-0001-orbit-lead-fix.md) | motion | EP-0012-US-0006-TK-0003-continuous-turn-in-combat | US7-AC1 | 3 |
| 19 / [EP-0012-US-0007-TK-0002-npc-aim-and-fire](EP-0012-US-0007-TK-0002-npc-aim-and-fire/EP-0012-US-0007-TK-0002-npc-aim-and-fire.md) | engine | EP-0012-US-0007-TK-0001-orbit-lead-fix | US7-AC2 | 4 |
| 20 / [EP-0012-US-0007-TK-0003-npc-evade-and-reaction](EP-0012-US-0007-TK-0003-npc-evade-and-reaction/EP-0012-US-0007-TK-0003-npc-evade-and-reaction.md) | engine | EP-0012-US-0007-TK-0002-npc-aim-and-fire | US7-AC3 | 4 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
