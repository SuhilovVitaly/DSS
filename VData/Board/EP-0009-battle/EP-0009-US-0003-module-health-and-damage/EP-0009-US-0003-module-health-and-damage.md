---
epic: EP-0009-module-combat
story: EP-0009-US-0003-module-health-and-damage
title: "HP в модулях"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# HP в модулях

**User story.** Как игрок, я вижу, что торпеда попадает в конкретный модуль. Броня гасит урон, модуль выключается на минуту и может погибнуть. Корабль гибнет, если потерял мостик, двигатель или треть модулей.

**Acceptance criteria**
- **US3-AC1:** Каждый установленный модуль имеет `ArmorMax` = значение класса брони и `StructureMax` = `structurePointsMax`. Текущие значения берутся из сценария или сохранения. Hull-состояние удалено (`_hullCombat` и его наследник из EP-0008).
- **US3-AC2:** Попадание выбирает живой модуль с весом по массе через поток `combat.hit.location.v1`. Урон по D09, выключение по A05. Пример: 150 урона по броне `light` — броня 60 → 0, структура 150 → 60; второе попадание уничтожает модуль.
- **US3-AC3:** Корабль гибнет по D11. Есть один путь гибели `DestroyShip(objectId, cause, t)`, его используют и бой, и охрана станции:
  - NPC оставляет обломок;
  - для игрока срабатывает терминальное поражение EP-0008 (US-0009 EP-0008), у которого lethal-условие «HP = 0» заменено на D11.

  Хардкод `"ship.tetrarch"` удалён из Engine и Client.
- **US3-AC4:** Выключенный или уничтоженный модуль не выполняет команд. ПРО при выключении отменяет очередь. Двигатель ведёт себя по A03: контроллер ИИ EP-0008 при выключенном двигателе не маневрирует.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 10 | [EP-0009-US-0003-TK-0001-module-health-runtime](EP-0009-US-0003-TK-0001-module-health-runtime/EP-0009-US-0003-TK-0001-module-health-runtime.md) | engine | US3-AC1 |
| 11 | [EP-0009-US-0003-TK-0002-module-damage](EP-0009-US-0003-TK-0002-module-damage/EP-0009-US-0003-TK-0002-module-damage.md) | engine | US3-AC2 |
| 12 | [EP-0009-US-0003-TK-0003-ship-destruction](EP-0009-US-0003-TK-0003-ship-destruction/EP-0009-US-0003-TK-0003-ship-destruction.md) | engine | US3-AC3 |
| 13 | [EP-0009-US-0003-TK-0004-disabled-module-effects](EP-0009-US-0003-TK-0004-disabled-module-effects/EP-0009-US-0003-TK-0004-disabled-module-effects.md) | engine | US3-AC4 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
