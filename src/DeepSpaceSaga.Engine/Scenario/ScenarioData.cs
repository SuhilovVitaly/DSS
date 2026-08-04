using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Root of the scenario JSON file.</summary>
public sealed record ScenarioFile(
    [property: JsonPropertyName("scenarioMetadata")] ScenarioMetadata Metadata,
    [property: JsonPropertyName("gameState")] GameStateData GameState);

/// <summary>Scenario identification.</summary>
public sealed record ScenarioMetadata(
    [property: JsonPropertyName("scenarioId")] string ScenarioId,
    [property: JsonPropertyName("name")] string Name);

/// <summary>Initial game state.</summary>
public sealed record GameStateData(
    [property: JsonPropertyName("gameTimeMs")] long GameTimeMs,
    [property: JsonPropertyName("currentSpeed")] string CurrentSpeed,
    [property: JsonPropertyName("playerShipObjectId")] string PlayerShipObjectId,
    [property: JsonPropertyName("focus")] FocusData? Focus,
    [property: JsonPropertyName("spaceObjects")] IReadOnlyList<SpaceObjectData> SpaceObjects);

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
    [property: JsonPropertyName("modules")] IReadOnlyList<ShipModuleData>? Modules);

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

/// <summary>Runtime progress for an active module cycle.</summary>
public sealed record ActiveCycleData(
    [property: JsonPropertyName("cycleId")] string CycleId,
    [property: JsonPropertyName("startedGameTimeMs")] long StartedGameTimeMs,
    [property: JsonPropertyName("durationMs")] long DurationMs);

/// <summary>A stack of cargo stored inside a ship module.</summary>
public sealed record CargoStackData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("quantity")] long Quantity);
