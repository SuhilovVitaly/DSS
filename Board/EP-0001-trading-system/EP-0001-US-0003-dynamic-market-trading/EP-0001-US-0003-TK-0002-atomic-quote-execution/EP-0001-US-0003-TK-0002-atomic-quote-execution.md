---
epic: EP-0001-trading-system
story: EP-0001-US-0003-dynamic-market-trading
ticket: EP-0001-US-0003-TK-0002-atomic-quote-execution
title: Атомарное исполнение котировки
stage: approved
layer: engine
depends_on: [EP-0001-US-0003-TK-0001-trade-execution-contract, EP-0001-US-0001-TK-0002-profile-market-bootstrap, EP-0001-US-0002-TK-0003-hourly-market-simulation]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-07]
created: 2026-09-21T09:33:31Z
revision: 1
---

# Атомарное исполнение котировки

## Why

Одна подтверждённая котировка должна изменить деньги, склад, cargo/tank и revision единой транзакцией. End state: Engine выдаёт точный durable receipt; rejected/stale/overflow и повтор не создают частичных или повторных эффектов.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj. Пути относительно D:/DeepSpaceSaga/DSS.
Дополнительная внешняя prerequisite: EP-0001-US-0015-authoritative-market-quotes, integration contract ниже. Не реализовывать quote subsystem в этом тикете.

## Decisions

2026-09-21T09:33:31Z — сообщение: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0003-dynamic-market-trading\EP-0001-US-0003-dynamic-market-trading.md». Других технических решений пользователя нет.

## Assumptions

Partial только Sell при budget/capacity. Новые profile markets всегда требуют quote; legacy station без MarketProfileId может сохранить unquoted path, но явно переданные quote fields никогда не игнорируются. Quote cache/revision persistence принадлежит US-0015. Journal4096 не расширяется: после eviction replay старого quote отклоняется stale без эффекта; original receipt доступен только пока хранится. Если prerequisite tests профильной торговли ещё посылают unquoted commands, их переход на quotes должен быть завершён в US-0015 до этого тикета; здесь не скрывать дополнительные test files.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | Grounding: :130–143 dedupe; :1660–1674 RecordCommandResult; :1875–2048 trade prepare/commit; CommandStartOutcome в этом файле | Dispatch quoted path, receipt plumbing в outcome/result, общий Fuel guard; сохранить legacy path |
| src/DeepSpaceSaga.Engine/SimulationEngine.TradeExecution.cs | Новый partial; текущая PrepareAndCommitTrade находится в SimulationEngine.cs | Quote validation, immutable staged execution, checked commit и receipt |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs | :76–99 normalizes command journal, :81–83 проверяет IDs | Structural validation новых saved receipts без migrations/нового SaveFormat |
| tests/DeepSpaceSaga.Engine.Tests/QuotedTradeExecutionTests.cs | Новый файл; fixture pattern TradeCommandTests.cs:38–76; command receive/snapshot API в SimulationEngine | Self-contained quote/profile fixtures, atomicity, stale, retry/save/overflow/anti-arbitrage tests |
| tests/DeepSpaceSaga.Engine.Tests/TradeCommandTests.cs | :28–36 command helpers; legacy no-profile fixture :38–76 | Сохранить legacy regressions, явно проверить Fuel guards и отсутствие fallback при quote metadata |

## Public API after the change

Нового session API нет. Partial internal/private methods:
`private CommandStartOutcome PrepareAndCommitQuotedTrade(PlayerCommand command, long gameTimeMs)`.
`RecordCommandResult(..., long? executedQuantity = null, TradeExecutionReceipt? tradeReceipt = null)` получает optional argument в конце. CommandStartOutcome получает optional TradeExecutionReceipt, чтобы rejection записывался существующим caller ровно один раз. Success уже записывается обработчиком, как в действующем flow.

Dependency US-0015 (точная часть integration seam):
- `public TradeQuoteSnapshot GetTradeQuote(TradeQuoteRequest request)` для тестов/transport;
- `private bool TryValidateTradeQuote(PlayerCommand command, out TradeQuoteSnapshot quote, out string reasonCode)` — проверка выданного token, current state и binding, без мутации;
- `private long NextMarketRevision(string stationObjectId)` — checked подготовка;
- `private void CommitMarketRevision(string stationObjectId, long nextRevision)` — non-fallible присвоение/инвалидация под тем же lock.
- Quote содержит QuoteId, MarketRevision, StationObjectId, ObjectId, ModuleId, CommandType, ItemTypeId, RequestedQuantity, ExecutableQuantity, MaximumQuantity, TotalCredits, Curve(Quantity,UnitPriceCredits), DisabledReason, LimitReasons. Curve covers executed prefix, all fields authoritative.
- US-0002: station.MarketBudgetCredits, bounded cargo stock cap из profile Economy; maxBudget=2*scaledInitialBudget. Неограниченные legacy markets используют Credits и существующее отсутствие cargo cap. Не получать лимит из client snapshot.

## Implementation steps

1. Сохранить CommandId gate до торговли: известная команда replay старого result до stale проверки. В trade dispatch при любом QuoteId/MarketRevision отправлять в quoted path. Оба отсутствуют: profile station => QuoteRequired; legacy => старый path. Частично заданные/пустые binding fields или отрицательная MarketRevision => InvalidQuote. Common basic validation сохраняет object/module availability, docked, positive quantity, item existence и command ownership.
2. Перед quoted/legacy mutation проверить storage semantics: trade.buy/trade.sell с item.fuel => FuelTradeForbidden; trade.refuel с любым item кроме item.fuel => FuelTradeForbidden. Refuel остаётся кг, не cargo units. Не конвертировать Buy Fuel в скрытый Refuel. Проверить тип модуля через существующие CommandTypeIds.
3. Под существующей world lock вызвать quote validator и получить issued immutable quote. Он обязан сверить станцию docking, ship/module/item/type/requested quantity и revision, также player balance/cargo/tank/module state с контекстом выдачи. Missing/expired/consumed token или изменение контекста => StaleQuote; token с чужим binding => InvalidQuote. Invalid/stale не обновляет market revision и не исполняет иной quote автоматически.
4. Подготовить local staged values без записи в runtime. Не пересчитывать price formula. Checked проверить curve consistency, executable<=requested и quote.TotalCredits=сумма segments. Buy/Refuel допускают только executable=requested; Sell требует requested<=actual cargo и допускает smaller executable только с station_budget/station_capacity в LimitReasons. Нулевой executable отклоняется; budget limit => StationBudgetExceeded, capacity => StationCapacityExceeded; остальные constraints используют existing reason codes. Quote integrity inconsistency => InvalidQuote.
5. Повторно проверить resource bounds без расчёта цены: whole cost<=player credits; Buy quantity<=stock и qty*UnitMassKg<=free module capacity; Refuel qty<=service stock и свободному баку; Sell actual<=budget/Credits и maxStock-stock. Cargo/player overflow не является partial fill. Stage complete inventory, cargo/tank, available capacity, PlayerCredits, station Credits/Budget. Sell вычитает proceeds из Credits и Budget; Buy/Refuel прибавляют полную cost к Credits, Budget +=min(maxBudget-Budget,cost). Fuel не входит в cargo cap.
6. Подготовить next revision и полный immutable receipt ДО commit. Затем присвоить staged player/station/ship state и revision под одной lock; no additional parsing, lookup, price calculation, arithmetic или await после начала commit. Record one success с TradeReceipt, ExecutedQuantity=actual только если partial (иначе null для old consumers). Rejected quoted command получает receipt с zero executed/total, raw request/quote fields и known unchanged revision. Overflow на preflight => value_overflow и zero-effect receipt; не перехватывать overflow после частичной записи.
7. Receipt сохраняется существующим RememberResult/CaptureCommandReceipts/RestoreCommandJournal без отдельного журнала. ScenarioLoader проверяет shape: nonnegative totals/actual и resolved ResultMarketRevision; success positive requested/actual<=requested + IDs + nonnegative quoted revision + next revision, Buy/Refuel actual=requested; rejected actual=total=0, исходные RequestedQuantity/QuotedMarketRevision могут быть null/отрицательными как причина отказа и не делают save невалидным. Legacy null receipts допустимы. Не требовать присутствия исторической станции в текущем мире и не пересчитывать старую сумму текущим catalog. На load invalid receipt => ScenarioException до замены мира.
8. Verify stale after interval/budget/stock mutation и concurrent two commands on same revision. Нет network atomicity workaround: после неопределённой доставки повторяется тот же CommandId. На save/load quote cache устаревает, но completed results возвращаются из journal до quote lookup.

## Out of scope

Формула/curve/quote transport, revision triggers US-0015, почасовые rates, глобальный unlimited ledger, UI, портовые сборы/долги, новая save migration, изменение иных command handlers.

## Invariants

- Stage-before-commit уже установлен в SimulationEngine.cs:1953–1970,1994–2012,2036–2045; все новые fallible computations остаются до assignments.
- Journal полных results: SimulationEngine.CommandJournal.cs:8–48; Save capture SimulationEngine.cs:811–815.
- Деньги целые, rounding только calculator: EngineRequirements.md:5263–5265.
- Скрытая касса не передаётся клиенту: StationTradeSnapshot.cs:17–23.

## Tests

QuotedTradeExecutionTests использует настоящий US-0015 calculator, typed profiles/registry и контролируемое время, не fake totals:
- `Buy_and_refuel_commit_whole_quote_or_leave_all_state_unchanged` — AC-01/03, theory money/stock/capacity/overflow.
- `Sell_fills_largest_budget_and_capacity_prefix_and_receipts_actual_total` — AC-02; отдельно каждый limit и оба вместе.
- `Zero_fill_missing_cargo_and_arithmetic_overflow_are_zero_effect` — AC-01/02; сравнить save projections денег/stock/cargo/tank/revision, исключая добавленный отказ в journal.
- `Fuel_cannot_be_bought_sold_or_refuelled_as_another_item` — AC-03, quoted и legacy paths.
- `Stale_and_cross_station_module_item_quantity_quotes_are_zero_effect` — AC-04.
- `Two_commands_from_one_revision_commit_once` и `Duplicate_command_returns_identical_receipt_before_and_after_reload` — AC-04/07.
- `Evicted_receipt_cannot_reexecute_consumed_quote` — AC-04; 4097 иных terminal commands, старый quote без эффекта.
- `Partial_receipt_survives_save_and_malformed_receipt_is_rejected` — AC-02/04.
- `Quote_prefix_equals_receipt_and_immediate_buy_sell_never_increases_balance` — AC-07; qty1 и large batch, boundaries shortage/normal/surplus, frozen time/no events, тот же station/item. Если реальная формула нарушает критерий, зафиксировать failure в prerequisite US-0015, не ослаблять test и не менять formula здесь.
- `Unquoted_profile_trade_is_rejected_but_legacy_behavior_is_preserved` — AC-04; профильные tests не обходят gate.
TradeCommandTests: сохранить existing checks, добавить `Quote_metadata_never_falls_back_to_legacy`.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~QuotedTradeExecutionTests|FullyQualifiedName~TradeCommandTests|FullyQualifiedName~PauseSimulationTests|FullyQualifiedName~ReviewRegressionTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все шаги только в пяти разрешённых файлах; prerequisite API присутствует, неподготовленная US-0015 не выдаётся за implementation.
- Каждый AC-01/02/03/04/07 покрыт named tests; rejected/replayed state equality и реальный anti-arbitrage проходят.
- Named tests/build/format пройдены либо исходные failures отдельно записаны; новая failure acceptance не считается done.
- API/invariants/out-of-scope соблюдены, нет hidden changes и незаписанных assumptions/вопросов.
- По одному receipt и diff world state можно проверить transferred quantity, total и единственное изменение revision.

## Self-containment check

Binding, staging, resource arithmetic, receipt semantics, dedupe/retention и prerequisite signatures заданы. Для implementation не требуется придумывать price formula или расширять dependency layer/files. Если US-0015 не предоставляет указанный seam, тикет ожидает dependency, а не реализует её скрыто.
