---
epic: EP-0011-short-fixes
title: Короткие фиксы
stage: in-progress
created: 2026-10-18T07:47:19+00:00
revision: 3
---

# EP-0011 — Короткие фиксы

US-0001/US-0002 сохраняют прежние implementation/publication evidence. Палитра US-0003/US-0004 реализована с автоматическими проверками по D-P03, без публикации и запуска игры. Итого4 истории /12 документов тикетов, из них TK-0001 US-0003 — агрегирующий контракт. Native/product acceptance остаётся OPEN, расширенный эпик не объявлен полностью принятым.

## Истории

| Story | Результат | Тикеты |
|---|---|---|
| [US-0001 — Наблюдаемый прогноз NPC](EP-0011-US-0001-observed-npc-trajectories/EP-0011-US-0001-observed-npc-trajectories.md) | Два равных секундных поворота подтверждают дугу; иначе прямая | 3 |
| [US-0002 — Документация](EP-0011-US-0002-project-documentation/EP-0011-US-0002-project-documentation.md) | Документы, Board/evidence, Graphify | 1 |
| [US-0003 — Палитра тактической карты](EP-0011-US-0003-tactical-map-palette/EP-0011-US-0003-tactical-map-palette.md) | Контракт и4 исполнительных пакета палитры | 5 |
| [US-0004 — Вторичные слои и регрессии](EP-0011-US-0004-map-background-palette/EP-0011-US-0004-map-background-palette.md) | Background/planet/AI/edge и совместимость | 3 |

## Scope и статус

Прежний реализованный scope US-0001/US-0002: Client observation history, собственная future line известных живых NPC и tests. Engine/Contracts/Motion, AI/RNG, saves, guidance, combat/launch forecasts и игрок не изменены. Финальная документация уточняет соответствующую границу, не вводит новые игровые правила. Историческое планирование 2026-10-18T07:47:19+00:00 не было разрешением реализации; отдельное прежнее поручение разрешило реализацию US-0001/US-0002, Board и per-ticket commit/push. Текущее D-P03 разрешает исполнение палитры, без commit/push/игры.

[Тикеты и зависимости](Tickets.md) · [grounding D-01–D-03 / A-01–A-08](Documentation.md) · [execution, SHA и открытые gates](ImplementationStatus.md) · [текущий контракт](../../Documentation/04-Engineering/NpcObservedTrajectories.md).

## Palette execution — 2026-10-18

D-P03 разрешил подготовку и выполнение пакетов с проверками; commit/push/игра запрещены. Client2085/2085 full; build PASS. Format baseline limitation и открытые gates: [ImplementationStatus](ImplementationStatus.md#palette-execution--2026-10-18).
