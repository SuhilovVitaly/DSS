---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
title: "Сохранения и приёмка"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Сохранения и приёмка

**User story.** Как игрок, я сохраняюсь посреди боя и продолжаю его без потерь. Как автор, я вижу, что бой в игре соответствует симулятору.

**Acceptance criteria**
- **US11-AC1:** Формат сохранения 16 (EP-0008) → **17**; payload: defense 3, combat 2, hostile state 2. Формат 17 хранит:
  - броню, структуру, выключение и уничтожение модулей;
  - для аппарата: `ReadyAt`, режим, поломку и список летящих торпед;
  - очереди ПРО;
  - потоки `combat.hit.location.v1`, `combat.torpedo.hit.v1`, `combat.launcher.breakdown.v1` со счётчиками;
  - параметры ИИ.

  Hull и auto-attempt flag удалены. Сохранения формата 16 и ниже отвергаются с понятным сообщением, без миграции (как Q49 EP-0008). Проверка `HitChancePercent == 100` заменена проверкой диапазона. Запрет сохранения после гибели (EP-0008) сохраняется.
- **US11-AC2:** Сквозные Engine-тесты обоих сценариев на настоящем ИИ. Прогон по 100 сидам на каждый сценарий сравнивается с эталоном §7, допуск ±10 п.п. Если допуск превышен, это фиксируется с причиной и не блокирует тикет. Возможные причины: A02/A03, контроллер EP-0008 вместо `AttackAI` симулятора, дискретные повороты.
- **US11-AC3:** Симулятор читает настоящие данные игры (O5). Нативная приёмка в реальном окне:
  - оба сценария;
  - смена режима в бою;
  - ручная очередь двух ПРО;
  - выключение модуля;
  - гибель по порогу и по ключевому модулю;
  - окно поражения.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 34 | [EP-0009-US-0011-TK-0001-save-format-17](EP-0009-US-0011-TK-0001-save-format-17/EP-0009-US-0011-TK-0001-save-format-17.md) | engine | US11-AC1 |
| 35 | [EP-0009-US-0011-TK-0002-combat-acceptance-and-balance](EP-0009-US-0011-TK-0002-combat-acceptance-and-balance/EP-0009-US-0011-TK-0002-combat-acceptance-and-balance.md) | engine | US11-AC2 |
| 36 | [EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native](EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native/EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native.md) | tooling | US11-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
