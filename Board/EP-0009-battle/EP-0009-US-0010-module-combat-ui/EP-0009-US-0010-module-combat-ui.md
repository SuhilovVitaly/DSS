---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
title: "Интерфейс модулей и журнал"
stage: implemented
created: 2026-10-12
validation_status: headless-pass-native-open
---

# Интерфейс модулей и журнал

**User story.** Как игрок, я вижу состояние каждого модуля и броски в журнале, а поражение наступает по новым правилам гибели.

**Acceptance criteria**
- **US10-AC1:** Окно поражения EP-0008 открывается, когда игрок гибнет по D11. Его поведение не меняется: кнопки «Загрузить» и «Главное меню», сохранение запрещено.
- **US10-AC2:** У выбранного корабля для каждого модуля видны броня, структура, таймер выключения и отметка «уничтожен». Полоса HP корпуса у метки на карте (на `99e15a2`: `ObjectLabelRenderer.cs:16-18`, `:348-357`) заменяется суммарной полосой модулей и больше не зависит от `"ship.tetrarch"`.
- **US10-AC3:** В журнале EP-0007/0008 появляются строки:
  - бросок попадания (атака, защита, шанс, бросок);
  - перехват по новой формуле;
  - попадание в модуль (было → стало);
  - уничтожение модуля;
  - гибель корабля с причиной;
  - пропуск выстрела очереди;
  - поломка при «Перегреве».

## Tickets

| Step | Ticket | Layer | Serves |
|---|---|---|---|
| 31 | [EP-0009-US-0010-TK-0001-defeat-from-module-rule](EP-0009-US-0010-TK-0001-defeat-from-module-rule/EP-0009-US-0010-TK-0001-defeat-from-module-rule.md) | engine | US10-AC1 |
| 32 | [EP-0009-US-0010-TK-0002-module-health-ui](EP-0009-US-0010-TK-0002-module-health-ui/EP-0009-US-0010-TK-0002-module-health-ui.md) | client | US10-AC2 |
| 33 | [EP-0009-US-0010-TK-0003-combat-journal-lines](EP-0009-US-0010-TK-0003-combat-journal-lines/EP-0009-US-0010-TK-0003-combat-journal-lines.md) | client | US10-AC3 |

## Evidence

Execution is authorized by the user and EpicExecutionPrompt. AC1 defeat timing/terminal/save guard, AC2 known-ship module panel and aggregate map bar, and AC3 journal details pass headless verification in the three ticket cards. Native defeat-window and hands-on combat UI remain NOT RUN/OPEN by user instruction. Graph rebuilding is SKIPPED by user instruction.
