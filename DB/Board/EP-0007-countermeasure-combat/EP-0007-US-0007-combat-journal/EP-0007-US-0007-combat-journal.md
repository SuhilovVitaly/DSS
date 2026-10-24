---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0007-combat-journal
title: "Сохраняемый журнал боя"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Сохраняемый журнал боя

## Вход и user story

Как игрок, я раскрываю журнал на тактической карте и понимаю, почему перехват удался или не удался.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Журнал содержит пуски, перехваты, промахи, попадания/урон, уничтожение и самоуничтожение; шанс, бросок и frozen расчёт доступны.
- **AC-0002:** Одна запись на событие, стабильные ID/порядок, журнал сохраняется; закрытие панели не удаляет историю.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0007-TK-0001-authoritative-combat-journal](EP-0007-US-0007-TK-0001-authoritative-combat-journal/EP-0007-US-0007-TK-0001-authoritative-combat-journal.md) — Авторитетные события боя | 27 | engine | AC-0001, AC-0002 |
| [EP-0007-US-0007-TK-0002-collapsible-combat-journal](EP-0007-US-0007-TK-0002-collapsible-combat-journal/EP-0007-US-0007-TK-0002-collapsible-combat-journal.md) — Сворачиваемая панель журнала | 28 | client | AC-0001, AC-0002 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
