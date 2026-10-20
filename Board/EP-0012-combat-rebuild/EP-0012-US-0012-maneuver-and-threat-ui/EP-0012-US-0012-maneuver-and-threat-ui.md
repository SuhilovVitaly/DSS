---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0012-maneuver-and-threat-ui"
title: "Манёвр и угрозы в UI"
stage: "in_progress"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0005-snapshot-privacy", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0010-tactical-map", "EP-0012-US-0011-aiming-and-launch", "EP-0012-US-0017-event-and-command-integration", "EP-0012-US-0018-legacy-combat-cleanup", "EP-0012-US-0019-scenario-and-client-regressions"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0012 — Манёвр и угрозы в UI

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я вовремя узнаю о вражеской торпеде и одной клавишей ухожу от неё. Все обязательные требования перечислены ниже.

**Acceptance criteria**
- **US12-AC1:** Уклонение и выбор угрозы:
  - A/D — уклонение от текущей угрозы (по умолчанию ближайшей по времени до рубежа);
  - клик по чужой торпеде или Tab меняет текущую угрозу;
  - удержание A/D показывает прогноз «выйду через N с / не успеваю», отпускание отправляет `maneuver.evade`;
  - стрелки с удержанием — непрерывный поворот, Shift — полный ход;
  - стрелка базового курса у корабля;
  - значок «двигатель отключён N с».
- **US12-AC2:** Панель угроз:
  - список опасных и близких торпед по времени до рубежа: профиль, «до рубежа N с», значок перехвата, кнопки ПРО / ← / →;
  - строка появляется <=200мс после принятия пуска в отображаемую подтверждённую сцену;
  - торпеды, проходящие дальше R + 3 км, в панель не попадают.
- **US12-AC3:** Сигналы:
  - цвет торпеды по классу (опасна / близко / мимо), различимый при дейтеранопии за счёт толщины и штриха;
  - звук и вспышка пуска врага, стрелка на краю экрана для врага вне экрана;
  - сигнал «опасная торпеда» и учащённый сигнал при рубеже ≤ 3 с;
  - не больше одного сигнала одного типа в 1,5 с.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0011-aiming-and-launch. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 38 / [EP-0012-US-0012-TK-0001-evade-input-and-threat-select](EP-0012-US-0012-TK-0001-evade-input-and-threat-select/EP-0012-US-0012-TK-0001-evade-input-and-threat-select.md) | client | EP-0012-US-0011-TK-0004-launch-commands-and-reasons | US12-AC1 | 8 |
| 39 / [EP-0012-US-0012-TK-0002-threat-panel](EP-0012-US-0012-TK-0002-threat-panel/EP-0012-US-0012-TK-0002-threat-panel.md) | client | EP-0012-US-0012-TK-0001-evade-input-and-threat-select | US12-AC2 | 7 |
| 40 / [EP-0012-US-0012-TK-0003-threat-alerts](EP-0012-US-0012-TK-0003-threat-alerts/EP-0012-US-0012-TK-0003-threat-alerts.md) | client | EP-0012-US-0012-TK-0002-threat-panel | US12-AC3 | 9 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
