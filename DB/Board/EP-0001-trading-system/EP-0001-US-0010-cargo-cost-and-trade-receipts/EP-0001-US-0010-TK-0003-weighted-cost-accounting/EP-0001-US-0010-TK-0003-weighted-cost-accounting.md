---
epic: EP-0001-trading-system
story: EP-0001-US-0010-cargo-cost-and-trade-receipts
ticket: EP-0001-US-0010-TK-0003-weighted-cost-accounting
title: Средневзвешенное списание и результат продажи
stage: approved
layer: engine
depends_on: [EP-0001-US-0010-TK-0001-cargo-result-contract, EP-0001-US-0010-TK-0002-persisted-cargo-cost-basis, EP-0001-US-0003-TK-0002-atomic-quote-execution]
files_touched: 5
serves: [AC-01, AC-02, AC-05, AC-06]
created: 2026-09-21T12:39:58Z
revision: 1
---

# Средневзвешенное списание и результат продажи

## Why

Persisted metadata становится полезным только если каждый authoritative cargo mutation меняет quantity и basis вместе. End state: покупки накапливают точную стоимость, partial Sell списывает одну пропорциональную часть и пишет immutable result, consumption/dialogue paths не оставляют orphan basis, replay остаётся idempotent.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Prerequisite US-0003 переносит quoted commit в новый partial file; formula/quote validation не реализуются здесь.

## Decisions

2026-09-21T12:39:58Z — пользователь запросил создание тикетов для US-0010. Других решений пользователя нет.

## Assumptions

Known stack использует pooled weighted-average basis; source set — provenance union, не FIFO lots. Legacy unknown остаётся unknown при смешивании с новой покупкой, чтобы не выдумывать стоимость. После полного удаления неизвестного stack следующая acquisition создаёт обычный known basis.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.CargoCost.cs | Новый partial file | Единственные helpers add/remove/validate cost basis и deterministic source union |
| src/DeepSpaceSaga.Engine/SimulationEngine.TradeExecution.cs | Создаётся prerequisite US-0003 TK-0002 для staged quoted commit | Stage purchase basis, Sell realized cost/gross и receipt до atomic commit |
| src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs | :54–90 rations уменьшают только quantity | Пропорционально уменьшать known basis через общий helper |
| src/DeepSpaceSaga.Engine/Dialogue/DialogueEffectTransaction.cs | :95–123 add/remove cargo создаёт stack только с quantity | Grant source/cost и proportional removal без нарушения transaction rollback |
| tests/DeepSpaceSaga.Engine.Tests/CargoCostAccountingTests.cs | Новый файл; fixtures можно строить по `TradeCommandTests.cs:38–76` и prerequisite quoted tests | Weighted-average, partial, unknown, non-trade mutation, replay/save integration |

## Public API after the change

Нового public API нет. Internal seam в partial:

```csharp
internal static CargoStackRuntime AddCargoCost(
    CargoStackRuntime? existing,
    int itemTypeIndex,
    long acquiredQuantity,
    long acquisitionCostCredits,
    string acquisitionSource);

internal static (CargoStackRuntime? Remaining, long? RealizedCostCredits)
    RemoveCargoCost(CargoStackRuntime existing, long removedQuantity);
```

Для known stack:

```text
realized = Round(oldBasis * removedQuantity / oldQuantity, AwayFromZero)
remainingBasis = oldBasis - realized
```

Расчёт выполняется через checked decimal/long, clamp `0..oldBasis`; полное удаление возвращает exact oldBasis. Unknown stack возвращает realized null и остаётся unknown до quantity0. Source union sorted ordinal/distinct.

## Implementation steps

1. Реализовать helpers с precondition checks: positive changed quantity, nonnegative acquisition cost, allowed non-legacy source, quantity/basis overflow. Никаких assignments до завершения checked calculation.
2. В prerequisite quoted Buy stage вызвать `AddCargoCost(..., quote.TotalCredits, "purchased")`. Две покупки складывают exact totals; known bootstrap/produced/mined basis участвует в том же pool. Refuel не меняет cargo basis.
3. В quoted Sell после определения `ExecutedQuantity` вызвать remove helper. Known result получает `RealizedCargoCostCredits` и checked `GrossResultCredits = receipt.TotalCredits - realized`; unknown получает оба null. Remaining stack quantity/basis коммитятся атомарно с money/stock/revision/receipt.
4. Duplicate CommandId проходит existing journal gate до нового расчёта и возвращает исходный receipt. Rejection/stale/overflow не меняют basis и получает null cargo-result fields.
5. Ration consumption использует remove helper для каждого фактически consumed amount; нереализованный расход не публикует trade profit.
6. Dialogue AddCargoItem использует source `dialogue-grant`, cost0; RemoveCargoItem использует remove helper. Candidate transaction остаётся copy-on-write: failure откатывает quantity, basis и sources вместе.
7. `produced`/`mined` поддерживаются helper/persistence, но не вызываются без существующего authoritative handler. Не добавлять fake production/mining command.
8. Tests сравнивают pre/post save projections и receipts; для weighted average используют две реальные quoted покупки с разными totals, затем full/partial sells. Остаточный basis проверяется прямо в saved cargo metadata.

## Out of scope

Quote formula, Client, locale, Finance, route costs, station production inventory, новые mining/production handlers, FIFO lots, изменение retention limits.

## Invariants

- Trade commit stage-before-assign обязателен: current path `SimulationEngine.cs:2033–2112`; prerequisite переносит его в partial.
- Rations и dialogue — текущие независимые cargo mutation paths: `SimulationEngine.EconomyTime.cs:54–90`; `DialogueEffectTransaction.cs:95–123`.
- Money/basis используют целые Credits и AwayFromZero: `EngineRequirements.md:5247–5265`.
- Receipt dedupe/save предоставлен command journal prerequisite; duplicate не пересчитывается по текущему stack.

## Tests

- `Two_purchases_at_different_totals_accumulate_exact_quantity_basis_and_sources` — AC-01.
- `Partial_sell_rounds_realized_weighted_cost_once_and_preserves_exact_remaining_basis` — AC-02.
- `Full_sell_realizes_all_remaining_basis_and_reports_profit_loss_or_break_even` — AC-02.
- `Legacy_unknown_sell_never_reports_zero_cost_or_gross_result` — AC-02/05.
- `Rejected_stale_overflow_and_duplicate_commands_do_not_change_basis` — AC-06.
- `Duplicate_sell_before_and_after_save_returns_identical_receipt_once` — AC-06.
- `Ration_and_dialogue_removal_reduce_basis_with_quantity_and_dialogue_grant_is_explicitly_free` — AC-05.
- `Failed_dialogue_transaction_rolls_back_quantity_basis_and_sources` — AC-05.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~CargoCostAccountingTests|FullyQualifiedName~QuotedTradeExecutionTests|FullyQualifiedName~EconomyTimeContinuityTests|FullyQualifiedName~DialogueEffectTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены только в пяти разрешённых files; prerequisite partial существует с согласованной signature.
- AC-01/02/05/06 покрыты named tests с exact save/receipt evidence.
- Tests/build/format проходят либо конкретный baseline failure записан отдельно.
- Каждая current cargo mutation сохраняет quantity/basis/source invariant; unknown не становится zero.
- Нет client calculation, fake handlers, hidden files или незаписанных assumptions.

## Self-containment check

Формула, rounding, unknown behavior, mutation paths, receipt assignment и test scenarios заданы. Реализация не требует искать финансовую политику или изменять quote calculator.

