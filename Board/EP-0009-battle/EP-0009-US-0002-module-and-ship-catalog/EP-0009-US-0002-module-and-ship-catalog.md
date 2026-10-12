---
epic: EP-0009-module-combat
story: EP-0009-US-0002-module-and-ship-catalog
title: "Каталог модулей и кораблей"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Каталог модулей и кораблей

**User story.** Как игрок, я получаю модули и корабли с числами, проверенными симуляцией.

**Acceptance criteria**
- **US2-AC1:** Торпедный аппарат, обе ПРО (двух- и одноствольная) и двигатели имеют значения D25–D28 вместо чисел EP-0008 (55/5, 200/100/200 км, 12 км/с, 60 с, 10 с).
- **US2-AC2:** `structurePointsMax` = 150 у всех модулей. Тест проверяет правило 1/2 (D22) для каждого модуля боевых сценариев.
- **US2-AC3:** Классы `ship.mukha` и `ship.shmel` имеют раскладку корпуса и изображение. Поле `hullHitPointsMax` удалено из схемы и данных.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 6 | [EP-0009-US-0002-TK-0001-weapon-catalog](EP-0009-US-0002-TK-0001-weapon-catalog/EP-0009-US-0002-TK-0001-weapon-catalog.md) | content-data | US2-AC1 |
| 7 | [EP-0009-US-0002-TK-0002-engine-catalog](EP-0009-US-0002-TK-0002-engine-catalog/EP-0009-US-0002-TK-0002-engine-catalog.md) | content-data | US2-AC1 |
| 8 | [EP-0009-US-0002-TK-0003-structure-normalization](EP-0009-US-0002-TK-0003-structure-normalization/EP-0009-US-0002-TK-0003-structure-normalization.md) | content-data | US2-AC2 |
| 9 | [EP-0009-US-0002-TK-0004-hostile-ship-classes](EP-0009-US-0002-TK-0004-hostile-ship-classes/EP-0009-US-0002-TK-0004-hostile-ship-classes.md) | content-data | US2-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
