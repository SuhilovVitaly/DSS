---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0005-chance-and-resolution
title: "Прозрачный шанс и единственный бросок при встрече"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Прозрачный шанс и единственный бросок при встрече

## Вход и user story

Как игрок, я заранее вижу шанс, рассчитанный по базам аппаратов и навыкам операторов. При встрече выполняется ровно один бросок с этим шансом. Успех уничтожает оба снаряда, а промах оставляет торпеду в полёте.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** R=base*skill/50, p=clamp(50+Rpr−Rt,0,100), округление до0.1%; шанс и breakdown фиксируются при PR launch.
- **AC-0002:** Бросок только при first separation<=500m. 0% не запускается;100% гарантированно успешен. Успех: обе удалены, один red ring.
- **AC-0003:** Промах: PR straight coasts2s physical no steering/collisions/second draw, затем исчезает и только тогда reload10s.
- **AC-0004:** Потеря цели немедленно удаляетPR и запускает reload без броска; save/reload не перебрасывает результат.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0005-TK-0001-defense-event-contract](EP-0007-US-0005-TK-0001-defense-event-contract/EP-0007-US-0005-TK-0001-defense-event-contract.md) — Результат перехвата и записи журнала | 20 | contracts | AC-0001, AC-0002, AC-0003, AC-0004 |
| [EP-0007-US-0005-TK-0002-resolve-interception](EP-0007-US-0005-TK-0002-resolve-interception/EP-0007-US-0005-TK-0002-resolve-interception.md) — Один контакт, успех или двухсекундный промах | 21 | engine | AC-0001, AC-0002, AC-0003, AC-0004 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
