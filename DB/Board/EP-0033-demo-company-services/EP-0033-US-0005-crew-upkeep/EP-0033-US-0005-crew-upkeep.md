---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0033-demo-company-services
story: EP-0033-US-0005-crew-upkeep
title: Понятное содержание компании
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0033-US-0004-pilot-recruitment, EP-0034-US-0002-passenger-contract]
created: 2026-11-16T19:49:59Z
implementation_status: implemented
---

# Понятное содержание компании

## Связь с техническим заданием

Одобренный план, Д3: Обслуживание и восстановление компании. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Сокращённое продолжение EP-0021 US-0001/0002/0003/0006/0010. Основа EP-0029 не дублируется; полная верфь US-0009 и пустой старт US-0008 отложены.

## User story

Как игрок, я вижу ежедневное жалованье и запас жизнеобеспечения до вылета. При недостатке денег получаю понятное предупреждение и единое правило долга, не теряя компанию из-за скрытой механики.

## Зависимости

- [EP-0033-US-0004-pilot-recruitment](../EP-0033-US-0004-pilot-recruitment/EP-0033-US-0004-pilot-recruitment.md)
- [EP-0034-US-0002-passenger-contract](../../EP-0034-demo-transport-contracts/EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md)

## Completion evidence

- **AC-01.** Показать календарное начисление зарплаты без удвоения при смене скорости или загрузке.
- **AC-02.** В расчёте питания учитываются фактические люди, а не резервные пустые места.
- **AC-03.** Долг виден отдельно от наличных; его погашение не дублируется и не блокирует все мирные способы заработка.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

A-01: предлагается видимый долг без автоматического увольнения/морали; точные ставки и порядок взыскания должны быть закреплены до тикетов, не выдавать это за отдельно одобренные числа.

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

- **AC-01.** Показать календарное начисление зарплаты без удвоения при смене скорости или загрузке.
- **AC-02.** В расчёте питания учитываются фактические люди, а не резервные пустые места.
- **AC-03.** Долг виден отдельно от наличных; его погашение не дублируется и не блокирует все мирные способы заработка.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0033-US-0004-pilot-recruitment](../EP-0033-US-0004-pilot-recruitment/EP-0033-US-0004-pilot-recruitment.md)
- [EP-0034-US-0002-passenger-contract](../../EP-0034-demo-transport-contracts/EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0033-US-0005-TK-0001-contracts](EP-0033-US-0005-TK-0001-contracts/EP-0033-US-0005-TK-0001-contracts.md) | Контракт: Понятное содержание компании | contracts | EP-0033-US-0004-TK-0004-client, EP-0034-US-0002-TK-0003-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0033-US-0005-TK-0002-engine](EP-0033-US-0005-TK-0002-engine/EP-0033-US-0005-TK-0002-engine.md) | Правила: Понятное содержание компании | engine | EP-0033-US-0005-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0033-US-0005-TK-0004-localized-feedback](EP-0033-US-0005-TK-0004-localized-feedback/EP-0033-US-0005-TK-0004-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0033-US-0005-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0033-US-0005-TK-0003-client](EP-0033-US-0005-TK-0003-client/EP-0033-US-0005-TK-0003-client.md) | Экран и причины отказа: Понятное содержание компании | client | EP-0033-US-0005-TK-0004-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Story-boundary validation and remaining dependency
Payroll/debt and actual crew/passenger food projection implemented. Final checks: 19 Engine, 12 Contracts, 100 Client PASS (includes real Engine-backed screen service flow and existing rearm regressions). Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0033/20261117-final/Engine.log, Contracts.log, Client3.log and screens/. Headless raster review is not native/product acceptance. EP-0034-US-0002 passenger-contract end-to-end integration remains OPEN; no passenger-contract runtime was fabricated.
