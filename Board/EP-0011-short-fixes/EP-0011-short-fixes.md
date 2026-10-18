---
epic: EP-0011-short-fixes
title: Короткие фиксы
stage: implemented
created: 2026-10-18T07:47:19+00:00
revision: 2
---

# EP-0011 — Короткие фиксы

Текущий запрос выполнен в scope US-0001 и обязательной завершающей US-0002 документации по EpicExecutionPrompt.md. Несвязанные фиксы не добавлялись. 2 истории / 4 тикета. Implementation и automated validation завершены; native/human acceptance OPEN / NOT RUN, игру не запускали. Это не полная игровая приёмка эпика.

## Истории

| Story | Результат | Тикеты |
|---|---|---|
| [US-0001 — Наблюдаемый прогноз NPC](EP-0011-US-0001-observed-npc-trajectories/EP-0011-US-0001-observed-npc-trajectories.md) | Два равных секундных поворота подтверждают дугу; иначе прямая | 3 |
| [US-0002 — Документация](EP-0011-US-0002-project-documentation/EP-0011-US-0002-project-documentation.md) | Документы, Board/evidence, Graphify | 1 |

## Scope и статус

Client observation history, собственная future line известных живых NPC и tests. Engine/Contracts/Motion, AI/RNG, saves, guidance, combat/launch forecasts и игрок не изменены. Финальная документация уточняет соответствующую границу, не вводит новые игровые правила. Историческое планирование 2026-10-18T07:47:19+00:00 не было разрешением реализации; текущий пользовательский запрос отдельно разрешил реализацию, Board, per-ticket commit/push.

[Тикеты и зависимости](Tickets.md) · [grounding D-01–D-03 / A-01–A-08](Documentation.md) · [execution, SHA и открытые gates](ImplementationStatus.md) · [текущий контракт](../../Documentation/04-Engineering/NpcObservedTrajectories.md).
