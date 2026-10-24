---
epic: EP-0001-trading-system
story: EP-0001-US-0008-route-risk-and-alternatives
ticket: EP-0001-US-0008-TK-0002-effective-route-evaluator
title: Effective route и гарантия альтернативы
stage: approved
layer: engine
depends_on: [EP-0001-US-0008-TK-0001-route-risk-contract]
files_touched: 2
serves: [AC-01, AC-03, AC-04, AC-06]
created: 2026-09-21T11:10:35Z
revision: 1
---

# Effective route и гарантия альтернативы

## Why

Свести base edge и активные event modifiers в одну чистую authoritative модель и запретить состояние без альтернативы. End state: pure evaluator детерминированно выдаёт effective routes и отвечает, допустим ли candidate event, без UI/runtime mutation.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj. Пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

Единственное сообщение пользователя приведено в story и TK-0001; новых технических решений нет.

## Assumptions

US-0004 предоставляет пять станций, materialized edges и cargo flows по signatures ниже. US-0007 нормализует только активные route modifiers; scheduler/persistence остаются вне pure evaluator. `Restricted` — проходимое ребро, `Unavailable` исключается из adjacency. Базовые DTO не мутируются.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Trading/TradingRouteEvaluator.cs | Новый файл; текущий Engine содержит только station price event runtime (`SimulationEngine.cs:3290–3309`), economic graph/risk evaluator отсутствует | Internal modifier/effective records, Evaluate/CanApply, fixed-point helpers и connectivity check |
| tests/DeepSpaceSaga.Engine.Tests/TradingRouteEvaluatorTests.cs | Новый файл; Engine pure tests используют in-memory scenario/registry fixtures | Полная матрица deterministic combination, overflow, expiry input и alternative invariants |

## Public API after the change

Нет нового public/session API. В `DeepSpaceSaga.Engine.Trading`:

```csharp
internal sealed record TradingRouteModifier(
    string EventId, string EventTypeId, int Priority, long StartedGameTimeMs,
    string FromStationObjectId, string ToStationObjectId,
    TradingRouteAvailability Availability,
    int TravelTimeMultiplierPermille, int FuelMultiplierPermille,
    string ReasonText);

internal sealed record EffectiveTradingRoute(
    TradingMapEdgeData BaseEdge,
    long EffectiveTravelEstimateGameTimeMs,
    int EffectiveFuelMultiplierPermille,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability,
    string? ReasonText,
    ImmutableArray<string> ActiveEventIds);

internal static class TradingRouteEvaluator
{
    internal static ImmutableArray<EffectiveTradingRoute> Evaluate(
        TradingMapStateData map, IReadOnlyList<TradingRouteModifier> activeModifiers);
    internal static bool CanApply(
        TradingMapStateData map,
        IReadOnlyList<TradingRouteModifier> activeModifiers,
        IReadOnlyList<TradingRouteModifier> candidateModifiers);
}
```

Dependency API is fixed by EP-0001-US-0004-TK-0001: `TradingMapStateData.Rules.Stations`, `.Edges`, `.CargoFlows`; edge fields are From/To/DistanceKm/TravelEstimateGameTimeMs/DistanceClass/FuelMultiplierPermille/RiskProfileId. TK-0001 provides Contracts enums. If implemented dependency signatures differ, stop and review; do not change extra files here.

## Implementation steps

1. Validate input defensively: canonical unordered endpoints, edge must exist, nonblank IDs/reason, positive multipliers, unique `(EventId,edge)`. Invalid dependency state throws `ScenarioException` with event/endpoint context; `CanApply` returns false only for alternative failure, not malformed data.
2. Sort matching modifiers by `Priority` descending, then `StartedGameTimeMs`, `EventId`, endpoints ordinal. Availability is the strictest enum. Compose travel/fuel permille one factor at a time with checked decimal/integer arithmetic and `MidpointRounding.AwayFromZero`; reject overflow/nonpositive result. Apply travel factors to base estimate and fuel factors to base fuel multiplier, never distance or physical speed.
3. Risk is `Elevated` when base `RiskProfileId` is not the configured safe profile or any active modifier restricts/changes the edge; otherwise `Safe`. Safe profile ID comes from `map.Rules.RiskProfiles` as the unique entry with fuel multiplier 1000 and ID `risk.safe`; missing/duplicate is invalid map content, not inferred from distance.
4. Preserve ordered unique `ActiveEventIds`; `ReasonText` joins nonempty ordered reasons with `; `. Base route with no modifiers is `Available`, has null reason and exact base values.
5. `CanApply` evaluates active+candidate, builds undirected adjacency from Available/Restricted edges, and BFS-checks all five declared stations from canonical first station. Additionally verify every CargoFlow from/to remains mutually reachable. Candidate with `Unavailable` disconnecting any station/flow returns false. It must not mutate/downgrade candidate.
6. Evaluator has no clock: caller US-0007 passes only events active for the evaluated GameTimeMs. An expiry regression compares evaluation with modifier present and absent, proving exact base restoration and no accumulated mutation.

## Out of scope

Event generation/lifecycle/save, station market effects, voyage mutation, snapshot construction, UI, content coefficients, route fuel charge and physical path planning.

## Invariants

- Economic edge is logical and does not alter Approach/speed: ../Documentation.md:97–100; EngineRequirements.md:5335–5343.
- Fixed-point monetary/economic convention 1000=1.0 and midpoint away from zero: EngineRequirements.md:5247–5265.
- Base map edges/cargo flows are dependencies, not files changed here: US-0004/TK-0001 Public API.
- Exactly two files, engine layer plus Engine.Tests.

## Tests

`TradingRouteEvaluatorTests`:

- `Base_safe_and_risky_routes_differ_in_time_and_fuel_without_mutation` (AC-01).
- `Modifiers_combine_in_priority_time_event_order_with_away_from_zero_rounding` (AC-03/06).
- `Unavailable_is_stricter_than_restricted_and_reasons_are_ordered` (AC-03).
- `Expired_modifier_absence_restores_exact_base_values` (AC-06).
- `CanApply_accepts_connected_quarantine_and_rejects_bridge_or_isolated_station` (AC-04).
- `CanApply_preserves_every_cargo_flow_endpoint_path` (AC-04).
- `Reordered_edges_modifiers_and_flows_produce_identical_canonical_result` (AC-06).
- `Malformed_reference_multiplier_duplicate_or_overflow_is_contextual_error`.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter FullyQualifiedName~TradingRouteEvaluatorTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены в двух allowed files; dependency files не редактируются.
- AC-01/03/04/06 покрыты named tests, включая ordering, connectivity, cargo flows, expiry и overflow.
- Named tests/build/format проходят либо конкретное исходное падение записано отдельно.
- API, invariants и out-of-scope соблюдены; нет hidden fallback или client calculation.
- Результат проверяется pure input/output tests без запуска UI.

## Self-containment check

Dependency signatures, combining order, arithmetic, risk rule, connectivity algorithm, error behavior и test corpus заданы полностью. Реальные event scheduler/content не нужны этому merge unit.
