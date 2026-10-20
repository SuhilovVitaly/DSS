---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0013-combat-state-and-feedback"
title: "Состояние, панели, обратная связь"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0005-snapshot-privacy", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0010-tactical-map", "EP-0012-US-0011-aiming-and-launch", "EP-0012-US-0012-maneuver-and-threat-ui", "EP-0012-US-0017-event-and-command-integration", "EP-0012-US-0018-legacy-combat-cleanup", "EP-0012-US-0019-scenario-and-client-regressions"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0013 — Состояние, панели, обратная связь

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я вижу состояние модулей своего корабля, готовность оружия и результат каждого выстрела, а в конце боя — его итоги. Все обязательные требования перечислены ниже.

**Acceptance criteria**
- **US13-AC1:** Схема модулей Тетрарха (UI-07-R1…R4):
  - броня, структура, «отключён N с», «уничтожен»;
  - рамка у критических модулей;
  - счётчик «Уничтожено модулей N / 3»;
  - мигание плитки и подпись «−N, модуль» при попадании.
  - Полоса HP корпуса в `ObjectLabelRenderer.cs` удалена.
- **US13-AC2:** Панели оружия (UI-08):
  - кольцо перезарядки (погрешность ≤ 0,1 с), профиль, оператор и accuracy, разброс, состояние, кнопка с хоткеем, переключатель кольца дальности;
  - кнопки Fire, SelfDestruct, Enable/Disable и строки шанса удалены;
  - кнопка «Подрыв» для своих торпед в полёте (O8).
- **US13-AC3:**
  - Значки врагов: «повреждён», «обездвижен N с», «уничтожен».
  - Метки результата: «ядро», «край», «мимо», «сбита».
  - Урон врагу числом не показывается.
- **US13-AC4:** `CombatJournalPanel` и отчёты EP-0010 (`CombatReportPanel`, `SimulationEngine.CombatReports.cs`) работают с событиями пуска, подрыва, перехвата и попадания. Слов «шанс» и «бросок» нет.
- **US13-AC5:**
  - Баннер «БОЙ», звук и рамка при `Active`.
  - Таймер боя в реальном времени.
  - Немодальная плашка итогов при `Won` с кнопкой «Продолжить».
  - `DefeatScreen` получает блок статистики.
  - В бою кнопки x5+ неактивны с подсказкой.


## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет полностью завершённые истории до начала этой; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0012-maneuver-and-threat-ui. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 60 / [EP-0012-US-0013-TK-0001-module-diagram](EP-0012-US-0013-TK-0001-module-diagram/EP-0012-US-0013-TK-0001-module-diagram.md) | client | EP-0012-US-0012-TK-0003-threat-alerts | US13-AC1 | 4 |
| 61 / [EP-0012-US-0013-TK-0002-weapon-panels-v3](EP-0012-US-0013-TK-0002-weapon-panels-v3/EP-0012-US-0013-TK-0002-weapon-panels-v3.md) | client | EP-0012-US-0013-TK-0001-module-diagram | US13-AC2 | 5 |
| 62 / [EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels](EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels/EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels.md) | client | EP-0012-US-0013-TK-0002-weapon-panels-v3 | US13-AC3 | 4 |
| 63 / [EP-0012-US-0013-TK-0004-journal-and-reports-v3](EP-0012-US-0013-TK-0004-journal-and-reports-v3/EP-0012-US-0013-TK-0004-journal-and-reports-v3.md) | client | EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels | US13-AC4 | 5 |
| 64 / [EP-0012-US-0013-TK-0005-combat-start-timer-summary](EP-0012-US-0013-TK-0005-combat-start-timer-summary/EP-0012-US-0013-TK-0005-combat-start-timer-summary.md) | client | EP-0012-US-0013-TK-0004-journal-and-reports-v3 | US13-AC5 | 5 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
