---
epic: EP-0011-short-fixes
story: EP-0011-US-0004-map-background-palette
title: Вторичные слои и совместимость палитры тактической карты
stage: approved
dependencies: []
created: 2026-10-18T10:15:17.848355+00:00
current_review: not-reviewed
revision: 1
---

# US-0004 — Вторичные слои и совместимость палитры

Как игрок, я хочу видеть планеты, поля и AI на карте с согласованными приглушёнными цветами. Это исполнительная декомпозиция [полного контракта US-0003](../EP-0011-US-0003-tactical-map-palette/EP-0011-US-0003-tactical-map-palette.md), без нового продукта.

## Acceptance criteria
AC-01–AC-06 исходного TK-0001 применяются к background/planet/AI/edge ролям и совместимости палитры. Intensity clamp0..1; Radiation fill6..12%, border25%; Dust/прочие fill5..10%, border20%; AI fill5%, border25%, без удвоения opacity. Планеты/edge pointers — точные RGB исходной таблицы. Existing render geometry, loaders и scenario behavior остаются проверенными.

## Decisions / Non-goals
D-P03 разрешил декомпозицию и код с проверками; игра, commit/push запрещены. A-P06: отдельная история нужна для лимита пяти тикетов. Общих story dependencies нет: каждый пакет имеет точную межтикетную зависимость. Engine/Contracts/Motion/save/knowledge/visibility неизменны.

## Approved ticket map

| Ticket | Layer | Dependencies | Status |
|---|---|---|---|
| [EP-0011-US-0004-TK-0001-background-layer-routing](EP-0011-US-0004-TK-0001-background-layer-routing/EP-0011-US-0004-TK-0001-background-layer-routing.md) | client | `[EP-0011-US-0003-TK-0002-object-and-combat-palette]` | implementation DONE; automated PASS; native/product OPEN |
| [EP-0011-US-0004-TK-0002-palette-compatibility-regressions](EP-0011-US-0004-TK-0002-palette-compatibility-regressions/EP-0011-US-0004-TK-0002-palette-compatibility-regressions.md) | client | `[EP-0011-US-0003-TK-0002-object-and-combat-palette]` | implementation DONE; automated PASS; native/product OPEN |
| [EP-0011-US-0004-TK-0003-render-palette-regressions](EP-0011-US-0004-TK-0003-render-palette-regressions/EP-0011-US-0004-TK-0003-render-palette-regressions.md) | client | `[EP-0011-US-0003-TK-0003-navigation-and-trails, EP-0011-US-0003-TK-0004-combat-role-routing, EP-0011-US-0003-TK-0005-labels-and-grid]` | implementation DONE; automated PASS; native/product OPEN |

## Evidence / gaps
Client175/175 focused,2085/2085 full; build PASS; native/product OPEN; comparative screenshots и native FPS NOT RUN. [Полные команды](../ImplementationStatus.md#palette-execution--2026-10-18).
