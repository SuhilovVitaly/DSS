---
epic: EP-0001-trading-system
story: EP-0001-US-0015-authoritative-market-quotes
ticket: EP-0001-US-0015-TK-0002-sequential-price-curve
title: Последовательная кривая цены
stage: approved
layer: engine
depends_on: [EP-0001-US-0015-TK-0001-trade-quote-contract, EP-0001-US-0001-TK-0005-profile-trade-presentation, EP-0001-US-0002-TK-0003-hourly-market-simulation]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-09-21T15:10:56Z
revision: 1
---

# Последовательная кривая цены

## Why

Текущий Engine умножает одну station price на количество. Нужен pure deterministic calculator, который буквально реализует baseline эпика, виртуально меняет stock по одной единице и возвращает компактную curve и объяснение. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

D-01, 2026-09-21T15:10:56Z: исходный запрос пользователя приведён в story; дополнительных ответов о формуле не было. Числа берутся из утверждённой таблицы решений эпика `Documentation.md:96`, не из старой static-price реализации.

## Assumptions

- Unit использует stock до виртуального изменения: Buy/Refuel `stock--`, Sell `stock++` для следующей unit.
- У row без positive TargetStock stock factor нейтрален 1000. Это сохраняет legacy/Fuel service; Refuel использует buy spread.
- Общий clamp применяется к точному decimal multiplier до одного final round. Positive BasePrice после lower clamp даёт минимум 1 Credit.
- Adjacent одинаковые unit prices сжимаются; calculator не создаёт один segment для несмежных одинаковых цен.
- Calculator не знает player/station budget/capacity: он получает уже ограниченный prefix quantity от issuer.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Content/StationPricing.cs | :1–27 вычисляет base×flat factor list через decimal и AwayFromZero | Добавить reusable exact multiplier/clamp primitive без изменения старого результата |
| src/DeepSpaceSaga.Engine/TradeQuoteCalculator.cs | отсутствует | Pure calculator unit price, sequential RLE curve, checked total и price reasons |
| tests/DeepSpaceSaga.Engine.Tests/StationPricingTests.cs | :14–73 покрывает neutral/multiple/order/rounding | Regression для clamp и прежней API |
| tests/DeepSpaceSaga.Engine.Tests/TradeQuoteCalculatorTests.cs | отсутствует | Boundary/formula/curve/rounding/overflow/anti-arbitrage tests |

## Public API after the change

В Engine namespace внутренний testable seam:

```csharp
internal enum TradeQuoteDirection { Buy, Sell, Refuel }

internal sealed record TradeQuotePriceInput(
    long BasePriceCredits,
    IReadOnlyList<int> StationAndEventFactorsPermille,
    long CurrentStock,
    long? TargetStock,
    TradeQuoteDirection Direction,
    long Quantity,
    IReadOnlyList<TradePriceReason> StaticReasons);

internal sealed record TradeQuotePriceResult(
    ImmutableArray<TradePriceStep> Curve,
    long TotalCredits,
    ImmutableArray<TradePriceReason> PriceReasons);

internal static class TradeQuoteCalculator
{
    internal static TradeQuotePriceResult Calculate(TradeQuotePriceInput input);
}
```

Если repository naming/style требует readonly struct вместо records, semantics/signatures полей сохраняются. Constants в calculator: `StockSlope=700`, `MinStockFactor=650`, `MaxStockFactor=1700`, `BuySpread=1150`, `SellSpread=850`, `MinFinal=500`, `MaxFinal=3000`, permille denominator 1000.

Unit algorithm:

1. `stockRatio = virtualStock / targetStock` в decimal; `stockFactor = clamp(1 + 0.70 × (1 − ratio), 0.65, 1.70)`; без target — 1.
2. `rawMultiplier = Product(station/event factors) × stockFactor × spread`.
3. `finalMultiplier = clamp(rawMultiplier, 0.50, 3.00)`.
4. `unitPrice = RoundAwayFromZero(BasePrice × finalMultiplier)`, checked `long`, minimum 1 для positive base.
5. Append/merge RLE step, checked add total, затем virtual stock `−1` для Buy/Refuel или `+1` для Sell.

Static `PriceReasons` содержит base/station/event; calculator добавляет стартовый stock band, direction spread и clamp reason, если clamp сработал. Не включать raw stock/budget values beyond already public stock/target semantics.

## Implementation steps

1. Расширить StationPricing internal primitive так, чтобы существующий `ComputeUnitPriceCredits(base, factors)` остался behavior-compatible и делегировал общему decimal path.
2. Реализовать input validation: BasePrice>0, Quantity≥0, CurrentStock≥0, TargetStock null или >0, factors positive, quantity/stock arithmetic checked. Quantity 0 возвращает empty curve/zero total, но issuer решает, disabled ли request.
3. Реализовать literal fixed-point/decimal formula и final clamp, без float/double и intermediate unit rounding до последнего шага.
4. Строить curve в одном проходе, сливая только соседние equal prices; total пересчитывается checked по каждому segment/unit без доверия входному total.
5. Добавить stable price reasons; source IDs передаются caller-ом и не влияют на arithmetic.
6. Проверить shortage/normal/surplus, exact clamp points, midpoint, buy/sell spread, sequential direction, RLE, overflow и immediate same-stock buy/sell inequality.

## Out of scope

Runtime lookup станции/модуля, maximum/limit reasons, QuoteId/cache/revision, snapshot, persistence, command execution, UI и balance retuning.

## Invariants

- Формула и коэффициенты: `Board/EP-0001-trading-system/Documentation.md:90–101,655–691`.
- Деньги/factors без float/double, one round AwayFromZero: `Documentation/01-Requirements/EngineRequirements.md:5247–5265`.
- Текущий factor order deterministic: `SimulationEngine.cs:1598–1630`; existing pricing tests обязаны остаться зелёными.
- Покупка/продажа имеют целый шаг 1: `EngineRequirements.md:5128–5160`.

## Tests

`TradeQuoteCalculatorTests`:

- `Shortage_normal_and_surplus_follow_baseline_and_final_clamp` — AC-01; включая exact 0.50/3.00 bounds.
- `Buy_curve_raises_later_units_and_sell_curve_lowers_them` — AC-02.
- `Curve_segments_are_adjacent_run_lengths_and_sum_exactly_to_total` — AC-02.
- `Quantity_one_matches_first_unit_of_larger_curve` — AC-02.
- `Buy_1150_and_sell_850_prevent_immediate_roundtrip_gain` — AC-01/02; qty 1 и large prefix на shortage/normal/surplus boundaries.
- `Legacy_and_refuel_without_target_use_neutral_stock_factor` — AC-01.
- `Midpoint_rounds_away_from_zero_once_after_all_factors` — AC-01.
- `Invalid_factor_stock_and_checked_overflow_fail_before_result` — safety.

`StationPricingTests`:

- Все существующие tests остаются без изменения expected values.
- `Final_multiplier_clamp_uses_decimal_before_rounding` — AC-01.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradeQuoteCalculatorTests|FullyQualifiedName~StationPricingTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все шаги выполнены только в четырёх разрешённых файлах.
- AC-01/02/03 покрыты named tests в пределах calculator contribution.
- Focused tests/build/format проходят либо baseline failure записано отдельно.
- Формула literal, checked и deterministic; existing StationPricing behavior не сломан.
- API/invariants/out-of-scope соблюдены, скрытых assumptions/работы нет.
- По input и curve можно независимо пересчитать каждую unit и итог.

## Self-containment check

Все constants, порядок clamp/round, stock transition, RLE и error semantics заданы. Implementer не должен искать формулу в концепте или придумывать обработку Fuel/legacy.

