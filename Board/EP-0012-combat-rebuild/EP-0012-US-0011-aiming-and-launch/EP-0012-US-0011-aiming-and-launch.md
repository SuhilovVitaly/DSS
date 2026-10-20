---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0011-aiming-and-launch"
title: "Прицеливание и пуск"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0005-snapshot-privacy", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0010-tactical-map", "EP-0012-US-0017-event-and-command-integration", "EP-0012-US-0018-legacy-combat-cleanup", "EP-0012-US-0019-scenario-and-client-regressions"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0011 — Прицеливание и пуск

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**User story.** Как игрок, я пускаю торпеду или противоракету за два ввода и до пуска вижу, куда и когда она придёт. Все обязательные требования перечислены ниже.

**Acceptance criteria**
- **US11-AC1:** Режим прицеливания:
  - Q — торпеда, W — ПРО (первый готовый аппарат), кнопки панелей — конкретный аппарат;
  - 1/2/3 или колесо — профиль; ЛКМ — пуск; ПКМ/Esc — отмена.
  - В режиме прицеливания 1–3 выбирают профиль, а не скорость. Раскладка хранится в данных.
  - Прицеливаться можно во время перезарядки, но пуска нет (O5).
- **US11-AC2:** Предпросмотр торпеды (UI-03-R1…R5):
  - R, разброс, трасса, ETA в реальных секундах;
  - линейная тень каждого врага на ETA с подсветкой накрытия (O4);
  - «вне дальности»;
  - «заденет свой» с подтверждением вторым кликом за 1 с.
  - Чисел шанса нет.
- **US11-AC3:** Предпросмотр противоракеты (UI-04-R1…R7):
  - прилипание к трассе (12 px);
  - опасный участок и рубеж по `BlastThreatMath`;
  - «через N с», «запас N с», «поздно», «недостижимо»;
  - Alt — свободный прицел;
  - «×N» для залпа;
  - «заденет свою торпеду»;
  - значок «перехват в пути».
- **US11-AC4:** Клиент отправляет `torpedo.launch` и `defense.launch` с точкой и профилем. Отказ сервера показывается локализованной причиной по коду. Отклик UI на клик <=100мс; снаряд появляется после подтверждения на общих часах сцены (D-USER-02).



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет тематические зависимости; до начала тикета обязательна готовность его API/schema prerequisites по актуальному DAG; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0010-tactical-map. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 34 / [EP-0012-US-0011-TK-0001-aim-mode-and-input-map](EP-0012-US-0011-TK-0001-aim-mode-and-input-map/EP-0012-US-0011-TK-0001-aim-mode-and-input-map.md) | client | EP-0012-US-0010-TK-0004-own-path-forecast-and-fit | US11-AC1 | 5 |
| 35 / [EP-0012-US-0011-TK-0002-torpedo-aim-preview](EP-0012-US-0011-TK-0002-torpedo-aim-preview/EP-0012-US-0011-TK-0002-torpedo-aim-preview.md) | client | EP-0012-US-0011-TK-0001-aim-mode-and-input-map | US11-AC2 | 4 |
| 36 / [EP-0012-US-0011-TK-0003-countermissile-aim-preview](EP-0012-US-0011-TK-0003-countermissile-aim-preview/EP-0012-US-0011-TK-0003-countermissile-aim-preview.md) | client | EP-0012-US-0011-TK-0002-torpedo-aim-preview | US11-AC3 | 4 |
| 37 / [EP-0012-US-0011-TK-0004-launch-commands-and-reasons](EP-0012-US-0011-TK-0004-launch-commands-and-reasons/EP-0012-US-0011-TK-0004-launch-commands-and-reasons.md) | client | EP-0012-US-0011-TK-0003-countermissile-aim-preview | US11-AC4 | 4 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
