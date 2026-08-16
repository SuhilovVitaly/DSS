using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>
/// Loads and validates a scenario JSON file.
/// </summary>
public static class ScenarioLoader
{
    private static readonly HashSet<string> KnownObjectTypes = new(StringComparer.OrdinalIgnoreCase)
        { "PlayerShip", "Station", "Asteroid" };

    private static readonly HashSet<string> KnownPersistenceTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Permanent", "Temporary" };

    private static readonly HashSet<string> KnownSpeeds = new(StringComparer.OrdinalIgnoreCase)
        { "Speed0", "Speed1", "Speed2", "Speed3", "Speed4" };

    private static readonly HashSet<string> KnownPowerStates = new(StringComparer.OrdinalIgnoreCase)
        { "On", "Off" };

    private static readonly HashSet<string> KnownOperationalStates = new(StringComparer.OrdinalIgnoreCase)
        { "Ready", "Disabled", "Damaged" };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly JsonSerializerOptions WriteJsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <param name="allowNonZeroGameTime">
    /// New Game scenarios must start at gameTimeMs = 0. Save files legitimately carry
    /// gameTimeMs &gt; 0 — pass true only for the explicit save/load bootstrap path
    /// (see SimulationEngine.CreateFromSaveFile / EngineContentLoader.CreateEngineFromSaveFile).
    /// </param>
    public static ScenarioFile LoadFromFile(string path, bool allowNonZeroGameTime = false)
    {
        if (!File.Exists(path))
            throw new ScenarioException($"Scenario file not found: {path}");

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new ScenarioException($"Failed to read scenario file: {path}", ex);
        }

        return LoadFromJson(json, allowNonZeroGameTime);
    }

    public static ScenarioFile LoadFromJson(string json, bool allowNonZeroGameTime = false)
    {
        ScenarioFile scenario;
        try
        {
            scenario = JsonSerializer.Deserialize<ScenarioFile>(json, JsonOptions)
                       ?? throw new ScenarioException("Scenario JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new ScenarioException($"Invalid scenario JSON: {ex.Message}", ex);
        }

        Validate(scenario, allowNonZeroGameTime);
        return scenario;
    }

    /// <summary>
    /// Serialize a scenario/save-state to JSON using the same schema LoadFromJson reads.
    /// Centralized here (Engine) so save-file writers never duplicate JSON format decisions.
    /// </summary>
    public static string Serialize(ScenarioFile scenario)
    {
        return JsonSerializer.Serialize(scenario, WriteJsonOptions);
    }

    /// <summary>Parse a speed string from the scenario. Throws on unknown values.</summary>
    public static SimulationSpeed ParseSpeed(string speed)
    {
        if (!KnownSpeeds.Contains(speed))
            throw new ScenarioException(
                $"Unknown currentSpeed '{speed}'. Expected one of: {string.Join(", ", KnownSpeeds)}.");

        return speed switch
        {
            "Speed0" => SimulationSpeed.Speed0,
            "Speed1" => SimulationSpeed.Speed1,
            "Speed2" => SimulationSpeed.Speed2,
            "Speed3" => SimulationSpeed.Speed3,
            "Speed4" => SimulationSpeed.Speed4,
            _ => throw new ScenarioException($"Unknown currentSpeed: {speed}")
        };
    }

    private static void Validate(ScenarioFile scenario, bool allowNonZeroGameTime = false)
    {
        var gs = scenario.GameState;
        if (gs is null)
            throw new ScenarioException("Missing gameState.");

        if (string.IsNullOrWhiteSpace(gs.PlayerShipObjectId))
            throw new ScenarioException("Missing playerShipObjectId.");

        if (string.IsNullOrWhiteSpace(gs.CurrentSpeed))
            throw new ScenarioException("Missing currentSpeed.");

        // Validate speed string
        ParseSpeed(gs.CurrentSpeed);

        // New Game scenarios must start at gameTimeMs = 0. Save files (allowNonZeroGameTime: true)
        // are the sole exception — they represent a paused, already-in-progress game.
        if (!allowNonZeroGameTime && gs.GameTimeMs != 0)
            throw new ScenarioException(
                $"gameTimeMs must be 0 for New Game, got {gs.GameTimeMs}.");

        var objects = gs.SpaceObjects;
        if (objects is null || objects.Count == 0)
            throw new ScenarioException("No spaceObjects in scenario.");

        // Duplicate objectIds (also catches null elements)
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in objects)
        {
            if (obj is null)
                throw new ScenarioException("spaceObjects contains a null element.");
            if (string.IsNullOrWhiteSpace(obj.ObjectId))
                throw new ScenarioException("Space object has empty objectId.");
            if (!ids.Add(obj.ObjectId))
                throw new ScenarioException($"Duplicate objectId: {obj.ObjectId}");
        }

        // Player ship exists and has correct type
        var playerShip = objects.FirstOrDefault(o =>
            string.Equals(o.ObjectId, gs.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase));
        if (playerShip is null)
            throw new ScenarioException($"Player ship '{gs.PlayerShipObjectId}' not found in spaceObjects.");

        if (!string.Equals(playerShip.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase))
            throw new ScenarioException(
                $"Player ship '{playerShip.ObjectId}' has objectType '{playerShip.ObjectType}', expected 'PlayerShip'.");

        // Validate each object (nulls already caught in the duplicate-check loop)
        foreach (var obj in objects)
        {
            ValidateObject(obj);
        }
    }

    private static void ValidateObject(SpaceObjectData obj)
    {
        if (string.IsNullOrWhiteSpace(obj.ObjectId))
            throw new ScenarioException("Space object has empty objectId.");

        if (string.IsNullOrWhiteSpace(obj.ObjectType))
            throw new ScenarioException($"Missing objectType for '{obj.ObjectId}'.");
        if (!KnownObjectTypes.Contains(obj.ObjectType))
            throw new ScenarioException($"Unknown objectType '{obj.ObjectType}' for '{obj.ObjectId}'.");

        if (string.IsNullOrWhiteSpace(obj.PersistenceType))
            throw new ScenarioException($"Missing persistenceType for '{obj.ObjectId}'.");
        if (!KnownPersistenceTypes.Contains(obj.PersistenceType))
            throw new ScenarioException($"Unknown persistenceType '{obj.PersistenceType}' for '{obj.ObjectId}'.");

        if (obj.DirectionDegrees < 0 || obj.DirectionDegrees > 359)
            throw new ScenarioException(
                $"directionDegrees {obj.DirectionDegrees} for '{obj.ObjectId}' is not in 0..359.");

        if (!double.IsFinite(obj.PositionX) || !double.IsFinite(obj.PositionY))
            throw new ScenarioException($"Non-finite coordinates for '{obj.ObjectId}'.");

        // Temporary objects must be asteroids
        if (string.Equals(obj.PersistenceType, "Temporary", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(obj.ObjectType, "Asteroid", StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException(
                    $"Temporary object '{obj.ObjectId}' must be Asteroid, got '{obj.ObjectType}'.");

            // Asteroid speed range: 100..20000 m/s
            if (obj.SpeedMps < 100 || obj.SpeedMps > 20000)
                throw new ScenarioException(
                    $"Asteroid '{obj.ObjectId}' speedMps {obj.SpeedMps} is outside 100..20000.");

            // Asteroid mass range: 1_000_000..1_000_000_000 kg
            if (obj.MassKg is not { } m || m < 1_000_000 || m > 1_000_000_000)
                throw new ScenarioException(
                    $"Asteroid '{obj.ObjectId}' massKg {obj.MassKg} is outside 1,000,000..1,000,000,000.");
        }

        if (obj.Modules is { Count: > 0 })
        {
            foreach (var module in obj.Modules)
            {
                ValidateModule(obj.ObjectId, module);
            }
        }
    }

    private static void ValidateModule(string objectId, ShipModuleData module)
    {
        if (module is null)
            throw new ScenarioException($"Object '{objectId}' modules contains a null element.");

        if (string.IsNullOrWhiteSpace(module.ModuleId))
            throw new ScenarioException($"Object '{objectId}' has a module with empty moduleId.");
        if (string.IsNullOrWhiteSpace(module.ModuleTypeId))
            throw new ScenarioException($"Module '{module.ModuleId}' is missing moduleTypeId.");

        if (module.OccupiedCells is null || module.OccupiedCells.Count == 0)
            throw new ScenarioException($"Module '{module.ModuleId}' must occupy at least one cell.");

        foreach (var cell in module.OccupiedCells)
        {
            if (cell is null)
                throw new ScenarioException($"Module '{module.ModuleId}' occupiedCells contains a null element.");
            if (cell.X < 0 || cell.Y < 0)
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' has occupied cell ({cell.X},{cell.Y}) with a negative coordinate.");
        }

        if (module.StructurePoints < 0)
            throw new ScenarioException($"Module '{module.ModuleId}' has negative structurePoints.");

        if (string.IsNullOrWhiteSpace(module.PowerState) || !KnownPowerStates.Contains(module.PowerState))
            throw new ScenarioException($"Module '{module.ModuleId}' has unknown powerState '{module.PowerState}'.");

        if (string.IsNullOrWhiteSpace(module.OperationalState) ||
            !KnownOperationalStates.Contains(module.OperationalState))
            throw new ScenarioException(
                $"Module '{module.ModuleId}' has unknown operationalState '{module.OperationalState}'.");

        if (module.ActiveCycle is { } activeCycle)
        {
            if (string.IsNullOrWhiteSpace(activeCycle.CycleId))
                throw new ScenarioException($"Module '{module.ModuleId}' has activeCycle with empty cycleId.");
            if (string.IsNullOrWhiteSpace(activeCycle.CommandType))
                throw new ScenarioException($"Module '{module.ModuleId}' has activeCycle with empty commandType.");
            if (activeCycle.StartedGameTimeMs < 0)
                throw new ScenarioException($"Module '{module.ModuleId}' has activeCycle with negative startedGameTimeMs.");
            if (activeCycle.DurationMs < 0)
                throw new ScenarioException($"Module '{module.ModuleId}' has activeCycle with negative durationMs.");
        }

        if (module.Cargo is { Count: > 0 })
        {
            foreach (var stack in module.Cargo)
            {
                if (stack is null)
                    throw new ScenarioException($"Module '{module.ModuleId}' cargo contains a null element.");
                if (string.IsNullOrWhiteSpace(stack.ItemTypeId))
                    throw new ScenarioException($"Module '{module.ModuleId}' cargo stack is missing itemTypeId.");
                if (stack.Quantity < 0)
                    throw new ScenarioException(
                        $"Module '{module.ModuleId}' cargo stack '{stack.ItemTypeId}' has negative quantity.");
            }
        }
    }
}

/// <summary>
/// Thrown when scenario loading or validation fails.
/// </summary>
public sealed class ScenarioException : Exception
{
    public ScenarioException(string message) : base(message) { }
    public ScenarioException(string message, Exception inner) : base(message, inner) { }
}
