---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0003-torpedo-self-destruct
title: "Игрок прекращает полёт своей торпеды"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Игрок прекращает полёт своей торпеды

## Вход и user story

Как игрок, я прекращаю затянувшуюся атаку кнопкой в панели аппарата. Торпеда взрывается без урона и освобождает оператора, даже на паузе. Преследовавшая её противоракета теряет цель.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Кнопка активна только для своей летящей торпеды; на паузе действие выполняется сразу.
- **AC-0002:** Нет урона/броска: один красный взрыв, оператор свободен; след самоуничтоженной торпеды исчезает через2real seconds.
- **AC-0003:** Потерявшая цель PR сразу удаляется, её reload начинается без coast; повтор команды не создаёт второй эффект.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0003-TK-0001-self-destruct-contract](EP-0007-US-0003-TK-0001-self-destruct-contract/EP-0007-US-0003-TK-0001-self-destruct-contract.md) — Команда и причина завершения торпеды | 12 | contracts | AC-0001, AC-0002 |
| [EP-0007-US-0003-TK-0002-self-destruct-execution](EP-0007-US-0003-TK-0002-self-destruct-execution/EP-0007-US-0003-TK-0002-self-destruct-execution.md) — Немедленное самоуничтожение без урона | 13 | engine | AC-0001, AC-0002, AC-0003 |
| [EP-0007-US-0003-TK-0003-self-destruct-content](EP-0007-US-0003-TK-0003-self-destruct-content/EP-0007-US-0003-TK-0003-self-destruct-content.md) — Команда самоуничтожения в каталоге | 14 | content-data | AC-0001 |
| [EP-0007-US-0003-TK-0004-self-destruct-panel](EP-0007-US-0003-TK-0004-self-destruct-panel/EP-0007-US-0003-TK-0004-self-destruct-panel.md) — Кнопка Самоуничтожение и визуальный результат | 16 | client | AC-0001, AC-0002 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
