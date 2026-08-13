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
    private readonly List<CommandResult> _commandResults = new();
    private readonly List<ShipEvent> _shipEvents = new();
    private int _receivedCommandCount;
    private ulong _nextSequence;
    private ulong _nextEngineCycleId;
    private ulong _nextShipEventId;
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

    /// <summary>
    /// Client-reported tactical-map ActiveObjectId (hover), authoritative once validated
    /// against the current world (§54). Reset to null by every LoadScenario (New Game,
    /// Quick Load) — it is session-interaction state, never persisted to save files.
    /// </summary>
    public string? ActiveObjectId { get; private set; }

    /// <summary>
    /// Client-reported tactical-map SelectedObjectId (click), authoritative once validated
    /// against the current world (§54). Reset to null by every LoadScenario (New Game,
    /// Quick Load) — it is session-interaction state, never persisted to save files.
    /// </summary>
    public string? SelectedObjectId { get; private set; }

    /// <summary>
    /// One per session, immutable for the session's lifetime (requirements §15). Set by
    /// LoadScenario: reused as-is when the incoming scenario/save already carries one,
    /// otherwise freshly randomly generated (New Game, or a legacy save missing it).
    /// </summary>
    public ulong MasterSeed { get; private set; }

    /// <summary>
    /// True if the most recent LoadScenario call had to generate MasterSeed because the
    /// incoming scenario/save didn't carry one. True for every New Game (expected — the
    /// DefaultScenario never specifies masterSeed) and for a legacy save missing it
    /// (unexpected — callers loading from a save file are expected to surface a warning;
    /// see Program.cs's LocalGameSessionFactory.CreateSessionFromSave).
    /// </summary>
    public bool MasterSeedWasMissingOnLoad { get; private set; }

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
    /// Accept the client's tactical-map ActiveObjectId (hover) and SelectedObjectId
    /// (click) as the new session-interaction state (§54). Each id is validated
    /// independently against the current world under <see cref="_worldStateLock"/> — an
    /// id that doesn't reference an object currently in the world normalizes to null.
    /// Works at any SimulationSpeed, including Speed0: pause stops the simulation, not
    /// UI/transport/session-control. Selection never starts simulation events or affects
    /// object motion.
    /// </summary>
    public void SetObjectInteractionState(string? activeObjectId, string? selectedObjectId)
    {
        lock (_worldStateLock)
        {
            ActiveObjectId = NormalizeObjectId(activeObjectId);
            SelectedObjectId = NormalizeObjectId(selectedObjectId);
        }
    }

    /// <summary>Caller must hold <see cref="_worldStateLock"/>.</summary>
    private string? NormalizeObjectId(string? objectId)
    {
        return !string.IsNullOrEmpty(objectId) && ObjectExists(objectId) ? objectId : null;
    }

    /// <summary>Caller must hold <see cref="_worldStateLock"/>.</summary>
    private bool ObjectExists(string objectId)
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            if (string.Equals(_objects[i].InitialMotion.ObjectId, objectId, StringComparison.Ordinal))
                return true;
        }

        return false;
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
                // Must equal gs.GameTimeMs, not 0: PositionX/Y/SpeedMps/DirectionDegrees are
                // defined as "state AT gs.GameTimeMs", so elapsed-time math (BuildSnapshot,
                // CaptureSaveState) must start counting from there. RunAsync's prologue
                // re-stamps this from the clock too (engineStartGameTime = _clock.GameTimeMs,
                // which SimulationClock.Reset just set to this same gs.GameTimeMs above) —
                // that makes RunAsync's stamp an idempotent no-op, not a second source of
                // truth. Without this, CaptureSaveState/BuildSnapshot called in the window
                // between construction and RunAsync's first iteration (e.g. F5 right after
                // F9, or CaptureSaveState() called directly on a freshly bootstrapped engine)
                // would double-count gs.GameTimeMs as elapsed motion from position zero.
                StartGameTimeMs: gs.GameTimeMs,
                Modules: modules,
                Name: obj.Name,
                PersistenceType: obj.PersistenceType,
                MassKg: obj.MassKg,
                CompositionType: obj.CompositionType,
                IsKnown: obj.IsKnown));
        }

        lock (_worldStateLock)
        {
            PlayerShipObjectId = gs.PlayerShipObjectId;
            // Session-interaction state (§54) — never carried over from the previous
            // world, and never read from scenario/save data. Every New Game and Quick
            // Load starts with both null; the first snapshot of the new session reports
            // null for both.
            ActiveObjectId = null;
            SelectedObjectId = null;
            _clock.Reset(gs.GameTimeMs, speed);
            _nextEngineCycleId = 0;
            _nextShipEventId = 0;
            _shipEvents.Clear();
            CollectLoadedEngineCycleIds(runtimeObjects);

            // masterSeed: reuse whatever the scenario/save already carries (continuing a
            // session must not reshuffle it). A missing value covers two distinct cases the
            // caller tells apart by context, not by this flag alone — New Game's
            // DefaultScenario never specifies one (expected, no warning warranted), while a
            // legacy save missing it is unexpected (callers loading from a save file are
            // expected to check MasterSeedWasMissingOnLoad and warn).
            if (gs.MasterSeed is { } masterSeed)
            {
                MasterSeed = masterSeed;
                MasterSeedWasMissingOnLoad = false;
            }
            else
            {
                MasterSeed = GenerateRandomMasterSeed();
                MasterSeedWasMissingOnLoad = true;
            }

            _objects.Clear();
            _objects.AddRange(runtimeObjects);
        }
    }

    private static ulong GenerateRandomMasterSeed()
    {
        Span<byte> bytes = stackalloc byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return BitConverter.ToUInt64(bytes);
    }

    /// <summary>Add a test object (legacy — prefer LoadScenario for production).</summary>
    public void AddTestObject(ObjectMotionSnapshot initial)
    {
        _objects.Add(new SpaceObjectRuntime(initial, "Test", 0, ImmutableArray<InstalledModuleRuntime>.Empty));
    }

    public async IAsyncEnumerable<AuthoritativeSnapshot> RunAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Stamp objects with the current game time at engine start. Same value LoadScenario
        // already stamped (gs.GameTimeMs) in the common case — this re-stamp only matters
        // when a session-control call (e.g. SetSimulationSpeedAsync) lands in the window
        // between LoadScenario and RunAsync's first iteration and nudges the clock forward
        // by whatever tiny real time elapsed at the pre-RunAsync speed. Locked because
        // CaptureSaveState()/BuildSnapshot are publicly reachable in that same window (e.g.
        // SaveAsync called immediately after construction, before this loop runs) and every
        // other _objects read/mutation goes through _worldStateLock — an unlocked mutation
        // here raced a concurrent foreach over _objects and threw
        // InvalidOperationException("Collection was modified").
        lock (_worldStateLock)
        {
            _clock.ResetRealBaseline();
            long engineStartGameTime = _clock.GameTimeMs;

            for (int i = 0; i < _objects.Count; i++)
            {
                _objects[i] = _objects[i] with { StartGameTimeMs = engineStartGameTime };
            }
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

            // Re-validate on every snapshot (not only when the client reports new
            // interaction state): if the selected/active object disappeared from the
            // world, the property and every subsequent snapshot must reflect that
            // immediately (§54 "Жизненный цикл объекта").
            ActiveObjectId = NormalizeObjectId(ActiveObjectId);
            SelectedObjectId = NormalizeObjectId(SelectedObjectId);

            var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>(_objects.Count);
            foreach (var obj in _objects)
            {
                long elapsed = gameTimeMs - obj.StartGameTimeMs;
                var motion = _motion.Predict(obj.InitialMotion, elapsed);
                var cycleMotion = GetActiveEngineCycleMotion(obj, gameTimeMs);
                // Render projection: the client may only see factual data
                // (type, relation, name) for objects the player knows about.
                // The player ship is always known — protects legacy saves
                // without isKnown. Unknown objects get the sentinel render type
                // and null factual fields.
                bool known = obj.IsKnown || obj.InitialMotion.ObjectId == PlayerShipObjectId;
                objects.Add(motion with
                {
                    ActiveEngineCommandType = cycleMotion.CommandType,
                    TurnStepDegrees = cycleMotion.TurnStepDegrees,
                    TurnStepRemainingMs = cycleMotion.TurnStepRemainingMs,
                    TurnStepIntervalMs = cycleMotion.TurnStepIntervalMs,
                    NavigationTargetX = cycleMotion.NavigationTargetX,
                    NavigationTargetY = cycleMotion.NavigationTargetY,
                    NavigationAngularInertiaDegPerSec = cycleMotion.NavigationAngularInertiaDegPerSec,
                    NavigationLockedCourseDegrees = cycleMotion.NavigationLockedCourseDegrees,
                    NavigationPhase = cycleMotion.NavigationPhase,
                    NavigationEscapeCourseDegrees = cycleMotion.NavigationEscapeCourseDegrees,
                    NavigationRequiredDepartureDistance = cycleMotion.NavigationRequiredDepartureDistance,
                    ObjectType = known ? obj.ObjectType : null,
                    RenderObjectType = known ? obj.ObjectType : SpaceObjectType.UnknownSpaceObject,
                    RelationToPlayer = known ? GetRelationToPlayer(obj.InitialMotion.ObjectId, obj.ObjectType) : null,
                    DisplayName = known ? obj.Name : null,
                    MaxSpeedKmS = GetMaxSpeedKmS(obj)
                });
            }

            // Drain results of commands processed since the previous snapshot.
            // Results accumulated by ApplyPendingCommands from CaptureSaveStateCore
            // are published by this next BuildSnapshot — correct: the command was
            // processed "since the previous snapshot".
            // Deduplicate by CommandId: keep only the last (final) disposition.
            // Both BuildSnapshot and CaptureSaveStateCore call ApplyPendingCommands,
            // so a command processed twice in one drain window produces two entries
            // — the last one is authoritative (e.g. Deferred then Executed).
            var commandResults = _commandResults
                .GroupBy(r => r.CommandId)
                .Select(g => g.Last())
                .ToImmutableArray();
            _commandResults.Clear();

            // Drain ship events — no deduplication needed: each event has a
            // unique EventId and is recorded exactly once.
            var shipEvents = _shipEvents.ToImmutableArray();
            _shipEvents.Clear();

            var installedModules = BuildInstalledModuleProjection();

            return new AuthoritativeSnapshot(
                SnapshotSequence: _nextSequence++,
                GameTimeMs: gameTimeMs,
                CurrentSpeed: clockState.Speed,
                Objects: objects.MoveToImmutable(),
                PlayerShipObjectId: PlayerShipObjectId,
                CommandResults: commandResults,
                ShipEvents: shipEvents,
                InstalledModules: installedModules,
                ActiveObjectId: ActiveObjectId,
                SelectedObjectId: SelectedObjectId);
        }
    }

    /// <summary>
    /// Build a minimal UI projection of installed modules for the player ship.
    /// The full list (including modules with empty CommandTypeIds) is returned —
    /// the Commands Panel performs its own data-driven filtering.
    /// Returns empty when there is no player ship or no module type registry.
    /// </summary>
    private ImmutableArray<InstalledModuleSnapshot> BuildInstalledModuleProjection()
    {
        if (string.IsNullOrWhiteSpace(PlayerShipObjectId))
            return ImmutableArray<InstalledModuleSnapshot>.Empty;

        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship == null || ship.Modules.IsEmpty)
            return ImmutableArray<InstalledModuleSnapshot>.Empty;

        if (_registry.ModuleTypes.Count == 0)
            return ImmutableArray<InstalledModuleSnapshot>.Empty;

        var builder = ImmutableArray.CreateBuilder<InstalledModuleSnapshot>(ship.Modules.Length);
        for (int i = 0; i < ship.Modules.Length; i++)
        {
            var module = ship.Modules[i];
            var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            builder.Add(new InstalledModuleSnapshot(
                ModuleId: module.ModuleId,
                ModuleTypeId: moduleType.TypeId,
                DisplayName: moduleType.DisplayName,
                Position: i,
                CommandTypeIds: moduleType.CommandTypeIds));
        }

        return builder.MoveToImmutable();
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
            SpaceObjects: spaceObjects,
            MasterSeed: MasterSeed);

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
                Cargo: BuildSaveCargo(module),
                FuelAmountKg: moduleType.FuelCapacityKg is > 0 ? module.FuelAmountKg : null,
                LastTurnGameTimeMs: moduleType.AngularInertiaDegPerSec is > 0
                    ? module.LastTurnGameTimeMs
                    : null));
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

            // Fuel: engine module types carry a FuelCapacityKg; the installed instance
            // stores its current FuelAmountKg. If the JSON omits FuelAmountKg for an
            // engine module, default to a full tank (§56.10).
            long fuelAmountKg = ResolveFuelAmountKg(module, moduleType, obj.ObjectId);

            // Last-turn timestamp: only engine modules with angular inertia track it.
            // A JSON null means the module never turned (or the save predates the field)
            // — valid, and it must not block the first turn.
            long? lastTurnGameTimeMs = ResolveLastTurnGameTimeMs(module, moduleType);

            modules.Add(new InstalledModuleRuntime(
                module.ModuleId,
                moduleTypeIndex,
                module.PlatformIndex,
                module.OccupiedCells.ToImmutableArray(),
                module.PowerState,
                module.OperationalState,
                module.StructurePoints,
                module.ActiveCycle,
                cargo,
                fuelAmountKg,
                lastTurnGameTimeMs));
        }

        return modules.ToImmutable();
    }

    private static void ValidateModulePlacement(
        string objectId,
        ShipModuleData module,
        ModuleTypeDefinition moduleType,
        Dictionary<int, HashSet<int>> occupiedCellsByPlatform)
    {
        if (module.PlatformIndex < 0)
        {
            throw new ScenarioException(
                $"Module '{module.ModuleId}' on '{objectId}' has negative platformIndex {module.PlatformIndex}.");
        }

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
            if (cell > 3)
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' on '{objectId}' has occupied cell {cell} outside the 2x2 platform grid (0..3).");

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

    /// <summary>
    /// Resolve and validate <see cref="InstalledModuleRuntime.FuelAmountKg"/> from JSON data.
    /// Engine modules (<see cref="ModuleTypeDefinition.FuelCapacityKg"/> &gt; 0) require
    /// 0 ≤ FuelAmountKg ≤ FuelCapacityKg. If the JSON omits the field (null), default to
    /// a full tank. Non-engine modules get 0 and skip validation (§56.10).
    /// </summary>
    private static long ResolveFuelAmountKg(ShipModuleData module, ModuleTypeDefinition moduleType, string objectId)
    {
        if (moduleType.FuelCapacityKg is not ( > 0))
            return 0;

        long fuelAmountKg = module.FuelAmountKg ?? moduleType.FuelCapacityKg.Value;

        if (fuelAmountKg < 0 || fuelAmountKg > moduleType.FuelCapacityKg.Value)
        {
            throw new ScenarioException(
                $"Module '{module.ModuleId}' on '{objectId}' fuelAmountKg {fuelAmountKg} " +
                $"is outside 0..{moduleType.FuelCapacityKg.Value}.");
        }

        return fuelAmountKg;
    }

    /// <summary>
    /// Resolve <see cref="InstalledModuleRuntime.LastTurnGameTimeMs"/> from JSON data.
    /// Only engine modules with angular inertia (<see cref="ModuleTypeDefinition.AngularInertiaDegPerSec"/>
    /// &gt; 0) track the last-turn timestamp; all other modules get null. A missing JSON
    /// field (null) is valid — the module never turned (or the save predates the field).
    /// </summary>
    private static long? ResolveLastTurnGameTimeMs(ShipModuleData module, ModuleTypeDefinition moduleType)
    {
        return moduleType.AngularInertiaDegPerSec is > 0 ? module.LastTurnGameTimeMs : null;
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
            var outcome = TryStartEngineCommand(command, gameTimeMs);
            if (outcome.Disposition == CommandStartDisposition.Deferred)
            {
                // Final disposition unknown yet — the command gets a second chance
                // below; its result is recorded after the second pass.
                deferred ??= [];
                deferred.Add(command);
            }
            else if (outcome.Disposition == CommandStartDisposition.Rejected)
            {
                // Rejected immediately — no cycle was created.
                RecordCommandResult(command, CommandResultStatus.Rejected, gameTimeMs, outcome.ReasonCode);
            }
            // Started: the cycle was created. The final CommandResult is written later
            // by CompleteActiveEngineCycles on completion/interruption (§56.5).
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
                var outcome = TryStartEngineCommand(command, gameTimeMs);
                if (outcome.Disposition == CommandStartDisposition.Deferred)
                {
                    stillDeferred ??= [];
                    stillDeferred.Add(command);
                }
                else if (outcome.Disposition == CommandStartDisposition.Rejected)
                {
                    RecordCommandResult(command, CommandResultStatus.Rejected, gameTimeMs, outcome.ReasonCode);
                }
                // Started: cycle created, result on completion.
            }

            if (stillDeferred is { Count: > 0 })
            {
                // Actually requeued — deferred for the remainder of this tick.
                foreach (var command in stillDeferred)
                {
                    _commandResults.Add(new CommandResult(
                        command.CommandId,
                        command.ObjectId,
                        command.ModuleId,
                        command.CommandType,
                        CommandResultStatus.Deferred,
                        gameTimeMs,
                        CommandReasonCodes.Busy));
                }

                RequeueDeferredCommands(stillDeferred);
            }
        }
    }

    private void RecordCommandResult(
        PlayerCommand command, CommandResultStatus status, long gameTimeMs, string? reasonCode = null)
    {
        _commandResults.Add(new CommandResult(
            command.CommandId,
            command.ObjectId,
            command.ModuleId,
            command.CommandType,
            status,
            gameTimeMs,
            reasonCode));
    }

    private void RecordCommandResultFromCycle(
        ActiveCycleData cycle, CommandResultStatus status, long gameTimeMs, string? reasonCode = null)
    {
        if (cycle.CommandId is null)
            return; // legacy save: cycle has no command tracing — nothing to report

        _commandResults.Add(new CommandResult(
            cycle.CommandId,
            cycle.ObjectId ?? "",
            cycle.ModuleId ?? "",
            cycle.CommandType,
            status,
            gameTimeMs,
            reasonCode));
    }

    private void RecordShipEvent(string objectId, string moduleId, string eventType, string? reasonCode, long gameTimeMs)
    {
        string eventId = $"EVE-{++_nextShipEventId:D6}";
        _shipEvents.Add(new ShipEvent(eventId, objectId, moduleId, eventType, reasonCode, gameTimeMs));
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

    private CommandStartOutcome TryStartEngineCommand(PlayerCommand command, long gameTimeMs)
    {
        if (!string.Equals(command.ObjectId, PlayerShipObjectId, StringComparison.Ordinal))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);

        int objectIndex = _objects.FindIndex(o =>
            string.Equals(o.InitialMotion.ObjectId, command.ObjectId, StringComparison.Ordinal) &&
            string.Equals(o.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase));
        if (objectIndex < 0)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);

        var obj = _objects[objectIndex];
        int moduleIndex = FindModuleIndex(obj.Modules, command.ModuleId);
        if (moduleIndex < 0)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownModule);

        var module = obj.Modules[moduleIndex];
        var moduleType = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
        if (!IsEngineCommandType(moduleType, command.CommandType))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);

        // Match commands (engine.match-target-speed / engine.match-target-course, §56.9)
        // carry an explicit, authoritative target. Parameter validation comes right after
        // UnknownCommandType and before CancelAll/state checks: a parameter-level error
        // (missing/unknown target) is more specific than a module-state error and is
        // published deterministically. A whitespace-only target id counts as missing.
        // The target may be the ship itself — it exists in the world, §56.9 does not forbid it.
        int? matchTargetIndex = null;
        if (IsMatchEngineCommand(command.CommandType))
        {
            if (string.IsNullOrWhiteSpace(command.TargetObjectId))
                return CommandStartOutcome.Rejected(CommandReasonCodes.MissingTarget);

            matchTargetIndex = _objects.FindIndex(o =>
                string.Equals(o.InitialMotion.ObjectId, command.TargetObjectId, StringComparison.Ordinal));
            if (matchTargetIndex < 0)
                return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);
        }

        // Navigate-to-point (engine.navigate-to-point) carries an explicit, authoritative
        // world-coordinate target. Like match-parameter validation, this runs right after
        // UnknownCommandType and before CancelAll/state checks: a parameter-level error is
        // more specific than a module-state error and is published deterministically.
        // Both coordinates are required and must be finite (NaN/±Infinity would poison
        // the deterministic navigation math).
        double? navigateTargetX = null;
        double? navigateTargetY = null;
        string? navPhase = null;
        double? initialEscapeCourse = null;
        double? initialRequiredDistance = null;
        if (command.CommandType == ShipEngineCommandTypes.NavigateToPoint)
        {
            if (command.TargetWorldX is not { } targetWorldX ||
                command.TargetWorldY is not { } targetWorldY ||
                !double.IsFinite(targetWorldX) ||
                !double.IsFinite(targetWorldY))
            {
                return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidTargetCoordinates);
            }

            navigateTargetX = targetWorldX;
            navigateTargetY = targetWorldY;

            // Staged maneuver: determine initial phase based on target proximity.
            long elapsed = Math.Max(0, gameTimeMs - obj.StartGameTimeMs);
            var motion = _motion.Predict(obj.InitialMotion, elapsed);
            int navInertia = moduleType.AngularInertiaDegPerSec ?? 0;
            if (!DeepSpaceSaga.Motion.NavigationWaypointMath.IsTargetSafe(
                    motion.X, motion.Y, motion.Direction, motion.SpeedKmS,
                    targetWorldX, targetWorldY, navInertia))
            {
                return CommandStartOutcome.Rejected(CommandReasonCodes.NavigationTargetTooClose);
            }

            // Determine initial navigation phase based on target proximity.
            if (navInertia > 0 && motion.SpeedKmS > 0)
            {
                double speedUnitsPerSec = motion.SpeedKmS * 10.0;
                double angularVelocity = navInertia * Math.PI / 180.0;
                double turnR = speedUnitsPerSec / angularVelocity;
                double dist = Math.Sqrt(
                    (targetWorldX - motion.X) * (targetWorldX - motion.X) +
                    (targetWorldY - motion.Y) * (targetWorldY - motion.Y));

                if (dist < turnR)
                {
                    // Inside turn radius — check if target is straight ahead.
                    double dx = targetWorldX - motion.X;
                    double dy = targetWorldY - motion.Y;
                    double dirRad = motion.Direction * Math.PI / 180.0;
                    bool ahead = dx * Math.Sin(dirRad) - dy * Math.Cos(dirRad) > 0;
                    double perpDist = Math.Abs(dx * Math.Cos(dirRad) + dy * Math.Sin(dirRad));

                    if (!ahead || perpDist > NavigationWaypointMath.ArrivalEpsilon)
                    {
                        navPhase = "EscapeTurn";
                        double escDeg = Math.Atan2(motion.X - targetWorldX, -(motion.Y - targetWorldY)) * 180.0 / Math.PI;
                        initialEscapeCourse = escDeg < 0 ? escDeg + 360 : escDeg;
                    }
                }
            }
        }

        if (command.CommandType == ShipEngineCommandTypes.CancelAll)
        {
            if (module.ActiveCycle is { } cancelledCycle)
            {
                // §56.5: complete → apply → write CommandResult → write ShipEvent.
                RecordCommandResultFromCycle(cancelledCycle, CommandResultStatus.Cancelled, gameTimeMs,
                    ShipEventReasonCodes.CancelledByCommand);
                RecordShipEvent(command.ObjectId, command.ModuleId,
                    ShipEventTypes.CycleCancelled, ShipEventReasonCodes.CancelledByCommand, gameTimeMs);
            }

            _objects[objectIndex] = UpdateModule(obj, moduleIndex, module => module with { ActiveCycle = null });
            // CancelAll itself succeeded — write its own CommandResult.
            RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs);
            return CommandStartOutcome.Started;
        }

        if (!CanExecuteEngineCommand(module, moduleType))
            return CommandStartOutcome.Rejected(CommandReasonCodes.ModuleUnavailable);

        // Angular inertia anti-spam: a step-turn command arriving sooner than
        // 1 / AngularInertiaDegPerSec seconds after the previous turn is Rejected
        // BEFORE the busy/auto-repeat branches — so a Rejected turn neither defers
        // nor cancels an active cycle. First turn (LastTurnGameTimeMs null) is never
        // blocked. Applies to repeated attempts of deferred commands too, since
        // TryStartEngineCommand runs the same check on the second pass.
        // Until-cancel commands are intentionally NOT blocked: mutual replacement
        // (TurnLeftUntilCancel ↔ TurnRightUntilCancel) and idempotent re-sends are
        // legitimate flows, and auto-repeat steps land ≥ 1000 ms apart — far outside
        // the 250 ms window at 4 deg/sec (decision: operator, 2026-08-10).
        if (IsStepTurnCommand(command.CommandType) &&
            moduleType.AngularInertiaDegPerSec is { } inertia &&
            module.LastTurnGameTimeMs is { } lastTurn &&
            gameTimeMs - lastTurn < MinTurnIntervalMs(inertia))
        {
            return CommandStartOutcome.Rejected(CommandReasonCodes.TurnInertiaBlocked);
        }

        if (module.ActiveCycle is { } activeCycle)
        {
            if (!activeCycle.IsAutoRepeat)
                return CommandStartOutcome.Deferred(CommandReasonCodes.Busy);

            // Same command type — idempotent, continue existing cycle. Navigation is an
            // exception: idempotent only when the world target matches exactly; a
            // navigate command with a DIFFERENT target is not a no-op — it falls through
            // to the cancel-and-replace branch below (AC8: new Ctrl+Click replaces the
            // old trajectory, old cycle Cancelled + ShipEvent).
            if (string.Equals(command.CommandType, activeCycle.CommandType, StringComparison.Ordinal))
            {
                bool sameNavigateTarget = command.CommandType != ShipEngineCommandTypes.NavigateToPoint ||
                                          (command.TargetWorldX == activeCycle.TargetWorldX &&
                                           command.TargetWorldY == activeCycle.TargetWorldY);
                if (sameNavigateTarget)
                {
                    // Idempotent re-send: the cycle continues. Write Executed for the
                    // re-sent command (the original command's final result comes on
                    // completion). This keeps the client informed that the re-send was
                    // accepted without waiting for the next cycle step.
                    RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs);
                    return CommandStartOutcome.Started;
                }
            }

            // Any other engine command implicitly cancels the active periodic
            // (auto-repeat) cycle and falls through to start its own cycle below.
            // §56.5: write CommandResult(Cancelled) for the old cycle, then ShipEvent.
            RecordCommandResultFromCycle(activeCycle, CommandResultStatus.Cancelled, gameTimeMs,
                ShipEventReasonCodes.CancelledByCommand);
            RecordShipEvent(command.ObjectId, command.ModuleId,
                ShipEventTypes.CycleCancelled, ShipEventReasonCodes.CancelledByCommand, gameTimeMs);
        }

        bool isAutoRepeat = IsCyclicEngineCommand(command.CommandType);

        // Capture the target's scalar state at cycle start (§56.9): cycle completion reads
        // ONLY the captured value stored in ActiveCycle, so later target changes or the
        // target disappearing do not affect the result. MatchTargetSpeed captures speed
        // only, MatchTargetCourse captures course only; TargetObjectId is always stored
        // (diagnostics + restore after save/load, §1253-1298).
        string? targetObjectId = null;
        double? capturedTargetSpeedKmS = null;
        double? capturedTargetCourseDegrees = null;
        if (matchTargetIndex is { } targetIndex)
        {
            var target = _objects[targetIndex];
            long targetElapsedMs = Math.Max(0, gameTimeMs - target.StartGameTimeMs);
            var targetMotion = _motion.Predict(target.InitialMotion, targetElapsedMs);
            targetObjectId = command.TargetObjectId;
            if (command.CommandType == ShipEngineCommandTypes.MatchTargetSpeed)
                capturedTargetSpeedKmS = targetMotion.SpeedKmS;
            else
                capturedTargetCourseDegrees = targetMotion.Direction;
        }

        _objects[objectIndex] = UpdateModule(
            obj,
            moduleIndex,
            current => current with
            {
                ActiveCycle = CreateEngineCycle(
                    command.CommandType,
                    gameTimeMs,
                    isAutoRepeat,
                    moduleType,
                    targetObjectId,
                    capturedTargetSpeedKmS,
                    capturedTargetCourseDegrees,
                    commandId: command.CommandId,
                    objectId: command.ObjectId,
                    moduleId: command.ModuleId,
                    targetWorldX: navigateTargetX,
                    targetWorldY: navigateTargetY,
                    navigationPhase: navPhase,
                    navigationEscapeCourseDegrees: initialEscapeCourse,
                    navigationRequiredDepartureDistance: initialRequiredDistance)
            });
        return CommandStartOutcome.Started;
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
               moduleType.AngularInertiaDegPerSec is > 0 &&
               string.Equals(module.PowerState, "On", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(module.OperationalState, "Ready", StringComparison.OrdinalIgnoreCase) &&
               module.StructurePoints > 0;
    }

    private ActiveCycleData CreateEngineCycle(
        string commandType,
        long gameTimeMs,
        bool isAutoRepeat,
        ModuleTypeDefinition moduleType,
        string? targetObjectId = null,
        double? capturedTargetSpeedKmS = null,
        double? capturedTargetCourseDegrees = null,
        string? commandId = null,
        string? objectId = null,
        string? moduleId = null,
        double? targetWorldX = null,
        double? targetWorldY = null,
        string? navigationPhase = null,
        double? navigationEscapeCourseDegrees = null,
        double? navigationRequiredDepartureDistance = null)
    {
        string cycleId = $"CYC-ENGINE-{++_nextEngineCycleId:D6}";
        long durationMs = commandType == ShipEngineCommandTypes.NavigateToPoint &&
                          moduleType.AngularInertiaDegPerSec is { } inertia
            ? MinTurnIntervalMs(inertia)
            : ComputeEffectiveCycleTimeMs(moduleType, commandType);
        return new ActiveCycleData(
            cycleId,
            gameTimeMs,
            durationMs,
            commandType,
            isAutoRepeat,
            targetObjectId,
            capturedTargetSpeedKmS,
            capturedTargetCourseDegrees,
            commandId,
            objectId,
            moduleId,
            targetWorldX,
            targetWorldY,
            NavigationPhase: navigationPhase,
            NavigationEscapeCourseDegrees: navigationEscapeCourseDegrees,
            NavigationRequiredDepartureDistance: navigationRequiredDepartureDistance);
    }

    /// <summary>
    /// Compute the effective cycle duration from the module type's base cycle time
    /// and the command definition's time factor (§56.3).
    /// EffectiveCycleTimeMs = Ceil(BaseCycleTimeMs * TimeFactor / 1000).
    /// </summary>
    private long ComputeEffectiveCycleTimeMs(ModuleTypeDefinition moduleType, string commandType)
    {
        int commandIndex = _registry.CommandDefinitions.GetIndex(commandType);
        var commandDef = _registry.CommandDefinitions.GetDefinition(commandIndex);
        long numerator = moduleType.BaseCycleTimeMs * commandDef.TimeFactor;
        return (numerator + CommandDefinition.Neutral - 1) / CommandDefinition.Neutral;
    }

    private static bool IsUntilCancelTurn(string commandType)
    {
        return commandType == ShipEngineCommandTypes.TurnLeftUntilCancel ||
               commandType == ShipEngineCommandTypes.TurnRightUntilCancel;
    }

    private static bool IsStepTurnCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.TurnLeftStep ||
               commandType == ShipEngineCommandTypes.TurnRightStep;
    }

    /// <summary>
    /// Minimum interval between two turns enforced by angular inertia:
    /// Ceil(1000 / AngularInertiaDegPerSec) milliseconds (integer ceil).
    /// E.g. 4 deg/sec → 250 ms, 1 deg/sec → 1000 ms.
    /// </summary>
    private static long MinTurnIntervalMs(int inertiaDegPerSec)
    {
        return (1000 + inertiaDegPerSec - 1) / inertiaDegPerSec;
    }

    private static bool IsCyclicEngineCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.Accelerate ||
               commandType == ShipEngineCommandTypes.Brake ||
               IsUntilCancelTurn(commandType) ||
               commandType == ShipEngineCommandTypes.NavigateToPoint;
    }

    private static bool IsMatchEngineCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.MatchTargetSpeed ||
               commandType == ShipEngineCommandTypes.MatchTargetCourse;
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

            if (cycle.CommandType == ShipEngineCommandTypes.NavigateToPoint)
            {
                // Navigation cycles report their authoritative world target and the
                // module's angular inertia — the client-side trajectory projector uses
                // these to draw the future path without any engine math of its own.
                long remainingMs = Math.Max(1, cycle.StartedGameTimeMs + cycle.DurationMs - gameTimeMs);
                return new ActiveEngineCycleMotion(
                    cycle.CommandType,
                    Math.Abs(moduleType.TurnStepDegrees!.Value),
                    remainingMs,
                    cycle.DurationMs,
                    cycle.TargetWorldX,
                    cycle.TargetWorldY,
                    moduleType.AngularInertiaDegPerSec ?? 0,
                    NavigationLockedCourseDegrees: cycle.NavigationLockedCourseDegrees,
                    NavigationPhase: cycle.NavigationPhase,
                    NavigationEscapeCourseDegrees: cycle.NavigationEscapeCourseDegrees,
                    NavigationRequiredDepartureDistance: cycle.NavigationRequiredDepartureDistance);
            }

            if (!IsUntilCancelTurn(cycle.CommandType))
                return new ActiveEngineCycleMotion(cycle.CommandType, 0, 0, 0);

            int turnSign = cycle.CommandType == ShipEngineCommandTypes.TurnLeftUntilCancel ? -1 : 1;
            long turnRemainingMs = Math.Max(1, cycle.StartedGameTimeMs + cycle.DurationMs - gameTimeMs);
            return new ActiveEngineCycleMotion(
                cycle.CommandType,
                turnSign * moduleType.TurnStepDegrees!.Value,
                turnRemainingMs,
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
                        // Derive interruption reason from module state before clearing the cycle.
                        string? interruptReason = module.StructurePoints <= 0
                            ? ShipEventReasonCodes.ModuleDestroyed
                            : !string.Equals(module.PowerState, "On", StringComparison.Ordinal)
                                ? ShipEventReasonCodes.PowerOff
                                : !string.Equals(module.OperationalState, "Ready", StringComparison.Ordinal)
                                    ? ShipEventReasonCodes.ModuleDisabled
                                    : ShipEventReasonCodes.IncompatibleState;

                        // §56.5: write CommandResult(Cancelled) → write ShipEvent.
                        RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, gameTimeMs, interruptReason);
                        _objects[objectIndex] = UpdateModule(obj, moduleIndex, current => current with { ActiveCycle = null });
                        obj = _objects[objectIndex];
                        RecordShipEvent(obj.InitialMotion.ObjectId, module.ModuleId,
                            ShipEventTypes.CycleInterrupted, interruptReason, gameTimeMs);
                        break;
                    }

                    long completionGameTimeMs = cycle.DurationMs == 0
                        ? gameTimeMs
                        : cycle.StartedGameTimeMs + cycle.DurationMs;
                    ActiveCycleData? nextCycle = cycle.IsAutoRepeat
                        ? CreateEngineCycle(cycle.CommandType, completionGameTimeMs, isAutoRepeat: true, moduleType,
                            commandId: cycle.CommandId, objectId: cycle.ObjectId, moduleId: cycle.ModuleId,
                            targetWorldX: cycle.TargetWorldX, targetWorldY: cycle.TargetWorldY,
                            navigationPhase: cycle.NavigationPhase,
                            navigationEscapeCourseDegrees: cycle.NavigationEscapeCourseDegrees,
                            navigationRequiredDepartureDistance: cycle.NavigationRequiredDepartureDistance)
                        : null;
                    _objects[objectIndex] = ApplyCompletedEngineCommand(
                        obj,
                        moduleIndex,
                        moduleType,
                        cycle,
                        completionGameTimeMs,
                        nextCycle);
                    obj = _objects[objectIndex];

                    // §56.5: write CommandResult(Executed) → write ShipEvent(CommandCompleted).
                    RecordCommandResultFromCycle(cycle, CommandResultStatus.Executed, completionGameTimeMs);
                    RecordShipEvent(obj.InitialMotion.ObjectId, module.ModuleId,
                        ShipEventTypes.CommandCompleted, reasonCode: null, completionGameTimeMs);

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
        ActiveCycleData cycle,
        long gameTimeMs,
        ActiveCycleData? nextCycle)
    {
        return cycle.CommandType switch
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

            // Match cycles (§56.9) complete using ONLY the scalar captured at cycle start —
            // later target changes or the target disappearing do not affect the result.
            // MatchTargetSpeed changes only the scalar speed (course untouched),
            // MatchTargetCourse changes only the course (speed untouched).
            // The captured value may exceed the ship's own MaxSpeedKmS (it came from a real
            // object), so speed is clamped the same way Accelerate clamps.
            // Legacy-save guard: a match cycle loaded from a save written by older code may
            // have a null captured field (match commands then passed without a target) —
            // such a cycle completes as a no-op instead of throwing.
            ShipEngineCommandTypes.MatchTargetSpeed when cycle.CapturedTargetSpeedKmS is { } capturedSpeedKmS => UpdateEngineMotion(
                obj,
                moduleIndex,
                gameTimeMs,
                module => module with { ActiveCycle = nextCycle },
                motion => motion with
                {
                    SpeedKmS = Math.Min(moduleType.MaxSpeedMps!.Value / 1000.0, capturedSpeedKmS)
                }),

            ShipEngineCommandTypes.MatchTargetCourse when cycle.CapturedTargetCourseDegrees is { } capturedCourseDegrees => UpdateEngineMotion(
                obj,
                moduleIndex,
                gameTimeMs,
                module => module with { ActiveCycle = nextCycle },
                motion => motion with
                {
                    Direction = NormalizeDirection(capturedCourseDegrees)
                }),

            // Navigation (engine.navigate-to-point): one discrete turn step per cycle
            // (DurationMs = MinTurnIntervalMs, so ~250 ms per step at 4 deg/sec), heading
            // for the authoritative world target. On arrival the auto-repeat chain is cut
            // (nextCycle = null) — the cycle completes normally, writing CommandResult(Executed)
            // + ShipEvent(CommandCompleted) in CompleteActiveEngineCycles. Legacy-save guard:
            // a navigation cycle loaded from a save written by older code may have null
            // target coordinates — such a cycle completes as a no-op instead of throwing.
            ShipEngineCommandTypes.NavigateToPoint when cycle.TargetWorldX is { } targetX && cycle.TargetWorldY is { } targetY =>
                ApplyNavigationStep(obj, moduleIndex, moduleType, targetX, targetY, gameTimeMs, nextCycle),

            ShipEngineCommandTypes.NavigateToPoint => obj,

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
            module => module with { ActiveCycle = nextCycle, LastTurnGameTimeMs = gameTimeMs },
            motion => motion with
            {
                Direction = NormalizeDirection(motion.Direction + turnSign * moduleType.TurnStepDegrees!.Value)
            });
    }

    /// <summary>
    /// Apply one discrete navigation step toward an authoritative world target
    /// (engine.navigate-to-point). Uses the same roll-then-apply shape as every other
    /// cycle completion (via <see cref="UpdateEngineMotion"/>): the ship's motion is
    /// rolled forward to the completion time, then NavigationWaypointMath decides the
    /// turn delta for THIS step. When the ship has arrived (within ArrivalEpsilon, or
    /// course-locked with the target dead ahead), nextCycle is dropped — the auto-repeat
    /// chain stops and the cycle completes.
    /// </summary>
    private SpaceObjectRuntime ApplyNavigationStep(
        SpaceObjectRuntime obj,
        int moduleIndex,
        ModuleTypeDefinition moduleType,
        double targetX,
        double targetY,
        long gameTimeMs,
        ActiveCycleData? nextCycle)
    {
        var cycle = obj.Modules[moduleIndex].ActiveCycle;
        long elapsedMs = Math.Max(0, gameTimeMs - obj.StartGameTimeMs);
        var durationMs = nextCycle?.DurationMs ?? 250;
        var currentMotion = _motion.Predict(obj.InitialMotion, elapsedMs);

        // Check segment arrival (pass-through detection).
        long prevElapsedMs = Math.Max(0, elapsedMs - durationMs);
        var prevMotion = prevElapsedMs == elapsedMs
            ? obj.InitialMotion
            : _motion.Predict(obj.InitialMotion, prevElapsedMs);
        var segmentArrival = DeepSpaceSaga.Motion.NavigationWaypointMath.CheckSegmentArrival(
            prevMotion.X, prevMotion.Y,
            currentMotion.X, currentMotion.Y,
            targetX, targetY);

        if (segmentArrival.IsArrived)
        {
            return UpdateEngineMotion(
                obj, moduleIndex, gameTimeMs,
                module => module with { ActiveCycle = null },
                motion => motion);
        }

        // Staged navigation: use StagedStep for EscapeTurn / EscapeDepart / Approach.
        var stepTimeMs = MinTurnIntervalMs(moduleType.AngularInertiaDegPerSec!.Value);
        var staged = NavigationWaypointMath.StagedStep(
            currentMotion.X, currentMotion.Y,
            currentMotion.Direction, currentMotion.SpeedKmS,
            targetX, targetY,
            moduleType.TurnStepDegrees!.Value,
            moduleType.AngularInertiaDegPerSec!.Value,
            stepTimeMs,
            phase: cycle?.NavigationPhase,
            lockedCourseDegrees: cycle?.NavigationLockedCourseDegrees,
            escapeCourseDegrees: cycle?.NavigationEscapeCourseDegrees,
            requiredDepartureDistance: cycle?.NavigationRequiredDepartureDistance);

        if (staged.IsArrived)
            nextCycle = null;
        else if (nextCycle is not null)
        {
            bool enteringApproach = string.Equals(
                staged.NextNavigationPhase, "Approach", StringComparison.Ordinal);
            nextCycle = nextCycle with
            {
                NavigationLockedCourseDegrees = enteringApproach
                    ? null
                    : staged.LockedCourseDegrees ?? nextCycle.NavigationLockedCourseDegrees,
                NavigationPhase = staged.NextNavigationPhase ?? nextCycle.NavigationPhase,
                NavigationEscapeCourseDegrees = staged.EscapeCourseDegrees ?? nextCycle.NavigationEscapeCourseDegrees,
                NavigationRequiredDepartureDistance = staged.RequiredDepartureDistance ?? nextCycle.NavigationRequiredDepartureDistance,
            };
        }

        return UpdateEngineMotion(
            obj,
            moduleIndex,
            gameTimeMs,
            module => module with
            {
                ActiveCycle = nextCycle,
                LastTurnGameTimeMs = staged.TurnDeltaDegrees != 0 ? gameTimeMs : module.LastTurnGameTimeMs
            },
            motion => staged.TurnDeltaDegrees != 0
                ? motion with { Direction = NormalizeDirection(motion.Direction + staged.TurnDeltaDegrees) }
                : motion);
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
    ImmutableArray<CargoStackRuntime> Cargo,
    long FuelAmountKg = 0,
    long? LastTurnGameTimeMs = null);

internal sealed record CargoStackRuntime(
    int ItemTypeIndex,
    long Quantity);

internal readonly record struct ActiveEngineCycleMotion(
    string? CommandType,
    int TurnStepDegrees,
    long TurnStepRemainingMs,
    long TurnStepIntervalMs,
    double? NavigationTargetX = null,
    double? NavigationTargetY = null,
    int NavigationAngularInertiaDegPerSec = 0,
    double? NavigationLockedCourseDegrees = null,
    string? NavigationPhase = null,
    double? NavigationEscapeCourseDegrees = null,
    double? NavigationRequiredDepartureDistance = null);

internal enum CommandStartDisposition
{
    Started,
    Deferred,
    Rejected
}

/// <summary>
/// Internal result of <see cref="SimulationEngine"/> command start: disposition plus
/// a machine-readable reason code for non-started commands (snake_case, §56.6 style).
/// </summary>
internal readonly record struct CommandStartOutcome(
    CommandStartDisposition Disposition,
    string? ReasonCode)
{
    public static CommandStartOutcome Started => new(CommandStartDisposition.Started, null);

    public static CommandStartOutcome Deferred(string reasonCode) => new(CommandStartDisposition.Deferred, reasonCode);

    public static CommandStartOutcome Rejected(string reasonCode) => new(CommandStartDisposition.Rejected, reasonCode);
}
