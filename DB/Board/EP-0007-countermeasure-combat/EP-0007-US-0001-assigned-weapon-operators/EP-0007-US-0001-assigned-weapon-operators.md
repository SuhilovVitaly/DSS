---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0001-assigned-weapon-operators
title: "Конкретные операторы и понятные рейтинги"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Конкретные операторы и понятные рейтинги

## Вход и user story

Как игрок, я вижу, кто управляет каждым аппаратом и с каким навыком. Назначения задаются сценарием, а отсутствие оператора блокирует работу. Рейтинг торпеды фиксируется при пуске.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Два разных оператора на корабль, отдельные навыки 0–100; без назначенного оператора аппарат не работает.
- **AC-0002:** Рейтинг равен базе × навык/50; обе базы30, оба профильных навыка50; рейтинг торпеды закреплён при пуске.
- **AC-0003:** Навыки/назначения валидируются и проецируются без раскрытия скрытых объектов; UI не рассчитывает авторитетный рейтинг.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0001-TK-0001-operator-contract](EP-0007-US-0001-TK-0001-operator-contract/EP-0007-US-0001-TK-0001-operator-contract.md) — Контракт оператора и рейтинга торпеды | 1 | contracts | AC-0001, AC-0002, AC-0003 |
| [EP-0007-US-0001-TK-0002-operator-schema](EP-0007-US-0001-TK-0002-operator-schema/EP-0007-US-0001-TK-0002-operator-schema.md) — Сценарные навыки и назначения экипажа | 2 | engine | AC-0001, AC-0003 |
| [EP-0007-US-0001-TK-0003-weapon-rating-content-schema](EP-0007-US-0001-TK-0003-weapon-rating-content-schema/EP-0007-US-0001-TK-0003-weapon-rating-content-schema.md) — Параметры рейтингов и противоракетного аппарата | 3 | engine | AC-0002, AC-0003 |
| [EP-0007-US-0001-TK-0004-authoritative-operators](EP-0007-US-0001-TK-0004-authoritative-operators/EP-0007-US-0001-TK-0004-authoritative-operators.md) — Проверка оператора и фиксация рейтинга при пуске | 10 | engine | AC-0001, AC-0002, AC-0003 |
| [EP-0007-US-0001-TK-0005-combat-id-compatibility](EP-0007-US-0001-TK-0005-combat-id-compatibility/EP-0007-US-0001-TK-0005-combat-id-compatibility.md) — Совместимость ID торпед с загрузчиком | 4 | engine | AC-0003 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
