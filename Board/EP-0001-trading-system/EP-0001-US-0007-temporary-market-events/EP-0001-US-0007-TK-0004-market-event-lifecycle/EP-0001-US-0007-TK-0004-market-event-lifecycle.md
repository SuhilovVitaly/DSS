---
epic: EP-0001-trading-system
story: EP-0001-US-0007-temporary-market-events
ticket: EP-0001-US-0007-TK-0004-market-event-lifecycle
title: Seeded lifecycle и экономические эффекты
stage: approved
layer: engine
depends_on: [EP-0001-US-0007-TK-0001-market-event-contract, EP-0001-US-0007-TK-0002-market-event-catalog, EP-0001-US-0007-TK-0003-market-event-content, EP-0001-US-0002-TK-0003-hourly-market-simulation, EP-0001-US-0003-TK-0002-atomic-quote-execution]
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-06]
created: 2026-09-21T11:08:58Z
revision: 1
---

# Seeded lifecycle и экономические эффекты

## Why

Data definitions становятся игровым поведением: календарный scheduler детерминированно активирует/завершает события, применяет их к рынку, инвалидирует stale quotes, публикует snapshot и сохраняет exact state. Это единственный runtime merge unit истории.

Matching test project: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Decisions

D-01, 2026-09-21T11:08:58Z: «сделай тикеты для D:\DeepSpaceSaga\DSS\Board\EP-0001-trading-system\EP-0001-US-0007-temporary-market-events\EP-0001-US-0007-temporary-market-events.md».

## Assumptions

- Выполнение начинается только после merged US-0002 hourly market and US-0003/0015 revision APIs. Несовпадающие signatures — review blocker, не разрешение добавить шестой файл.
- Generated events стартуют/заканчиваются только на whole-hour boundaries; route application остаётся US-0008.
- Shipping profile productionSource=Profile; production/demand multipliers применяются к profile hourly output/input/consumption. Recipe/module output modulation и включение/выключение module — backlog.
- Старый save без event fingerprint продолжает со следующей ещё не обработанной hourly boundary без retroactive rolls; первый новый capture становится v10. V10 mismatch отклоняется.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.MarketEvents.cs | Новый partial file | Deterministic candidate roll/duration/instance ID, expiry, activation delta, effect resolvers, save validation helpers |
| src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs | :12–51 ordered calendar boundary loop; US-0002 добавляет market-hour boundary | На market-hour boundary вызвать expire/activate before ApplyMarketHour; one revision commit after all station changes |
| src/DeepSpaceSaga.Engine/SimulationEngine.MarketTime.cs | Planned US-0002/TK-0003 new partial with ApplyMarketHour/stock limits | Multipliers for profile flow, activation delta clamp integration, changed-station reporting; no second scheduler |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :196–339 candidate world/load; :560–598 trade projection; :756–918 save; :1418–1471 event resolve; :1597–1648 price factors; :3238–3309 runtime | Catalog fingerprint gate, resolved runtime/save/projection, generated-event validation, price/revision hooks |
| tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs | Current clock/chunk/save tests; US-0002 extends it for hourly markets | Добавить deterministic lifecycle/effect/save/legacy tests and helpers здесь же |

## Public API after the change

Новых public Engine methods нет. `StationTradeSnapshot.ActiveEvents` — TK-0001 public contract. Internal runtime `StationEventRuntime` расширяется до полного resolved shape: `DefinitionId`, three keys, `ItemEffects`, `RouteEffect`, `ActivationStockDeltaApplied`; legacy display fields/PriceFactors сохраняются.

В новом partial:

```csharp
private bool ApplyMarketEventBoundary(long boundaryGameTimeMs);
private ImmutableArray<StationEventRuntime> ResolveMarketEventsForLoad(
    SpaceObjectData station, bool isSave, string? saveFingerprint);
private static ulong MarketEventRoll(ulong masterSeed, string stationId,
    string definitionId, long hourIndex, string purpose);
private static int ResolveEventMultiplier(
    SpaceObjectRuntime station, int itemTypeIndex, MarketEventFlow flow, long atGameTimeMs);
private bool ApplyActivationStockDeltas(ref SpaceObjectRuntime station, long atGameTimeMs);
private ImmutableArray<StationMarketEventSnapshot> BuildActiveEventProjection(
    SpaceObjectRuntime station, long atGameTimeMs);
```

`MarketEventFlow` internal enum: Production, Demand. Price continues through existing `ResolveStationPriceFactors`; no new price API. US-0002 `ApplyMarketHour` returns/collects station IDs whose stock/budget changed. Event boundary adds event-set/delta changes. For each station with any combined visible change, call prerequisite revision staging/commit exactly once after all fallible candidate calculations succeed.

## Implementation steps

1. Preflight prerequisite APIs before editing. Require US-0002 bounded profile market/`SimulationEngine.MarketTime.cs`; US-0015 `NextMarketRevision/CommitMarketRevision` (or documented merged equivalent) and US-0003 quote invalidation. Record exact mismatch and return ticket to review rather than duplicate them.
2. Candidate function: `hourIndex=boundary/GameCalendar.HourMs`; roll seed=`RngStreamSeedDerivation.DeriveStreamSeed(masterSeed,$"market-event:roll:{stationId}:{definitionId}:{hourIndex}")`; candidate iff `seed % 1000 < ChancePermillePerHour`. Duration seed uses purpose `duration`; `hours=min + seed % (max-min+1)`. No shared RNG or enumeration-order dependence.
3. At each market-hour boundary, for stations ordered ordinal ID: remove finite events with `end<=boundary`; identify catalog definitions eligible for profile and not already active; compute all candidates; sort priority descending then definition ID ordinal; fill available slots to total active count2. Permanent legacy events count toward slots. Instance ID is `${definitionId}@${stationId}@${hourIndex}` and must be ordinal-unique.
4. Prepare all new runtime events and checked end times before mutating. End overflow/collision/invalid loaded state throws contextual ScenarioException and leaves candidate world unchanged. New event contains resolved item/route effects and existing price factors derived from item PriceMultiplier!=1000.
5. Apply each new event's ActivationStockDelta once in deterministic definition/item order after activation and before hourly market batch. Clamp to `[0,maxStock]` from US-0002; no PendingOutput. Mark applied even when cap makes actual delta0. Generated event cannot reach snapshot/save with false marker.
6. `ResolveEventMultiplier` selects active `[start,end)` event item effects, ordered `(start,eventId)`, checked-multiplies permille using decimal, rounds final hourly rate once AwayFromZero in US-0002 market helper. A multiplier component is no-op where the profile has no matching supply/demand flow. Profile inputs and consumption both use Demand. Recipe production remains unchanged.
7. Price path reuses `PriceFactors`: when resolving generated item effects, create item-specific `StationEventPriceFactorRuntime` only for PriceMultiplier!=1000. Existing size/stock/spread/quote formula and single final rounding stay owned by US-0015.
8. Boundary transaction order: expire → calculate/activate → activation deltas → ApplyMarketHour with current events → budget restore → collect changed station IDs → commit one market revision/invalidation per changed station → commit calendar cursor. If any checked calculation fails, do not partially mutate station/event/revision.
9. Snapshot projects only currently active events in `(start,eventId)` order. Use TK-0001 keys/end/route DTO and compute `RemainingGameTimeMs=max(0,end-current snapshot GameTimeMs)`; permanent legacy uses `long.MaxValue`. Legacy event without keys uses empty keys plus fallback display text; expired event absent.
10. New Game with catalog and no fingerprint enables scheduler from first hour. V10 save requires exact registry event fingerprint and fully valid generated events; mismatch or malformed event fails before replacing world. Old save/scenario without fingerprint loads existing legacy events and starts future rolls only after its restored `_processedWorldTimeMs`, never replays earlier hours.
11. Load validation: max two simultaneous active events/station at loaded time; generated definition exists; keys/effects/route equal resolved save payload and catalog under matching fingerprint; finite positive duration; unique instance/definition; delta marker true. Legacy raw event retains existing rules and may be permanent.
12. Save writes fingerprint only when registry event catalog nonempty and materializes full active event state. Capture after expiry contains no ended generated event. Load mid-event preserves exact end and never reapplies delta.
13. Blockade/quarantine projection carries the catalog descriptor. Do not inspect trading map, choose an edge, mutate route availability or promise alternative path here.
14. Add tests below using tiny deterministic catalogs/chances (`1000` for forced activation) via direct registry with complete-set gate disabled. Shipping exact values remain tested by TK-0003.

## Out of scope

Contracts/content/client changes; route edge selection/application; voyage commands; recipe/module production modifiers; battle/damage; probability tuning; ten-day balance; remote market knowledge; general save migrations. Не менять calendar/motion multipliers or Approach.

## Invariants

- Calendar and motion time remain separate: SimulationEngine.EconomyTime.cs:12–20; no event may accelerate physical ship motion.
- Scheduler uses authoritative game time and existing boundary loop, never real time: :21–51.
- Price factors are fixed-point and rounded once: EngineRequirements.md:5251–5265.
- Event price state persists: EngineRequirements.md:5288–5299.
- World replacement occurs only after successful load/preflight: current SimulationEngine.cs:196–240.
- Market quote execution/revision remains atomic under US-0003 assumptions; rejected/stale commands do not mutate.

## Tests

В `EconomyTimeContinuityTests.cs` добавить:

- `Forced_event_activates_on_hour_boundary_and_expires_at_half_open_end` (AC-02/03): H−1,H,end−1,end; exact active set and price/flow restoration.
- `Candidate_selection_is_seeded_order_independent_and_capped_at_two` (AC-02): shuffled definitions/stations yield same IDs/durations; priority then ID chooses same two.
- `Chunked_accelerated_and_single_step_runs_produce_identical_event_markets` (AC-02): same seed/commands/final GameTime, Speed1/Speed4 and different capture chunks; snapshot stock/events/revision equal, motion baseline unchanged.
- `Paused_real_time_does_not_start_or_expire_market_events` (AC-02).
- `Activation_delta_applies_once_and_clamps_to_bounded_stock` (AC-03): full/near-zero cases, save immediately after activation, no repeat.
- `Production_demand_and_price_effects_apply_only_inside_event_window` (AC-03): exact baseline/event/after rates and quote values, no client calculation.
- `Event_boundary_invalidates_quote_once_per_changed_station` (AC-03): activation+delta+hour batch one revision; expiry one; stale quote rejected, no double increment.
- `Mid_event_save_load_preserves_end_effects_revision_and_no_replay` (AC-06): compare uninterrupted run through end.
- `Event_fingerprint_or_resolved_payload_mismatch_rejects_without_world_replacement` (AC-06).
- `Legacy_price_event_and_pre_v10_save_remain_readable` (AC-06): future rolls start after restored cursor only.
- `Blockade_and_quarantine_project_descriptor_without_mutating_routes` (AC-04): exactly route DTO; graph/voyage untouched.
- `Generated_event_load_rejects_overlap_over_two_duplicate_or_unapplied_delta` (AC-02/06).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~EconomyTimeContinuityTests|FullyQualifiedName~StationEconomyBatch2Tests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все runtime steps выполнены ровно в пяти разрешённых файлах после verified prerequisites.
- AC-02/03/04/06 покрыты named deterministic/state tests.
- Engine tests/build/format проходят; unrelated baseline failures записаны отдельно.
- No partial boundary mutation, duplicate delta, repeated quote invalidation or motion-time change.
- Save/fingerprint/legacy behavior и route boundary соблюдены.
- Результат наблюдаем через snapshot, stock/quote/revision и save comparison.

## Self-containment check

Ticket задаёт exact hash inputs, candidate ordering, duration, lifecycle ordering, multiplier semantics, revision transaction, load/save rules and named tests. Требуется лишь наличие перечисленных prerequisite signatures; никакие дополнительные продуктовые решения или файлы искать не нужно. End state: один seed воспроизводит ограниченные временные события и их authoritative market effects через Save/Load.
