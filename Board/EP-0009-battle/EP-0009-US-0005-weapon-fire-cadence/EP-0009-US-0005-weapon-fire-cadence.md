---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
title: "Темп стрельбы"
stage: draft
created: 2026-10-12
validation_status: not-started
---

# Темп стрельбы

**User story.** Как игрок, я стреляю по перезарядке и не жду, пока долетит прошлая торпеда. Моя ПРО бьёт очередью из двух стволов.

**Acceptance criteria**
- **US5-AC1:** Торпедный аппарат игрока или NPC готов, когда `t ≥ ReadyAt`, где `ReadyAt = t_пуска + launcherReloadMs × reloadMultiplier`. Правила EP-0008 «один активный снаряд на аппарат» и «пуск NPC после окончания полёта» (A07 EP-0008) удалены. В полёте может быть несколько торпед.
- **US5-AC2:** Очередь ПРО работает по D14 и A06. Отложенные выстрелы — это события цикла боя с порядком по §3.2.
- **US5-AC3:** Перезарядка ПРО считается по D13, а не «после удаления противоракеты». Общий auto-attempt flag EP-0008 удалён и заменён правилом D15. Арбитраж двух ПРО из EP-0008 сохраняется.

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 17 | [EP-0009-US-0005-TK-0001-launcher-cooldown](EP-0009-US-0005-TK-0001-launcher-cooldown/EP-0009-US-0005-TK-0001-launcher-cooldown.md) | engine | US5-AC1 |
| 18 | [EP-0009-US-0005-TK-0002-pd-burst](EP-0009-US-0005-TK-0002-pd-burst/EP-0009-US-0005-TK-0002-pd-burst.md) | engine | US5-AC2 |
| 19 | [EP-0009-US-0005-TK-0003-pd-reload-and-attempts](EP-0009-US-0005-TK-0003-pd-reload-and-attempts/EP-0009-US-0005-TK-0003-pd-reload-and-attempts.md) | engine | US5-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. Planning text is not PASS/APPROVED. Actual results go into ticket cards and ImplementationStatus; deferred native/human acceptance stays OPEN. Graph rebuilding is SKIPPED by user instruction.
