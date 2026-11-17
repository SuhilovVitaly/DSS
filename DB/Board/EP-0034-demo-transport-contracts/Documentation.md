---
epic: EP-0034-demo-transport-contracts
title: Контракты пассажирских и грузовых перевозок
stage: approved
created: 2026-11-16T19:49:59Z
source_request: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
current_review: complete
plan_status: approved
approval_scope: "Одобренный план; отдельное human approval детализации историй не заявляется"
epic_approval: "План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации."
epic_approval_utc: 2026-11-16T19:49:59Z
priority: P0
workstream: feature
dependencies: [EP-0032-demo-campaign-continuity]
implementation_status: not-started
---

# EP-0034 — Контракты пассажирских и грузовых перевозок

Д4 одобренного плана. **Объём: 7 пользовательских историй, 27 тикетов** (26 по исходной декомпозиции + исправляющий EP-0034-US-0006-TK-0006-feasible-deadlines, 2026-11-17). Порядок определяется зависимостями конкретных историй, а не номером эпика.

## Исходный запрос и граница поручения

> План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.

Запрос разрешает создание/уточнение Board. Ранее приложенный расширенный план — продуктовый материал, заменённый одобренной сокращённой версией; он не разрешает исполнение или публикацию. Исходное поручение ограничивало этап историями. Текущее поручение ниже разрешает тикеты и дополнительные истории; реализация и публикация не запрашивались.

## Grounding

`src/DeepSpaceSaga.Client/UI/Screens/Contracts/ContractsScreen.cs:69` — экран пока заглушка. `src/DeepSpaceSaga.Engine/SimulationEngine.Carrier.cs:96` — резерв двух мест; BuildCabinReservationProjection на строке 119 вычисляет свободные каюты. `src/DeepSpaceSaga.Contracts/TimedContractState.cs` — record TimedContractState содержит срок, выплату и PassengerIds.

## Переиспользование и граница нового объёма

Переиспользовать пассажирские данные, календарь и voyage ledger; закончить доступный игроку цикл. Новая демо-комплектация не меняет старые сохранения EP-0029.

[Матрица соответствия старым эпикам](../EP-0031-board-and-demo-scope/DemoPlan.md#переиспользование-ep-0021-и-ep-0029) определяет владельцев без дублирования. Старые тикеты не копируются и не становятся автоматически разрешением на реализацию демо.

## Целевой результат

- Готовый носитель с пассажирскими местами
- Пассажирский заказ от посадки до оплаты
- Просрочка и отмена перевозки
- Доставка контрактного груза
- Продолжение рейса со всеми обязательствами

Результаты оцениваются в общем цикле рейса. [Полный сокращённый контракт](../EP-0031-board-and-demo-scope/DemoPlan.md) содержит лимиты, исключения и контрольные точки.

## Пользовательские истории и зависимости

| ID | Результат | Приоритет | Обязательные story dependencies |
|---|---|---|---|
| [EP-0034-US-0001-passenger-ready-start](EP-0034-US-0001-passenger-ready-start/EP-0034-US-0001-passenger-ready-start.md) | Готовый носитель с пассажирскими местами | P0 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start |
| [EP-0034-US-0002-passenger-contract](EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md) | Пассажирский заказ от посадки до оплаты | P0 | EP-0034-US-0001-passenger-ready-start, EP-0034-US-0006-station-job-board |
| [EP-0034-US-0003-contract-failure](EP-0034-US-0003-contract-failure/EP-0034-US-0003-contract-failure.md) | Просрочка и отмена перевозки | P0 | EP-0034-US-0002-passenger-contract |
| [EP-0034-US-0004-cargo-contract](EP-0034-US-0004-cargo-contract/EP-0034-US-0004-cargo-contract.md) | Доставка контрактного груза | P0 | EP-0034-US-0002-passenger-contract, EP-0034-US-0006-station-job-board |
| [EP-0034-US-0005-transport-continuation](EP-0034-US-0005-transport-continuation/EP-0034-US-0005-transport-continuation.md) | Продолжение рейса со всеми обязательствами | P0 | EP-0034-US-0003-contract-failure, EP-0034-US-0004-cargo-contract, EP-0032-US-0003-persistent-outcomes |
| [EP-0034-US-0006-station-job-board](EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md) | Доска заказов станции | P0 | EP-0032-US-0004-demo-campaign-start |
| [EP-0034-US-0007-documentation](EP-0034-US-0007-documentation/EP-0034-US-0007-documentation.md) | Документация проекта по итогам эпика | P0 | EP-0034-US-0001-passenger-ready-start, EP-0034-US-0002-passenger-contract, EP-0034-US-0003-contract-failure, EP-0034-US-0004-cargo-contract, EP-0034-US-0005-transport-continuation, EP-0034-US-0006-station-job-board |

Aggregate dependencies в frontmatter — перечень внешних источников результатов, не барьер «весь эпик завершён». Независимые истории могут разрабатываться после своих prerequisites. Наличие старого implemented статуса не закрывает сквозную приёмку.

## Ограничения, допущения и gaps

- **EP-0034-US-0001-passenger-ready-start:** A-02: точное число добавленных мест и допустимая компоновка — результат выбора content до тикетов; минимум один доступный пассажирский оффер.
- **EP-0034-US-0002-passenger-contract:** Без VIP, комфорта, самостоятельных диалоговых веток пассажиров и привлечения пиратов контрактом.
- **EP-0034-US-0003-contract-failure:** A-03: точный штраф и место высадки при отмене требуют продуктового уточнения перед тикетами; нельзя просто удалить людей в открытом космосе.
- **EP-0034-US-0004-cargo-contract:** Без сопровождения; точные штрафы используют общий договорённый принцип US-0003.
- **EP-0034-US-0005-transport-continuation:** Сохранение является частью контрактов, а не отложенной функцией выпуска.

Общий backlog: пленные, сопровождение, VIP-спавн, полная верфь, пустой старт, фракционная эскалация, бой с базой, большая прокачка, универсальный квестовый движок и тактическое отступление. Тикеты уточняют прежние gaps рабочими A-assumptions; баланс и приёмочные gates остаются открытыми до исполнения.

## Review log

2026-11-16T19:49:59Z — зафиксирован дословный запрос выше (время записи, не время отправки). Уже данное одобрение плана учтено; повторный approval того же объёма не запрашивался. Истории нарезаны как draft-detail под approved план; индивидуальные подтверждения не выдуманы.

- EP-0034-US-0001-passenger-ready-start: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0034-US-0002-passenger-contract: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0034-US-0003-contract-failure: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0034-US-0004-cargo-contract: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- EP-0034-US-0005-transport-continuation: авторская проверка ценности, границ, зависимостей и наблюдаемого evidence; отдельное human review — не проводилось.
- 2026-11-16T20:02:53Z — сверка с сокращённым планом по поручению «на основе этого пройди по созданными эпикам и сторисам … и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие» (время записи): добавлены истории без владельца в плане и обязательная завершающая документационная история; отдельное human review — не проводилось.

- 2026-11-16T20:31:00Z — проверка достаточности EP-0032…EP-0041 для первой демо (время записи): добавлена история без владельца в плане, документационная история перенумерована на последнюю позицию; отдельное human review — не проводилось.


## Epic review log

2026-11-16T19:49:59Z — состав сопоставлен с Д4, сокращённым объёмом и существующими EP-0021/0029. Тикеты не созданы. Проверка ID, локальных ссылок и ацикличности выполняется для всего набора; результаты фиксируются в [общем плане](../EP-0031-board-and-demo-scope/DemoPlan.md).

## Статусы поставки

Planning: подготовлено по одобренному плану. Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Никаких новых свидетельств готовности игры этот документ не создаёт.


## Тикетная декомпозиция первого демо

2026-11-16T20:32:46.441Z — «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Workflow approval, без приписывания human review деталей. Подготовлено 26 тикетов; implementation NOT STARTED.

| Story | Тикетов | Текущие prerequisites |
|---|---:|---|
| [EP-0034-US-0001-passenger-ready-start](EP-0034-US-0001-passenger-ready-start/EP-0034-US-0001-passenger-ready-start.md) | 5 | EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0004-demo-campaign-start |
| [EP-0034-US-0002-passenger-contract](EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md) | 4 | EP-0034-US-0001-passenger-ready-start, EP-0034-US-0006-station-job-board |
| [EP-0034-US-0003-contract-failure](EP-0034-US-0003-contract-failure/EP-0034-US-0003-contract-failure.md) | 4 | EP-0034-US-0002-passenger-contract |
| [EP-0034-US-0004-cargo-contract](EP-0034-US-0004-cargo-contract/EP-0034-US-0004-cargo-contract.md) | 4 | EP-0034-US-0002-passenger-contract, EP-0034-US-0006-station-job-board |
| [EP-0034-US-0005-transport-continuation](EP-0034-US-0005-transport-continuation/EP-0034-US-0005-transport-continuation.md) | 2 | EP-0034-US-0003-contract-failure, EP-0034-US-0004-cargo-contract, EP-0032-US-0003-persistent-outcomes |
| [EP-0034-US-0006-station-job-board](EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md) | 6 (5 + исправляющий TK-0006) | EP-0032-US-0004-demo-campaign-start |
| [EP-0034-US-0007-documentation](EP-0034-US-0007-documentation/EP-0034-US-0007-documentation.md) | 2 | EP-0034-US-0001-passenger-ready-start, EP-0034-US-0002-passenger-contract, EP-0034-US-0003-contract-failure, EP-0034-US-0004-cargo-contract, EP-0034-US-0005-transport-continuation, EP-0034-US-0006-station-job-board |

Порядок ниже — локальная проекция глобального dependency order (не числовая сортировка): EP-0034-US-0001-passenger-ready-start → EP-0034-US-0006-station-job-board → EP-0034-US-0002-passenger-contract → EP-0034-US-0003-contract-failure → EP-0034-US-0004-cargo-contract → EP-0034-US-0005-transport-continuation → EP-0034-US-0007-documentation. Документационная история исполняется после всех функциональных, включая добавленные с большим ID.

[Глобальная карта, полные пути, release gates и открытые допущения](../EP-0041-demo-validation-and-release/TicketPlan.md). Новые API/файлы в тикетах — проект, а не текущая реализация.
