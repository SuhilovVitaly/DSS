using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Save file format version constants.</summary>
public static class SaveFormat
{
    /// <summary>
    /// Current save format version written by <see cref="SimulationEngine.CaptureSaveState"/>.
    /// Bump when the save schema changes incompatibly (see requirements §3891 migration policy —
    /// not implemented yet, only the version field is laid down in this iteration).
    /// Bumped to 2 when module placement moved from platformIndex+occupiedCells(0..3) to a
    /// hull-grid coordinate model (requirements §57) — no migration of old saves is provided.
    /// </summary>
    public const int CurrentSaveFormatVersion = 2;
}

/// <summary>Root of the scenario JSON file. Also used as the save-file format.</summary>
public sealed record ScenarioFile(
    [property: JsonPropertyName("scenarioMetadata")] ScenarioMetadata Metadata,
    [property: JsonPropertyName("gameState")] GameStateData GameState,
    [property: JsonPropertyName("saveFormatVersion")] int SaveFormatVersion = 0);

/// <summary>Scenario identification.</summary>
/// <param name="Description">
/// Player-facing summary shown in the client's New Game scenario picker. Optional (null)
/// for backward compatibility with scenario/save files predating it — a save's metadata
/// is never shown in that picker, so it never needs one.
/// </param>
public sealed record ScenarioMetadata(
    [property: JsonPropertyName("scenarioId")] string ScenarioId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description = null);

/// <summary>Initial game state.</summary>
/// <param name="MasterSeed">
/// One per session, immutable for its lifetime. Absent (null) in New Game scenario files —
/// SimulationEngine.LoadScenario generates a fresh random value whenever this is null,
/// which covers both New Game (expected) and a legacy save missing it (unexpected — the
/// caller is expected to warn; see SimulationEngine.MasterSeedWasMissingOnLoad). Present
/// and reused as-is when continuing/restoring a save.
/// </param>
public sealed record GameStateData(
    [property: JsonPropertyName("gameTimeMs")] long GameTimeMs,
    [property: JsonPropertyName("currentSpeed")] string CurrentSpeed,
    [property: JsonPropertyName("playerShipObjectId")] string PlayerShipObjectId,
    [property: JsonPropertyName("focus")] FocusData? Focus,
    [property: JsonPropertyName("spaceObjects")] IReadOnlyList<SpaceObjectData> SpaceObjects,
    [property: JsonPropertyName("masterSeed")] ulong? MasterSeed = null,
    /// <summary>
    /// Player's Credits balance (Docs\FirstRelease\Mechanics\Money.md). Null means "not yet
    /// resolved" — SimulationEngine.LoadScenario treats a missing value as 0 (a New Game
    /// player always starts with 0 Credits; this is a plain default, never randomized).
    /// </summary>
    [property: JsonPropertyName("playerCredits")] long? PlayerCredits = null);

/// <summary>Camera focus configuration.</summary>
public sealed record FocusData(
    [property: JsonPropertyName("mode")] string Mode,
    [property: JsonPropertyName("objectId")] string ObjectId);

/// <summary>A single space object from the scenario.</summary>
public sealed record SpaceObjectData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("objectType")] string ObjectType,
    [property: JsonPropertyName("persistenceType")] string PersistenceType,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("positionX")] double PositionX,
    [property: JsonPropertyName("positionY")] double PositionY,
    [property: JsonPropertyName("speedMps")] int SpeedMps,
    [property: JsonPropertyName("directionDegrees")] int DirectionDegrees,
    [property: JsonPropertyName("movementType")] string MovementType,
    [property: JsonPropertyName("massKg")] long? MassKg,
    [property: JsonPropertyName("compositionType")] string? CompositionType,
    [property: JsonPropertyName("modules")] IReadOnlyList<ShipModuleData>? Modules,
    [property: JsonPropertyName("isKnown")] bool IsKnown = false,
    /// <summary>
    /// Hull grid geometry (width/height + structural cells) for any ship-like object that
    /// carries modules (requirements §57 — replaces the platformIndex/occupiedCells(0..3)
    /// model). Nullable at the DTO level; the domain requires it whenever Modules is
    /// non-empty (see SimulationEngine.ValidateModulePlacement).
    /// </summary>
    [property: JsonPropertyName("hullLayout")] HullLayoutData? HullLayout = null,
    /// <summary>
    /// True when this object (the player ship) is docked to a station
    /// (navigation.dock). Persisted across save/load. Absent/false for every
    /// scenario/save file predating docking and for every non-ship object.
    /// </summary>
    [property: JsonPropertyName("isDocked")] bool IsDocked = false,
    /// <summary>ObjectId of the station this object is docked to. Null unless <see cref="IsDocked"/>.</summary>
    [property: JsonPropertyName("dockedStationObjectId")] string? DockedStationObjectId = null,
    /// <summary>
    /// Station's Credits balance (Docs\FirstRelease\Mechanics\Money.md). Only meaningful for
    /// ObjectType == Station. Null means "not yet resolved" — SimulationEngine.LoadScenario
    /// generates a deterministic value from masterSeed the first time; a subsequent save
    /// always carries the resolved value explicitly, so it is never regenerated again.
    /// </summary>
    [property: JsonPropertyName("credits")] long? Credits = null,
    /// <summary>
    /// Station's price coefficient, fixed-point where 1000 == 1.0x (Docs\FirstRelease\
    /// Mechanics\StationInventory.md's 0.5..2.0 range == 500..2000 here — the project
    /// forbids float/double for authoritative values). Same "null == not yet resolved,
    /// resolved once and then always explicit" rule as <see cref="Credits"/>.
    /// </summary>
    [property: JsonPropertyName("priceCoefficient")] int? PriceCoefficient = null,
    /// <summary>
    /// Station's tradeable stock, one entry per sellable item type. Same "null == not yet
    /// resolved" rule as <see cref="Credits"/>.
    /// </summary>
    [property: JsonPropertyName("inventory")] IReadOnlyList<StationInventoryItemData>? Inventory = null,
    /// <summary>
    /// Station's size classification (requirements §59, Docs\FirstRelease\TechnicalTasks\
    /// StationEconomyProductionAndSizing.md "Размеры станции") — one of "Huge"/"Large"/
    /// "Medium"/"Outpost" (case-insensitive). Only meaningful for ObjectType == Station. Null
    /// means "not yet resolved": unlike <see cref="Credits"/>/<see cref="PriceCoefficient"/>/
    /// <see cref="Inventory"/>, this is never RNG-generated — SimulationEngine.LoadScenario
    /// resolves a fixed fallback the first time and persists it explicitly from then on (see
    /// SimulationEngine.ResolveStationSize).
    /// </summary>
    [property: JsonPropertyName("stationSize")] string? StationSize = null,
    /// <summary>
    /// Station's producing-module instances (requirements §59 "Производящие модули станции").
    /// Fully explicit — never RNG-generated. Null/empty means the station has no producing
    /// modules (the common case; every existing scenario/save predates this field).
    /// </summary>
    [property: JsonPropertyName("producingModules")] IReadOnlyList<StationProducingModuleData>? ProducingModules = null,
    /// <summary>
    /// Station's active events/buffs/debuffs (requirements §59 "События, бафы и дебафы
    /// станции") — schema + persistence only (story-20260825-084409 CP-3): no triggering
    /// engine exists yet, so this is always empty for every scenario file the game ships, but
    /// round-trips through save/load and participates in the price formula when present.
    /// </summary>
    [property: JsonPropertyName("events")] IReadOnlyList<StationEventData>? Events = null,
    /// <summary>
    /// Ship's crew members (story-20260901-112254, "Crew and cabin occupancy"). Only
    /// meaningful for ObjectType == PlayerShip. Null/empty means the ship has no crew (the
    /// common case; every existing scenario/save predates this field).
    /// </summary>
    [property: JsonPropertyName("crew")] IReadOnlyList<ShipCrewMemberData>? Crew = null);

/// <summary>
/// One crew member aboard a ship (story-20260901-112254). Flat list, no roles/skills/
/// dialogue — only enough to count crew and pair them with cabins in the station toolbar.
/// </summary>
/// <param name="CrewId">Stable id, unique per ship (e.g. "CHR-0001").</param>
/// <param name="DisplayName">UI display name.</param>
public sealed record ShipCrewMemberData(
    [property: JsonPropertyName("crewId")] string CrewId,
    [property: JsonPropertyName("displayName")] string DisplayName);

/// <summary>
/// One producing-module instance installed on a station (requirements §59 "Производящие
/// модули станции"). <see cref="ProducingModuleTypeId"/> refers to a
/// <c>FactoryTypeDefinition.TypeId</c> in the content registry.
/// </summary>
public sealed record StationProducingModuleData(
    [property: JsonPropertyName("producingModuleTypeId")] string ProducingModuleTypeId,
    /// <summary>
    /// Whether this producing module is currently active/available. Only active producing
    /// modules make their input Resources "ConsumedResource" for price-factor selection (§59
    /// "Минимальное правило для торговли"). Defaults to true — an explicitly-listed producing
    /// module is assumed active unless a scenario/save says otherwise.
    /// </summary>
    [property: JsonPropertyName("active")] bool Active = true);

/// <summary>
/// One station event/buff/debuff (requirements §59 "События, бафы и дебафы станции"),
/// schema + persistence only — story-20260825-084409 CP-3. No triggering/lifecycle engine
/// exists; an event present in scenario/save data simply participates in the price formula
/// for as long as it is present.
/// </summary>
/// <param name="EventId">Stable id, unique per station.</param>
/// <param name="DisplayName">UI display name.</param>
/// <param name="Description">Optional UI description.</param>
/// <param name="StartedGameTimeMs">
/// GameTimeMs the event started — together with <see cref="EventId"/>, defines the
/// deterministic application order the requirement calls for when two events share the same
/// start time (ordered by (StartedGameTimeMs, EventId), StringComparer.Ordinal).
/// </param>
/// <param name="DurationMs">
/// Null means a permanent scenario-authored effect (§59 "время действия или флаг
/// постоянного сценарного эффекта"); a non-null value is the event's duration in ms. Neither
/// value is interpreted by any engine lifecycle in this iteration — schema-only.
/// </param>
/// <param name="PriceFactors">
/// The multiplicative <c>StationPriceFactor</c>s this event contributes. See
/// <see cref="StationEventPriceFactorData"/>.
/// </param>
public sealed record StationEventData(
    [property: JsonPropertyName("eventId")] string EventId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("startedGameTimeMs")] long StartedGameTimeMs,
    [property: JsonPropertyName("durationMs")] long? DurationMs,
    [property: JsonPropertyName("priceFactors")] IReadOnlyList<StationEventPriceFactorData> PriceFactors);

/// <summary>
/// One multiplicative price factor contributed by a <see cref="StationEventData"/>. Addresses
/// either a whole <see cref="ItemTypeId"/>+null case is the specific-item addressing mode,
/// <see cref="Category"/>+null <see cref="ItemTypeId"/> is the category-wide mode, and both
/// null applies the factor to every tradeable item on the station (§59: "список price factors
/// по категории или конкретному TradeItem").
/// </summary>
/// <param name="Category">"Resource" or "Good" (case-insensitive), or null.</param>
/// <param name="ItemTypeId">A specific <c>ItemTypeDefinition.TypeId</c>, or null.</param>
/// <param name="Factor">Fixed-point multiplier, 1000 == 1.0x.</param>
public sealed record StationEventPriceFactorData(
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("itemTypeId")] string? ItemTypeId,
    [property: JsonPropertyName("factor")] int Factor);

/// <summary>A ship module declared in a scenario.</summary>
public sealed record ShipModuleData(
    [property: JsonPropertyName("moduleId")] string ModuleId,
    [property: JsonPropertyName("moduleTypeId")] string ModuleTypeId,
    [property: JsonPropertyName("occupiedCells")] IReadOnlyList<HullCellCoordinate> OccupiedCells,
    [property: JsonPropertyName("structurePoints")] int StructurePoints,
    [property: JsonPropertyName("powerState")] string PowerState,
    [property: JsonPropertyName("operationalState")] string OperationalState,
    [property: JsonPropertyName("activeCycle")] ActiveCycleData? ActiveCycle,
    [property: JsonPropertyName("cargo")] IReadOnlyList<CargoStackData>? Cargo,
    [property: JsonPropertyName("fuelAmountKg")] long? FuelAmountKg = null,
    [property: JsonPropertyName("lastTurnGameTimeMs")] long? LastTurnGameTimeMs = null);

/// <summary>A single structural cell coordinate on a ship's hull grid (requirements §57).</summary>
public sealed record HullCellCoordinate(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y);

/// <summary>
/// Hull grid geometry for a ship-like object: overall bounding size plus the set of
/// structural cells modules may occupy (requirements §57).
/// </summary>
public sealed record HullLayoutData(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("cells")] IReadOnlyList<HullCellCoordinate> Cells);

/// <summary>
/// Runtime progress for an active module cycle.
/// </summary>
/// <param name="TargetObjectId">
/// ObjectId of the target for match commands (engine.speedSynchronization /
/// engine.directionSynchronization, requirements §56.9). Filled when a match cycle starts;
/// always set for match cycles (diagnostics + §1253-1298 restore), null otherwise.
/// </param>
/// <param name="CapturedTargetSpeedKmS">
/// Target scalar speed captured at cycle start (km/s). Filled only for
/// engine.speedSynchronization. Cycle completion applies only this captured value —
/// later target changes or the target disappearing do not affect the result.
/// Persisted in save and restored on load.
/// </param>
/// <param name="CapturedTargetCourseDegrees">
/// Target course captured at cycle start (degrees). Filled only for
/// engine.directionSynchronization. Cycle completion applies only this captured value —
/// later target changes or the target disappearing do not affect the result.
/// Persisted in save and restored on load.
/// </param>
/// <param name="CommandId">
/// PlayerCommand.CommandId that started this cycle. Stored so completion, cancellation,
/// or interruption can publish a final <see cref="DeepSpaceSaga.Contracts.CommandResult"/>
/// (§56.5: complete → roll → apply → write CommandResult → write ShipEvent). Null for
/// cycles loaded from a save written before this field existed, and for auto-repeat
/// renewal cycles (which inherit the original CommandId via CreateEngineCycle).
/// </param>
/// <param name="ObjectId">
/// ObjectId of the module that owns this cycle. Stored together with CommandId so
/// completion can reconstruct a full CommandResult without threading extra state.
/// </param>
/// <param name="ModuleId">
/// ModuleId of the module that owns this cycle. Same rationale as ObjectId.
/// </param>
public sealed record ActiveCycleData(
    [property: JsonPropertyName("cycleId")] string CycleId,
    [property: JsonPropertyName("startedGameTimeMs")] long StartedGameTimeMs,
    [property: JsonPropertyName("durationMs")] long DurationMs,
    [property: JsonPropertyName("commandType")] string CommandType,
    [property: JsonPropertyName("isAutoRepeat")] bool IsAutoRepeat,
    [property: JsonPropertyName("targetObjectId")] string? TargetObjectId = null,
    [property: JsonPropertyName("capturedTargetSpeedKmS")] double? CapturedTargetSpeedKmS = null,
    [property: JsonPropertyName("capturedTargetCourseDegrees")] double? CapturedTargetCourseDegrees = null,
    [property: JsonPropertyName("commandId")] string? CommandId = null,
    [property: JsonPropertyName("objectId")] string? ObjectId = null,
    [property: JsonPropertyName("moduleId")] string? ModuleId = null,
    /// <summary>
    /// World-coordinate target of a navigation cycle, world units. Always set for
    /// navigation cycles; null for every other command type. Persisted in save and
    /// restored on load. Dual meaning depending on <see cref="CommandType"/>:
    /// for <see cref="DeepSpaceSaga.Contracts.ShipEngineCommandTypes.Orbit"/> this is a
    /// fixed locked point set once and never overwritten while the maneuver runs (AC9 —
    /// a loaded navigation cycle keeps heading for the same point); for
    /// <see cref="DeepSpaceSaga.Contracts.NavigationComputerCommandTypes.Approach"/> this
    /// holds the *last recomputed* aim point instead, overwritten every completed cycle
    /// as the target moves. Null targets are tolerated as a no-op (legacy-save guard).
    /// </summary>
    [property: JsonPropertyName("targetWorldX")] double? TargetWorldX = null,
    /// <summary>World-coordinate target of a navigation cycle; see <see cref="TargetWorldX"/>.</summary>
    [property: JsonPropertyName("targetWorldY")] double? TargetWorldY = null,
    /// <summary>
    /// Locked course for navigation (pure-pursuit avoidance), degrees. Null when not yet
    /// locked. Dual meaning depending on <see cref="CommandType"/>, same convention as
    /// <see cref="TargetWorldX"/>: for <see cref="DeepSpaceSaga.Contracts.ShipEngineCommandTypes.Orbit"/>
    /// this is a permanent lock, set once and held until arrival. For
    /// <see cref="DeepSpaceSaga.Contracts.NavigationComputerCommandTypes.Approach"/>
    /// (story-20260827-083137.md, Post-implementation bug fix #2) this is instead
    /// cycle-scoped and NOT permanent — <see cref="DeepSpaceSaga.Motion.ApproachPursuitMath.Step"/>
    /// itself drops and re-derives it whenever the live aim point drifts meaningfully,
    /// since (unlike Orbit's fixed point) the aim point genuinely keeps moving as the
    /// target moves. Threaded from cycle to auto-repeat cycle every completed cycle.
    /// </summary>
    [property: JsonPropertyName("navLockedCourseDegrees")] double? NavigationLockedCourseDegrees = null,
    /// <summary>Current phase of a staged navigation maneuver.</summary>
    [property: JsonPropertyName("navPhase")] string? NavigationPhase = null,
    /// <summary>Escape course for the EscapeTurn phase: bearing from target to ship.</summary>
    [property: JsonPropertyName("navEscapeCourseDegrees")] double? NavigationEscapeCourseDegrees = null,
    /// <summary>Required departure distance before turning back (world units).</summary>
    [property: JsonPropertyName("navRequiredDepartureDistance")] double? NavigationRequiredDepartureDistance = null,
    /// <summary>
    /// Target's live speed (km/s), baked in on the most recently completed cycle of a
    /// <see cref="DeepSpaceSaga.Contracts.NavigationComputerCommandTypes.Approach"/> cycle
    /// only. Unlike <see cref="TargetWorldX"/>/<see cref="TargetWorldY"/> (dual meaning,
    /// see above), this field is Approach-specific and always null for every other
    /// command type. Overwritten every completed cycle with the target's freshly re-read
    /// value. Persisted in save and restored on load.
    /// </summary>
    [property: JsonPropertyName("navTargetSpeedKmS")] double? NavigationTargetSpeedKmS = null,
    /// <summary>
    /// Target's live heading (degrees), baked in on the most recently completed cycle of a
    /// <see cref="DeepSpaceSaga.Contracts.NavigationComputerCommandTypes.Approach"/> cycle
    /// only; see <see cref="NavigationTargetSpeedKmS"/>.
    /// </summary>
    [property: JsonPropertyName("navTargetDirectionDegrees")] double? NavigationTargetDirectionDegrees = null,
    /// <summary>Effective behind-target staging distance for Approach, in world units.</summary>
    [property: JsonPropertyName("navApproachTrailDistanceWorldUnits")] double? NavigationApproachTrailDistanceWorldUnits = null);

/// <summary>A stack of cargo stored inside a ship module.</summary>
public sealed record CargoStackData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("quantity")] long Quantity);

/// <summary>One tradeable item's stock on a station (see StationInventoryItemRuntime).</summary>
public sealed record StationInventoryItemData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("quantity")] long Quantity);
