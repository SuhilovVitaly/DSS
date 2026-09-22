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

        return ValidateAndNormalize(scenario, allowNonZeroGameTime);
    }

    internal static ScenarioFile ValidateAndNormalize(ScenarioFile scenario, bool allowNonZeroGameTime)
    {
        Validate(scenario, allowNonZeroGameTime);
        var gs = scenario.GameState;
        var ids = gs.SpaceObjects.ToDictionary(o => o.ObjectId, o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        if ((gs.CommandReceipts?.Any(r => r is null || string.IsNullOrWhiteSpace(r.CommandId)) ?? false) ||
            (gs.PendingCommands?.Any(c => c is null || string.IsNullOrWhiteSpace(c.CommandId)) ?? false))
            throw new ScenarioException("Invalid saved command journal.");
        if (gs.CommandReceipts?.Any(r => r.TradeReceipt is not null && !IsValidTradeReceipt(r)) ?? false)
            throw new ScenarioException("Invalid saved trade receipt.");
        string? Resolve(string? id) => id is not null && ids.TryGetValue(id, out var canonical) ? canonical : id;
        var objects = gs.SpaceObjects.Select(o => o with
        {
            ObjectType = KnownObjectTypes.Single(t => t.Equals(o.ObjectType, StringComparison.OrdinalIgnoreCase)),
            PersistenceType = KnownPersistenceTypes.Single(t => t.Equals(o.PersistenceType, StringComparison.OrdinalIgnoreCase)),
            DockedStationObjectId = Resolve(o.DockedStationObjectId),
            Modules = o.Modules?.Select(m => m with
            {
                PowerState = KnownPowerStates.Single(t => t.Equals(m.PowerState, StringComparison.OrdinalIgnoreCase)),
                OperationalState = KnownOperationalStates.Single(t => t.Equals(m.OperationalState, StringComparison.OrdinalIgnoreCase)),
                ActiveCycle = m.ActiveCycle is not { } c ? null : c with
                { ObjectId = Resolve(c.ObjectId), TargetObjectId = Resolve(c.TargetObjectId) }
            }).ToArray()
        }).ToArray();
        return scenario with
        {
            GameState = gs with
            {
                PlayerShipObjectId = ids[gs.PlayerShipObjectId],
                CurrentSpeed = KnownSpeeds.Single(s => s.Equals(gs.CurrentSpeed, StringComparison.OrdinalIgnoreCase)),
                SpaceObjects = objects
            }
        };
    }

    /// <summary>
    /// Structural shape of a saved trade receipt (EP-0001-US-0003-TK-0002). Deliberately not checked
    /// against the current world or catalog: the historical station may be gone and prices may have
    /// changed. A rejection echoes the raw request, so its item/quote/quantity fields are not checked.
    /// </summary>
    private static bool IsValidTradeReceipt(CommandResult result)
    {
        var receipt = result.TradeReceipt!;
        if (result.CommandType is not (TradeCommandTypes.Buy or TradeCommandTypes.Sell or TradeCommandTypes.Refuel) ||
            receipt.ExecutedQuantity < 0 || receipt.TotalCredits < 0 ||
            (!receipt.LimitReasons.IsDefault && receipt.LimitReasons.Any(string.IsNullOrWhiteSpace)))
            return false;

        if (result.Status != CommandResultStatus.Executed)
        {
            return receipt.ExecutedQuantity == 0 && receipt.TotalCredits == 0 &&
                (receipt.StationObjectId is null
                    ? receipt.ResultMarketRevision is null
                    : receipt.ResultMarketRevision is >= 1);
        }

        return result.ReasonCode is null &&
            !string.IsNullOrWhiteSpace(receipt.StationObjectId) &&
            !string.IsNullOrWhiteSpace(receipt.ItemTypeId) &&
            !string.IsNullOrWhiteSpace(receipt.QuoteId) &&
            receipt.QuotedMarketRevision is >= 1 and < long.MaxValue &&
            receipt.ResultMarketRevision == receipt.QuotedMarketRevision + 1 &&
            receipt.RequestedQuantity is > 0 and var requested &&
            receipt.ExecutedQuantity > 0 && receipt.ExecutedQuantity <= requested &&
            (result.CommandType == TradeCommandTypes.Sell || receipt.ExecutedQuantity == requested);
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
        if (scenario.SaveFormatVersion < 0 || scenario.SaveFormatVersion > SaveFormat.CurrentSaveFormatVersion)
            throw new ScenarioException($"Unsupported saveFormatVersion: {scenario.SaveFormatVersion}.");
        var gs = scenario.GameState;
        if (gs is null)
            throw new ScenarioException("Missing gameState.");
        if (scenario.SaveFormatVersion >= 7 && gs.CatalogCompatibility is null)
            throw new ScenarioException("Missing catalogCompatibility in save format 7 or later.");
        if (scenario.SaveFormatVersion >= 6 && gs.SimulationTimeMs is null)
            throw new ScenarioException("Missing simulationTimeMs in save format 6.");
        if (gs.GameTimeMs < 0 || gs.SimulationTimeMs < 0 || gs.PlayerTokens < 0)
            throw new ScenarioException("Game time and player balance must be nonnegative.");

        if (string.IsNullOrWhiteSpace(gs.PlayerShipObjectId))
            throw new ScenarioException("Missing playerShipObjectId.");

        if (string.IsNullOrWhiteSpace(gs.CurrentSpeed))
            throw new ScenarioException("Missing currentSpeed.");

        // Validate speed string
        ParseSpeed(gs.CurrentSpeed);

        // New Game scenarios must start at gameTimeMs = 0. Save files (allowNonZeroGameTime: true)
        // are the sole exception — they represent a paused, already-in-progress game.
        if (!allowNonZeroGameTime && (gs.GameTimeMs != 0 || gs.MotionTimeMs != 0))
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

        ValidateEconomyTime(scenario);

        // Validate each object (nulls already caught in the duplicate-check loop)
        foreach (var obj in objects)
        {
            ValidateObject(obj);
            ValidateMarketProfileMetadata(scenario.SaveFormatVersion, obj);
        }
    }

    private static void ValidateMarketProfileMetadata(int saveFormatVersion, SpaceObjectData obj)
    {
        if (obj.MarketProfileId is not null && string.IsNullOrWhiteSpace(obj.MarketProfileId))
            throw new ScenarioException($"Object '{obj.ObjectId}', marketProfileId must not be blank.");
        if (obj.MarketProfileFingerprint is not null && string.IsNullOrWhiteSpace(obj.MarketProfileFingerprint))
            throw new ScenarioException($"Object '{obj.ObjectId}', marketProfileFingerprint must not be blank.");
        if (obj.MarketProfileFingerprint is not null && obj.MarketProfileId is null)
            throw new ScenarioException($"Object '{obj.ObjectId}', marketProfileFingerprint requires marketProfileId.");

        // Market revision (EP-0001-US-0015-TK-0003): saved for profile markets only, and never below 1.
        if (obj.MarketProfileId is null && obj.MarketRevision is not null)
            throw new ScenarioException($"Object '{obj.ObjectId}', marketRevision requires a market profile.");
        if (obj.MarketProfileId is not null && obj.MarketRevision is < 1)
            throw new ScenarioException($"Station '{obj.ObjectId}', marketRevision must be at least 1.");

        bool hasProfileMetadata = obj.MarketProfileId is not null || obj.MarketProfileFingerprint is not null;
        if (hasProfileMetadata && !obj.ObjectType.Equals("Station", StringComparison.OrdinalIgnoreCase))
            throw new ScenarioException($"Object '{obj.ObjectId}', market profile metadata is allowed only on Station objects.");
        if (hasProfileMetadata && saveFormatVersion is > 0 and < 8)
            throw new ScenarioException($"Station '{obj.ObjectId}', marketProfileId is not supported before save format 8.");
        if (saveFormatVersion >= 8 && obj.MarketProfileId is not null)
        {
            if (obj.MarketProfileFingerprint is null)
                throw new ScenarioException($"Station '{obj.ObjectId}', marketProfileFingerprint is required in save format 8.");
            if (obj.Credits is null)
                throw new ScenarioException($"Station '{obj.ObjectId}', credits are required for a profiled save.");
            if (obj.StationSize is null)
                throw new ScenarioException($"Station '{obj.ObjectId}', stationSize is required for a profiled save.");
            if (obj.Inventory is null)
                throw new ScenarioException($"Station '{obj.ObjectId}', inventory is required for a profiled save.");
        }
    }

    private static void ValidateEconomyTime(ScenarioFile scenario)
    {
        var state = scenario.GameState;
        if (scenario.SaveFormatVersion >= 5 && state.EconomyTime is null)
            throw new ScenarioException("Missing versioned economy time state.");
        if (state.EconomyTime is { } economy)
        {
            if (economy.RulesVersion != EconomyTimeData.CurrentRulesVersion)
                throw new ScenarioException($"Unsupported economic rules version: {economy.RulesVersion}. Save was not modified.");
            if (!Enum.IsDefined(economy.StationDistrict) || economy.RouteArrivalGameTimeMs < 0 || economy.MissingRations < 0)
                throw new ScenarioException("Invalid saved station district or route arrival time.");
            if (economy.TravelReceipts is { } receipts &&
                (receipts.Any(string.IsNullOrWhiteSpace) || receipts.Distinct(StringComparer.Ordinal).Count() != receipts.Count))
                throw new ScenarioException("Invalid station travel receipts.");
            var contractIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var contract in economy.ActiveContracts ?? [])
                if (contract is null || string.IsNullOrWhiteSpace(contract.ContractId) || !contractIds.Add(contract.ContractId) ||
                    contract.DeadlineGameTimeMs < 0 || contract.ExpectedPayout < 0 || contract.PassengerIds.IsDefault ||
                    contract.PassengerIds.Any(string.IsNullOrWhiteSpace) ||
                    !state.SpaceObjects.Any(o => o.ObjectId == contract.DestinationStationObjectId && o.ObjectType.Equals("Station", StringComparison.OrdinalIgnoreCase)))
                    throw new ScenarioException("Invalid saved contract timing or payout.");
        }
        foreach (var obj in state.SpaceObjects)
        {
            if (scenario.SaveFormatVersion > 0 && obj.IsDocked && obj.FirstPortFeeGameTimeMs is null)
                throw new ScenarioException("Docked save has incompatible economic rules: first port payment time is missing. Save was not modified.");
            if (obj.PortFeeDebt < 0 || obj.FirstPortFeeGameTimeMs < 0 || obj.FirstPortFeeGameTimeMs > state.GameTimeMs)
                throw new ScenarioException("Invalid first port payment or debt.");
            if (obj.IsDocked && !state.SpaceObjects.Any(o => string.Equals(o.ObjectId, obj.DockedStationObjectId, StringComparison.OrdinalIgnoreCase)
                && o.ObjectType.Equals("Station", StringComparison.OrdinalIgnoreCase)))
                throw new ScenarioException("Docked ship references an unknown station.");
            if (obj.NextPortFeeDueGameTimeMs is { } next &&
                (obj.FirstPortFeeGameTimeMs is not { } first || next <= first ||
                 (next - first) % GameCalendar.DayMs != 0))
                throw new ScenarioException("Invalid next port payment time.");
            // Only the controlled ship's billing schedule advances in this session.
            // Other ships retain their own payment metadata without charging PlayerCredits.
            if (string.Equals(obj.ObjectId, state.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase) &&
                obj.NextPortFeeDueGameTimeMs is { } playerNext &&
                (playerNext <= state.GameTimeMs || playerNext - state.GameTimeMs > GameCalendar.DayMs))
                throw new ScenarioException("Next port payment must be the first scheduled payment after saved game time.");
            if (obj.ProducingModules?.Any(m => m.NextProductionDueGameTimeMs is { } due && due <= state.GameTimeMs) == true)
                throw new ScenarioException("Invalid saved production deadline.");
            if (obj.Passengers is { } passengers &&
                (passengers.Any(p => p is null || string.IsNullOrWhiteSpace(p.PassengerId)) ||
                 passengers.Select(p => p.PassengerId).Distinct(StringComparer.Ordinal).Count() != passengers.Count))
                throw new ScenarioException("Invalid onboard passenger manifest.");
        }
    }

    private static void ValidateObject(SpaceObjectData obj)
    {
        if (!double.IsFinite(obj.SpeedMps) || obj.SpeedMps < 0 || !double.IsFinite(obj.DirectionDegrees))
            throw new ScenarioException($"Invalid precise motion for '{obj.ObjectId}'.");
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

        if (obj.DirectionDegrees < 0 || obj.DirectionDegrees >= 360)
            throw new ScenarioException(
                $"directionDegrees {obj.DirectionDegrees} for '{obj.ObjectId}' is not in 0..359.");

        if (!double.IsFinite(obj.PositionX) || !double.IsFinite(obj.PositionY))
            throw new ScenarioException($"Non-finite coordinates for '{obj.ObjectId}'.");

        if (obj.Credits < 0)
            throw new ScenarioException($"Object '{obj.ObjectId}' has negative credits.");
        if (obj.Inventory is { } inventory)
        {
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in inventory)
            {
                if (entry is null)
                    throw new ScenarioException($"Object '{obj.ObjectId}' inventory contains a null element.");
                if (string.IsNullOrWhiteSpace(entry.ItemTypeId))
                    throw new ScenarioException($"Object '{obj.ObjectId}' inventory contains a blank itemTypeId.");
                if (!itemIds.Add(entry.ItemTypeId))
                    throw new ScenarioException($"Object '{obj.ObjectId}' inventory contains duplicate item '{entry.ItemTypeId}'.");
                if (entry.Quantity < 0)
                    throw new ScenarioException($"Object '{obj.ObjectId}' inventory item '{entry.ItemTypeId}' has negative quantity.");
            }
        }

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
            if (activeCycle.ApproachRoute is { } route &&
                (activeCycle.CommandType != NavigationComputerCommandTypes.Approach ||
                 route.Type is null || route.Type.Length != 3 || route.Type.Any(c => c is not ('L' or 'R' or 'S')) ||
                 route.SpeedKmS <= 0 || route.TurnRate <= 0 || route.TargetSpeedKmS < 0 ||
                 route.First < 0 || route.Second < 0 || route.Third < 0 || route.TrailDistance < 0 ||
                 route.ElapsedMs < 0 || route.ElapsedMs > route.DurationMs ||
                 new[] { route.X, route.Y, route.Direction, route.SpeedKmS, route.First, route.Second,
                     route.Third, route.TargetX, route.TargetY, route.TargetDirection, route.TargetSpeedKmS,
                     route.TrailDistance, route.ElapsedMs, route.DurationMs }.Any(v => !double.IsFinite(v))))
                throw new ScenarioException($"Module '{module.ModuleId}' has an invalid Approach route.");
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
