---
epic: EP-0007-countermeasure-combat
story: EP-0007-US-0002-countermeasure-equipped-tetrarch
title: "Два аппарата и два оператора на Тетрархе"
stage: draft
created: 2026-10-04
revision: 1
current_review: complete
---

# Два аппарата и два оператора на Тетрархе

## Вход и user story

Как игрок, я начинаю существующий бой на Тетрархе с торпедным и противоракетным аппаратами. Пират имеет такую же комплектацию и двух операторов, но использует только защиту. Новое помещение не сдвигает старые модули.

Источник: интервью по противоракетам, [решения и assumptions эпика](../Documentation.md). Числовые ответы трактуются в контексте вопроса, не как самостоятельные спецификации.

## Acceptance criteria

- **AC-0001:** Оба корабля PlayerShipOnly получают PR в новой cell(5,2), двух разных операторов с навыками50 и base ratings30.
- **AC-0002:** Автозащита включена по умолчанию; PR speed12km/s, turn90°/s, radius100km, reload10s; бесконечный боезапас.
- **AC-0003:** Другие стандартные Тетрархи сохраняют согласованную схему; MarketProfiles не переклассифицируется; исходные позиции/курсы/HP и пассивное торпедное оружие пирата сохранены.

## Ticket map

План draft, не результат реализации. Зависимости и порядок приведены для каждого тикета; они могут пересекать истории.

| Тикет | Шаг | Layer | Критерии |
|---|---|---|---|
| [EP-0007-US-0002-TK-0001-countermeasure-contract](EP-0007-US-0002-TK-0001-countermeasure-contract/EP-0007-US-0002-TK-0001-countermeasure-contract.md) — Состояния аппарата и противоракеты | 5 | contracts | AC-0001, AC-0002 |
| [EP-0007-US-0002-TK-0002-countermeasure-catalog](EP-0007-US-0002-TK-0002-countermeasure-catalog/EP-0007-US-0002-TK-0002-countermeasure-catalog.md) — Каталог аппарата и команд защиты | 6 | content-data | AC-0001, AC-0002 |
| [EP-0007-US-0002-TK-0003-combat-scenario-operators](EP-0007-US-0002-TK-0003-combat-scenario-operators/EP-0007-US-0002-TK-0003-combat-scenario-operators.md) — Новые помещения и экипажи боевого сценария | 7 | content-data | AC-0001, AC-0003 |
| [EP-0007-US-0002-TK-0004-other-tetrarch-scenarios](EP-0007-US-0002-TK-0004-other-tetrarch-scenarios/EP-0007-US-0002-TK-0004-other-tetrarch-scenarios.md) — Комплектация остальных стандартных Тетрархов | 8 | content-data | AC-0003 |
| [EP-0007-US-0002-TK-0005-defense-bootstrap](EP-0007-US-0002-TK-0005-defense-bootstrap/EP-0007-US-0002-TK-0005-defense-bootstrap.md) — Инициализация автоматической защиты обоих кораблей | 11 | engine | AC-0001, AC-0002, AC-0003 |

## Dependencies и invariants

Исполнять по [общему порядку](../Tickets.md), после реальной готовности зависимостей. Engine authoritative; physical иreal time разделены; результаты не перебрасываются приSaveLoad. Каждое AC покрыто named tests тикетов.

## Non-goals и backlog

Только результат этой истории; соседние реализации не добавлять внеallowlist. Общие исключения и технические допущения находятся в эпике. Native acceptance подтверждается отдельно в US9; статус draft не означает готовность к релизу.
