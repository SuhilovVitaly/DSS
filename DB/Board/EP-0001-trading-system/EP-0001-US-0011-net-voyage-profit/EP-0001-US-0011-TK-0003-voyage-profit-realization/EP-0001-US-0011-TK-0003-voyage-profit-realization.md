---
epic: EP-0001-trading-system
story: EP-0001-US-0011-net-voyage-profit
ticket: EP-0001-US-0011-TK-0003-voyage-profit-realization
title: Реализация грузовой прибыли и snapshot
stage: approved
layer: engine
depends_on: [EP-0001-US-0011-TK-0001-voyage-finance-contract, EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle, EP-0001-US-0010-TK-0003-weighted-cost-accounting]
files_touched: 4
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-09-21T12:56:24Z
revision: 1
---

# Реализация грузовой прибыли и snapshot

## Why

Lifecycle/расходы сами по себе не показывают прибыль. Нужно привязать authoritative Sell receipt к реально привезённому остатку, учесть только исполненную часть, безопасно обработать unknown COGS, вычислить net в Engine и публиковать одинаковый bounded read model для обоих Client views.

## Decisions

Дополнительных решений пользователя нет. Применяются A-02/A-03/A-06: продажи атрибутируются максимум в пределах carried remainder, post-arrival Buy не является расходом входящего рейса, unknown COGS делает итог unavailable.

## Assumptions

US-0010 TK-0003 создаёт `SimulationEngine.TradeExecution.cs` и successful Sell receipt с `ExecutedQuantity`, `TotalCredits`, nullable `RealizedCargoCostCredits`; TK-0002 уже создал in-memory ledger и opening cargo. Если dependency receipt или commit seam отличаются, тикет возвращается в review до правки разрешённых файлов.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/SimulationEngine.VoyageLedger.cs` | Создан TK-0002; владеет ledger/state/posting IDs | Добавить sale attribution, proportional allocation, net recompute и Contracts projection |
| `src/DeepSpaceSaga.Engine/SimulationEngine.TradeExecution.cs` | Планируемый owner US-0003; US-0010 добавляет receipt COGS (`EP-0001-US-0010-TK-0003-weighted-cost-accounting.md:35–70`) | После успешного staged Sell commit один раз передать immutable receipt в ledger; Buy/Refuel/rejection не post |
| `src/DeepSpaceSaga.Engine/SimulationEngine.cs` | Snapshot строится под world lock и заполняет trailing projections (`:500–534`) | Заполнить `VoyageFinances` из ledger, не выполнять расчёт в constructor call |
| `tests/DeepSpaceSaga.Engine.Tests/VoyageProfitAccountingTests.cs` | Новый файл; текущая Sell mutation `SimulationEngine.cs:2072–2112`, dependency переносит её в partial | Profit/loss/break-even, partial, mixed cargo, unknown, replay, unsold, formula/projection tests |

## Public API after the change

Нового public API сверх TK-0001 нет. Internal seam:

```csharp
private void RecordVoyageSale(string postingId, TradeExecutionReceipt receipt);
private ImmutableArray<VoyageFinanceSnapshot> BuildVoyageFinanceProjection();
```

`postingId` использует immutable command/receipt identity и принимается один раз. `RecordVoyageSale` ничего не делает для non-Sell, rejected, zero executed quantity, interrupted/finalized/in-transit ledger, другой станции или item без remaining carried quantity.

## Implementation steps

1. После успешного atomic Sell commit вызвать `RecordVoyageSale` ровно с persisted authoritative receipt; command replay возвращает прежний receipt, но duplicate posting ID не меняет ledger.
2. Найти newest `awaiting_realization` с destination = текущая station. `attributedQty = min(receipt.ExecutedQuantity, remaining carried quantity item)`. Requested quantity, quoted quantity и непроданный excess не учитывать.
3. Если attributedQty меньше executed, распределить `TotalCredits` и known `RealizedCargoCostCredits` как `Round(total × attributedQty / executedQty, MidpointRounding.AwayFromZero)` через checked decimal/long и clamp `0..total`. При равенстве использовать exact receipt totals без пересчёта.
4. Уменьшить только carried remainder item; nullable remaining basis уменьшается на attributed COGS. Quantity 0 удаляет строку. Post-arrival Buy не увеличивает этот remainder и не входит в inbound ledger.
5. Known sale увеличивает GrossSales и COGS exact allocated values. Unknown sale увеличивает GrossSales, выставляет `HasUnknownCostOfGoodsSold`; с этого момента COGS/net остаются null даже после known sales.
6. Recompute known net только в Engine checked-формулой AC-01 после каждого posting. Assessed fee, event cost и passenger penalty вычитать; payout прибавлять. Unsold basis/quantity и paid/debt breakdown в формулу не добавлять.
7. Projection копирует последние максимум 50 ledger entries oldest-first/newest-last в TK-0001 DTO. `default` internal collections превращаются в empty immutable arrays; никаких mutable references.
8. Tests проходят весь public quote/command/snapshot flow dependencies, включая partial station-budget Sell и duplicate CommandId; прямой internal вызов допускается только для checked-overflow rollback.

## Out of scope

Quote/pricing, trade execution/cost-basis implementation, lifecycle/fuel/fee posting, UI/locale, persistence/migration, passenger/event producers, market value unsold cargo, route comparison/balance.

## Invariants

- Receipt — единственный источник executed quantity/total/COGS; Client/cached unit price не используется: US-0010 contract `EP-0001-US-0010-TK-0001-cargo-result-contract.md:43–57`.
- Current command dedupe хранит receipt по CommandId: `SimulationEngine.cs:130–143`; ledger дополнительно дедуплицирует posting identity.
- Money/Credits используют checked `long`; allocation округляется один раз AwayFromZero: `EngineRequirements.md:5247–5265`.
- Unknown historical basis не превращается в zero profit: US-0010 story `EP-0001-US-0010-cargo-cost-and-trade-receipts.md:58–67`.
- Snapshot строится под world lock: `SimulationEngine.cs:500–534`.
- Четыре files, production layer engine, matching `DeepSpaceSaga.Engine.Tests`.

## Tests

Named tests:

- `Known_sale_publishes_exact_profit_components_and_positive_net` (AC-01).
- `Known_sale_can_publish_loss_and_break_even` (AC-01).
- `Partial_sell_uses_executed_quantity_and_leaves_unsold_remainder` (AC-03).
- `Sale_above_carried_quantity_allocates_receipt_once_and_excludes_local_purchase` (AC-02/03).
- `Unknown_cogs_keeps_gross_sales_but_net_unavailable` (AC-03).
- `Port_debt_reduces_net_by_assessed_fee_not_paid_fee` (AC-01/04).
- `Fuel_settlement_is_subtracted_once_and_refuel_receipt_is_not_posted` (AC-01/02).
- `Repeated_command_or_snapshot_does_not_duplicate_voyage_sale` (AC-02/04).
- `Snapshot_orders_and_caps_voyage_finances_at_fifty` (AC-04).
- `Overflow_rejects_posting_without_partial_ledger_mutation` (AC-01/02).

Commands:

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~VoyageProfitAccountingTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в разрешённых файлах.
- Каждый пункт acceptance criteria, указанный в `serves`, покрыт изменением и named tests.
- Именованные тесты тикета проходят; указаны точные команды проверки.
- Build/lint соответствующего layer проходят либо конкретное исходное падение записано отдельно и не скрыто.
- Публичные API, invariants и out-of-scope ограничения соблюдены.
- Нет незаписанных assumptions, незакрытых блокирующих вопросов или скрытой работы вне `Code context`.
- Результат можно проверить по команде, тесту, diff evidence или наблюдаемому поведению.

## Self-containment check

Exact source receipt, attribution window, proportional formula, unknown behavior, net formula, projection order/limit, four allowed files и named tests заданы. Реализация не требует выбора accounting policy или поиска дополнительных mutation paths.
