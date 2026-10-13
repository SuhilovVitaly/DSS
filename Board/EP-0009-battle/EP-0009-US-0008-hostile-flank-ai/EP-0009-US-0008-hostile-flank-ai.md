---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
title: "Мухи и Шмели на ИИ EP-0008"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Мухи и Шмели на ИИ EP-0008

**User story.** Как игрок, я встречаю вражеские истребители. Они заходят с флангов, кружат на расстоянии 50 км и стреляют по перезарядке. ПРО Шмеля защищает его от моих торпед.

**Acceptance criteria**
- **US8-AC1:** NPC стреляют через исполнитель EP-0008. Пуск разрешён, если аппарат готов (US5-AC1), цель не дальше дальности пуска и расчётный перехват ≤ 0,8 × `torpedoMaxFlightTimeMs` (D33). Торпеды NPC живут 120 с; исключение EP-0008 «NPC не получает TTL торпед» отменено.
- **US8-AC2:** Контроллер EP-0008 получает из сценария параметры `standoffKm`, `bandKm` и `flankOffsetDeg`. По умолчанию поведение остаётся как в EP-0008: радиус `0.5 × TorpedoRangeKm`, без смещения. Фланговый заход и отход — по D33. Орбита, выбор направления, гистерезис и шаг решений 100 мс остаются как в EP-0008.
- **US8-AC3:** При выключенном двигателе ИИ не маневрирует (US3-AC4). Состояние ИИ (фаза, направление, параметры) сохраняется (US-0011).

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 25 | [EP-0009-US-0008-TK-0001-npc-fire-cadence](EP-0009-US-0008-TK-0001-npc-fire-cadence/EP-0009-US-0008-TK-0001-npc-fire-cadence.md) | engine | US8-AC1 |
| 26 | [EP-0009-US-0008-TK-0002-flank-approach-math](EP-0009-US-0008-TK-0002-flank-approach-math/EP-0009-US-0008-TK-0002-flank-approach-math.md) | motion | US8-AC2 |
| 27 | [EP-0009-US-0008-TK-0003-ai-params-from-scenario](EP-0009-US-0008-TK-0003-ai-params-from-scenario/EP-0009-US-0008-TK-0003-ai-params-from-scenario.md) | engine | US8-AC2, US8-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
