---
epic: EP-maintenance
story: EP-maintenance-US-0016-battle-grid-and-fire-arrows
title: Сетка и стрелки тактического боя
stage: approved
implementation_status: implemented
---

# Сетка и стрелки тактического боя

[Эпик «Исправления и доделки»](../../Documentation.md)

Игрок видит масштаб поля по вложенной сетке и читает направления выстрелов по лёгким стрелкам.

- [AC1. Стрелки планирования огня](./EP-maintenance-US-0016-TK-0001-fire-plan-arrows/EP-maintenance-US-0016-TK-0001-fire-plan-arrows.md)
- [AC2. Стрелки результатов залпа](./EP-maintenance-US-0016-TK-0002-salvo-result-arrows/EP-maintenance-US-0016-TK-0002-salvo-result-arrows.md)
- [AC3. Вложенная сетка боя](./EP-maintenance-US-0016-TK-0003-nested-battle-grid/EP-maintenance-US-0016-TK-0003-nested-battle-grid.md)

Масштабирование торпед ведётся в [существующем тикете](../EP-maintenance-US-0015-tactical-battle-feedback/EP-maintenance-US-0015-TK-0004-align-torpedo-flight/EP-maintenance-US-0015-TK-0004-align-torpedo-flight.md).
## Проверки

Сборка Client и Client.Tests и проверка форматирования прошли. Сверены 23 эталона Classic и Tabletop. Широкий прогон до уточнения диагностических границ снаряда прошёл 589 из 590 проверок. После уточнения повторный прогон проверок сетки, снарядов, геометрии скинов и растровых эталонов прошёл 84 из 84. Окно игры не запускалось.

- Широкий прогон — `D:/DeepSpaceSaga/artifacts/validation/maintenance-battle-grid-fire/20261113-grid-fire-01/final/grid-fire.trx`.
- Повторная проверка — `D:/DeepSpaceSaga/artifacts/validation/maintenance-battle-grid-fire/20261113-grid-fire-01/recheck/grid-fire-recheck.trx`.

- [Контракт броска](EP-maintenance-US-0016-TK-0004-shot-roll-contract/EP-maintenance-US-0016-TK-0004-shot-roll-contract.md)
- [Запись броска движком](EP-maintenance-US-0016-TK-0005-capture-shot-rolls/EP-maintenance-US-0016-TK-0005-capture-shot-rolls.md)
