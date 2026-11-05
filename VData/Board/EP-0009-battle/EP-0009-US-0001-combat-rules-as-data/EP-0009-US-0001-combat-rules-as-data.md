---
epic: EP-0009-module-combat
story: EP-0009-US-0001-combat-rules-as-data
title: "Правила боя как данные"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Правила боя как данные

**User story.** Как дизайнер, я задаю правила боя, классы брони, режимы и боевые параметры модулей в JSON, а движок отвергает некорректные значения при загрузке.

Review after TK-0005 (2026-10-12): schema, JSON loading, validation and Save/Load regressions passed. US1-AC1 is covered by content validation; US1-AC2 is partial until the real module catalog is migrated in US-0002; US1-AC3 is partial until new skills feed the hit/interception roll in later runtime tickets. Story acceptance remains OPEN. Graphify/game/native NOT RUN per user.

**Acceptance criteria**
- **US1-AC1:** Файл правил загружается через `Settings.json`. На каждое невалидное значение — `ContentException` с указанием файла и поля:
  - база шанса не в диапазоне 0..100;
  - min > max;
  - `hitDisableMs` < 0;
  - доля для правила гибели вне (0, 1];
  - неизвестная роль модуля;
  - класс брони < 0.
- **US1-AC2:** Схема модулей EP-0008 расширена полями EP-0009, каждое проверяется только для своей категории:
  - торпедный аппарат: `accuracy`, `armorPiercingPercent` 0..100, `armorDamagePercent` > 0, `launcherReloadMs` > 0, `torpedoMaxFlightTimeMs` > 0;
  - ПРО: `barrels` ≥ 1, `burstIntervalMs` ≥ 0;
  - двигатель: `maneuverability`;
  - мостик: `combatRole`.

  Поля EP-0008 (`accuracy` ПРО, `torpedoManeuverability`, дальности, `maxFlightTimeMs`) переиспользуются, а не дублируются.
- **US1-AC3:** У экипажа есть навыки `accuracySkill` и `maneuverabilitySkill` (0..30), и они влияют на бросок. У установленного модуля в сценарии есть `armorClass`. Сценарии без этих полей загружаются со значениями по умолчанию: `none` и навыки 0.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 1 | [EP-0009-US-0001-TK-0001-module-health-contracts](EP-0009-US-0001-TK-0001-module-health-contracts/EP-0009-US-0001-TK-0001-module-health-contracts.md) | contracts | US1-AC1, US1-AC3 |
| 2 | [EP-0009-US-0001-TK-0002-fire-contracts](EP-0009-US-0001-TK-0002-fire-contracts/EP-0009-US-0001-TK-0002-fire-contracts.md) | contracts | US1-AC2, US1-AC3 |
| 3 | [EP-0009-US-0001-TK-0003-combat-rules-content](EP-0009-US-0001-TK-0003-combat-rules-content/EP-0009-US-0001-TK-0003-combat-rules-content.md) | engine | US1-AC1 |
| 4 | [EP-0009-US-0001-TK-0004-module-combat-schema](EP-0009-US-0001-TK-0004-module-combat-schema/EP-0009-US-0001-TK-0004-module-combat-schema.md) | engine | US1-AC2 |
| 5 | [EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema](EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema/EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema.md) | engine | US1-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
