---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0008-defense-save-load
title: "Бой продолжается после сохранения"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Бой продолжается после сохранения

## Вход и user story

Как игрок, я сохраняю бой в любой фазе и после загрузки получаю то же продолжение без нового броска и повторных эффектов.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Сохраняются операторы, настройкиauto, PR route/trail/phase, frozenchance, attempted flag, reload/coast deadlines, journal, RNG state/version/counter и ID sequences.
- **AC-0002:** SaveLoad до/после встречи не меняет будущий результат; эффекты не восстанавливаются; legacy saves имеют явную migration policy.
- **AC-0003:** Ошибочные связи/состояния отклоняются атомарно; пауза/ускорение и временное разбиение не меняют исход.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0008-TK-0001-defense-save-schema](EP-0007-US-0008-TK-0001-defense-save-schema/EP-0007-US-0008-TK-0001-defense-save-schema.md) — Версия и валидация сохранения защиты | 29 | engine | AC-0001, AC-0002, AC-0003 |
| [EP-0007-US-0008-TK-0002-defense-persistence](EP-0007-US-0008-TK-0002-defense-persistence/EP-0007-US-0008-TK-0002-defense-persistence.md) — Сохранение и восстановление всех фаз | 30 | engine | AC-0001, AC-0002, AC-0003 |
| [EP-0007-US-0008-TK-0003-defense-client-reset](EP-0007-US-0008-TK-0003-defense-client-reset/EP-0007-US-0008-TK-0003-defense-client-reset.md) — Загрузка истории без повторных анимаций | 31 | client | AC-0001, AC-0002 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
