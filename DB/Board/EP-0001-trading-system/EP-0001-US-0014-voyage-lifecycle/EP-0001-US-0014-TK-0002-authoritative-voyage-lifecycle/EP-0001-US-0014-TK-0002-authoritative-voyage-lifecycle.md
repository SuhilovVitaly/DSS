---
epic: EP-0001-trading-system
story: EP-0001-US-0014-voyage-lifecycle
ticket: EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle
title: Authoritative lifecycle и продолжение voyage
stage: approved
layer: engine
depends_on: [EP-0001-US-0014-TK-0001-voyage-contract, EP-0001-US-0004-seeded-trading-map]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06, AC-07, AC-09]
created: 2026-09-21T14:55:44Z
revision: 1
---

# Authoritative lifecycle и продолжение voyage

## Why

Связать существующие Undock/Dock с одним сохраняемым рейсом, не подменяя физическое движение. End state: Engine валидирует выбранное ребро, проводит observable phases, публикует monotonic progress/route options, ограничивает docking destination и точно продолжает active voyage после JSON round-trip.

Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Пути относительно `D:/DeepSpaceSaga/DSS`.

## Decisions

Точное пользовательское сообщение приведено в story и TK-0001; отдельных решений нет.

## Assumptions

US-0004 после merge предоставляет `GameStateData.TradingMap`, `TradingMapStateData.Edges` и materialized Station objects по API её TK-0001/TK-0004. Progress использует motion coordinates, не calendar ETA. Debt blocker — `PortFeeDebt > 0`. Fuel/event evaluator не выдумывается: method boundary возвращает только доступные сейчас structural/debt blockers, а US-0008/US-0009 добавят свои reason codes.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :7–24 save version 8; :52–78 GameStateData не содержит voyage | Добавить optional `VoyageStateData`; не резервировать новый numeric version и не менять другие DTO |
| src/DeepSpaceSaga.Engine/SimulationEngine.Voyage.cs | Новый partial; voyage runtime/projection отсутствуют | Вся validation/state-machine/progress/route-option логика и internal helpers |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | :196–359 staged load; :401–533 snapshot; :737–833 save; :1830–1963 Dock/Undock | Минимальные вызовы voyage helper при load/advance/save/projection и в существующих navigation branches |
| tests/DeepSpaceSaga.Engine.Tests/VoyageLifecycleTests.cs | Новый файл; existing navigation fixture — DockCommandTests.cs:20–138 | Map-backed fixture и lifecycle/block/trade/idempotency regressions |
| tests/DeepSpaceSaga.Engine.Tests/VoyagePersistenceTests.cs | Новый файл; save pattern — DockCommandTests.cs:260–279; SaveLoadContinuityTests | JSON round-trip, invalid-save и continuation equivalence |

## Public API after the change

В `GameStateData` последний optional параметр:

```csharp
[property: JsonPropertyName("voyageState")]
VoyageStateData? VoyageState = null
```

В namespace `DeepSpaceSaga.Engine.Scenario`:

```csharp
public sealed record VoyageStateData(
    [property: JsonPropertyName("phase")] string Phase,
    [property: JsonPropertyName("voyageId")] string? VoyageId = null,
    [property: JsonPropertyName("originStationObjectId")] string? OriginStationObjectId = null,
    [property: JsonPropertyName("destinationStationObjectId")] string? DestinationStationObjectId = null,
    [property: JsonPropertyName("startedMotionTimeMs")] long StartedMotionTimeMs = 0,
    [property: JsonPropertyName("initialDistanceWorldUnits")] double InitialDistanceWorldUnits = 0,
    [property: JsonPropertyName("progressPermille")] int ProgressPermille = 0,
    [property: JsonPropertyName("blockReasonCode")] string? BlockReasonCode = null);
```

No new public Engine method. Existing `navigation.undock` requires `TargetObjectId` only when a materialized trading map exists; legacy mapless behavior stays unchanged. Snapshot API is TK-0001.

## Implementation steps

1. При staged load канонизировать/валидировать optional voyage до commit. `Docked`: player ship docked, active IDs null, progress 0. Active phases: nonblank unique `VoyageId`, distinct existing Station origin/destination joined by exactly one materialized edge, finite initial distance >0, progress 0..1000, nonnegative start motion time, player not docked. Unknown phase/ID, partial fields, active voyage without map, `Docking` without matching active docking dialogue или contradictory ship state — `ScenarioException` до изменения мира. Legacy null materializes `Docked` from current ship only in runtime; mapless undocked remains null.
2. Для docked ship построить sorted route options только из adjacent `TradingMapStateData.Edges`; destination display name берётся из runtime Station, estimate/class — из edge. Structural/active/debt blocker вычисляет Engine; Client не получает hidden market/budget. Route option от US-0008/US-0009 может later добавить blocker через один `ResolveVoyageDepartureBlock` helper, не меняя DTO.
3. В map-backed Undock требовать destination. Отклонить missing/unknown/non-adjacent, active voyage, debt или unavailable option соответствующим TK-0001 reason; записать reason в docked voyage state и обычный durable `CommandResult`. Успех использует `CommandId` как `VoyageId`, origin = текущая docking station, initial distance = live ship-to-destination distance, очищает reason, создаёт `Undocking`, затем выполняет существующее motion-preserving undock ровно один раз.
4. Existing untargeted Undock mapless scenario оставить без voyage. Повторный `CommandId` обслуживает существующий journal (`SimulationEngine.cs:130–143`) и не создаёт новую запись. Любая новая start-команда при active voyage — `voyage_already_active`.
5. На каждом `AdvanceWorldTo` после physical motion update: `Undocking → InTransit` только если `simulationTimeMs > StartedMotionTimeMs`; для `InTransit` вычислить remaining live distance и монотонный formula из A-02. Не изменять object motion, cycle, speed, target, ApproachRoute или calendar cursor. Destination missing/destroyed не телепортирует и не завершает voyage; сохраняет lifecycle и публикует `voyage_destination_unavailable`.
6. При Dock во время active voyage разрешить только destination; другой target — `voyage_wrong_destination`. После всех существующих station/range/synchronization checks и успешного старта docking dialogue установить `Docking`/1000. Пока dialogue active — сохранить phase. Если dialogue завершился без docking, вернуть `InTransit`; если ship стал docked к destination, очистить active fields и materialize `Docked`. Вызывать reconciliation после pending dialogue processing и до snapshot/save serialization, чтобы не записать contradictory state.
7. Snapshot: `Docked` содержит route options и last blocker; active phases содержат voyage ID/origin/destination/display/progress, route options empty. Existing `BuildDockedStationTradeProjection` не менять: его guard уже даёт null после Undock и рынок фактического target после Dock (`SimulationEngine.cs:560–597`). Lifecycle tests явно фиксируют это.
8. Save пишет normalized `_voyageState` в единый `GameStateData`; load продолжает без повторной команды/phase edge. Не bump `CurrentSaveFormatVersion` в этом ticket: field optional/additive, а единый final migration bump принадлежит US-0012. Если merged prerequisite уже требует version bump, сохранить его и не понижать.

## Out of scope

Fuel requirement/reservation/settlement, event route closure, ledger, UI, new connection method, auto movement/docking, изменение trading map DTO, cargo copy, price/quote и завершение US-0004.

## Invariants

- Current Undock сохраняет motion и очищает docking state: `SimulationEngine.cs:1883–1903`; это поведение переиспользуется, не переписывается альтернативной физикой.
- Dock station/range/synchronization/dialogue остаются authoritative: `SimulationEngine.cs:1906–1963`; `DialogueEffectTransaction.cs:79–94`.
- Trade projection существует только при реальном `IsDocked` и берёт `DockedStationObjectId`: `SimulationEngine.cs:560–597`.
- Full runtime строится до world commit: `SimulationEngine.cs:196–240,342–359`; invalid voyage не должен повредить текущий world.
- Save catches up motion/commands and serializes one state root: `SimulationEngine.cs:737–833`.
- Physical motion использует simulation time отдельно от calendar effects: `SimulationEngine.EconomyTime.cs:12–52`; Approach speed/route invariant — `EngineRequirements.md:5335–5343`.

## Tests

`VoyageLifecycleTests`:

- `Map_backed_undock_requires_adjacent_destination_and_publishes_options` (AC-01/02/06).
- `Undock_transitions_once_to_in_transit_without_changing_motion_or_approach` (AC-02/03/09; сравнить speed/direction/ApproachRoute).
- `Progress_is_monotonic_when_ship_advances_then_turns_away` (AC-03).
- `Trade_closes_at_departure_and_reopens_only_for_destination_after_docking_dialogue` (AC-04/05).
- `Dock_to_non_destination_is_rejected_and_abort_returns_in_transit` (AC-04).
- `Debt_blocks_departure_and_duplicate_command_does_not_create_second_voyage` (AC-02/06).
- `Legacy_mapless_untargeted_undock_keeps_existing_behavior` (AC-09; сохранить DockCommandTests regressions).

`VoyagePersistenceTests`:

- `Every_active_phase_roundtrips_ids_progress_and_block_reason` (AC-07).
- `Loaded_in_transit_voyage_continues_with_same_next_progress_and_docking_result` (AC-07).
- `Load_rejects_unknown_phase_partial_ids_bad_progress_non_edge_and_ship_state_conflict_without_replacing_world` (AC-07).
- `Save_after_pending_undock_or_terminal_docking_does_not_replay_transition` (AC-02/04/07).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~VoyageLifecycleTests|FullyQualifiedName~VoyagePersistenceTests|FullyQualifiedName~DockCommandTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все implementation steps выполнены в пяти разрешённых файлах; dependency signatures US-0004/TK-0004 подтверждены перед началом.
- AC-01…07/09 покрыты named lifecycle/persistence/legacy tests; fuel/event integrations не выданы за реализованные.
- Named tests, matching layer build и format проходят либо конкретный baseline failure записан отдельно.
- Save validation atomic, command replay idempotent, trade/docking/Approach invariants соблюдены.
- Нет hidden files, второго cargo/market state или незаписанных assumptions; результат проверяем snapshot, JSON round-trip и diff.

## Self-containment check

Состояния, persisted fields, validation, command semantics, progress formula, docking reconciliation, downstream seams и exact dependency API перечислены. Дополнительный поиск решений не требуется; при несовпадении merged US-0004 ticket возвращается в review.
