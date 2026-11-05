---
epic: EP-0001-trading-system
story: EP-0001-US-0015-authoritative-market-quotes
ticket: EP-0001-US-0015-TK-0004-authoritative-quote-issuer
title: Выдача и валидация котировки
stage: approved
layer: engine
depends_on: [EP-0001-US-0015-TK-0002-sequential-price-curve, EP-0001-US-0015-TK-0003-market-revision-lifecycle]
files_touched: 4
serves: [AC-02, AC-03, AC-04, AC-05, AC-06, AC-07]
created: 2026-09-21T15:10:56Z
revision: 1
---

# Выдача и валидация котировки

## Why

Pure curve становится authoritative quote только после проверки docking/module/item/resources, вычисления exact Max, выдачи opaque token и сохранения immutable binding/context для последующего execution. Этот тикет создаёт Engine issuer/cache/validator, но не выполняет сделку. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

D-01, 2026-09-21T15:10:56Z: исходный запрос сохранён в story. Concrete consumer fields согласованы с уже approved US-0003 integration contract; quote execution остаётся в US-0003.

## Assumptions

- Runtime session nonce генерируется при construction/load и не входит в simulation/save state; QuoteId имеет вид `QTE-{nonce}-{counter}` и не переиспользуется после load. Это transport token, не источник gameplay randomness.
- Cache хранит максимум 1024 issued quotes FIFO. Binding RequestId→QuoteId живёт столько же. Eviction/revision invalidation удаляет обе записи.
- Identical RequestId+binding при неизменном context/revision возвращает тот же DTO. Collision RequestId с иным binding возвращает disabled `request_id_conflict`, не вытесняя исходную quote.
- Context validator сравнивает не только market revision, но и player credits, docking, module readiness/cargo/tank/capacity и relevant station stock/budget/capacity, сохранённые во внутреннем issued record. DTO скрытых значений не содержит.
- Exact Maximum — longest positive-price prefix, допустимый всеми current authoritative constraints. Buy/Refuel request выше maximum disabled; Sell partial только budget/free-capacity, не cargo/invalid binding.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.TradeQuotes.cs | отсутствует | Issuer/cache/binding/context/Max/GetTradeQuote/TryValidateTradeQuote |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :560–598 projection; :1598–1648 factors; :3233–3355 station/inventory runtime | Expose reason-aware factor resolution and call cache reset/invalidation hooks only |
| tests/DeepSpaceSaga.Engine.Tests/TradeQuoteIssuanceTests.cs | отсутствует | Real engine quote/Max/cache/stale/binding/hidden-budget tests |
| tests/DeepSpaceSaga.Engine.Tests/TradeSnapshotProjectionTests.cs | :280–320 item projection and current prices | Revision projection and quote-first regression assertions |

## Public API after the change

```csharp
public TradeQuoteSnapshot GetTradeQuote(TradeQuoteRequest request);

private bool TryValidateTradeQuote(
    PlayerCommand command,
    out TradeQuoteSnapshot quote,
    out string reasonCode);
```

`GetTradeQuote` locks `_worldStateLock`, never mutates market/player state, and returns a disabled DTO for invalid gameplay input; programmer null input throws `ArgumentNullException`. `TryValidateTradeQuote` requires both `PlayerCommand.QuoteId` and `.MarketRevision`, performs cache lookup, exact binding comparison and current context/revision comparison, consumes nothing and mutates nothing. It returns literal stable reason `invalid_quote` for missing/partial/mismatched binding and `stale_quote` for expired/evicted/current-state mismatch; US-0003 adds shared `CommandReasonCodes` constants with these exact values.

Internal issued entry stores DTO plus current station index/id/revision, player credits, docking target, module readiness/cargo/tank/capacity, item stock, market budget/free stock capacity and any event/factor fingerprint required to prove calculation context unchanged. Raw station Credits/budget never enters public DTO.

Quote semantics:

- Resolve player ship/docked station/addressed module/command ownership/item and positive quantity.
- Fuel: only `trade.refuel` + `item.fuel`; ordinary Buy/Sell Fuel and Refuel non-Fuel disabled. Profile/legacy enforcement of quoted commands happens in US-0003.
- Calculate physical candidate limit: station stock + player money + cargo/tank for Buy/Refuel; cargo + budget + free station capacity + player balance-overflow for Sell. Monetary prefix uses sequential curve totals, not division by first unit price.
- `MaximumQuantity` is independent of requested quantity. `Curve` covers only `ExecutableQuantity`.
- Buy/Refuel: request≤Max → executable=request; otherwise executable/total/curve zero and DisabledReason is limiting code.
- Sell: missing cargo/invalid module/request>cargo is disabled whole. Budget/capacity may reduce executable to positive prefix with null DisabledReason and ordered unique LimitReasons. Zero prefix disabled.
- PriceReasons comes from resolved size/profile/event factors plus calculator stock/spread/clamp reasons. Active event reasons include EventId source; no localized text.

## Implementation steps

1. Добавить bounded dictionaries/queues by QuoteId и RequestId, per-load nonce/counter reset, station invalidation hook invoked from TK-0003 `CommitMarketRevision`.
2. Реализовать strict request validation with stable disabled reason codes. Не бросать на user data/overflow; overflow даёт disabled `value_overflow`, empty curve и не кэшируется как executable quote.
3. Разделить factor resolution на arithmetic values и `TradePriceReason` metadata без изменения current static projection до US-0003. Deterministic order: station/profile first, events by `(StartedGameTimeMs, EventId)` как сейчас `SimulationEngine.cs:1598–1630`.
4. Вычислить exact physical/monetary maximum через calculator prefixes. Не использовать `UnitPriceCredits` snapshot или station budget value из client.
5. Выдать immutable DTO, записать full private context, обеспечить identical RequestId idempotency/conflict behavior и FIFO eviction 1024.
6. Реализовать exact `TryValidateTradeQuote(PlayerCommand,...)`: оба binding field required, station/object/module/type/item/quantity/revision match, token current, private context unchanged. Validation не удаляет token; consumption/commit — US-0003.
7. Reset cache при любом LoadScenario/new session и invalidate station entries on revision commit. Pre-load token после load stale даже при совпавших IDs/revision.
8. Добавить tests с реальными profile/economy runtime fixtures и настоящим calculator; forbidden shortcuts/fake totals не использовать.

## Out of scope

Command execution/token consumption, CommandResult receipt/reason constants, UI/localization, HTTP/network transport, remote market knowledge, event lifecycle и balance retuning.

## Invariants

- World reads и validation защищены `_worldStateLock`: `SimulationEngine.cs:25–28,146–153`.
- Budget не публикуется: `StationTradeSnapshot.cs:15–23`.
- Factor order и applicability: `SimulationEngine.cs:1598–1648`.
- Existing client local calculation не считается authoritative: `TradeModel.cs:9–55`; его замена — US-0003.
- US-0003 требует signatures `GetTradeQuote` и `TryValidateTradeQuote`: его story, раздел Integration contract required from US-0015.

## Tests

`TradeQuoteIssuanceTests`:

- `Quote_binds_station_ship_module_direction_item_quantity_and_revision` — AC-03/07.
- `Buy_refuel_and_sell_maximum_use_full_sequential_prefix_and_all_authoritative_limits` — AC-02/03.
- `Sell_partial_is_only_budget_or_station_capacity_and_never_leaks_amounts` — AC-03.
- `Same_request_is_idempotent_but_request_id_collision_is_disabled` — AC-06.
- `Market_revision_change_evicts_quote_and_validator_reports_stale_without_mutation` — AC-04.
- `Player_module_or_docking_context_change_makes_quote_stale` — AC-03/04.
- `Cross_station_module_item_direction_quantity_or_revision_binding_is_invalid` — AC-03.
- `Evicted_1025th_quote_and_preload_quote_are_stale_and_ids_are_not_reused` — AC-05.
- `Fuel_refuel_uses_buy_curve_and_forbidden_fuel_routes_are_disabled` — AC-02/03.
- `Invalid_and_overflow_requests_return_zero_effect_disabled_quote` — safety.
- `Price_reasons_explain_profile_stock_event_spread_and_clamp_in_deterministic_order` — AC-03.

`TradeSnapshotProjectionTests`:

- `Docked_profile_market_projects_revision_without_budget` — AC-05/06.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeQuoteIssuanceTests|FullyQualifiedName~TradeSnapshotProjectionTests|FullyQualifiedName~TradeQuoteCalculatorTests|FullyQualifiedName~MarketRevisionTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены только в четырёх разрешённых файлах.
- AC-02/03/04/05/06/07 покрыты named tests в пределах issuer contribution.
- Focused tests/build/format проходят либо baseline failure записано отдельно.
- Quote binding/context/Max/curve/reasons authoritative; hidden money не сериализуется.
- API/invariants/out-of-scope соблюдены; validator не мутирует и cache не сохраняется.
- Нет незаписанных assumptions, blocking questions или поиска вне Code context.

## Self-containment check

Заданы cache policy, token identity, binding/context, Max/partial rules, validation results и exact methods. Реализация не требует придумывать semantics исполнения или читать Client code.

