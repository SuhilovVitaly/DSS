---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
title: "Боевые сценарии"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Боевые сценарии

**User story.** Как игрок, в New Game я выбираю бой «Тетрарх против 3 Мух» (лёгкий) или «Тетрарх против 3 Шмелей» (равный).

**Acceptance criteria**
- **US9-AC1:** `Scenarios/CombatMukha/scenario.json` виден в New Game и имеет описание.
  - Тетрарх: комплектация EP-0008 (2 ПРО, 3 оператора + пилот), броня `light` на всех 9 модулях, позиция (0, 0) км, курс 0°, 2 км/с. Бой начинается на паузе, как в EP-0008.
  - Три Мухи с `relationToPlayer: enemy`: позиции (−40, 150), (0, 165) и (40, 150) км, курс 180°, 3 км/с, ИИ `standoffKm` 50, `bandKm` 10, `flankOffsetDeg` −35 / 0 / +35, броня `none`.
  - Навыки всех операторов 20.
- **US9-AC2:** `Scenarios/CombatShmel/scenario.json`: то же, но с тремя Шмелями (одноствольная ПРО и её оператор).
- **US9-AC3:** Остальные сценарии загружаются по новой схеме:
  - `structurePoints` ≤ 150;
  - у экипажа навыки `accuracy` и `maneuverability`;
  - устаревшие поля навыков удалены из данных и схемы;
  - `PlayerShipOnly` — по решению O6.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 28 | [EP-0009-US-0009-TK-0001-scenario-combat-mukha](EP-0009-US-0009-TK-0001-scenario-combat-mukha/EP-0009-US-0009-TK-0001-scenario-combat-mukha.md) | content-data | US9-AC1 |
| 29 | [EP-0009-US-0009-TK-0002-scenario-combat-shmel](EP-0009-US-0009-TK-0002-scenario-combat-shmel/EP-0009-US-0009-TK-0002-scenario-combat-shmel.md) | content-data | US9-AC2 |
| 30 | [EP-0009-US-0009-TK-0003-scenarios-migrate-schema](EP-0009-US-0009-TK-0003-scenarios-migrate-schema/EP-0009-US-0009-TK-0003-scenarios-migrate-schema.md) | content-data | US9-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
