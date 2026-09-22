---
epic: EP-0001-trading-system
story: EP-0001-US-0013-economy-balance-evidence
ticket: EP-0001-US-0013-TK-0003-market-health-evaluation
title: Проверка доступности и здоровья складов
stage: approved
layer: tooling
depends_on: [EP-0001-US-0013-TK-0002-balance-run-matrix]
files_touched: 2
serves: [AC-02, AC-05]
created: 2026-09-21T14:55:45Z
revision: 1
---

# Проверка доступности и здоровья складов

## Why

Преобразовать почасовые evidence в строгие, объяснимые market-health verdicts. End state: для каждого seed/config проверяются пять станций, связность, покупка/продажа, bounded stock/budget, zero-stock ratio и восстановление supply; нарушение всегда указывает точный station/item/time/rule.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj`.

## Decisions

Единственное сообщение пользователя приведено в story; дополнительных решений нет.

## Assumptions

- Zero-stock denominator использует 240 post-boundary samples и исключает только часы active event, чьи resolved effects затрагивают конкретный item/category.
- `Restricted` route остаётся проходимым, `Unavailable` исключается из adjacency.
- Обязательный supply считается восстановимым, если в каждом rolling окне 24 post-boundary samples вне влияющего события есть хотя бы один positive-stock sample.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.EconomyBalance/MarketHealthEvaluator.cs` | Новый файл; TK-0002 предоставляет immutable hourly station/route/event evidence | Pure evaluator, stable violation codes, exact ratios/bounds/connectivity/recovery rules |
| `tests/DeepSpaceSaga.EconomyBalance.Tests/MarketHealthEvaluatorTests.cs` | Новый файл; matching project создаётся TK-0002 | Table-driven boundary, event-exclusion, graph and diagnostic tests |

## Public API after the change

Internal tooling API:

```csharp
internal sealed record BalanceViolation(
    string Code,
    ulong Seed,
    string ShipConfigurationId,
    string? StationObjectId,
    string? RouteId,
    string? ItemTypeId,
    long? GameTimeMs,
    string Expected,
    string Observed);

internal static class MarketHealthEvaluator
{
    internal static ImmutableArray<BalanceViolation> Evaluate(
        BalanceCaseEvidence evidence);
}
```

Stable codes: `station_count`, `route_disconnected`, `station_cannot_buy`, `station_cannot_sell`, `stock_below_zero`, `stock_above_max`, `budget_below_zero`, `budget_above_max`, `zero_stock_ratio`, `supply_not_recovered`, `state_hash_mismatch`.

## Implementation steps

1. Reject malformed evidence with a violation, not an evaluator exception: duplicate/non-hourly times, missing t=0/240h, duplicate station/item/route keys, invalid targets/max/budget bounds or mismatched seed/config.
2. Для каждого post-boundary sample потребовать ровно пять unique stations. Построить undirected adjacency из `Available`/`Restricted` effective routes, BFS от ordinal first station; перечислить unreachable IDs в `Observed`.
3. Для каждой станции в каждом normal sample требовать минимум одну authoritative buyable position (`MaxBuyableQuantity>0`) и sellable position (`MaxSellableQuantity>0`). Event-affected station/item не исключает всю станцию: проверка работает по остальным positions.
4. Проверить `0 <= stock <= maxStock`, `targetStock <= maxStock`, `0 <= budget <= maxBudget` для каждого sample. Использовать сохранённые authoritative bounds; не выводить max из текущего stock.
5. Для каждого обязательного cargo-flow item посчитать `zeroCount/eligibleCount` после item-scoped event exclusion. `eligibleCount==0` даёт `zero_stock_ratio` с `Observed=no eligible samples`; иначе violation при `zeroCount * 4 > eligibleCount` (строго больше 25%, без floating point).
6. Sliding window 24 часа для каждого producer/item: после event exclusion не допускается окно без positive stock. В violation указать начало/конец первого окна; это operational definition необратимого исчезновения supply.
7. Сравнить `ContinuousStateHash`/`SaveLoadStateHash`; mismatch выдаёт stable violation до остальных агрегатов, но evaluator продолжает собирать все причины.
8. Отсортировать violations ordinal по code, seed, config, station, route, item, time; Expected/Observed должны быть invariant-culture и не содержать temp paths.

## Out of scope

Profit/margin, route ranking, trade execution, коэффициент tuning, автоматический repair, report serialization/CLI, Client UI, изменение Engine/content.

## Invariants

- Требование пяти достижимых станций и buy/sell задано `Documentation/02-FirstRelease/Mechanics/TradingSystemMvpStories.md:388–400`.
- Stock threshold `<=25%` и bounded economy закреплены story AC-02 и `Board/EP-0001-trading-system/Documentation.md:95,104`.
- Events не должны делать экономику неиграбельной: `TradingSystemMvpStories.md:398–400`.
- Два files, только tooling layer и dedicated matching tests.

## Tests

`MarketHealthEvaluatorTests`:

- `Healthy_five_station_ten_day_case_has_no_violations` (AC-02).
- `Unavailable_bridge_reports_sorted_unreachable_stations_but_restricted_route_connects` (AC-02/05).
- `Missing_buy_or_sell_reports_station_and_exact_hour` (AC-02/05).
- `Zero_stock_at_exactly_twenty_five_percent_passes_and_one_more_sample_fails` (AC-02).
- `Only_item_scoped_active_event_samples_leave_zero_stock_denominator` (AC-02).
- `Stock_and_budget_outside_authoritative_bounds_are_reported` (AC-02/05).
- `Twenty_four_hour_positive_stock_gap_reports_first_unrecovered_window` (AC-02/05).
- `Save_load_hash_mismatch_does_not_hide_other_violations` (AC-01/05).
- `Violation_order_and_text_are_input_order_independent` (AC-05).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.EconomyBalance.Tests\DeepSpaceSaga.EconomyBalance.Tests.csproj --no-restore --filter FullyQualifiedName~MarketHealthEvaluatorTests
dotnet build D:\DeepSpaceSaga\DSS\tools\DeepSpaceSaga.EconomyBalance\DeepSpaceSaga.EconomyBalance.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только два allowed files; evaluator pure и не открывает Engine/content files.
- AC-02/05 покрыты named tests для exact 25%, item-scoped exclusion, reachability, bounds и recovery.
- Named tests/build/format проходят либо baseline failure записано отдельно.
- Arithmetic целочисленная, violation order стабильный, никакой automatic tuning нет.
- Нет скрытых exceptions/assumptions или работы вне `Code context`.
- Каждый failure воспроизводим по seed/config/station/item/time из violation.

## Self-containment check

Входной тип, все predicates, event exclusion, 25% comparison, rolling window, graph semantics, codes и ordering заданы. Implementer не решает, что считать мёртвым рынком или как скрывать дефицит событием.
