---
epic: EP-0011-short-fixes
story: EP-0011-US-0003-tactical-map-palette
title: Читаемая палитра тактической карты
stage: approved
dependencies: []
created: 2026-10-18T10:03:20Z
source_request: "сделай из этого тикет в боарде и последнем эпике"
current_review: not-reviewed
revision: 1
---

# US-0003 — Читаемая палитра тактической карты

Как игрок, я хочу различать объекты, оружие, защиту, реальные следы и прогнозы по согласованной палитре. Полный RGB/HSL/opacity контракт хранится в TK-0001. Изначальное planning-only поручение заменено прямым D-P03: «Да, подготовь исполнительные тикеты в Board и сразу выполни их с проверками. Без commit/push и запуска игры.» Источник — текущий чат, 2026-10-18; точное время сообщения неизвестно.

## Критерии приёмки

- AC-01: все объекты, линии и дополнительные слои используют RGB и непрозрачность из таблиц тикета; существующие видимые слои не пропущены.
- AC-02: корабли и подписи читаются на чёрном фоне; название нейтрально-светлое, принадлежность показана маркером и цветовой полоской. Рамка выделения не перекрашивает объект.
- AC-03: навигация синяя, торпеды янтарные, защита бирюзовая, preview лиловый; сплошные реальные следы и маршруты отличимы от пунктирных прогнозов, прогноз контакта обозначен крестиком. Геометрия и доступность прогнозов сохранены.
- AC-04: фон, сетка, орбиты, поля и радиусы визуально вторичны; градиенты следов/взрывов соблюдают заданные конечные значения, след торпеды остаётся скрытым.
- AC-05: Client-only изменения не меняют simulation, AI, RNG, Motion/Contracts, save, timing, прогнозную математику, pause/load и факт контакта. Согласованы runtime-настройки и fallback.
- AC-06: automated и native/product проверки имеют отдельные результаты; визуальная приёмка проводится по повторным скриншотам сопоставимых сцен с исходными.

## Approved ticket map

| Ticket | Layer | Dependencies | Status |
|---|---|---|---|
| [TK-0001 — продуктовый контракт](EP-0011-US-0003-TK-0001-tactical-map-color-redesign/EP-0011-US-0003-TK-0001-tactical-map-color-redesign.md) | client | none | approved aggregate; full native/product OPEN |
| [EP-0011-US-0003-TK-0002-object-and-combat-palette](EP-0011-US-0003-TK-0002-object-and-combat-palette/EP-0011-US-0003-TK-0002-object-and-combat-palette.md) | client | `[]` | implementation DONE; automated PASS; native/product OPEN |
| [EP-0011-US-0003-TK-0003-navigation-and-trails](EP-0011-US-0003-TK-0003-navigation-and-trails/EP-0011-US-0003-TK-0003-navigation-and-trails.md) | client | `[EP-0011-US-0003-TK-0002-object-and-combat-palette, EP-0011-US-0004-TK-0001-background-layer-routing]` | implementation DONE; automated PASS; native/product OPEN |
| [EP-0011-US-0003-TK-0004-combat-role-routing](EP-0011-US-0003-TK-0004-combat-role-routing/EP-0011-US-0003-TK-0004-combat-role-routing.md) | client | `[EP-0011-US-0003-TK-0002-object-and-combat-palette]` | implementation DONE; automated PASS; native/product OPEN |
| [EP-0011-US-0003-TK-0005-labels-and-grid](EP-0011-US-0003-TK-0005-labels-and-grid/EP-0011-US-0003-TK-0005-labels-and-grid.md) | client | `[EP-0011-US-0003-TK-0002-object-and-combat-palette, EP-0011-US-0004-TK-0001-background-layer-routing]` | implementation DONE; automated PASS; native/product OPEN |

## Non-goals / Invariants
Engine/Contracts/Motion/save/AI/RNG/timing, прогнозная математика, geometry/visibility и неизвестное знание игрока не меняются. Данные траекторий торпед сохраняются, линии скрыты. Runtime JSON и fallback совпадают. Никаких изменений layout/HUD или соседних фич.

## Dependencies / gaps
Background и compatibility часть того же исходного результата вынесена в [US-0004](../EP-0011-US-0004-map-background-palette/EP-0011-US-0004-map-background-palette.md) ради лимита пяти тикетов; зависимости задаются точными TK IDs, циклов нет. Native screenshot comparison/product readability OPEN, FPS не измерялся. Full scope не объявлен принятым человеком.

## Execution
Client focused175/175, full2085/2085; build0 warnings/errors; diff PASS. Scoped format: все кроме прежних9 WHITESPACE diagnostics GridRendererTests PASS. [Команды, evidence и ограничения](../ImplementationStatus.md#palette-execution--2026-10-18). Commit/push/игра не выполнялись. Существующие чужие Client/Tests/scenario изменения сохранены.
