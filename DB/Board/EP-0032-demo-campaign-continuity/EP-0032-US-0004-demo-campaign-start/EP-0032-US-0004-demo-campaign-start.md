---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0032-demo-campaign-continuity
story: EP-0032-US-0004-demo-campaign-start
title: Новая кампания в демо-кластере
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0031-US-0005-reduced-demo-contract, EP-0032-US-0007-consistent-campaign-actions]
created: 2026-11-16T20:02:53Z
implementation_status: implemented
---

# Новая кампания в демо-кластере

## Связь с техническим заданием

Одобренный план, Д1–Д2: граница мира и старт кампании. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). История добавлена при сверке Board с сокращённым планом 2026-11-16: у этого результата плана не было владельца.

## User story

Как новый игрок, я выбираю «Новая игра», прохожу настоящий учебный бой и попадаю на карту демо-кластера. В кластере 4–6 станций с различимыми ролями: торговля, доски заказов, место первого сюжетного контакта и район пиратской активности. Я не вижу служебных сценариев разработки и не могу случайно уйти в непроверенную часть мира.

## Зависимости

- EP-0031-US-0005-reduced-demo-contract — канонический внешний prerequisite.
- [EP-0032-US-0007-consistent-campaign-actions](../EP-0032-US-0007-consistent-campaign-actions/EP-0032-US-0007-consistent-campaign-actions.md)

## Completion evidence

- **AC-01.** Публичный путь «Новая игра» → учебный бой → карта ведёт в один демо-кластер; служебные сценарии (Default, Default_500, Docked, Undocked, PlayerShipOnly, MarketProfiles) не предлагаются в публичном профиле и остаются доступны разработке.
- **AC-02.** Состав кластера (4–6 станций, их роли, торговые профили, район пиратов и стартовая станция) закреплён контентом или фиксированным seed и одинаково материализуется при новой игре и после загрузки.
- **AC-03.** Объекты вне демо-кластера недоступны для полёта или явно помечены как не входящие в демо; карта не обещает недоступных маршрутов и станций.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Решить до тикетов: рукописный сценарий кластера или фиксированный seed текущего генератора (EP-0018). Роли станций должны дать опору EP-0034, EP-0035, EP-0037 и EP-0039. Учебный бой остаётся настоящим по EP-0029.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Добавлена агентом по поручению владельца найти и добавить недостающие истории. Отдельное human approval формулировок не заявляется: story stage — draft, plan_status — approved (сокращённый план одобрен ранее).

- 2026-11-16T20:02:53Z — время фиксации поручения в Board, не доказанное время отправки: «на основе этого пройди по созданными эпикам и сторисам в  D:\DeepSpaceSaga\DSS\DB\Board\ и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Публичный путь «Новая игра» → учебный бой → карта ведёт в один демо-кластер; служебные сценарии (Default, Default_500, Docked, Undocked, PlayerShipOnly, MarketProfiles) не предлагаются в публичном профиле и остаются доступны разработке.
- **AC-02.** Состав кластера (4–6 станций, их роли, торговые профили, район пиратов и стартовая станция) закреплён контентом или фиксированным seed и одинаково материализуется при новой игре и после загрузки.
- **AC-03.** Объекты вне демо-кластера недоступны для полёта или явно помечены как не входящие в демо; карта не обещает недоступных маршрутов и станций.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- EP-0031-US-0005-reduced-demo-contract — канонический внешний prerequisite.
- [EP-0032-US-0007-consistent-campaign-actions](../EP-0032-US-0007-consistent-campaign-actions/EP-0032-US-0007-consistent-campaign-actions.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0032-US-0004-TK-0001-contracts](EP-0032-US-0004-TK-0001-contracts/EP-0032-US-0004-TK-0001-contracts.md) | Контракт: Новая кампания в демо-кластере | contracts | EP-0031-US-0005-reduced-demo-contract, EP-0032-US-0007-TK-0005-campaign-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0032-US-0004-TK-0002-content](EP-0032-US-0004-TK-0002-content/EP-0032-US-0004-TK-0002-content.md) | Контент: Новая кампания в демо-кластере | content-data | EP-0032-US-0004-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0032-US-0004-TK-0003-engine](EP-0032-US-0004-TK-0003-engine/EP-0032-US-0004-TK-0003-engine.md) | Правила: Новая кампания в демо-кластере | engine | EP-0032-US-0004-TK-0002-content | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0032-US-0004-TK-0005-localized-feedback](EP-0032-US-0004-TK-0005-localized-feedback/EP-0032-US-0004-TK-0005-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0032-US-0004-TK-0003-engine | AC-01, AC-02, AC-03, AC-04 |
| 5. [EP-0032-US-0004-TK-0004-client](EP-0032-US-0004-TK-0004-client/EP-0032-US-0004-TK-0004-client.md) | Экран и причины отказа: Новая кампания в демо-кластере | client | EP-0032-US-0004-TK-0005-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Execution — 2026-11-17

Five tickets implemented. Public demo uses fixed seed, five stations and shared starter; tutorial stays in the same world and save/load does not regenerate it. Development profile retains service scenarios. Existing five-node TradingMap geometry reused rather than the full multi-district generator. Cumulative Engine 22, Contracts 7, Client 14 passed. Native/product acceptance OPEN. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0032/20261117-us0004/.
