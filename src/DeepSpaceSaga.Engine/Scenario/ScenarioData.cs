using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Save file format version constants.</summary>
public static class SaveFormat
{
    /// <summary>
    /// Current save format version written by <see cref="SimulationEngine.CaptureSaveState"/>.
    /// Bump when the save schema changes incompatibly (see requirements §3891 migration policy —
    /// not implemented yet, only the version field is laid down in this iteration).
    /// </summary>
    public const int CurrentSaveFormatVersion = 1;
}

/// <summary>Root of the scenario JSON file. Also used as the save-file format.</summary>
public sealed record ScenarioFile(
    [property: JsonPropertyName("scenarioMetadata")] ScenarioMetadata Metadata,
    [property: JsonPropertyName("gameState")] GameStateData GameState,
    [property: JsonPropertyName("saveFormatVersion")] int SaveFormatVersion = 0);

/// <summary>Scenario identification.</summary>
public sealed record ScenarioMetadata(
    [property: JsonPropertyName("scenarioId")] string ScenarioId,
    [property: JsonPropertyName("name")] string Name);

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
    [property: JsonPropertyName("masterSeed")] ulong? MasterSeed = null);

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
    [property: JsonPropertyName("isKnown")] bool IsKnown = false);

/// <summary>A ship module declared in a scenario.</summary>
public sealed record ShipModuleData(
    [property: JsonPropertyName("moduleId")] string ModuleId,
    [property: JsonPropertyName("moduleTypeId")] string ModuleTypeId,
    [property: JsonPropertyName("platformIndex")] int PlatformIndex,
    [property: JsonPropertyName("occupiedCells")] IReadOnlyList<int> OccupiedCells,
    [property: JsonPropertyName("structurePoints")] int StructurePoints,
    [property: JsonPropertyName("powerState")] string PowerState,
    [property: JsonPropertyName("operationalState")] string OperationalState,
    [property: JsonPropertyName("activeCycle")] ActiveCycleData? ActiveCycle,
    [property: JsonPropertyName("cargo")] IReadOnlyList<CargoStackData>? Cargo);

/// <summary>
/// Runtime progress for an active module cycle.
/// </summary>
/// <param name="TargetObjectId">
/// ObjectId of the target for match commands (engine.match-target-speed /
/// engine.match-target-course, requirements §56.9). Filled when a match cycle starts;
/// always set for match cycles (diagnostics + §1253-1298 restore), null otherwise.
/// </param>
/// <param name="CapturedTargetSpeedKmS">
/// Target scalar speed captured at cycle start (km/s). Filled only for
/// engine.match-target-speed. Cycle completion applies only this captured value —
/// later target changes or the target disappearing do not affect the result.
/// Persisted in save and restored on load.
/// </param>
/// <param name="CapturedTargetCourseDegrees">
/// Target course captured at cycle start (degrees). Filled only for
/// engine.match-target-course. Cycle completion applies only this captured value —
/// later target changes or the target disappearing do not affect the result.
/// Persisted in save and restored on load.
/// </param>
public sealed record ActiveCycleData(
    [property: JsonPropertyName("cycleId")] string CycleId,
    [property: JsonPropertyName("startedGameTimeMs")] long StartedGameTimeMs,
    [property: JsonPropertyName("durationMs")] long DurationMs,
    [property: JsonPropertyName("commandType")] string CommandType,
    [property: JsonPropertyName("isAutoRepeat")] bool IsAutoRepeat,
    [property: JsonPropertyName("targetObjectId")] string? TargetObjectId = null,
    [property: JsonPropertyName("capturedTargetSpeedKmS")] double? CapturedTargetSpeedKmS = null,
    [property: JsonPropertyName("capturedTargetCourseDegrees")] double? CapturedTargetCourseDegrees = null);

/// <summary>A stack of cargo stored inside a ship module.</summary>
public sealed record CargoStackData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("quantity")] long Quantity);
