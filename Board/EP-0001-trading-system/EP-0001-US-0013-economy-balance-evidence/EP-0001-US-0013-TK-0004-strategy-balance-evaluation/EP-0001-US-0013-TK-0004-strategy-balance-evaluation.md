---
epic: EP-0001-trading-system
story: EP-0001-US-0013-economy-balance-evidence
ticket: EP-0001-US-0013-TK-0004-strategy-balance-evaluation
title: Проверка маржи, разнообразия и повторных рейсов
stage: approved
layer: tooling
depends_on: [EP-0001-US-0013-TK-0002-balance-run-matrix, EP-0001-US-0011-TK-0003-voyage-profit-realization]
files_touched: 2
serves: [AC-03, AC-04, AC-05]
created: 2026-09-21T14:55:45Z
revision: 1
---

# Проверка маржи, разнообразия и повторных рейсов

## Why

Оценить фактически исполненные route/item strategies по authoritative voyage ledgers, не дублируя экономические формулы. End state: Short/Medium/Long thresholds, `2× median`, смена лидера под events/risk и защита от stale/replayed round-trip exploit дают deterministic violations с полным ledger breakdown.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj`.

## Decisions

Единственное сообщение пользователя приведено в story; дополнительных решений нет.

## Assumptions

- Сравнимой стратегией является completed route/item run с positive known COGS, known net, executed buy/sell и одним matching voyage ledger; rejected/partial/unknown результаты сохраняются, но не входят в median.
- Для каждого seed/config/class нужен хотя бы один comparable strategy. Short и Medium проходят, если существует хотя бы одна стратегия в целевом диапазоне; все остальные всё равно участвуют в dominance/ranking evidence.
- Long-upgrade criterion считается по одной и той же `(seed,state,route,item)` паре: starter `NetProfit<=0`, cargo-upgrade `NetProfit>0`; достаточно минимум одной пары во всём 12-seed corpus.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `tools/DeepSpaceSaga.EconomyBalance/StrategyBalanceEvaluator.cs` | Новый файл; TK-0002 предоставляет executed strategies и authoritative ledger components | Pure margin/rank/dominance/event/replay evaluator и stable codes |
| `tests/DeepSpaceSaga.EconomyBalance.Tests/StrategyBalanceEvaluatorTests.cs` | Новый файл; matching project создаётся TK-0002 | Boundary arithmetic, median, long crossover, event leader and round-trip replay tests |

## Public API after the change

Internal tooling API:

```csharp
internal static class StrategyBalanceEvaluator
{
    internal static ImmutableArray<BalanceViolation> EvaluateCase(
        BalanceCaseEvidence evidence);

    internal static ImmutableArray<BalanceViolation> EvaluateCorpus(
        ImmutableArray<BalanceCaseEvidence> evidence);
}
```

Stable codes: `missing_comparable_strategy`, `short_margin_band`, `medium_margin_band`, `long_upgrade_crossover`, `route_margin_dominance`, `route_always_best`, `event_did_not_change_leader`, `ledger_formula_mismatch`, `duplicate_ledger_posting`, `stale_quote_or_command_reapplied`, `unchanged_market_created_profit`.

## Implementation steps

1. Validate each completed strategy: one origin/destination route, item/config IDs, executed quantities, strictly advancing authoritative revisions for newly accepted trades, matching voyage ID and nonnegative expense components. Missing/ambiguous ledger gives `ledger_formula_mismatch`, not a guessed result.
2. Recompute only the published identity `GrossSales − COGS − RouteFuelCost − PortFees − EventCosts + PassengerPayout − PassengerPenalty` using checked integers and compare to published `NetProfitCredits`. Это audit equality, не source of gameplay result; unknown COGS/net остаются incomparable.
3. Margin = decimal `NetProfit * 1000 / COGS`, rounded once `AwayFromZero` to Int64. Zero/unknown COGS is incomparable. Require at least one safe Short in `50..150` and one Medium in `150..350` per seed/config; violation lists observed comparable `(route,item,margin)` values.
4. `EvaluateCorpus` pairs starter/cargo-upgrade by seed/state/route/item and требует минимум один Long crossover `starter<=0 && upgrade>0`; improved batch must respect TK-0002 capacity ceiling and unchanged fuel multiplier.
5. Group comparable positive strategies by `(seed,config,stateId)`. Sort margins; median for even count is arithmetic midpoint in decimal. Violation when `maxMargin > 2 * median`; fewer than two candidates gives `missing_comparable_strategy`. Preserve exact max/median/routes.
6. Determine winner by highest `NetProfitCredits`, then margin, route ID, item ID ordinal. For each seed with both normal and active-event states, require at least one leader change. Across all checked states no route/item may win every state; ties resolved canonically, full tie-set also recorded to avoid order artifacts.
7. Inspect TK-0002 three-round-trip evidence: repeated CommandId and stale QuoteId must be rejected/replayed without second receipt or ledger posting; unique accepted operations must advance market revision. If revision did not change, delta realized profit must be zero. Posting IDs unique per ledger component.
8. Sort violations by shared `BalanceViolation` ordering. Include seed/config/state/route/item plus complete authoritative ledger values in `Observed`; never infer profit from player Credits delta.

## Out of scope

Engine arithmetic or command changes, threshold tuning, automatic route choice, player upgrade implementation, market-health checks, JSON/CLI, UI, performance metrics.

## Invariants

- Net formula and no double fuel/fee are epic truth: `Board/EP-0001-trading-system/Documentation.md:101–104`.
- Voyage finance fields and nullable semantics are fixed by `EP-0001-US-0011-TK-0001-voyage-finance-contract.md:37–78`.
- Trade execution must use quote-bound authoritative receipts: `EP-0001-US-0006-repeatable-trading-voyage.md:50–56`.
- Permille uses integer/decimal `AwayFromZero`: `Documentation/01-Requirements/EngineRequirements.md:5247–5265`.
- Два files, only tooling layer and dedicated matching tests.

## Tests

`StrategyBalanceEvaluatorTests`:

- `Published_ledger_identity_is_audited_without_using_player_credit_delta` (AC-03/05).
- `Short_fifty_and_one_fifty_and_medium_one_fifty_and_three_fifty_are_inclusive` (AC-03).
- `Unknown_or_zero_cogs_is_reported_incomparable_without_division` (AC-03/05).
- `Same_long_route_crosses_from_nonpositive_starter_to_positive_cargo_upgrade` (AC-03).
- `Maximum_equal_to_twice_median_passes_and_one_permille_above_fails` (AC-04).
- `Even_median_and_tie_winner_are_canonical_under_reordered_input` (AC-04/05).
- `Event_state_changes_leader_and_unavailable_routes_do_not_compete` (AC-04).
- `One_route_winning_every_normal_and_event_state_is_reported` (AC-04).
- `Replayed_command_stale_quote_and_duplicate_posting_cannot_add_profit` (AC-04/05).
- `Unchanged_market_revision_requires_zero_new_realized_profit` (AC-04).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.EconomyBalance.Tests\DeepSpaceSaga.EconomyBalance.Tests.csproj --no-restore --filter FullyQualifiedName~StrategyBalanceEvaluatorTests
dotnet build D:\DeepSpaceSaga\DSS\tools\DeepSpaceSaga.EconomyBalance\DeepSpaceSaga.EconomyBalance.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Изменены только два allowed files; evaluator не открывает Engine/content и не мутирует evidence.
- AC-03/04/05 покрыты named tests для inclusive bands, crossover, median, leader change и replay/posting guards.
- Named tests/build/format проходят либо baseline failure записано отдельно.
- Ledger identity audit использует authoritative components; client/tool formula не становится gameplay source.
- Нет hidden cherry-picking, floating-point rounding, незаписанных assumptions или scope вне `Code context`.
- Violation содержит достаточный ledger/route context для точного повтора.

## Self-containment check

Comparable-set rules, exact bands, crossover pairing, median, tie-breaking, leader semantics, ledger identity и exploit predicates заданы. Implementer не выбирает выгодную формулу или способ исключить неудобный маршрут.
