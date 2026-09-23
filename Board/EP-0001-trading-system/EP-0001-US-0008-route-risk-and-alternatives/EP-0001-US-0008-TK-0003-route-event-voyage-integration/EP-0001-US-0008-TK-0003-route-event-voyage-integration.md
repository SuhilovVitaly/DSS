---
epic: EP-0001-trading-system
story: EP-0001-US-0008-route-risk-and-alternatives
ticket: EP-0001-US-0008-TK-0003-route-event-voyage-integration
title: События, выбор рейса и snapshot
stage: approved
layer: engine
depends_on: [EP-0001-US-0008-TK-0002-effective-route-evaluator]
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-06]
created: 2026-09-21T11:10:35Z
revision: 1
---

# События, выбор рейса и snapshot

## Why

Подключить pure effective-route model к двум authoritative решениям: запуску события и старту рейса, затем опубликовать тот же результат docked player. End state: закрывающее сеть событие не активируется, недоступный рейс отклоняется, restricted duration фиксируется в voyage, snapshot и Save/Load согласованы.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj. Пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

Пользователь указал только canonical story path; иных решений нет. Точные integration rules приняты как безопасные assumptions story A-06…A-08.

## Assumptions

Тикет исполняется только после US-0004, US-0007 и US-0006/0014. US-0007 владеет persisted event lifecycle и предоставляет normalized modifiers; US-0006/0014 владеет persisted voyage и command reason `route_unavailable`. Их фактические файлы должны совпасть с таблицей или ticket возвращается в review до изменений. Route event expiry не меняет уже начатый voyage.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.MarketEvents.cs | Планируемый dependency US-0007; сейчас trigger/lifecycle отсутствует, а текущие price events schema-only (`ScenarioData.cs:156–160`, `SimulationEngine.cs:1420–1428`) | Проверка route candidate через `CanApply`, stable fallback без дополнительного RNG draw |
| src/DeepSpaceSaga.Engine/SimulationEngine.Voyages.cs | Планируемый dependency US-0006/0014; сейчас Undock отсутствует (EngineRequirements.md:5076–5081) | Revalidate destination edge, reject Unavailable, capture effective time/fuel/risk/events in voyage |
| src/DeepSpaceSaga.Engine/SimulationEngine.TradingRoutes.cs | Новый partial; route projection отсутствует | Адаптер active event state → normalized modifiers и docked outgoing projection |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | `BuildSnapshot` создаёт transport DTO в :510–534; docked station найден authoritative в `BuildDockedStationTradeProjection` :560–597 | Добавить один вызов `TradingRoutes: BuildTradingRouteProjection(clockState.GameTimeMs)` |
| tests/DeepSpaceSaga.Engine.Tests/RouteRiskIntegrationTests.cs | Новый файл; current station event/route integration coverage отсутствует | Scheduler/voyage/snapshot/expiry/save-load regressions через public engine surface |

## Public API after the change

No new public API beyond TK-0001. Internal partial seam:

```csharp
private ImmutableArray<TradingRouteSnapshot> BuildTradingRouteProjection(long gameTimeMs);
private ImmutableArray<TradingRouteModifier> BuildActiveRouteModifiers(long gameTimeMs);
```

Required dependency contracts at implementation start:

- US-0004: loaded `TradingMapStateData? _tradingMap` with materialized Edges/CargoFlows.
- US-0007: active event state contains EventId, EventTypeId, Priority, StartedGameTimeMs, DurationMs, player text and zero-or-more route effects with canonical endpoints/availability/travel/fuel permille; expired events are not returned as active.
- US-0006/0014: voyage start receives destination station ID; persisted voyage stores `TravelEstimateGameTimeMs`, `FuelMultiplierPermille`, `RiskProfileId`, event IDs and arrival time; rejection reason `route_unavailable` already belongs to that command contract.

If a dependency contract differs, update this ticket/story through review; do not add Contracts fields or persistence schema in this engine ticket.

## Implementation steps

1. Adapter maps every active US-0007 route effect to `TradingRouteModifier`, preserving event text/priority/start. It does not read expired/future events and does not derive display reason in Client.
2. Before US-0007 commits pirate-blockade/quarantine route effects, combine current modifiers with each seed-derived candidate and call `CanApply`. Iterate the already-generated canonical candidate list; activate the first valid candidate. Rejected candidates consume no extra RNG and create no partial market/route mutation. If none valid, event stays inactive for this interval and US-0007 records its normal deterministic next attempt.
3. At voyage command validation and authoritative execution boundary, evaluate current edge. Unknown/nonadjacent or `Unavailable` uses dependency reason `route_unavailable`; `Restricted` is allowed. On success copy effective travel/fuel/risk/event IDs into the new voyage and compute arrival from effective travel exactly once. Do not mutate `SpeedKmS`, motion time or Approach.
4. `BuildTradingRouteProjection` returns empty unless player ship is authoritatively docked and `_tradingMap` exists. Filter evaluated edges incident to docked station, orient origin→destination, map to TK-0001 DTO, sort destination ordinal. Never publish station price/stock/budget or unrelated global edges.
5. Add trailing `TradingRoutes` argument in `BuildSnapshot`; route evaluation uses the same snapshot calendar time. No new mutable/persisted field is introduced by US-0008.
6. Integration tests cover event activation, departure before/during/after event, failed candidate atomicity, save/load from dependency state and a repeated snapshot at same time. Verify route terms in an active voyage remain frozen while new docked projections after expiry return base values.

## Out of scope

US-0007 scheduling/schema/price rules, US-0014 command/UI design, SaveFormat migration, actual fuel reservation, net profit, Client rendering, map generation/content and physical trajectory.

## Invariants

- Engine alone owns route availability; client never mutates world: EngineRequirements.md:362–419.
- Current snapshot is built under world-state lock and docked trade is locality-gated: SimulationEngine.cs:510–534,560–597; route projection follows the same boundary.
- Calendar and motion remain separate: SimulationEngine.EconomyTime.cs:12–51. Changing travel schedule does not accelerate ship motion.
- Events apply deterministically by time/ID in current price path: SimulationEngine.cs:1597–1628; route adapter adds priority first per epic rule, without changing price ordering.
- Exactly five files, one production layer engine, matching Engine.Tests.

## Tests

`RouteRiskIntegrationTests`:

- `Blockade_activates_first_candidate_that_preserves_connected_alternative_without_extra_rng` (AC-04/06).
- `Quarantine_candidate_that_disconnects_a_station_is_not_partially_applied` (AC-04).
- `Docked_snapshot_publishes_only_sorted_outgoing_effective_routes` (AC-02).
- `Unavailable_destination_is_rejected_and_restricted_destination_uses_effective_duration` (AC-03).
- `Started_voyage_keeps_captured_terms_when_event_expires` (AC-03/06).
- `Expiry_restores_base_projection_once_without_replaying_event` (AC-06).
- `Save_load_active_event_and_voyage_rebuilds_identical_effective_state` (AC-06; persistence itself supplied by dependencies).
- `Route_effects_do_not_change_ship_speed_motion_time_or_approach_route` (AC-03).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter FullyQualifiedName~RouteRiskIntegrationTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все steps выполнены только в пяти files; Contracts/content/client/dependency schemas не расширены.
- AC-02/03/04/06 покрыты named tests через authoritative engine surface.
- Named tests/build/format проходят либо конкретное baseline failure записано отдельно.
- Event candidate application atomic/deterministic, voyage terms frozen, legacy no-map path empty and valid.
- Нет скрытых assumptions, блокирующих вопросов или работы вне `Code context`; результат виден в command result/snapshot/save-load diff.

## Self-containment check

Allowed integration seams, dependency API, ordering, rejection, snapshot locality, voyage capture и tests перечислены. Implementer не должен проектировать новый event/voyage contract; несовпадение dependency возвращает ticket в review.
