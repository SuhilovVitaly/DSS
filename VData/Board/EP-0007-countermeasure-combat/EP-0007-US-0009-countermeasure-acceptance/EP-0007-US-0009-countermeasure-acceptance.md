---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0009-countermeasure-acceptance
title: "Проверяемая поставка второго этапа боя"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Проверяемая поставка второго этапа боя

## Вход и user story

Как разработчик, я получаю доказательства полного сценария и явные ручные проверки вместо утверждения готовности по отдельным unit-тестам.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Старые EP6 проверки остаются содержательными; в их fixtures защита явно отключена, в новых доказательствах используется настоящий сценарий с включённой защитой.
- **AC-0002:** End-to-end проверяет успех/промах, три фактических попадания до уничтожения, toggle/selfdestruct иSaveLoad, детерминизм и временные границы.
- **AC-0003:** Native smoke выполнен и записан отдельно; до выполнения статус NOT RUN, а не APPROVED.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0009-TK-0001-legacy-combat-fixtures](EP-0007-US-0009-TK-0001-legacy-combat-fixtures/EP-0007-US-0009-TK-0001-legacy-combat-fixtures.md) — Сохранение доказательств базового боя | 9 | engine | AC-0001 |
| [EP-0007-US-0009-TK-0002-defense-end-to-end](EP-0007-US-0009-TK-0002-defense-end-to-end/EP-0007-US-0009-TK-0002-defense-end-to-end.md) — Полный сценарий и детерминированные границы | 32 | engine | AC-0001, AC-0002 |
| [EP-0007-US-0009-TK-0003-native-defense-acceptance](EP-0007-US-0009-TK-0003-native-defense-acceptance/EP-0007-US-0009-TK-0003-native-defense-acceptance.md) — Интерфейс и ручная приёмка | 33 | client | AC-0001, AC-0002, AC-0003 |
| [EP-0007-US-0009-TK-0004-catalog-regression-compatibility](EP-0007-US-0009-TK-0004-catalog-regression-compatibility/EP-0007-US-0009-TK-0004-catalog-regression-compatibility.md) — Совместимость проверок каталога команд | 15 | engine | AC-0001 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
