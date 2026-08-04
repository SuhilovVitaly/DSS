using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Authoritative game simulation.
/// Produces immutable snapshots on a fixed interval (1 Hz by default).
/// Uses DeepSpaceSaga.Motion for deterministic position calculation —
/// the same library the client uses for prediction.
/// </summary>
public sealed class SimulationEngine : IDisposable
{
    public const int SnapshotIntervalMs = 1000;

    private readonly SimulationClock _clock;
    private readonly GameDataRegistry _registry;
    private readonly LinearMotionPredictor _motion = new();
    private readonly List<SpaceObjectRuntime> _objects = new();
    private readonly List<PlayerCommand> _pendingCommands = new();
    private ulong _nextSequence;
    private bool _disposed;

    /// <summary>Number of commands received (test seam).</summary>
    internal int ReceivedCommandCount => _pendingCommands.Count;

    /// <summary>Current authoritative simulation speed.</summary>
    public SimulationSpeed CurrentSpeed => _clock.Speed;

    /// <summary>ObjectId of the player ship (set by scenario).</summary>
    public string? PlayerShipObjectId { get; private set; }

    public SimulationEngine()
        : this(GameDataRegistry.Empty)
    {
    }

    public static SimulationEngine CreateFromSettingsFile(string settingsPath)
    {
        return EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);
    }

    internal SimulationEngine(GameDataRegistry registry)
    {
        _registry = registry;
        _clock = new SimulationClock(SimulationSpeed.Speed1);
    }

    public void ReceiveCommand(PlayerCommand command)
    {
        _pendingCommands.Add(command);
    }

    /// <summary>Set the authoritative simulation speed (e.g. Speed0 for pause).</summary>
    public void SetSpeed(SimulationSpeed speed)
    {
        _clock.SetSpeed(speed);
    }

    /// <summary>
    /// Load initial state from a scenario file. Replaces any previously added objects.
    /// Sets the clock speed and game time from scenario data.
    /// </summary>
    public void LoadScenario(ScenarioFile scenario)
    {
        var gs = scenario.GameState;
        var speed = ScenarioLoader.ParseSpeed(gs.CurrentSpeed);
        var runtimeObjects = new List<SpaceObjectRuntime>(gs.SpaceObjects.Count);

        // Build the full runtime world before mutating engine state. A scenario with
        // invalid type references or placement must not destroy the currently loaded world.
        foreach (var obj in gs.SpaceObjects)
        {
            // Convert m/s to km/s for the existing motion system
            double speedKmS = (double)obj.SpeedMps / 1000.0;

            var modules = BuildRuntimeModules(obj);

            runtimeObjects.Add(new SpaceObjectRuntime(
                new ObjectMotionSnapshot(
                ObjectId: obj.ObjectId,
                X: obj.PositionX,
                Y: obj.PositionY,
                SpeedKmS: speedKmS,
                Direction: obj.DirectionDegrees),
                StartGameTimeMs: 0,
                Modules: modules)); // startGameTime stamped at RunAsync
        }

        PlayerShipObjectId = gs.PlayerShipObjectId;
        _clock.Reset(gs.GameTimeMs, speed);

        _objects.Clear();
        _objects.AddRange(runtimeObjects);
    }

    /// <summary>Add a test object (legacy — prefer LoadScenario for production).</summary>
    public void AddTestObject(ObjectMotionSnapshot initial)
    {
        _objects.Add(new SpaceObjectRuntime(initial, 0, ImmutableArray<InstalledModuleRuntime>.Empty));
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stamp objects with the current game time at engine start.
        _clock.ResetRealBaseline();
        long engineStartGameTime = _clock.GameTimeMs;

        for (int i = 0; i < _objects.Count; i++)
        {
            _objects[i] = _objects[i] with { StartGameTimeMs = engineStartGameTime };
        }

        // Yield the initial snapshot immediately (before any delay).
        // Capture atomically — no time has passed, so we read without advancing.
        yield return BuildSnapshot(_clock.Capture());

        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            await Task.Delay(SnapshotIntervalMs, cancellationToken);

            yield return BuildSnapshot(_clock.UpdateAndCapture());
        }
    }

    private AuthoritativeSnapshot BuildSnapshot(SimulationClockState clockState)
    {
        long gameTimeMs = clockState.GameTimeMs;

        var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>(_objects.Count);
        foreach (var obj in _objects)
        {
            long elapsed = gameTimeMs - obj.StartGameTimeMs;
            objects.Add(_motion.Predict(obj.InitialMotion, elapsed));
        }

        return new AuthoritativeSnapshot(
            SnapshotSequence: _nextSequence++,
            GameTimeMs: gameTimeMs,
            CurrentSpeed: clockState.Speed,
            Objects: objects.MoveToImmutable(),
            PlayerShipObjectId: PlayerShipObjectId);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private ImmutableArray<InstalledModuleRuntime> BuildRuntimeModules(SpaceObjectData obj)
    {
        if (obj.Modules is not { Count: > 0 })
            return ImmutableArray<InstalledModuleRuntime>.Empty;
        if (_registry.ModuleTypes.Count == 0)
        {
            throw new ScenarioException(
                "Scenario contains module instances, but this SimulationEngine has no type registry. " +
                "Create production engines with SimulationEngine.CreateFromSettingsFile(...).");
        }

        var modules = ImmutableArray.CreateBuilder<InstalledModuleRuntime>(obj.Modules.Count);
        var moduleIds = new HashSet<string>(StringComparer.Ordinal);
        var occupiedCellsByPlatform = new Dictionary<int, HashSet<int>>();

        foreach (var module in obj.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.ModuleId))
                throw new ScenarioException($"Module on '{obj.ObjectId}' has empty moduleId.");
            if (!moduleIds.Add(module.ModuleId))
                throw new ScenarioException($"Duplicate moduleId '{module.ModuleId}' on '{obj.ObjectId}'.");

            int moduleTypeIndex = _registry.ModuleTypes.GetIndex(module.ModuleTypeId);
            var moduleType = _registry.ModuleTypes.GetDefinition(moduleTypeIndex);
            ValidateModulePlacement(obj.ObjectId, module, moduleType, occupiedCellsByPlatform);
            if (module.StructurePoints < 0 || module.StructurePoints > moduleType.StructurePointsMax)
            {
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' structurePoints {module.StructurePoints} is outside 0..{moduleType.StructurePointsMax}.");
            }

            var cargo = BuildRuntimeCargo(obj, module);
            modules.Add(new InstalledModuleRuntime(
                module.ModuleId,
                moduleTypeIndex,
                module.PlatformIndex,
                module.OccupiedCells.ToImmutableArray(),
                module.PowerState,
                module.OperationalState,
                module.StructurePoints,
                module.ActiveCycle,
                cargo));
        }

        return modules.ToImmutable();
    }

    private static void ValidateModulePlacement(
        string objectId,
        ShipModuleData module,
        ModuleTypeDefinition moduleType,
        Dictionary<int, HashSet<int>> occupiedCellsByPlatform)
    {
        if (module.OccupiedCells.Count != moduleType.SlotSize)
        {
            throw new ScenarioException(
                $"Module '{module.ModuleId}' on '{objectId}' occupies {module.OccupiedCells.Count} cells, " +
                $"but module type '{moduleType.TypeId}' requires {moduleType.SlotSize}.");
        }

        var moduleCells = new HashSet<int>();
        foreach (int cell in module.OccupiedCells)
        {
            if (cell < 0)
                throw new ScenarioException($"Module '{module.ModuleId}' on '{objectId}' has negative occupied cell {cell}.");

            if (!moduleCells.Add(cell))
                throw new ScenarioException($"Module '{module.ModuleId}' on '{objectId}' duplicates occupied cell {cell}.");
        }

        if (!occupiedCellsByPlatform.TryGetValue(module.PlatformIndex, out var platformCells))
        {
            platformCells = new HashSet<int>();
            occupiedCellsByPlatform.Add(module.PlatformIndex, platformCells);
        }

        foreach (int cell in moduleCells)
        {
            if (!platformCells.Add(cell))
            {
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' on '{objectId}' overlaps occupied cell {cell} " +
                    $"on platform {module.PlatformIndex}.");
            }
        }
    }

    private ImmutableArray<CargoStackRuntime> BuildRuntimeCargo(SpaceObjectData obj, ShipModuleData module)
    {
        if (module.Cargo is not { Count: > 0 })
            return ImmutableArray<CargoStackRuntime>.Empty;

        var cargo = ImmutableArray.CreateBuilder<CargoStackRuntime>(module.Cargo.Count);
        foreach (var stack in module.Cargo)
        {
            int itemTypeIndex = _registry.ItemTypes.GetIndex(stack.ItemTypeId);
            if (stack.Quantity < 0)
            {
                throw new ScenarioException(
                    $"Cargo stack '{stack.ItemTypeId}' in module '{module.ModuleId}' on '{obj.ObjectId}' has negative quantity.");
            }

            cargo.Add(new CargoStackRuntime(itemTypeIndex, stack.Quantity));
        }

        return cargo.ToImmutable();
    }

    internal ImmutableArray<SpaceObjectRuntime> RuntimeObjects => _objects.ToImmutableArray();
}

internal sealed record SpaceObjectRuntime(
    ObjectMotionSnapshot InitialMotion,
    long StartGameTimeMs,
    ImmutableArray<InstalledModuleRuntime> Modules);

internal sealed record InstalledModuleRuntime(
    string ModuleId,
    int ModuleTypeIndex,
    int PlatformIndex,
    ImmutableArray<int> OccupiedCells,
    string PowerState,
    string OperationalState,
    int StructurePoints,
    ActiveCycleData? ActiveCycle,
    ImmutableArray<CargoStackRuntime> Cargo);

internal sealed record CargoStackRuntime(
    int ItemTypeIndex,
    long Quantity);
