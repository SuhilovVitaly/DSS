---
epic: EP-0009-module-combat
story: EP-0009-US-0007-manual-defense-bursts
title: "Ручная ПРО EP-0008 под очередь"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Ручная ПРО EP-0008 под очередь

**User story.** Как игрок, я по-прежнему сам выбираю торпеду и стреляю из каждой ПРО, но теперь каждый пуск — это очередь из двух стволов.

**Acceptance criteria**
- **US7-AC1:** `defense.fire` из EP-0008 запускает очередь (D14). Ограничения EP-0008 сохраняются:
  - ручная дальность, теперь 150 км;
  - готовность установки и наличие оператора;
  - пуск разрешён, даже если по торпеде уже летит другая противоракета;
  - шанс 0% и неизвестная точка встречи пуск не блокируют.

  Новые причины отказа: `ModuleDisabled`, `ModuleDestroyed`, `NotReady` с `ReadyAt`.
- **US7-AC2:** Панели двух ПРО из EP-0008 показывают число стволов, отложенный второй выстрел, перезарядку от первого выстрела и шанс D06 с разложением.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 23 | [EP-0009-US-0007-TK-0001-manual-fire-burst](EP-0009-US-0007-TK-0001-manual-fire-burst/EP-0009-US-0007-TK-0001-manual-fire-burst.md) | engine | US7-AC1 |
| 24 | [EP-0009-US-0007-TK-0002-defense-panels-burst-ui](EP-0009-US-0007-TK-0002-defense-panels-burst-ui/EP-0009-US-0007-TK-0002-defense-panels-burst-ui.md) | client | US7-AC2 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
