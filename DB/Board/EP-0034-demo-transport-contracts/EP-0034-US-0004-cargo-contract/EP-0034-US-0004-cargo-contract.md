---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0004-cargo-contract
title: Доставка контрактного груза
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0034-US-0002-passenger-contract, EP-0034-US-0006-station-job-board]
created: 2026-11-16T19:49:59Z
implementation_status: implemented
---

# Доставка контрактного груза

## Связь с техническим заданием

Одобренный план, Д4: Контракты пассажирских и грузовых перевозок. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Переиспользовать пассажирские данные, календарь и voyage ledger; закончить доступный игроку цикл. Новая демо-комплектация не меняет старые сохранения EP-0029.

## User story

Как игрок, я беру грузовой заказ на той же доске и вижу его отдельно от собственного торгового товара. Доставка приносит фиксированную оплату и не требует покупки контрактного груза.

## Зависимости

- [EP-0034-US-0002-passenger-contract](../EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md)
- [EP-0034-US-0006-station-job-board](../EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md)

## Completion evidence

- **AC-01.** Приём резервирует фактическую массу трюма и явно различает контрактный груз и товар игрока.
- **AC-02.** Груз нельзя продать как свободный товар; сдача на нужной станции освобождает трюм и оплачивается один раз.
- **AC-03.** Потеря, отмена и просрочка имеют видимые последствия без дублирования денег или груза.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Без сопровождения; точные штрафы используют общий договорённый принцип US-0003.

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

- **AC-01.** Приём резервирует фактическую массу трюма и явно различает контрактный груз и товар игрока.
- **AC-02.** Груз нельзя продать как свободный товар; сдача на нужной станции освобождает трюм и оплачивается один раз.
- **AC-03.** Потеря, отмена и просрочка имеют видимые последствия без дублирования денег или груза.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0034-US-0002-passenger-contract](../EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md)
- [EP-0034-US-0006-station-job-board](../EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0034-US-0004-TK-0001-contracts](EP-0034-US-0004-TK-0001-contracts/EP-0034-US-0004-TK-0001-contracts.md) | Контракт: Доставка контрактного груза | contracts | EP-0034-US-0002-TK-0003-client, EP-0034-US-0006-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0034-US-0004-TK-0002-engine](EP-0034-US-0004-TK-0002-engine/EP-0034-US-0004-TK-0002-engine.md) | Правила: Доставка контрактного груза | engine | EP-0034-US-0004-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0034-US-0004-TK-0004-localized-feedback](EP-0034-US-0004-TK-0004-localized-feedback/EP-0034-US-0004-TK-0004-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0034-US-0004-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0034-US-0004-TK-0003-client](EP-0034-US-0004-TK-0003-client/EP-0034-US-0004-TK-0003-client.md) | Экран и причины отказа: Доставка контрактного груза | client | EP-0034-US-0004-TK-0004-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Story validation (2026-11-17)

Implementation: 4/4 тикета реализованы и опубликованы в `feature/ep-0034-demo-transport-contracts` (origin). Автоматические story-тесты: собственные PASS; соседние регрессии без новых падений относительно baseline US-0003. Нативное окно, ручной плейтест и продуктовая приёмка: **NOT RUN / OPEN** (headless-рендер экрана Contracts не является нативной проверкой).

| Ticket | Commit |
|---|---|
| EP-0034-US-0004-TK-0001-contracts | `130cf088` |
| EP-0034-US-0004-TK-0002-engine | `0e3d4b9b` |
| EP-0034-US-0004-TK-0004-localized-feedback | `84595dde` |
| EP-0034-US-0004-TK-0003-client | `1bdf2bb2` |

Результат: грузовой заказ принимается той же командой доски (`AcceptJobOfferCommand`) и в том же commit опечатывает фактическую массу груза в трюме. Опечатанный груз — запись кампании, а не стек модуля: обычная торговля не может его продать, а его место недоступно для покупок и новых заказов (quote, исполнение, legacy Buy и блокер приёма учитывают резерв). Подтверждённая стыковка у станции назначения не позже календарного срока закрывает контракт `Completed`, освобождает трюм и платит один раз (сначала гасится долг по контрактам US-0003); выплата проводится в voyage ledger (`cargo:{contractId}`). Отмена/просрочка — общий механизм US-0003 (штраф 25%, долг, `penalty:{contractId}`); груз проваленного контракта остаётся в трюме (`return_pending`) до подтверждённой стыковки с человеческой станцией и сдаётся без оплаты (`returned`; при отмене у станции — сразу); при потере носителя — `lost` без штрафа. Срок груза откалиброван реальным командным полётом: `cargoPermille` 2500 → 12000. Экран Contracts показывает разбиение трюма (опечатано / свои товары / свободно для торговли) до приёма, грузовые контракты с товаром, маршрутом, оплатой, сроком и состоянием, подтверждённое опечатывание, доставку и возврат (RU/EN).

Вклад в AC: AC-01 — резерв `MassKg` при приёме, стеки модуля не меняются, разбиение трюма в snapshot и на экране; нехватка места → `insufficient_cargo_space` без частичной мутации (engine + client). AC-02 — quote продажи опечатанного товара неисполним, покупка ограничена местом после резерва; доставка у станции назначения освобождает трюм и платит один раз (сравнение с полётом-двойником, повторные снапшоты, дубликат команды, Save/Load) (engine). AC-03 — отмена у станции и в космосе, просрочка в космосе и у станции назначения (граница deadline/deadline+1), потеря носителя: штраф один раз, груз не дублируется и не продаётся, возврат один раз; долг другого контракта гасится из выплаты за груз (engine + client). AC-04 — RU/EN тексты состояний/результатов, отказ `insufficient_cargo_space`/`stale_revision` без повторной отправки, реальная сессия и кнопка, повторное открытие без повторной команды, Save/Load резерва/доставки/возврата, старое сохранение US-0006 без секции, атомарный отказ Load на повреждённых данных. Native/product evidence AC-01…AC-04 — OPEN.

Команды (корень `D:/DeepSpaceSaga/Dss-Main`, каждый процесс ≤ 300000 ms, артефакты `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0004-20261117/story/`):

- `dotnet test tests/DeepSpaceSaga.Contracts.Tests -c Release --filter "FullyQualifiedName~Demo.|FullyQualifiedName~ArchitectureTests|FullyQualifiedName~Ep0029"` → 38 passed (`contracts.log`), включая `DemoEp0034Us0004Tk0001Tests` 3/3.
- `dotnet test tests/DeepSpaceSaga.Engine.Tests -c Release --filter "<engine-filter.txt>"` (фильтр US-0003: регрессии US-0001 + `Engine.Tests.Demo.` + `.Carrier.` + `VoyageLedger`) → 374 passed / 8 failed (`engine-reg.log`); 8 — ровно baseline (`engine-failed.txt` совпадает с `us0003-20261117/story/engine-failed.txt`). `DemoEp0034Us0004Tk0002Tests` 3/3 и тесты US-0001/0006/0002/0003 проходят.
- Торговля/трюм/рынок: `--filter "<engine-trade-filter.txt>"` (`CargoCostAccounting`, `MarketKnowledge`, `MarketRevision`, `MarketSaveLoadContinuity`, `QuotedTradeExecution`, `TradeCommand`, `TradeQuoteCalculator`, `TradeQuoteIssuance`, `TradeSnapshotProjection`, `StationMarket*`) → 316 passed / 0 failed (`engine-trade.log`).
- Соседи `EconomyTimeContinuityTests` и `ClusterVoyageIntegrationTests` (раздельно) → 7 + 7 failed (`engine-econ.log`, `engine-cluster.log`); `head-econ-cluster-failed.txt` идентичен baseline `us0003-20261117/story/head-econ-cluster-failed.txt`.
- `dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --filter "<client-filter.txt>"` (фильтр US-0003 + `Trade`) → 506 passed / 32 failed (`client-reg.log`); 32 — ровно набор US-0003 (30 + 2 `NoAmmo`, `client-failed.txt`). Новые `DemoEp0034Us0004Tk0003Tests` 5/5 и `Tk0004Tests` 3/3, клиентские тесты торговли проходят.
- Сборки Contracts.Tests, Engine.Tests, Client.Tests (Release, TreatWarningsAsErrors) — без ошибок.

Изменения прежних ожиданий: нет — существующие тесты не правились. Save format: bump нет (`cargoContracts` — новый необязательный раздел campaign data schema 1; A-P2 не сработал).

Допущения (подробно в тикетах): TK-0001 A-01…A-03; TK-0002 A-01…A-06 и ограничение выдачи груза диалогом/повреждения трюма; TK-0004 A-01 (и уточнение `Demo.TransportFailures.CancelConfirm`); TK-0003 A-01. Только self-review; независимое review не заявляется.
