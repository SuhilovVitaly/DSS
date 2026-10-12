---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0007-independent-defense-panels
title: "Две адресуемые панели ПРО"
stage: draft
dependencies: ["EP-0008-US-0006-lifetime-and-auto-arbitration"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-pass-native-open
---

## Актуализация EP-0008 — 2026-10-12

Функциональные тикеты реализованы, проверены и опубликованы; self-review завершено. [Фактическое evidence](../../../Documentation/04-Engineering/PhaseThreeCombatAcceptance.md), [ticket → SHA → push](../PublicationStatus.md). `stage: draft` — исходная метка планирования, не утверждение о пользовательском APPROVED. Дальнейшие game/native запуски отложены пользователем.


# Две адресуемые панели ПРО

## User story

Игрок отдельно управляет каждым аппаратом: пуск, автоматика, оператор и таймеры. Для выбранной торпеды обе панели показывают собственный шанс и его числовой расчёт.

## Источник и границы

Решения Q06,Q29,Q36,Q38,Q46; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Каждая ПРО module instance имеет отдельную панель со стабильной identity; сворачивание/toggle/pending не перетекают между аппаратами.
- **AC-0002:** Fire активен только для валидной выбранной вражеской торпеды<=200km и ready operator;0%/unknown intercept не запрещают его.
- **AC-0003:** Chance/breakdown/ttl берутся из authoritative module/flight state, reload и lifetime подписаны раздельно; устаревший preview не показывается для новой цели.
- **AC-0004:** Панели помещаются/доступны при1280x720,1920x1080, UI100/120/150%; input панелей не уходит на карту.

## Зависимости

- [EP-0008-US-0006-lifetime-and-auto-arbitration](../EP-0008-US-0006-lifetime-and-auto-arbitration/EP-0008-US-0006-lifetime-and-auto-arbitration.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 23 | [EP-0008-US-0007-TK-0001-panel-instances](EP-0008-US-0007-TK-0001-panel-instances/EP-0008-US-0007-TK-0001-panel-instances.md) — Командные панели по конкретным экземплярам модулей | client | AC-0001, AC-0004 |
| 24 | [EP-0008-US-0007-TK-0002-panel-command-routing](EP-0008-US-0007-TK-0002-panel-command-routing/EP-0008-US-0007-TK-0002-panel-command-routing.md) — Ручные команды и независимый pending каждого аппарата | client | AC-0001, AC-0002, AC-0003, AC-0004 |
| 25 | [EP-0008-US-0007-TK-0003-combat-info](EP-0008-US-0007-TK-0003-combat-info/EP-0008-US-0007-TK-0003-combat-info.md) — Расчёт шанса, lifetime и поведение выбранного врага | client | AC-0002, AC-0003 |
| 26 | [EP-0008-US-0007-TK-0004-combat-localization](EP-0008-US-0007-TK-0004-combat-localization/EP-0008-US-0007-TK-0004-combat-localization.md) — Локализованные команды, причины и состояния | content-data | AC-0002, AC-0003 |

## Completion evidence

Покрытие AC и фактические ограничения приведены в текущем execution status и acceptance report; наличие запланированных проверок само по себе не является evidence. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Исходная запись описывала план; текущий запрос разрешает автономное исполнение.

## Execution review — 2026-10-11

All four tickets implemented and individually published by this execution chain. AC-0001: owner/module identity, independent collapse/pending/toggles and reorder tests. AC-0002: real paused LocalSession manual launches from both modules at100%/150%, stale selection/preview/power guards and valid0% cases. AC-0003: own authoritative previews, active frozen chance, physical flight/reload labels, selected NPC/PR facts and both locales. AC-0004: full Client1811/1811 includes dual-panel small viewport/scrolling, existing100/120/150% UI flows and overlay hit tests; native window acceptance remains OPEN for US-0012. Own review complete, not independent approval. Next story: manual map preview/ranges.
