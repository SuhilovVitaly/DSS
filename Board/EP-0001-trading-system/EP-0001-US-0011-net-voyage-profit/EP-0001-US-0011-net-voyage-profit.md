---
epic: EP-0001-trading-system
story: EP-0001-US-0011-net-voyage-profit
title: Чистая прибыль завершённого рейса
stage: approved
dependencies: [EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0010-cargo-cost-and-trade-receipts]
created: 2026-09-21T12:56:24Z
source_request: "создай тикеты D:\\DeepSpaceSaga\\DSS\\Board\\EP-0001-trading-system\\EP-0001-US-0011-net-voyage-profit\\EP-0001-US-0011-net-voyage-profit.md"
current_review: complete
revision: 1
---

# Чистая прибыль завершённого рейса

## Входное техническое задание

Точное сообщение пользователя сохранено в `source_request`. Grounding зафиксирован 2026-09-21T12:56:24Z UTC; это время фиксации контекста, а не утверждение о времени отправки сообщения.

Источники: `Documentation/02-FirstRelease/Mechanics/TradingSystemConcept.md:137–147,207–219`, `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:403–417,463–465`, решения эпика `../Documentation.md:97–104,147–150`. Все относительные пути ниже считаются от `D:/DeepSpaceSaga/DSS`.

## User story

Как игрок, я хочу видеть чистую прибыль или убыток завершённого рейса, чтобы сравнивать маршруты по реальному заработку. Authoritative сводка разделяет выручку, реализованную себестоимость проданного груза, фактический расход топлива рейса, начисленные и оплаченные портовые сборы, событийные расходы и реальные пассажирские выплаты/штрафы. Непроданный груз остаётся активом и не создаёт реализованную прибыль; частичная продажа учитывает только исполненное количество. Finance и существующая Trade history показывают одни и те же готовые значения Engine без повторного расчёта или повторного списания.

## Acceptance criteria

- AC-01: Для прибыльного, убыточного и безубыточного рейса Engine публикует компоненты и `NetProfitCredits = GrossSalesCredits - CostOfGoodsSoldCredits - RouteFuelCostCredits - PortFeesAssessedCredits - EventCostsCredits + PassengerPayoutCredits - PassengerPenaltyCredits`. Все денежные поля — checked `Int64`; Client не пересчитывает формулу.
- AC-02: Рейс начинается только после принятого Undock, получает stable `VoyageId`, закрывает транспортную фазу при Docking или явном прерывании и не принимает одно событие/receipt дважды. Fuel входит только через `VoyageFuelSettlementSnapshot.RouteFuelCostCredits`; Refuel purchase не списывается второй раз. Каждый port fee относится ровно к ledger, активному на границе начисления.
- AC-03: Sell учитывает только `TradeExecutionReceipt.ExecutedQuantity`, `TotalCredits` и nullable `RealizedCargoCostCredits`. Непроданный остаток и его known/unknown basis отображаются отдельно и не входят в net profit; partial Sell оставляет остаток. При unknown COGS выручка остаётся видимой, но COGS и net profit помечаются unavailable, а не считаются от нуля.
- AC-04: `PortFeesAssessedCredits` уменьшает прибыль даже при возникновении долга; отдельно видны `PortFeesPaidCredits` и `OutstandingPortFeeDebtCredits`. Ненулевые passenger payout/penalty и event costs показываются отдельными строками, но история не создаёт фиктивные операции для отсутствующих механик. Finance и Trade history дедуплицируют один и тот же `VoyageId` и показывают согласованные authoritative значения.

## Non-goals

Не реализовывать Undock/state machine, route selection, топливную формулу, cargo cost basis, торговые котировки/исполнение, пассажирские контракты, рыночные события, Save/Load ledger или balance tuning заново. Не создавать новый экран, бухгалтерскую книгу без ограничения, налоговую модель, переоценку непроданного cargo по рынку или прибыль по разнице общего Credits. Общая persistence/migration экономики принадлежит US-0012, десятидневный баланс — US-0013.

## Dependencies

- `EP-0001-US-0006-repeatable-trading-voyage` предоставляет проверенный A → B → A flow и consumer contract lifecycle US-0014. Его approved tickets остаются planning artifacts, а не доказательством runtime API (`EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-repeatable-trading-voyage.md:40–56,79–95`).
- `EP-0001-US-0009-voyage-fuel-cost` предоставляет nullable active fuel fields и authoritative `VoyageFuelSettlementSnapshot` с stable `VoyageId` и route cost (`EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0001-voyage-fuel-contract/EP-0001-US-0009-TK-0001-voyage-fuel-contract.md:41–70`), а TK-0004 — exactly-once settlement.
- `EP-0001-US-0010-cargo-cost-and-trade-receipts` предоставляет `TradeExecutionReceipt.TotalCredits`, `ExecutedQuantity`, nullable realized COGS/gross result (`EP-0001-US-0010-cargo-cost-and-trade-receipts/EP-0001-US-0010-TK-0001-cargo-result-contract/EP-0001-US-0010-TK-0001-cargo-result-contract.md:38–57`) и persisted cargo basis.
- Транзитивный gate `EP-0001-US-0014-voyage-lifecycle` пока draft и не имеет production API (`EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-voyage-lifecycle.md:15–31`). До исполнения TK-0002 её actual signatures сверяются с assumptions, а несовпадение возвращает тикет в review.

## Grounding

- Engine владеет механикой и состоянием; Client читает immutable snapshot и не обращается к Engine из render loop: `Documentation/00-Process/CLAUDE.md:22–56,96–103`; `Documentation/01-Requirements/EngineRequirements.md:362–419`.
- Текущий `AuthoritativeSnapshot` заканчивается `SimulationTimeMs` и не содержит voyage finance: `src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs:11–60`.
- Текущий trade result содержит только nullable `ExecutedQuantity`; richer receipt является planned dependency US-0003/US-0010: `src/DeepSpaceSaga.Contracts/CommandResult.cs:56–71`.
- Sell сейчас меняет Credits/cargo и сообщает partial quantity, но не строит финансовый ledger: `src/DeepSpaceSaga.Engine/SimulationEngine.cs:2072–2112`.
- Портовый daily fee отдельно вычисляет assessed amount, paid amount и debt, но не атрибутирует их рейсу: `src/DeepSpaceSaga.Engine/SimulationEngine.PortFees.cs:29–48`. Первичный fee при docking проходит через dialogue transaction: `src/DeepSpaceSaga.Engine/Dialogue/DialogueEffectTransaction.cs:31–53,79–93` и commit `SimulationEngine.Dialogue.cs:165–174`.
- Finance всё ещё рисует торговый placeholder и только текущий next fee/debt: `src/DeepSpaceSaga.Client/UI/Screens/Finance/FinanceScreen.cs:90–95,178–203`.
- Trade journal живёт в session handle, ограничен 50 строками и сейчас выводит локальное `quantity × UnitPrice`: `src/DeepSpaceSaga.Client/GameSessionHandle.cs:86–87`; `src/DeepSpaceSaga.Client/UI/Screens/Trade/TradeModel.cs:58–82`; `TradeScreen.Render.cs:173–215`. US-0010 заменяет торговую строку authoritative receipt, но не добавляет voyage grouping.
- Денежный путь не использует float/double, округление половины — away from zero: `Documentation/01-Requirements/EngineRequirements.md:5247–5265`.

## Invariants and assumptions

- A-01: Docking/прерывание закрывает физическую фазу рейса. Финансовая запись переходит в `awaiting_realization`, принимает продажи на станции назначения и окончательно замораживается непосредственно перед следующим принятым Undock. Это позволяет выполнить одновременно заданную границу рейса и показать post-arrival Sell; новый рейс не поглощает продажи предыдущего.
- A-02: При Undock ledger фиксирует по item количество и known/unknown basis реально увозимого cargo. После arrival Sell относится к последнему прибывшему voyage максимум в пределах его remaining quantity; лишнее исполненное количество остаётся вне этого ledger. Proceeds и known realized COGS делятся пропорционально attributed quantity один раз с `MidpointRounding.AwayFromZero`; сумма никогда не превышает receipt totals.
- A-03: Buy на станции назначения не является расходом завершённого входящего рейса; он попадёт в opening cargo следующего принятого Undock. Unsold value не включается в net profit.
- A-04: Для net profit вычитается весь assessed port fee, включая созданный долг. Paid и outstanding показываются отдельно; погашение старого долга не создаёт второй expense.
- A-05: Route fuel cost приходит только из terminal fuel settlement US-0009. Цена Refuel остаётся basis топлива/актива до фактического consumption и не записывается как второй expense.
- A-06: Любая attributed продажа с `RealizedCargoCostCredits = null` ставит `HasUnknownCostOfGoodsSold = true`, оставляет `CostOfGoodsSoldCredits`/`NetProfitCredits` nullable и не скрывает GrossSales.
- A-07: Passenger/event поля принимают только фактические authoritative postings. Текущий код имеет пассажиров/deadline state, но не monetary payout/penalty handler (`src/DeepSpaceSaga.Contracts/TimedContractState.cs:7–8`; `src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs:41–48`), поэтому нули означают отсутствие операции, а не реализованную механику.
- A-08: Snapshot хранит последние 50 записей newest-last по stable `VoyageId`, согласованно с текущим bounded Trade journal; это session history. Persistence и migration коллекции выполняет US-0012.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| EP-0001-US-0011-TK-0001-voyage-finance-contract | Контракт финансового результата рейса | contracts | US-0009 TK-0001, US-0010 TK-0001, US-0014 contracts | AC-01, AC-03, AC-04 |
| EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle | Жизненный цикл и расходы рейсного ledger | engine | TK-0001, US-0006, US-0009 TK-0004, US-0010 TK-0002, US-0014 | AC-01, AC-02, AC-04 |
| EP-0001-US-0011-TK-0003-voyage-profit-realization | Реализация грузовой прибыли и snapshot | engine | TK-0001, TK-0002, US-0010 TK-0003 | AC-01, AC-02, AC-03, AC-04 |
| EP-0001-US-0011-TK-0004-voyage-profit-texts | Локализованные строки рейсовой прибыли | content-data | TK-0001 | AC-01, AC-03, AC-04 |
| EP-0001-US-0011-TK-0005-voyage-profit-presentation | Сводка в Finance и Trade history | client | TK-0001, TK-0003, TK-0004, US-0010 TK-0005 | AC-01, AC-03, AC-04 |

Dependency order: TK-0001 → TK-0002 → TK-0003; TK-0004 можно выполнить после TK-0001 параллельно с Engine; TK-0005 — последним. Files touched: 3 / 5 / 4 / 3 / 5. Matching tests: Contracts.Tests / Engine.Tests / Engine.Tests / Client.Tests / Client.Tests.

## Gaps and backlog

- G-01: US-0014 остаётся draft без tickets/runtime API. US-0006/0009/0010 и их signatures — approved planning artifacts, не текущий код. Перед реализацией каждый ticket проверяет фактические dependency files; scope нельзя расширять молча.
- G-02: Save/Load active/recent ledgers, migration и восстановление без повторного posting принадлежат US-0012. US-0011 задаёт runtime state и snapshot, которые US-0012 обязана persist.
- G-03: Реальных monetary passenger payout/penalty и route event cost handlers в проверенном коде нет. Контракт и единая posting seam готовы, но никакие суммы не генерируются до stories-владельцев этих механик.
- G-04: Unlimited audit archive, аналитика по нескольким сессиям, налоги, амортизация, страхование и market-value непроданного cargo не входят.
- G-05: `$requirements-engineer`, упомянутый `Documentation/00-Process/AGENTS.md:3–14`, отсутствует среди доступных skills; применён предоставленный workflow DSS-StoryBuilder.
- Блокирующих вопросов нет; противоречие Docking vs. post-arrival Sell разрешено явной двухфазной моделью A-01 без изменения пользовательского результата.

## Decision and review log

- 2026-09-21T12:56:24Z — выбран canonical story; IDs эпика/story в пути, frontmatter, папке и имени файла совпали, ticket folders отсутствовали, следующий номер `0001`.
- Grounding: прочитаны обязательные process/architecture/EngineRequirements, эпик, story, dependency stories/tickets и релевантные code/test surfaces. Обнаружены пользовательские незакоммиченные изменения вне US-0011; они не изменялись.
- Plan-review: автоматически принят split из пяти merge units; assumptions A-01…A-08 закрывают неблокирующие пробелы и не реализуют соседние stories.
- Созданы TK-0001…TK-0005 в dependency order; production code, epic Documentation и requirements не менялись.
- 2026-09-21T12:56:24Z — complete: canonical story и пять ticket artifacts подготовлены; implementation, dotnet tests/build и UI smoke не выполнялись.
