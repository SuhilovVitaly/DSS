---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0034-demo-transport-contracts
story: EP-0034-US-0002-passenger-contract
title: Пассажирский заказ от посадки до оплаты
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0034-US-0001-passenger-ready-start, EP-0034-US-0006-station-job-board]
created: 2026-11-16T19:49:59Z
implementation_status: implemented
---

# Пассажирский заказ от посадки до оплаты

## Связь с техническим заданием

Одобренный план, Д4: Контракты пассажирских и грузовых перевозок. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Переиспользовать пассажирские данные, календарь и voyage ledger; закончить доступный игроку цикл. Новая демо-комплектация не меняет старые сохранения EP-0029.

## User story

Как игрок, я выбираю обычный или срочный пассажирский заказ с известными сроком, пунктом назначения и оплатой. После принятия реальные пассажиры занимают места, а законная доставка завершает обязательство.

## Зависимости

- [EP-0034-US-0001-passenger-ready-start](../EP-0034-US-0001-passenger-ready-start/EP-0034-US-0001-passenger-ready-start.md)
- [EP-0034-US-0006-station-job-board](../EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md)

## Completion evidence

- **AC-01.** До принятия видны маршрут, люди, срок и выплата; неподходящая вместимость блокирует приём без списаний.
- **AC-02.** Журнал показывает активный заказ; пассажиры участвуют в жизнеобеспечении.
- **AC-03.** Доставка на нужную станцию высаживает нужных людей и даёт ровно одну выплату с проводкой рейса.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Без VIP, комфорта, самостоятельных диалоговых веток пассажиров и привлечения пиратов контрактом.

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

- **AC-01.** До принятия видны маршрут, люди, срок и выплата; неподходящая вместимость блокирует приём без списаний.
- **AC-02.** Журнал показывает активный заказ; пассажиры участвуют в жизнеобеспечении.
- **AC-03.** Доставка на нужную станцию высаживает нужных людей и даёт ровно одну выплату с проводкой рейса.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0034-US-0001-passenger-ready-start](../EP-0034-US-0001-passenger-ready-start/EP-0034-US-0001-passenger-ready-start.md)
- [EP-0034-US-0006-station-job-board](../EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0034-US-0002-TK-0001-contracts](EP-0034-US-0002-TK-0001-contracts/EP-0034-US-0002-TK-0001-contracts.md) | Контракт: Пассажирский заказ от посадки до оплаты | contracts | EP-0034-US-0001-TK-0004-client, EP-0034-US-0006-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0034-US-0002-TK-0002-engine](EP-0034-US-0002-TK-0002-engine/EP-0034-US-0002-TK-0002-engine.md) | Правила: Пассажирский заказ от посадки до оплаты | engine | EP-0034-US-0002-TK-0001-contracts | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0034-US-0002-TK-0004-localized-feedback](EP-0034-US-0002-TK-0004-localized-feedback/EP-0034-US-0002-TK-0004-localized-feedback.md) | RU/EN условия, результаты и причины отказа | content-data | EP-0034-US-0002-TK-0002-engine | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0034-US-0002-TK-0003-client](EP-0034-US-0002-TK-0003-client/EP-0034-US-0002-TK-0003-client.md) | Экран и причины отказа: Пассажирский заказ от посадки до оплаты | client | EP-0034-US-0002-TK-0004-localized-feedback | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Story validation (2026-11-17)

Implementation: 4/4 tickets implemented and published on `feature/ep-0034-demo-transport-contracts` (origin). Automated story-scoped tests: own tests PASS; neighbouring regressions show no new failure versus the US-0006 base `1f3b1eb1`. Native window, manual playtest and product acceptance: **NOT RUN / OPEN** (a headless raster render of the Contracts screen was inspected, which is not a native check).

| Ticket | Commit(s) |
|---|---|
| EP-0034-US-0002-TK-0001-contracts | `7533bdd1` |
| EP-0034-US-0002-TK-0002-engine | `d40e99f1` |
| EP-0034-US-0002-TK-0004-localized-feedback | `82eade83` |
| EP-0034-US-0002-TK-0003-client | `29f221bf` |

Result: accepting a passenger offer of the station board (the single `AcceptJobOfferCommand`) boards that many real passengers (`{contractId}:pax-{n}` in `ship.Passengers`) in the same commit — they take cabins (capacity and carrier reservation) and eat rations; nothing is charged. A confirmed docking at the destination (docking dialogue finished) no later than the calendar deadline disembarks exactly those passengers, credits the payout once, closes the obligation `Completed` and posts the payout to the arrival voyage ledger (`passenger:{contractId}`). Other stations, repeated snapshots, saves/loads and duplicate commands pay nothing; late arrival is `overdue` without payment (failure handling — US-0003). The Contracts screen shows the terms before acceptance, the confirmed boarding, people aboard, every contract with its delivery state and the confirmed delivery with its payout, in RU/EN.

AC contribution: AC-01 — route, people, deadline and payout on the offer card; no free cabins / no rations / damaged quarters block acceptance by the engine verdict without commands or charges (engine + client tests). AC-02 — the journal and the passenger section show the active contract; passengers eat rations (9 instead of 6 per meal in the engine test) and hold cabins. AC-03 — real command-driven flight with the docking dialogue to the destination: exactly the contract passengers leave, one payout, `PassengerPayoutCredits = payout` on the arrival ledger, credits = twin flight without the contract + payout. AC-04 — RU/EN texts for states and refusals, no resubmission on reopen, Save/Load keeps people and contracts, delivery confirmed on Load is paid once and never again after another Save/Load, corrupted contract/passenger data and a persisted projection reject Load atomically. Native/product evidence for AC-01…AC-04 remains OPEN.

Commands (repository root `D:/DeepSpaceSaga/Dss-Main`, each process ≤ 300000 ms, artifacts in `D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0002-20261117/story/`):

- `dotnet test tests/DeepSpaceSaga.Contracts.Tests -c Release --filter "Demo.|ArchitectureTests|Ep0029"` → 32 passed (`contracts.log`), incl. `DemoEp0034Us0002Tk0001Tests` 3/3.
- `dotnet test tests/DeepSpaceSaga.Engine.Tests -c Release --filter "<US-0001 regression-filter.txt>|DeepSpaceSaga.Engine.Tests.Demo.|.Carrier.|VoyageLedger"` → 368 passed / 8 failed (`engine-reg.log`); the 8 are exactly the US-0006 baseline set (`engine-failed.txt` = `us0006-20261117/story/engine-failed.txt`). All `DemoEp0034*`/`DemoEp0032*` pass, incl. `DemoEp0034Us0002Tk0002Tests` 3/3 and the updated `DemoEp0034Us0006Tk0003Tests`.
- Extra neighbours `EconomyTimeContinuityTests|ClusterVoyageIntegrationTests` (rations, voyages): 14 failures on this head (`engine-econ.log`, `engine-cluster.log`) — the identical 14 fail at base `1f3b1eb1` in a temporary sparse worktree (`base-econ-cluster.log`, `base-econ-cluster-failed.txt` = `head-econ-cluster-failed.txt`; causes: `Carrier state is attached to a ship without a carrier hull profile`, `insufficient_voyage_fuel`), i.e. pre-existing, not from this story. A first combined attempt timed out at 300 s and was split; its output was discarded.
- `dotnet test tests/DeepSpaceSaga.Client.Tests -c Release --filter "<client-filter.txt>"` (US-0001 client filter + `DemoEp0034|ContractsScreenTests|MechanicsPanelTests|StationScreenTests|ManualManeuverInputTests|GameSessionDockAutoOpenTests|Locali|.Carrier.`) → 305 passed / 32 failed (`client-reg.log`); the 32 are exactly the US-0006 set (30 baseline + 2 `SessionHandleBattleTests…Localized` on the missing `TacticalBattle.Refusal.NoAmmo`, absent in main `83782f77` too). Own `DemoEp0034Us0002Tk0003Tests` 5/5 and `Tk0004Tests` 3/3 pass.
- Builds: Contracts.Tests, Engine.Tests, Client.Tests (Release) — 0 warnings, 0 errors.

Defects found during the story: US-0006 deadlines (×2 / ×1.25 of the max-speed ETA, minimum 1 h) were infeasible for any real flight (shortest route ≈ 1.2 h with a modest pilot) — fixed in `d40e99f1` by content multipliers 12000‰ / 8000‰ (TK-0002 A-04); cargo deadline (2500‰) has the same problem and is left to US-0004.

Assumptions (details in tickets): TK-0001 A-01…A-03 (single accept command, passenger view of the obligation, `overdue` without failure semantics); TK-0002 A-01, A-04…A-08 (deadline basis and multipliers, confirmed-docking delivery in the tick, ledger posting only on the arrival voyage, no save-format bump, legacy obligations, names = ids); TK-0004 A-01/A-02; TK-0003 A-01…A-03. Self-review only; no independent review is claimed.
