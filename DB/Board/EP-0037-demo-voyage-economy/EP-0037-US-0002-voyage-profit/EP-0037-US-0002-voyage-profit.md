---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0037-demo-voyage-economy
story: EP-0037-US-0002-voyage-profit
title: Расходы и прибыль понятны игроку
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0033-US-0001-station-repairs, EP-0033-US-0002-ammunition-supply, EP-0033-US-0005-crew-upkeep, EP-0034-US-0005-transport-continuation, EP-0036-US-0004-reward-continuation]
created: 2026-11-16T19:49:59Z
implementation_status: not-started
---

# Расходы и прибыль понятны игроку

## Связь с техническим заданием

Одобренный план, Д7: Экономика рейса и первое улучшение. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Развитие EP-0001 и сокращённый баланс EP-0021 US-0007. Старые стартовые 56000 Tokens и пустая корзина сборки не являются параметрами демо.

## User story

Как игрок, я вижу деньги до рейса, обязательные расходы и финансовый результат после него. Торговля, перевозки и боевой доход представлены отдельно и складываются без двойного учёта.

## Зависимости

- [EP-0033-US-0001-station-repairs](../../EP-0033-demo-company-services/EP-0033-US-0001-station-repairs/EP-0033-US-0001-station-repairs.md)
- [EP-0033-US-0002-ammunition-supply](../../EP-0033-demo-company-services/EP-0033-US-0002-ammunition-supply/EP-0033-US-0002-ammunition-supply.md)
- [EP-0033-US-0005-crew-upkeep](../../EP-0033-demo-company-services/EP-0033-US-0005-crew-upkeep/EP-0033-US-0005-crew-upkeep.md)
- [EP-0034-US-0005-transport-continuation](../../EP-0034-demo-transport-contracts/EP-0034-US-0005-transport-continuation/EP-0034-US-0005-transport-continuation.md)
- [EP-0036-US-0004-reward-continuation](../../EP-0036-demo-bounties-and-loot/EP-0036-US-0004-reward-continuation/EP-0036-US-0004-reward-continuation.md)

## Completion evidence

- **AC-01.** Сводка включает себестоимость проданного товара, топливо, сборы, ремонт, найм/жалованье, выплаты и штрафы.
- **AC-02.** До вылета видны сроки, каюты, топливо и готовность крыла; прогноз отделён от факта.
- **AC-03.** Неизвестная себестоимость и неоплаченные обязательства обозначены явно.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Не превращать экран в новый источник экономических правил; решение принадлежит Engine.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Пользователь одобрил общий план и поручил материализацию. Детализация этой истории подготовлена агентом в рамках одобренного объёма; отдельное human approval формулировок и допущений не заявляется. Исторически story detail был draft; текущий workflow status указан в frontmatter и новой карте ниже. Это не препятствует завершению запрошенного создания документов и не разрешает реализацию.

- 2026-11-16T19:49:59Z — время фиксации запроса в Board, не доказанное время отправки: «План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась. При будущей подготовке соблюдать предел пяти тикетов на историю; если детализация не помещается, сначала разделить story.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Создание Markdown не закрывает эти gates.


## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Сводка включает себестоимость проданного товара, топливо, сборы, ремонт, найм/жалованье, выплаты и штрафы.
- **AC-02.** До вылета видны сроки, каюты, топливо и готовность крыла; прогноз отделён от факта.
- **AC-03.** Неизвестная себестоимость и неоплаченные обязательства обозначены явно.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0033-US-0001-station-repairs](../../EP-0033-demo-company-services/EP-0033-US-0001-station-repairs/EP-0033-US-0001-station-repairs.md)
- [EP-0033-US-0002-ammunition-supply](../../EP-0033-demo-company-services/EP-0033-US-0002-ammunition-supply/EP-0033-US-0002-ammunition-supply.md)
- [EP-0033-US-0005-crew-upkeep](../../EP-0033-demo-company-services/EP-0033-US-0005-crew-upkeep/EP-0033-US-0005-crew-upkeep.md)
- [EP-0034-US-0005-transport-continuation](../../EP-0034-demo-transport-contracts/EP-0034-US-0005-transport-continuation/EP-0034-US-0005-transport-continuation.md)
- [EP-0036-US-0004-reward-continuation](../../EP-0036-demo-bounties-and-loot/EP-0036-US-0004-reward-continuation/EP-0036-US-0004-reward-continuation.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0037-US-0002-TK-0001-contracts](EP-0037-US-0002-TK-0001-contracts/EP-0037-US-0002-TK-0001-contracts.md) | Контракт: Расходы и прибыль понятны игроку | contracts | EP-0033-US-0001-TK-0004-client, EP-0033-US-0002-TK-0004-client, EP-0033-US-0005-TK-0003-client, EP-0034-US-0005-TK-0002-transport-flow, EP-0036-US-0004-TK-0002-reward-report | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0037-US-0002-TK-0002-engine](EP-0037-US-0002-TK-0002-engine/EP-0037-US-0002-TK-0002-engine.md) | Правила: Расходы и прибыль понятны игроку | engine | EP-0037-US-0002-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0037-US-0002-TK-0004-localized-feedback](EP-0037-US-0002-TK-0004-localized-feedback/EP-0037-US-0002-TK-0004-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0037-US-0002-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0037-US-0002-TK-0003-client](EP-0037-US-0002-TK-0003-client/EP-0037-US-0002-TK-0003-client.md) | Экран и причины отказа: Расходы и прибыль понятны игроку | client | EP-0037-US-0002-TK-0004-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.

