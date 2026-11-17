---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: implementation-review
approval_kind: workflow
revision: 2
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0004-reward-continuation
title: Прозрачный итог боя в кампании
stage: approved
plan_status: executed
priority: P0
workstream: feature
dependencies: [EP-0036-US-0002-bounty-payment, EP-0036-US-0003-simple-loot, EP-0035-US-0004-encounter-continuation, EP-0036-US-0006-module-salvage, EP-0036-US-0007-postbattle-loot-lifecycle, EP-0036-US-0009-prisoner-custody]
created: 2026-11-16T19:49:59Z
implementation_status: implemented
---

# Прозрачный итог боя в кампании

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Связь с техническим заданием

Одобренный план, Д6: Боевые заказы и простая добыча. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Общая доска EP-0034 и мировые встречи EP-0035; вознаграждение за контракт отделено от добычи. Боевой settlement EP-0029 переиспользуется.

## User story

Как игрок, я вижу вместе награду, добычу, расход торпед и потери, понимая цену победы. Продолжение кампании сохраняет этот итог.

## Зависимости

- [EP-0036-US-0002-bounty-payment](../EP-0036-US-0002-bounty-payment/EP-0036-US-0002-bounty-payment.md)
- [EP-0036-US-0003-simple-loot](../EP-0036-US-0003-simple-loot/EP-0036-US-0003-simple-loot.md)
- [EP-0035-US-0004-encounter-continuation](../../EP-0035-demo-pirate-encounters/EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md)
- [EP-0036-US-0006-module-salvage](../EP-0036-US-0006-module-salvage/EP-0036-US-0006-module-salvage.md)
- [EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle/EP-0036-US-0007-postbattle-loot-lifecycle.md)
- [EP-0036-US-0009-prisoner-custody](../EP-0036-US-0009-prisoner-custody/EP-0036-US-0009-prisoner-custody.md)

## Completion evidence

- **AC-01.** Сквозной заказ проходит принятие → бой → добычу → выплату → загрузку без двойного дохода.
- **AC-02.** Неполученная выплата не считается наличными; неизвестная стоимость не показывается как ноль.
- **AC-03.** Поражение и уход не выдают награду за уничтожение цели.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Полный расчёт прибыльности и стоимость ремонта калибруются в EP-0037.

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

- **AC-01.** Сквозной заказ проходит принятие → бой → добычу → выплату → загрузку без двойного дохода.
- **AC-02.** Неполученная выплата не считается наличными; неизвестная стоимость не показывается как ноль.
- **AC-03.** Поражение и уход не выдают награду за уничтожение цели.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0036-US-0002-bounty-payment](../EP-0036-US-0002-bounty-payment/EP-0036-US-0002-bounty-payment.md)
- [EP-0036-US-0003-simple-loot](../EP-0036-US-0003-simple-loot/EP-0036-US-0003-simple-loot.md)
- [EP-0035-US-0004-encounter-continuation](../../EP-0035-demo-pirate-encounters/EP-0035-US-0004-encounter-continuation/EP-0035-US-0004-encounter-continuation.md)
- [EP-0036-US-0006-module-salvage](../EP-0036-US-0006-module-salvage/EP-0036-US-0006-module-salvage.md)
- [EP-0036-US-0007-postbattle-loot-lifecycle](../EP-0036-US-0007-postbattle-loot-lifecycle/EP-0036-US-0007-postbattle-loot-lifecycle.md)
- [EP-0036-US-0009-prisoner-custody](../EP-0036-US-0009-prisoner-custody/EP-0036-US-0009-prisoner-custody.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0036-US-0004-TK-0001-reward-roundtrip](EP-0036-US-0004-TK-0001-reward-roundtrip/EP-0036-US-0004-TK-0001-reward-roundtrip.md) | Единственность добычи и выплат после Load | engine | EP-0036-US-0007-TK-0005-postbattle-continuation, EP-0036-US-0009-TK-0003-capacity-and-rations, EP-0036-US-0002-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0036-US-0004-TK-0002-reward-report](EP-0036-US-0004-TK-0002-reward-report/EP-0036-US-0004-TK-0002-reward-report.md) | Проверяемая сводка боя и оплаты | client | EP-0036-US-0004-TK-0001-reward-roundtrip, EP-0036-US-0006-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0036-US-0004-TK-0003-restored-loot-route](EP-0036-US-0004-TK-0003-restored-loot-route/EP-0036-US-0004-TK-0003-restored-loot-route.md) | Восстановленный маршрут на карту лута | client | EP-0036-US-0007-TK-0004-postbattle-map, EP-0036-US-0007-TK-0005-postbattle-continuation | AC-01, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Актуальный контракт продолжения после груминга

Эта история объединяет postbattle source geometry/loot ledger, целые cargo trophies, prisoners и bounty entitlement. Предыдущие формулировки «сохранение результата после боя» расширены: LootOpen сохраняется и возвращает на тактическую карту без active battle и без prebattle replay. Частичный сбор, terminal attempts, arrest identity и единственный sortie commit переживают Load. Награда к получению, выплаченная награда, стоимость trophies, взятые/оставленные предметы и боевые потери показываются раздельно; оценка не является cash.

Дополнительный тикет [EP-0036-US-0004-TK-0003-restored-loot-route](EP-0036-US-0004-TK-0003-restored-loot-route/EP-0036-US-0004-TK-0003-restored-loot-route.md) — client routing через SkiaWindow/TacticalBattleFlow/GameSessionHandle. Он зависит от US7 TK4/TK5, а не запускает бой заново.

Текущая карта: TK0001 integration roundtrip → TK0002 report; независимо после US7TK4/TK5 выполняется TK0003 recovered route. Все три обязательны для AC-01…04. Формальные dependencies на новые истории перечислены в frontmatter; их incomplete runtime не объявляется готовым тестовым scaffold.
