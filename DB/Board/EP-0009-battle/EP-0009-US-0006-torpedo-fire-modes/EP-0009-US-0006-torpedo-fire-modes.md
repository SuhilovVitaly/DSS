---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
title: "Режимы торпедного аппарата"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Режимы торпедного аппарата

**User story.** Как игрок, я выбираю режим аппарата («Стандарт», «Манёвренность» или «Перегрев») и понимаю, чем за него плачу: нестандартный режим стреляет реже, а «Перегрев» может сломать аппарат.

**Acceptance criteria**
- **US6-AC1:** Бонусы режима захватываются при пуске: точность — в атаку броска попадания, манёвренность — в защиту торпеды от перехвата. Перезарядка умножается на `reloadMultiplier`.
- **US6-AC2:** «Перегрев»: после пуска делается бросок в потоке `combat.launcher.breakdown.v1`. При поломке аппарат сломан до дока, в журнале появляется запись `LauncherBreakdown`.
- **US6-AC3:** Команда `torpedo.setMode`:
  - вне боя (нет живых торпед и врагов в пределах дальности пуска) действует сразу;
  - в бою выставляет `ReadyAt = max(ReadyAt, t) + launcherReloadMs`;
  - идемпотентна;
  - отклоняется, если режим неприменим к категории модуля;
  - на паузе ведёт себя так же, как ручные команды EP-0008 (A08).

  Клиент показывает выбор режима и задержку.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 20 | [EP-0009-US-0006-TK-0001-mode-runtime](EP-0009-US-0006-TK-0001-mode-runtime/EP-0009-US-0006-TK-0001-mode-runtime.md) | engine | US6-AC1, US6-AC2 |
| 21 | [EP-0009-US-0006-TK-0002-set-mode-command](EP-0009-US-0006-TK-0002-set-mode-command/EP-0009-US-0006-TK-0002-set-mode-command.md) | engine | US6-AC3 |
| 22 | [EP-0009-US-0006-TK-0003-mode-selector-ui](EP-0009-US-0006-TK-0003-mode-selector-ui/EP-0009-US-0006-TK-0003-mode-selector-ui.md) | client | US6-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
