---
epic: EP-0009-module-combat
story: EP-0009-US-0004-combat-skill-rolls
title: "Броски навыков"
stage: draft
created: 2026-10-12
validation_status: headless-pass-native-open
---

# Броски навыков

**User story.** Как игрок, я понимаю, почему торпеда попала или промахнулась. Журнал и превью показывают атаку, защиту, шанс и бросок, а навыки экипажа на них влияют.

**Acceptance criteria**
- **US4-AC1:** Одна функция шанса D04 заменяет формулу EP-0008 `clamp(Accuracy − Maneuverability)` во всех вызовах, включая превью. Тесты: 60 против 60 → 500, 200 против 0 → 1000, 0 против 200 → 0. Пример EP-0008 «55 − 5 = 50%» больше не действует.
- **US4-AC2:** Бросок D05 выполняется в момент контакта торпеды с кораблём, в потоке `combat.torpedo.hit.v1`. Аппарат 40 + оператор 20 против «Стрижа» 40 + пилот 20 → 50%. Промах — по A01.
- **US4-AC3:** Перехват считается по D06 в существующем потоке EP-0008 `combat.countermeasure.intercept.v1`. ПРО 58 + оператор 20 против торпеды 50 + оператор 20 → 58%. Правило EP-0008 «при контакте с шансом 0% или 100% бросок всё равно расходуется» сохраняется.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 14 | [EP-0009-US-0004-TK-0001-chance-math](EP-0009-US-0004-TK-0001-chance-math/EP-0009-US-0004-TK-0001-chance-math.md) | engine | US4-AC1 |
| 15 | [EP-0009-US-0004-TK-0002-torpedo-hit-roll](EP-0009-US-0004-TK-0002-torpedo-hit-roll/EP-0009-US-0004-TK-0002-torpedo-hit-roll.md) | engine | US4-AC2 |
| 16 | [EP-0009-US-0004-TK-0003-intercept-roll-skills](EP-0009-US-0004-TK-0003-intercept-roll-skills/EP-0009-US-0004-TK-0003-intercept-roll-skills.md) | engine | US4-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.

2026-10-12 story review: US4-AC1 shared D04 formula, configured base and preview PASS in headless tests. US4-AC2 launch-time attack, contact-time Strizh/pilot defense, named hit RNG, miss with zero damage, partition invariance and Save/Load continuation PASS in headless tests. US4-AC3 production 58% example, frozen torpedo/PD ratings, preview breakdown and 0%/100% interception draws PASS in headless tests. Final Engine non-corpus 1972/1972, Client 1859/1859, solution build zero warnings/errors. Custom chance-base Save v16 roundtrip remains OPEN pending a versioned rules identity. Game/native/human acceptance NOT RUN by user instruction; Graphify rebuild SKIPPED.
