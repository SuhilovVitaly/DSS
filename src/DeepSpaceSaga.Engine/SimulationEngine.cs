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
    private readonly object _commandGate = new();
    private readonly object _worldStateLock = new();
    private readonly List<PlayerCommand> _pendingCommands = new();
    private int _receivedCommandCount;
    private ulong _nextSequence;
    private ulong _nextEngineCycleId;
    private bool _disposed;

    /// <summary>Number of commands received (test seam).</summary>
    internal int ReceivedCommandCount
    {
        get
        {
            lock (_commandGate)
            {
                return _receivedCommandCount;
            }
        }
    }

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

    /// <summary>
    /// Bootstrap a new engine from a save file instead of the settings' defaultScenario.
    /// Mirrors CreateFromSettingsFile but the scenario source is a save (gameTimeMs may be &gt; 0).
    /// </summary>
    public static SimulationEngine CreateFromSaveFile(string settingsPath, string savePath)
    {
        return EngineContentLoader.CreateEngineFromSaveFile(settingsPath, savePath);
    }

    internal SimulationEngine(GameDataRegistry registry)
    {
        _registry = registry;
        _clock = new SimulationClock(SimulationSpeed.Speed1);
    }

    public void ReceiveCommand(PlayerCommand command)
    {
        lock (_commandGate)
        {
            _pendingCommands.Add(command);
            _receivedCommandCount++;
        }
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
                ObjectType: obj.ObjectType,
                StartGameTimeMs: 0,
                Modules: modules,
                Name: obj.Name, // startGameTime stamped at RunAsync
                PersistenceType: obj.PersistenceType,
                MassKg: obj.MassKg,
                CompositionType: obj.CompositionType,
                IsKnown: obj.IsKnown));
        }

        lock (_worldStateLock)
        {
            PlayerShipObjectId = gs.PlayerShipObjectId;
            _clock.Reset(gs.GameTimeMs, speed);
            _nextEngineCycleId = 0;
            CollectLoadedEngineCycleIds(runtimeObjects);

            _objects.Clear();
            _objects.AddRange(runtimeObjects);
        }
    }

    /// <summary>Add a test object (legacy — prefer LoadScenario for production).</summary>
    public void AddTestObject(ObjectMotionSnapshot initial)
    {
        _objects.Add(new SpaceObjectRuntime(initial, "Test", 0, ImmutableArray<InstalledModuleRuntime>.Empty));
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
        lock (_worldStateLock)
        {
            long gameTimeMs = clockState.GameTimeMs;

            // Gating this on the CURRENT speed (rather than always calling it) is wrong: the
            // snapshot loop yields once per real second regardless of speed, so the first
            // snapshot built right after a pause can carry a gameTimeMs that already includes
            // a running period from before the pause took effect (SimulationClock.SetSpeed
            // correctly accumulates that time before switching). Skipping cycle completion for
            // that snapshot leaves turn-cycle steps that are genuinely due un-applied — the
            // object's stored Direction goes stale, TurnStepRemainingMs's Math.Max(1, ...)
            // clamp silently hides how overdue it is, and the NEXT running snapshot's
            // completion loop then catches up several steps at once, visible as an unexplained
            // snap. Completion is itself correctly gated on gameTimeMs progression already (its
            // loop condition no-ops when no time has passed), so no external speed check is
            // needed here.
            CompleteActiveEngineCycles(gameTimeMs);
            ApplyPendingCommands(gameTimeMs);

            var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>(_objects.Count);
            foreach (var obj in _objects)
            {
                long elapsed = gameTimeMs - obj.StartGameTimeMs;
                var motion = _motion.Predict(obj.InitialMotion, elapsed);
                var cycleMotion = GetActiveEngineCycleMotion(obj, gameTimeMs);
                objects.Add(motion with
                {
                    ActiveEngineCommandType = cycleMotion.CommandType,
                    TurnStepDegrees = cycleMotion.TurnStepDegrees,
                    TurnStepRemainingMs = cycleMotion.TurnStepRemainingMs,
                    TurnStepIntervalMs = cycleMotion.TurnStepIntervalMs,
                    ObjectType = obj.ObjectType,
                    RelationToPlayer = GetRelationToPlayer(obj.InitialMotion.ObjectId, obj.ObjectType),
                    DisplayName = obj.InitialMotion.ObjectId == PlayerShipObjectId ? obj.Name : null,
                    MaxSpeedKmS = GetMaxSpeedKmS(obj)
                });
            }

            return new AuthoritativeSnapshot(
                SnapshotSequence: _nextSequence++,
                GameTimeMs: gameTimeMs,
                CurrentSpeed: clockState.Speed,
                Objects: objects.MoveToImmutable(),
                PlayerShipObjectId: PlayerShipObjectId);
        }
    }

    /// <summary>
    /// Take a thread-safe, consistent snapshot of the full authoritative world state
    /// (all objects, all modules including ActiveCycle/PowerState/OperationalState/
    /// StructurePoints/Cargo, IsKnown) as a save-file-shaped ScenarioFile.
    /// Safe to call concurrently with the background 1 Hz simulation loop — both
    /// share <see cref="_worldStateLock"/>. Camera/zoom/focus are client-side only
    /// and are never captured here (Focus is always null in the result).
    /// </summary>
    public ScenarioFile CaptureSaveState()
    {
        lock (_worldStateLock)
        {
            return CaptureSaveStateCore(_clock.Capture());
        }
    }

    /// <summary>Test seam: capture save state at an explicit (gameTimeMs, speed) instead of the real clock — mirrors CaptureSnapshotForTests.</summary>
    internal ScenarioFile CaptureSaveStateForTests(long gameTimeMs, SimulationSpeed speed)
    {
        lock (_worldStateLock)
        {
            return CaptureSaveStateCore(new SimulationClockState(gameTimeMs, speed));
        }
    }

    private ScenarioFile CaptureSaveStateCore(SimulationClockState clockState)
    {
        long gameTimeMs = clockState.GameTimeMs;

        // Bring ActiveCycle/position/direction fully up to date for gameTimeMs before
        // capturing — otherwise a cycle that has already logically completed (but whose
        // completion hasn't been applied yet because the 1 Hz BuildSnapshot loop hasn't
        // ticked since) would be captured stale. Idempotent: a cycle already caught up to
        // gameTimeMs is a no-op here (same guard BuildSnapshot relies on).
        CompleteActiveEngineCycles(gameTimeMs);

        // Mirror BuildSnapshot's other half: a command the player sent in the narrow
        // window between the last 1 Hz tick and this save (e.g. F5 pressed right after
        // clicking a command button) is still sitting in _pendingCommands otherwise —
        // captured nowhere, and lost outright once the old session is disposed on F9.
        // Applying it here, in the same order BuildSnapshot uses (cycles, then commands),
        // makes "continue after F9" match "continue without saving" for this case too.
        ApplyPendingCommands(gameTimeMs);

        var spaceObjects = new List<SpaceObjectData>(_objects.Count);
        foreach (var obj in _objects)
        {
            long elapsed = gameTimeMs - obj.StartGameTimeMs;
            var motion = _motion.Predict(obj.InitialMotion, elapsed);

            spaceObjects.Add(new SpaceObjectData(
                ObjectId: obj.InitialMotion.ObjectId,
                ObjectType: obj.ObjectType,
                PersistenceType: obj.PersistenceType,
                Name: obj.Name,
                PositionX: motion.X,
                PositionY: motion.Y,
                SpeedMps: (int)Math.Round(motion.SpeedKmS * 1000.0, MidpointRounding.AwayFromZero),
                DirectionDegrees: ToDirectionDegreesInt(motion.Direction),
                MovementType: motion.SpeedKmS > 0 ? "Linear" : "Stationary",
                MassKg: obj.MassKg,
                CompositionType: obj.CompositionType,
                Modules: BuildSaveModules(obj),
                IsKnown: obj.IsKnown));
        }

        var gameState = new GameStateData(
            GameTimeMs: gameTimeMs,
            CurrentSpeed: clockState.Speed.ToString(),
            PlayerShipObjectId: PlayerShipObjectId ?? string.Empty,
            Focus: null, // camera/focus is client-side only — never saved (decision G.20)
            SpaceObjects: spaceObjects);

        return new ScenarioFile(
            Metadata: new ScenarioMetadata(ScenarioId: "quicksave", Name: "Quicksave"),
            GameState: gameState,
            SaveFormatVersion: SaveFormat.CurrentSaveFormatVersion);
    }

    private IReadOnlyList<ShipModuleData> BuildSaveModules(SpaceObjectRuntime obj)
    {
        if (obj.Modules.Length == 0)
            return Array.Empty<ShipModuleData>();

        var modules = new List<ShipModuleData>(obj.Modules.Length);
        foreach (var module in obj.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            modules.Add(new ShipModuleData(
                ModuleId: module.ModuleId,
                ModuleTypeId: moduleType.TypeId,
                PlatformIndex: module.PlatformIndex,
                OccupiedCells: module.OccupiedCells,
                StructurePoints: module.StructurePoints,
                PowerState: module.PowerState,
                OperationalState: module.OperationalState,
                ActiveCycle: module.ActiveCycle,
                Cargo: BuildSaveCargo(module)));
        }

        return modules;
    }

    private IReadOnlyList<CargoStackData> BuildSaveCargo(InstalledModuleRuntime module)
    {
        if (module.Cargo.Length == 0)
            return Array.Empty<CargoStackData>();

        var cargo = new List<CargoStackData>(module.Cargo.Length);
        foreach (var stack in module.Cargo)
        {
            var itemType = _registry.ItemTypes.GetDefinition(stack.ItemTypeIndex);
            cargo.Add(new CargoStackData(ItemTypeId: itemType.TypeId, Quantity: stack.Quantity));
        }

        return cargo;
    }

    private static int ToDirectionDegreesInt(double direction)
    {
        double normalized = NormalizeDirection(direction);
        int rounded = (int)Math.Round(normalized, MidpointRounding.AwayFromZero);
        if (rounded >= 360)
            rounded -= 360;
        if (rounded < 0)
            rounded += 360;
        return rounded;
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

    private string? GetRelationToPlayer(string objectId, string objectType)
    {
        if (objectId == PlayerShipObjectId)
            return PlayerRelation.Self;
        if (objectType == SpaceObjectType.NpcShip)
            return PlayerRelation.Neutral; // future: faction system
        return null;
    }

    internal AuthoritativeSnapshot CaptureSnapshotForTests(
        long gameTimeMs = 0,
        SimulationSpeed? speed = null)
    {
        return BuildSnapshot(new SimulationClockState(gameTimeMs, speed ?? _clock.Speed));
    }

    private void ApplyPendingCommands(long gameTimeMs)
    {
        var commands = DrainPendingCommands();
        List<PlayerCommand>? deferred = null;

        foreach (var command in commands)
        {
            if (TryStartEngineCommand(command, gameTimeMs) == CommandStartDisposition.Deferred)
            {
                deferred ??= [];
                deferred.Add(command);
            }
        }

        if (deferred is { Count: > 0 })
        {
            // Complete any zero-duration one-shot cycles that were just started
            // in the first pass — this gives deferred commands a second chance
            // within the same snapshot instead of waiting a full second.
            CompleteActiveEngineCycles(gameTimeMs);

            List<PlayerCommand>? stillDeferred = null;
            foreach (var command in deferred)
            {
                if (TryStartEngineCommand(command, gameTimeMs) == CommandStartDisposition.Deferred)
                {
                    stillDeferred ??= [];
                    stillDeferred.Add(command);
                }
            }

            if (stillDeferred is { Count: > 0 })
                RequeueDeferredCommands(stillDeferred);
        }
    }

    private List<PlayerCommand> DrainPendingCommands()
    {
        lock (_commandGate)
        {
            if (_pendingCommands.Count == 0)
                return [];

            var commands = new List<PlayerCommand>(_pendingCommands);
            _pendingCommands.Clear();
            return commands;
        }
    }

    private void RequeueDeferredCommands(List<PlayerCommand> commands)
    {
        lock (_commandGate)
        {
            _pendingCommands.InsertRange(0, commands);
        }
    }

    private CommandStartDisposition TryStartEngineCommand(PlayerCommand command, long gameTimeMs)
    {
        if (!string.Equals(command.ObjectId, PlayerShipObjectId, StringComparison.Ordinal))
            return CommandStartDisposition.Rejected;

        int objectIndex = _objects.FindIndex(o =>
            string.Equals(o.InitialMotion.ObjectId, command.ObjectId, StringComparison.Ordinal) &&
            string.Equals(o.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase));
        if (objectIndex < 0)
            return CommandStartDisposition.Rejected;

        var obj = _objects[objectIndex];
        int moduleIndex = FindModuleIndex(obj.Modules, command.ModuleId);
        if (moduleIndex < 0)
            return CommandStartDisposition.Rejected;

        var module = obj.Modules[moduleIndex];
        var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (!IsEngineCommandType(moduleType, command.CommandType))
            return CommandStartDisposition.Rejected;

        if (command.CommandType == ShipEngineCommandTypes.CancelAll)
        {
            _objects[objectIndex] = UpdateModule(obj, moduleIndex, module => module with { ActiveCycle = null });
            return CommandStartDisposition.Started;
        }

        if (!CanExecuteEngineCommand(module, moduleType))
            return CommandStartDisposition.Rejected;

        if (module.ActiveCycle is { } activeCycle)
        {
            if (!activeCycle.IsAutoRepeat)
                return CommandStartDisposition.Deferred;

            // Same command type — idempotent, continue existing cycle.
            if (string.Equals(command.CommandType, activeCycle.CommandType, StringComparison.Ordinal))
                return CommandStartDisposition.Started;

            // Any other engine command implicitly cancels the active periodic
            // (auto-repeat) cycle and falls through to start its own cycle below.
        }

        bool isAutoRepeat = IsCyclicEngineCommand(command.CommandType);
        _objects[objectIndex] = UpdateModule(
            obj,
            moduleIndex,
            current => current with
            {
                ActiveCycle = CreateEngineCycle(command.CommandType, gameTimeMs, isAutoRepeat)
            });
        return CommandStartDisposition.Started;
    }

    private static int FindModuleIndex(ImmutableArray<InstalledModuleRuntime> modules, string moduleId)
    {
        for (int i = 0; i < modules.Length; i++)
        {
            if (string.Equals(modules[i].ModuleId, moduleId, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static bool IsEngineCommandType(ModuleTypeDefinition moduleType, string commandType)
    {
        return string.Equals(moduleType.TypeId, "module.engine.basic", StringComparison.Ordinal) &&
               moduleType.CommandTypeIds.Contains(commandType, StringComparer.Ordinal);
    }

    private static bool CanExecuteEngineCommand(InstalledModuleRuntime module, ModuleTypeDefinition moduleType)
    {
        return moduleType.MaxSpeedMps is > 0 &&
               moduleType.TurnStepDegrees is > 0 &&
               moduleType.LinearInertiaMps2 is > 0 &&
               string.Equals(module.PowerState, "On", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(module.OperationalState, "Ready", StringComparison.OrdinalIgnoreCase) &&
               module.StructurePoints > 0;
    }

    private ActiveCycleData CreateEngineCycle(string commandType, long gameTimeMs, bool isAutoRepeat)
    {
        string cycleId = $"CYC-ENGINE-{++_nextEngineCycleId:D6}";
        long durationMs = IsUntilCancelTurn(commandType) ? 1000 : 0;
        return new ActiveCycleData(cycleId, gameTimeMs, durationMs, commandType, isAutoRepeat);
    }

    private static bool IsUntilCancelTurn(string commandType)
    {
        return commandType == ShipEngineCommandTypes.TurnLeftUntilCancel ||
               commandType == ShipEngineCommandTypes.TurnRightUntilCancel;
    }

    private static bool IsCyclicEngineCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.Accelerate ||
               commandType == ShipEngineCommandTypes.Brake ||
               IsUntilCancelTurn(commandType);
    }

    private void CollectLoadedEngineCycleIds(IEnumerable<SpaceObjectRuntime> objects)
    {
        const string EngineCyclePrefix = "CYC-ENGINE-";
        foreach (var obj in objects)
        {
            foreach (var module in obj.Modules)
            {
                if (module.ActiveCycle is not { } cycle)
                    continue;

                if (cycle.CycleId.StartsWith(EngineCyclePrefix, StringComparison.Ordinal) &&
                    ulong.TryParse(cycle.CycleId[EngineCyclePrefix.Length..], out ulong loadedCycleNumber))
                {
                    _nextEngineCycleId = Math.Max(_nextEngineCycleId, loadedCycleNumber);
                }
            }
        }
    }

    private double? GetMaxSpeedKmS(SpaceObjectRuntime obj)
    {
        foreach (var module in obj.Modules)
        {
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (string.Equals(moduleType.TypeId, "module.engine.basic", StringComparison.Ordinal) &&
                moduleType.MaxSpeedMps is > 0)
                return moduleType.MaxSpeedMps.Value / 1000.0;
        }
        return null;
    }

    private ActiveEngineCycleMotion GetActiveEngineCycleMotion(SpaceObjectRuntime obj, long gameTimeMs)
    {
        for (int moduleIndex = 0; moduleIndex < obj.Modules.Length; moduleIndex++)
        {
            var module = obj.Modules[moduleIndex];
            if (module.ActiveCycle is not { } cycle)
                continue;

            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            if (!IsEngineCommandType(moduleType, cycle.CommandType) || !CanExecuteEngineCommand(module, moduleType))
                continue;

            if (!IsUntilCancelTurn(cycle.CommandType))
                return new ActiveEngineCycleMotion(cycle.CommandType, 0, 0, 0);

            int turnSign = cycle.CommandType == ShipEngineCommandTypes.TurnLeftUntilCancel ? -1 : 1;
            long remainingMs = Math.Max(1, cycle.StartedGameTimeMs + cycle.DurationMs - gameTimeMs);
            return new ActiveEngineCycleMotion(
                cycle.CommandType,
                turnSign * moduleType.TurnStepDegrees!.Value,
                remainingMs,
                cycle.DurationMs);
        }

        return default;
    }

    private void CompleteActiveEngineCycles(long gameTimeMs)
    {
        for (int objectIndex = 0; objectIndex < _objects.Count; objectIndex++)
        {
            var obj = _objects[objectIndex];
            if (!string.Equals(obj.InitialMotion.ObjectId, PlayerShipObjectId, StringComparison.Ordinal))
                continue;

            for (int moduleIndex = 0; moduleIndex < obj.Modules.Length; moduleIndex++)
            {
                while (true)
                {
                    var module = obj.Modules[moduleIndex];
                    if (module.ActiveCycle is not { } cycle || gameTimeMs <= cycle.StartedGameTimeMs ||
                        gameTimeMs - cycle.StartedGameTimeMs < cycle.DurationMs)
                    {
                        break;
                    }

                    var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                    if (!IsEngineCommandType(moduleType, cycle.CommandType) || !CanExecuteEngineCommand(module, moduleType))
                    {
                        _objects[objectIndex] = UpdateModule(obj, moduleIndex, current => current with { ActiveCycle = null });
                        obj = _objects[objectIndex];
                        break;
                    }

                    long completionGameTimeMs = cycle.DurationMs == 0
                        ? gameTimeMs
                        : cycle.StartedGameTimeMs + cycle.DurationMs;
                    ActiveCycleData? nextCycle = cycle.IsAutoRepeat
                        ? CreateEngineCycle(cycle.CommandType, completionGameTimeMs, isAutoRepeat: true)
                        : null;
                    _objects[objectIndex] = ApplyCompletedEngineCommand(
                        obj,
                        moduleIndex,
                        moduleType,
                        cycle.CommandType,
                        completionGameTimeMs,
                        nextCycle);
                    obj = _objects[objectIndex];

                    if (!cycle.IsAutoRepeat)
                        break;
                }
            }
        }
    }

    private SpaceObjectRuntime ApplyCompletedEngineCommand(
        SpaceObjectRuntime obj,
        int moduleIndex,
        ModuleTypeDefinition moduleType,
        string commandType,
        long gameTimeMs,
        ActiveCycleData? nextCycle)
    {
        return commandType switch
        {
            ShipEngineCommandTypes.Accelerate => UpdateEngineMotion(
                obj,
                moduleIndex,
                gameTimeMs,
                module => module with { ActiveCycle = nextCycle },
                motion => motion with
                {
                    SpeedKmS = Math.Min(
                        moduleType.MaxSpeedMps!.Value / 1000.0,
                        motion.SpeedKmS + ComputeLinearInertiaDeltaKmS(obj, moduleType, gameTimeMs))
                }),

            ShipEngineCommandTypes.Brake => UpdateEngineMotion(
                obj,
                moduleIndex,
                gameTimeMs,
                module => module with { ActiveCycle = nextCycle },
                motion => motion with
                {
                    SpeedKmS = Math.Max(
                        0,
                        motion.SpeedKmS - ComputeLinearInertiaDeltaKmS(obj, moduleType, gameTimeMs))
                }),

            ShipEngineCommandTypes.TurnLeftStep or ShipEngineCommandTypes.TurnLeftUntilCancel => ApplyTurn(
                obj,
                moduleIndex,
                moduleType,
                turnSign: -1,
                gameTimeMs,
                nextCycle),

            ShipEngineCommandTypes.TurnRightStep or ShipEngineCommandTypes.TurnRightUntilCancel => ApplyTurn(
                obj,
                moduleIndex,
                moduleType,
                turnSign: 1,
                gameTimeMs,
                nextCycle),

            _ => obj
        };
    }

    private static double ComputeLinearInertiaDeltaKmS(
        SpaceObjectRuntime obj, ModuleTypeDefinition moduleType, long gameTimeMs)
    {
        long elapsedMs = Math.Max(0, gameTimeMs - obj.StartGameTimeMs);
        return moduleType.LinearInertiaMps2!.Value / 1000.0 * (elapsedMs / 1000.0);
    }

    private SpaceObjectRuntime ApplyTurn(
        SpaceObjectRuntime obj,
        int moduleIndex,
        ModuleTypeDefinition moduleType,
        int turnSign,
        long gameTimeMs,
        ActiveCycleData? nextCycle)
    {
        return UpdateEngineMotion(
            obj,
            moduleIndex,
            gameTimeMs,
            module => module with { ActiveCycle = nextCycle },
            motion => motion with
            {
                Direction = NormalizeDirection(motion.Direction + turnSign * moduleType.TurnStepDegrees!.Value)
            });
    }

    private SpaceObjectRuntime UpdateEngineMotion(
        SpaceObjectRuntime obj,
        int moduleIndex,
        long gameTimeMs,
        Func<InstalledModuleRuntime, InstalledModuleRuntime> updateModule,
        Func<ObjectMotionSnapshot, ObjectMotionSnapshot> updateMotion)
    {
        long elapsedMs = Math.Max(0, gameTimeMs - obj.StartGameTimeMs);
        var currentMotion = _motion.Predict(obj.InitialMotion, elapsedMs);
        var modules = obj.Modules.SetItem(moduleIndex, updateModule(obj.Modules[moduleIndex]));

        return obj with
        {
            InitialMotion = updateMotion(currentMotion),
            StartGameTimeMs = gameTimeMs,
            Modules = modules
        };
    }

    private static SpaceObjectRuntime UpdateModule(
        SpaceObjectRuntime obj,
        int moduleIndex,
        Func<InstalledModuleRuntime, InstalledModuleRuntime> updateModule)
    {
        return obj with { Modules = obj.Modules.SetItem(moduleIndex, updateModule(obj.Modules[moduleIndex])) };
    }

    private static double NormalizeDirection(double degrees)
    {
        double normalized = degrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}

internal sealed record SpaceObjectRuntime(
    ObjectMotionSnapshot InitialMotion,
    string ObjectType,
    long StartGameTimeMs,
    ImmutableArray<InstalledModuleRuntime> Modules,
    string? Name = null,
    string PersistenceType = "Permanent",
    long? MassKg = null,
    string? CompositionType = null,
    bool IsKnown = false);

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

internal readonly record struct ActiveEngineCycleMotion(
    string? CommandType,
    int TurnStepDegrees,
    long TurnStepRemainingMs,
    long TurnStepIntervalMs);

internal enum CommandStartDisposition
{
    Started,
    Deferred,
    Rejected
}
