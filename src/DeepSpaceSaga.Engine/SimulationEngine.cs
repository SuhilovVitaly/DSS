using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
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

    /// <summary>
    /// Player's Credits balance (Docs\FirstRelease\Mechanics\Money.md). Never negative.
    /// A New Game player starts at 0 (plain default — never RNG-generated). Set by
    /// LoadScenario and projected back into GameStateData.PlayerCredits by
    /// CaptureSaveStateCore.
    /// </summary>
    public long PlayerCredits { get; private set; }

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

    /// <summary>
    /// Bootstrap a new engine from an explicitly chosen scenario file (the New Game -&gt;
    /// scenario picker path) instead of settings' defaultScenario. Mirrors
    /// CreateFromSettingsFile's New Game semantics (gameTimeMs must be 0) but reads the
    /// scenario from an arbitrary path rather than settings.DefaultScenario.
    /// </summary>
    public static SimulationEngine CreateFromScenarioFile(string settingsPath, string scenarioPath)
    {
        return EngineContentLoader.CreateEngineFromScenarioFile(settingsPath, scenarioPath);
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

        // masterSeed: reuse whatever the scenario/save already carries (continuing a
        // session must not reshuffle it). A missing value covers two distinct cases the
        // caller tells apart by context, not by this flag alone — New Game's
        // DefaultScenario never specifies one (expected, no warning warranted), while a
        // legacy save missing it is unexpected (callers loading from a save file are
        // expected to check MasterSeedWasMissingOnLoad and warn). Resolved here, before the
        // object-building loop below, because station Credits/PriceCoefficient/Inventory
        // generation (ResolveStation*) needs a masterSeed to deterministically seed from —
        // it must not be deferred until the later lock block.
        ulong resolvedMasterSeed;
        bool resolvedMasterSeedWasMissingOnLoad;
        if (gs.MasterSeed is { } masterSeedFromScenario)
        {
            resolvedMasterSeed = masterSeedFromScenario;
            resolvedMasterSeedWasMissingOnLoad = false;
        }
        else
        {
            resolvedMasterSeed = GenerateRandomMasterSeed();
            resolvedMasterSeedWasMissingOnLoad = true;
        }

        // Build the full runtime world before mutating engine state. A scenario with
        // invalid type references or placement must not destroy the currently loaded world.
        foreach (var obj in gs.SpaceObjects)
        {
            // Convert m/s to km/s for the existing motion system
            double speedKmS = (double)obj.SpeedMps / 1000.0;

            var modules = BuildRuntimeModules(obj);

            bool isStation = obj.ObjectType == SpaceObjectType.Station;
            long credits = isStation ? ResolveStationCredits(obj, resolvedMasterSeed) : 0;
            int priceCoefficient = isStation ? ResolveStationPriceCoefficient(obj, resolvedMasterSeed) : 1000;
            var inventory = isStation
                ? ResolveStationInventory(obj, resolvedMasterSeed)
                : ImmutableArray<StationInventoryItemRuntime>.Empty;
            var stationSize = isStation ? ResolveStationSize(obj) : StationSize.Medium;
            var producingModules = isStation
                ? ResolveStationProducingModules(obj)
                : ImmutableArray<StationProducingModuleRuntime>.Empty;
            var events = isStation
                ? ResolveStationEvents(obj)
                : ImmutableArray<StationEventRuntime>.Empty;

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
                IsKnown: obj.IsKnown,
                HullLayout: obj.HullLayout,
                IsDocked: obj.IsDocked,
                DockedStationObjectId: obj.DockedStationObjectId,
                Credits: credits,
                PriceCoefficient: priceCoefficient,
                Inventory: inventory,
                StationSize: stationSize,
                ProducingModules: producingModules,
                Events: events));
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

            // Assign the masterSeed resolved above (before the object-building loop) —
            // not recomputed here, so this stays a pure assignment of the single value
            // already used to seed station generation.
            MasterSeed = resolvedMasterSeed;
            MasterSeedWasMissingOnLoad = resolvedMasterSeedWasMissingOnLoad;

            // Player Credits (Docs\FirstRelease\Mechanics\Money.md): a New Game player
            // always starts with 0 — plain default, never randomized (unlike station
            // Credits, which the docs explicitly call out as RNG-generated).
            PlayerCredits = gs.PlayerCredits ?? 0;

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
                    NavigationTargetSpeedKmS = cycleMotion.NavigationTargetSpeedKmS,
                    NavigationTargetDirectionDegrees = cycleMotion.NavigationTargetDirectionDegrees,
                    NavigationApproachTrailDistanceWorldUnits = cycleMotion.NavigationApproachTrailDistanceWorldUnits,
                    ObjectType = known ? obj.ObjectType : null,
                    RenderObjectType = known ? obj.ObjectType : SpaceObjectType.UnknownSpaceObject,
                    RelationToPlayer = known ? GetRelationToPlayer(obj.InitialMotion.ObjectId, obj.ObjectType) : null,
                    DisplayName = known ? obj.Name : null,
                    MaxSpeedKmS = GetMaxSpeedKmS(obj),
                    IsDocked = obj.IsDocked,
                    DockedStationObjectId = obj.DockedStationObjectId
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
                SelectedObjectId: SelectedObjectId,
                PlayerCredits: PlayerCredits,
                DockedStationTrade: BuildDockedStationTradeProjection());
        }
    }

    /// <summary>
    /// Build the docked station's tradeable inventory projection (Docs\FirstRelease\
    /// Mechanics\{Money,StationInventory,Trading}.md), story-20260822-193700 Batch 4.
    /// Non-null only while the player ship is actually docked to a station.
    /// The station's raw <see cref="SpaceObjectRuntime.Credits"/> balance is never
    /// serialized here — only the derived <see cref="StationInventoryItemSnapshot.
    /// MaxSellableQuantity"/> (see CP-1 in the story), so the client never learns the
    /// station's hidden balance.
    /// </summary>
    private StationTradeSnapshot? BuildDockedStationTradeProjection()
    {
        if (string.IsNullOrWhiteSpace(PlayerShipObjectId))
            return null;

        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || !ship.IsDocked || ship.DockedStationObjectId is null)
            return null;

        var station = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == ship.DockedStationObjectId);
        if (station is null || station.Inventory.IsDefaultOrEmpty)
            return null;

        var items = ImmutableArray.CreateBuilder<StationInventoryItemSnapshot>(station.Inventory.Length);
        foreach (var item in station.Inventory)
        {
            var itemType = _registry.ItemTypes.GetDefinition(item.ItemTypeIndex);
            // Story-20260825-084409 Batch 2 (U5/U7/U8): station-size factor (by category and
            // ConsumedResource status) plus any applicable station-event factors. Legacy
            // station.PriceCoefficient still never participates in this formula (§59).
            var factors = ResolveStationPriceFactors(station, item.ItemTypeIndex, itemType);
            long unitPrice = StationPricing.ComputeUnitPriceCredits(itemType.BasePriceCredits ?? 0, factors);
            long rawMaxSellable = unitPrice > 0 ? station.Credits / unitPrice : 0;
            // MaxSellableQuantity must floor to whole sell packages (§59) so the UI hint never
            // promises a quantity the authoritative Sell package-validation (U9) would reject.
            long packageSizeKg = SellPackageSizeKg(itemType.Category);
            long maxSellable = rawMaxSellable / packageSizeKg * packageSizeKg;

            items.Add(new StationInventoryItemSnapshot(
                ItemTypeId: itemType.TypeId,
                StockQuantity: item.StockQuantity,
                UnitPriceCredits: unitPrice,
                MaxSellableQuantity: maxSellable,
                Category: ToTradeItemCategory(itemType.Category)));
        }

        return new StationTradeSnapshot(station.InitialMotion.ObjectId, items.MoveToImmutable());
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
                CommandTypeIds: moduleType.CommandTypeIds,
                PowerState: module.PowerState,
                OperationalState: module.OperationalState,
                StructurePoints: module.StructurePoints,
                ActiveCommandType: module.ActiveCycle?.CommandType,
                FuelAmountKg: moduleType.FuelCapacityKg is > 0 ? module.FuelAmountKg : null,
                Commands: BuildModuleCommands(moduleType.CommandTypeIds),
                Cargo: BuildCargoProjection(module.Cargo)));
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    /// Project a module's cargo stacks (item type index -> item type id) for the
    /// <see cref="InstalledModuleSnapshot.Cargo"/> field (story-20260822-193700 Batch 4).
    /// Empty for modules that carry no cargo — mirrors the module.Cargo shape as-is,
    /// no cross-module aggregation (that's a Client concern).
    /// </summary>
    private ImmutableArray<CargoStackSnapshot> BuildCargoProjection(ImmutableArray<CargoStackRuntime> cargo)
    {
        if (cargo.IsDefaultOrEmpty)
            return ImmutableArray<CargoStackSnapshot>.Empty;

        var builder = ImmutableArray.CreateBuilder<CargoStackSnapshot>(cargo.Length);
        foreach (var stack in cargo)
            builder.Add(new CargoStackSnapshot(_registry.ItemTypes.GetDefinition(stack.ItemTypeIndex).TypeId, stack.Quantity));

        return builder.MoveToImmutable();
    }

    /// <summary>
    /// Per-module command metadata (display name + target requirement) for the
    /// Commands Panel UI. One entry per CommandTypeId, in declaration order, filled
    /// from the command definitions registry. Every commandTypeId is guaranteed to
    /// resolve: GameDataRegistry.Create rejects module types that reference an
    /// unknown command definition.
    /// </summary>
    private ImmutableArray<ModuleCommandSnapshot> BuildModuleCommands(ImmutableArray<string> commandTypeIds)
    {
        if (commandTypeIds.IsDefaultOrEmpty)
            return ImmutableArray<ModuleCommandSnapshot>.Empty;

        var builder = ImmutableArray.CreateBuilder<ModuleCommandSnapshot>(commandTypeIds.Length);
        foreach (string commandTypeId in commandTypeIds)
        {
            var commandDef = _registry.CommandDefinitions.GetDefinition(
                _registry.CommandDefinitions.GetIndex(commandTypeId));
            builder.Add(new ModuleCommandSnapshot(commandTypeId, commandDef.DisplayName, commandDef.Target));
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
            bool isStation = obj.ObjectType == SpaceObjectType.Station;

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
                IsKnown: obj.IsKnown,
                HullLayout: obj.HullLayout,
                IsDocked: obj.IsDocked,
                DockedStationObjectId: obj.DockedStationObjectId,
                Credits: isStation ? obj.Credits : null,
                PriceCoefficient: isStation ? obj.PriceCoefficient : null,
                Inventory: isStation && !obj.Inventory.IsDefaultOrEmpty
                    ? obj.Inventory.Select(BuildSaveInventoryItem).ToList()
                    : null,
                StationSize: isStation ? obj.StationSize.ToString() : null,
                ProducingModules: isStation && !obj.ProducingModules.IsDefaultOrEmpty
                    ? obj.ProducingModules.Select(BuildSaveProducingModule).ToList()
                    : null,
                Events: isStation && !obj.Events.IsDefaultOrEmpty
                    ? obj.Events.Select(BuildSaveEvent).ToList()
                    : null));
        }

        var gameState = new GameStateData(
            GameTimeMs: gameTimeMs,
            CurrentSpeed: clockState.Speed.ToString(),
            PlayerShipObjectId: PlayerShipObjectId ?? string.Empty,
            Focus: null, // camera/focus is client-side only — never saved (decision G.20)
            SpaceObjects: spaceObjects,
            MasterSeed: MasterSeed,
            PlayerCredits: PlayerCredits);

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
                OccupiedCells: module.OccupiedCells
                    .Select(cell => new HullCellCoordinate(cell.X, cell.Y))
                    .ToArray(),
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

    private StationInventoryItemData BuildSaveInventoryItem(StationInventoryItemRuntime item)
    {
        var itemType = _registry.ItemTypes.GetDefinition(item.ItemTypeIndex);
        return new StationInventoryItemData(ItemTypeId: itemType.TypeId, Quantity: item.StockQuantity);
    }

    private StationProducingModuleData BuildSaveProducingModule(StationProducingModuleRuntime module)
    {
        var factoryType = _registry.FactoryTypes.GetDefinition(module.FactoryTypeIndex);
        return new StationProducingModuleData(
            ProducingModuleTypeId: factoryType.TypeId,
            Active: module.Active);
    }

    private StationEventData BuildSaveEvent(StationEventRuntime evt)
    {
        return new StationEventData(
            EventId: evt.EventId,
            DisplayName: evt.DisplayName,
            Description: evt.Description,
            StartedGameTimeMs: evt.StartedGameTimeMs,
            DurationMs: evt.DurationMs,
            PriceFactors: evt.PriceFactors.Select(BuildSaveEventPriceFactor).ToList());
    }

    private StationEventPriceFactorData BuildSaveEventPriceFactor(StationEventPriceFactorRuntime factor)
    {
        string? itemTypeId = factor.ItemTypeIndex is { } index
            ? _registry.ItemTypes.GetDefinition(index).TypeId
            : null;

        return new StationEventPriceFactorData(
            Category: factor.Category?.ToString(),
            ItemTypeId: itemTypeId,
            Factor: factor.Factor);
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

        // Hull cells occupied so far by any module on this object — checked and filled as
        // one flat set across the whole ship (requirements §57 replaces the old
        // per-platform Dictionary<int,HashSet<int>> model).
        var occupiedHullCells = new HashSet<(int X, int Y)>();

        foreach (var module in obj.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.ModuleId))
                throw new ScenarioException($"Module on '{obj.ObjectId}' has empty moduleId.");
            if (!moduleIds.Add(module.ModuleId))
                throw new ScenarioException($"Duplicate moduleId '{module.ModuleId}' on '{obj.ObjectId}'.");

            int moduleTypeIndex = _registry.ModuleTypes.GetIndex(module.ModuleTypeId);
            var moduleType = _registry.ModuleTypes.GetDefinition(moduleTypeIndex);
            var placedCells = ValidateModulePlacement(obj.ObjectId, obj.HullLayout, module, moduleType, occupiedHullCells);
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
                placedCells,
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

    /// <summary>
    /// Validate a module's hull-grid placement (requirements §57): the occupied cell
    /// count must match the module type's SlotSize, every cell must belong to the
    /// object's HullLayout, and no cell may already be occupied by another module on
    /// the same object. Returns the module's occupied cells as a coordinate tuple array
    /// for the runtime model, and adds them to <paramref name="occupiedHullCells"/> so
    /// subsequent modules on the same object are checked against them too.
    /// </summary>
    private static ImmutableArray<(int X, int Y)> ValidateModulePlacement(
        string objectId,
        HullLayoutData? hullLayout,
        ShipModuleData module,
        ModuleTypeDefinition moduleType,
        HashSet<(int X, int Y)> occupiedHullCells)
    {
        if (module.OccupiedCells.Count != moduleType.SlotSize)
        {
            throw new ScenarioException(
                $"Module '{module.ModuleId}' on '{objectId}' occupies {module.OccupiedCells.Count} cells, " +
                $"but module type '{moduleType.TypeId}' requires {moduleType.SlotSize}.");
        }

        if (hullLayout is null)
        {
            throw new ScenarioException(
                $"Object '{objectId}' has modules but no hullLayout to place them on.");
        }

        var hullCells = new HashSet<(int X, int Y)>(hullLayout.Cells.Select(c => (c.X, c.Y)));

        var placedCells = ImmutableArray.CreateBuilder<(int X, int Y)>(module.OccupiedCells.Count);
        var moduleCells = new HashSet<(int X, int Y)>();
        foreach (var cell in module.OccupiedCells)
        {
            var coordinate = (cell.X, cell.Y);

            if (!moduleCells.Add(coordinate))
            {
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' on '{objectId}' duplicates occupied cell ({cell.X},{cell.Y}).");
            }

            if (!hullCells.Contains(coordinate))
            {
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' on '{objectId}' occupies cell ({cell.X},{cell.Y}) " +
                    "which is outside the object's hull layout.");
            }

            if (!occupiedHullCells.Add(coordinate))
            {
                throw new ScenarioException(
                    $"Module '{module.ModuleId}' on '{objectId}' overlaps occupied cell ({cell.X},{cell.Y}) " +
                    "with another module.");
            }

            placedCells.Add(coordinate);
        }

        return placedCells.MoveToImmutable();
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

    /// <summary>
    /// Resolve a station's Credits balance (Docs\FirstRelease\Mechanics\Money.md): explicit
    /// scenario/save value used as-is, otherwise a deterministic value in 10,000..50,000
    /// (inclusive) derived from masterSeed via the station's own named RNG stream — so
    /// generation never regenerates once the value has been resolved and saved once.
    /// </summary>
    private static long ResolveStationCredits(SpaceObjectData obj, ulong masterSeed)
    {
        if (obj.Credits is { } explicitCredits)
            return explicitCredits;

        var random = RngStreamNames.CreateDeterministicRandom(
            RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, RngStreamNames.StationCredits(obj.ObjectId)));
        // NextInt64(min, max) has an exclusive upper bound — +1 to include 50,000.
        return random.NextInt64(10_000, 50_001);
    }

    /// <summary>
    /// Resolve a station's price coefficient (Docs\FirstRelease\Mechanics\
    /// StationInventory.md): explicit scenario/save value used as-is, otherwise a
    /// deterministic value in 500..2000 (inclusive) — fixed-point representation of the
    /// documented 0.5..2.0 range (1000 == 1.0x; the project forbids float/double for
    /// authoritative values) — derived from masterSeed via the station's own named RNG
    /// stream.
    /// </summary>
    private static int ResolveStationPriceCoefficient(SpaceObjectData obj, ulong masterSeed)
    {
        if (obj.PriceCoefficient is { } explicitCoefficient)
            return explicitCoefficient;

        var random = RngStreamNames.CreateDeterministicRandom(
            RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, RngStreamNames.StationPriceCoefficient(obj.ObjectId)));
        // Next(min, max) has an exclusive upper bound — +1 to include 2000.
        return random.Next(500, 2001);
    }

    /// <summary>
    /// Resolve a station's tradeable stock (Docs\FirstRelease\Mechanics\
    /// StationInventory.md): one entry per registered item type that carries a
    /// BasePriceCredits (i.e. is currently sellable by a station at all). Each entry uses
    /// its explicit scenario/save quantity when present, otherwise a deterministic value in
    /// 20..500 (inclusive) derived from masterSeed via a stream named for this specific
    /// (station, item type) pair — not one shared stream for the whole station's
    /// inventory — so a future tradeable good never shifts the sequence already consumed
    /// by an existing one for the same station.
    /// </summary>
    private ImmutableArray<StationInventoryItemRuntime> ResolveStationInventory(SpaceObjectData obj, ulong masterSeed)
    {
        if (_registry.ItemTypes.Count == 0)
            return ImmutableArray<StationInventoryItemRuntime>.Empty;

        var explicitByItemTypeId = obj.Inventory?.ToDictionary(
            i => i.ItemTypeId, i => i.Quantity, StringComparer.Ordinal);

        var inventory = ImmutableArray.CreateBuilder<StationInventoryItemRuntime>();
        for (int i = 0; i < _registry.ItemTypes.Count; i++)
        {
            var itemType = _registry.ItemTypes.GetDefinition(i);
            if (itemType.BasePriceCredits is null)
                continue;

            long stockQuantity;
            if (explicitByItemTypeId is not null && explicitByItemTypeId.TryGetValue(itemType.TypeId, out long explicitQuantity))
            {
                stockQuantity = explicitQuantity;
            }
            else
            {
                var random = RngStreamNames.CreateDeterministicRandom(
                    RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, RngStreamNames.StationInventory(obj.ObjectId, itemType.TypeId)));
                // NextInt64(min, max) has an exclusive upper bound — +1 to include 500.
                stockQuantity = random.NextInt64(20, 501);
            }

            inventory.Add(new StationInventoryItemRuntime(i, stockQuantity));
        }

        return inventory.ToImmutable();
    }

    /// <summary>
    /// Resolve a station's size (requirements §59, Docs\FirstRelease\TechnicalTasks\
    /// StationEconomyProductionAndSizing.md "Размеры станции"): explicit scenario/save value
    /// used as-is, otherwise a fixed fallback — <b>never</b> RNG-generated (unlike
    /// Credits/PriceCoefficient/Inventory above; story-20260825-084409 Batch 2 explicitly
    /// forbids randomizing StationSize).
    /// </summary>
    /// <remarks>
    /// Fallback choice: <see cref="StationSize.Medium"/>. §59 defines StationSize only as a
    /// per-station property and gives no guidance for a station whose scenario/save omits it
    /// (every real station the game ships an explicit size for — SPC-0002 gets "Large" per
    /// the acceptance criteria — this fallback only matters for content/test fixtures that
    /// predate the field). Medium sits in the middle of both coefficient tables it drives
    /// (§59 "Коэффициенты общих ресурсов" 1.00..1.20 и "Коэффициенты товаров" 1.00..1.20) —
    /// it is deliberately not the cheapest (Outpost) nor the most expensive (Huge/Outpost,
    /// depending on category) extreme in either table, making it the least-surprising
    /// "typical station" default absent other information.
    /// </remarks>
    private static StationSize ResolveStationSize(SpaceObjectData obj)
    {
        if (obj.StationSize is { } explicitValue)
        {
            if (!Enum.TryParse<StationSize>(explicitValue, ignoreCase: true, out var parsed))
            {
                throw new ScenarioException(
                    $"Station '{obj.ObjectId}' has unknown stationSize '{explicitValue}' " +
                    "(expected Huge, Large, Medium, or Outpost).");
            }

            return parsed;
        }

        return StationSize.Medium;
    }

    /// <summary>
    /// Resolve a station's producing-module instances (§59 "Производящие модули станции"):
    /// fully explicit, never RNG-generated. Missing/empty scenario data resolves to an empty
    /// list (the common case — most stations have no producing modules).
    /// </summary>
    private ImmutableArray<StationProducingModuleRuntime> ResolveStationProducingModules(SpaceObjectData obj)
    {
        if (obj.ProducingModules is not { Count: > 0 })
            return ImmutableArray<StationProducingModuleRuntime>.Empty;

        var modules = ImmutableArray.CreateBuilder<StationProducingModuleRuntime>(obj.ProducingModules.Count);
        foreach (var module in obj.ProducingModules)
        {
            // Unknown factory type id surfaces as ContentException — same convention as
            // BuildRuntimeModules' module.ModuleTypeId -> _registry.ModuleTypes.GetIndex.
            int factoryTypeIndex = _registry.FactoryTypes.GetIndex(module.ProducingModuleTypeId);
            modules.Add(new StationProducingModuleRuntime(factoryTypeIndex, module.Active));
        }

        return modules.ToImmutable();
    }

    /// <summary>
    /// Resolve a station's active events/buffs/debuffs (§59 "События, бафы и дебафы
    /// станции") — schema + persistence only (story-20260825-084409 CP-3): no scenario ever
    /// populates this today, so the common path is always the empty-list fast return, but a
    /// synthetic test/save station carrying explicit events round-trips them faithfully.
    /// </summary>
    private ImmutableArray<StationEventRuntime> ResolveStationEvents(SpaceObjectData obj)
    {
        if (obj.Events is not { Count: > 0 })
            return ImmutableArray<StationEventRuntime>.Empty;

        var events = ImmutableArray.CreateBuilder<StationEventRuntime>(obj.Events.Count);
        foreach (var evt in obj.Events)
        {
            if (string.IsNullOrWhiteSpace(evt.EventId))
                throw new ScenarioException($"Station '{obj.ObjectId}' has an event with empty eventId.");

            // A missing/null "priceFactors" array is tolerated as "this event contributes no
            // price factors" rather than a hard load error — schema-only (CP-3), so an event
            // that exists purely for its display name/description (no price effect yet) is a
            // legitimate authoring choice, not malformed data.
            var priceFactorsData = evt.PriceFactors ?? Array.Empty<StationEventPriceFactorData>();
            var factors = ImmutableArray.CreateBuilder<StationEventPriceFactorRuntime>(priceFactorsData.Count);
            foreach (var factor in priceFactorsData)
            {
                TradeCategory? category = null;
                if (factor.Category is { } categoryValue)
                {
                    if (!Enum.TryParse<TradeCategory>(categoryValue, ignoreCase: true, out var parsedCategory))
                    {
                        throw new ScenarioException(
                            $"Station '{obj.ObjectId}' event '{evt.EventId}' has unknown price factor " +
                            $"category '{categoryValue}' (expected Resource or Good).");
                    }

                    category = parsedCategory;
                }

                // Unknown itemTypeId surfaces as ContentException — same convention as every
                // other type-registry lookup in this file.
                int? itemTypeIndex = factor.ItemTypeId is { } itemTypeId
                    ? _registry.ItemTypes.GetIndex(itemTypeId)
                    : null;

                factors.Add(new StationEventPriceFactorRuntime(category, itemTypeIndex, factor.Factor));
            }

            events.Add(new StationEventRuntime(
                evt.EventId, evt.DisplayName, evt.Description, evt.StartedGameTimeMs, evt.DurationMs,
                factors.ToImmutable()));
        }

        return events.ToImmutable();
    }

    /// <summary>
    /// True when <paramref name="itemTypeId"/> is a <see cref="TradeCategory.Resource"/> input
    /// of at least one of the station's currently active producing modules (§59 "Минимальное
    /// правило для торговли": "если станция имеет производящий модуль, которому нужен
    /// Resource, этот Resource считается ConsumedResource... если один ресурс нужен нескольким
    /// производящим модулям, для выбора категории достаточно факта потребления хотя бы одним
    /// модулем"). Inactive producing modules (<see cref="StationProducingModuleRuntime.Active"/>
    /// == false) do not make their inputs ConsumedResource.
    /// </summary>
    private bool IsConsumedResource(SpaceObjectRuntime station, string itemTypeId)
    {
        if (station.ProducingModules.IsDefaultOrEmpty)
            return false;

        foreach (var producingModule in station.ProducingModules)
        {
            if (!producingModule.Active)
                continue;

            var factoryType = _registry.FactoryTypes.GetDefinition(producingModule.FactoryTypeIndex);
            if (factoryType.Recipe.Inputs.Any(input => string.Equals(input.ItemTypeId, itemTypeId, StringComparison.Ordinal)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Resolve every applicable <c>StationPriceFactor</c> (§59 "Формула цены" — "Минимальные
    /// применимые факторы: коэффициент размера станции по категории номенклатуры;
    /// коэффициенты station events / buffs / debuffs") for one tradeable item at one station,
    /// in a fixed, deterministic order: the station-size factor first, then each applicable
    /// station-event factor ordered by (StartedGameTimeMs, EventId) (§59 "deterministic
    /// порядок применения при одинаковом времени начала") — though
    /// <see cref="StationPricing.ComputeUnitPriceCredits"/> multiplies in one pass and its
    /// result is provably order-independent regardless, so this ordering is for
    /// determinism/readability, not correctness.
    /// </summary>
    private ImmutableArray<int> ResolveStationPriceFactors(SpaceObjectRuntime station, int itemTypeIndex, ItemTypeDefinition itemType)
    {
        bool isConsumedResource = itemType.Category == TradeCategory.Resource
            && IsConsumedResource(station, itemType.TypeId);

        var factors = ImmutableArray.CreateBuilder<int>();
        factors.Add(StationSizeFactors.Resolve(station.StationSize, itemType.Category, isConsumedResource));

        if (!station.Events.IsDefaultOrEmpty)
        {
            var applicableEventFactors = station.Events
                .OrderBy(e => e.StartedGameTimeMs)
                .ThenBy(e => e.EventId, StringComparer.Ordinal)
                .SelectMany(e => e.PriceFactors)
                .Where(f => StationEventPriceFactorApplies(f, itemTypeIndex, itemType.Category));

            foreach (var factor in applicableEventFactors)
                factors.Add(factor.Factor);
        }

        return factors.ToImmutable();
    }

    /// <summary>
    /// Addressing rule for a <see cref="StationEventPriceFactorRuntime"/> (see
    /// <see cref="StationEventPriceFactorData"/>): a factor with a specific
    /// <see cref="StationEventPriceFactorRuntime.ItemTypeIndex"/> applies only to that item; a
    /// factor with only a <see cref="StationEventPriceFactorRuntime.Category"/> applies to
    /// every item of that category; a factor with neither applies station-wide.
    /// </summary>
    private static bool StationEventPriceFactorApplies(StationEventPriceFactorRuntime factor, int itemTypeIndex, TradeCategory category)
    {
        if (factor.ItemTypeIndex is { } expectedIndex)
            return expectedIndex == itemTypeIndex;

        if (factor.Category is { } expectedCategory)
            return expectedCategory == category;

        return true;
    }

    internal ImmutableArray<SpaceObjectRuntime> RuntimeObjects => _objects.ToImmutableArray();

    /// <summary>
    /// Test seam: remove an object from the world (simulates it disappearing —
    /// destroyed or otherwise no longer resolvable) without going through any real
    /// destruction mechanic, which does not exist yet. Used to exercise navigation.approach's
    /// target-invalid-mid-cycle cancellation path (Checkpoint 3).
    /// </summary>
    internal void RemoveObjectForTests(string objectId)
    {
        lock (_worldStateLock)
        {
            _objects.RemoveAll(o => string.Equals(o.InitialMotion.ObjectId, objectId, StringComparison.Ordinal));
        }
    }

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
            var outcome = TryStartCommand(command, gameTimeMs);
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
                var outcome = TryStartCommand(command, gameTimeMs);
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
        PlayerCommand command, CommandResultStatus status, long gameTimeMs, string? reasonCode = null,
        long? executedQuantity = null)
    {
        _commandResults.Add(new CommandResult(
            command.CommandId,
            command.ObjectId,
            command.ModuleId,
            command.CommandType,
            status,
            gameTimeMs,
            reasonCode,
            executedQuantity));
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

    /// <summary>
    /// Routes a pending command to the handler owned by its module type. Engine module
    /// commands (module.engine.basic) go through the existing, heavily-tested
    /// <see cref="TryStartEngineCommand"/> unchanged; navigation.dock is the only
    /// currently-implemented NavigationComputer command and goes through
    /// <see cref="TryStartNavigationCommand"/> instead; trade.buy/trade.sell/trade.refuel
    /// (station trading, Docs\FirstRelease\Mechanics\{Money,StationInventory,Trading}.md) go
    /// through <see cref="TryStartTradeCommand"/>. Everything else (including
    /// navigation.stationsList, not implemented yet) falls through to
    /// TryStartEngineCommand, which rejects it with UnknownCommandType exactly as before
    /// this dispatcher existed.
    /// </summary>
    private CommandStartOutcome TryStartCommand(PlayerCommand command, long gameTimeMs)
    {
        if (command.CommandType == NavigationComputerCommandTypes.Dock)
            return TryStartNavigationCommand(command, gameTimeMs);

        if (command.CommandType is TradeCommandTypes.Buy or TradeCommandTypes.Sell or TradeCommandTypes.Refuel)
            return TryStartTradeCommand(command, gameTimeMs);

        return TryStartEngineCommand(command, gameTimeMs);
    }

    /// <summary>1 world unit = 100 m (CLAUDE.md motion conventions), so 1 km = 10 world units.</summary>
    private const double WorldUnitsPerKm = 10.0;

    /// <summary>
    /// Fallback range if a Dock command definition omits rangeKm (should not happen with
    /// real content — Data/Commands/NavigationComputer/commands.json always sets it).
    /// Matches the documented first-release default (Docking.md).
    /// </summary>
    private const double DefaultDockRangeKm = 200.0;

    /// <summary>
    /// Handles navigation.dock — the only implemented NavigationComputer command
    /// (requirements Docking.md, Station.md). Unlike Engine module commands, this is an
    /// immediate one-shot authoritative action (no ActiveCycle/duration): it validates the
    /// target station, range, and speed/direction synchronization, then physically snaps
    /// the ship onto the station (position/speed/direction, local offset (1, 1) world units
    /// per the documented old synchronization model) and marks it docked. A proper timed
    /// docking maneuver through the shared ActiveCycle pipeline is deferred — see the
    /// dispatcher's doc comment on why this is a separate method rather than an extension
    /// of TryStartEngineCommand.
    /// </summary>
    private CommandStartOutcome TryStartNavigationCommand(PlayerCommand command, long gameTimeMs)
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
        if (!IsNavigationCommandType(moduleType, command.CommandType))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);

        if (!CanExecuteModuleCommand(module))
            return CommandStartOutcome.Rejected(CommandReasonCodes.ModuleUnavailable);

        if (string.IsNullOrWhiteSpace(command.TargetObjectId))
            return CommandStartOutcome.Rejected(CommandReasonCodes.MissingTarget);

        int targetIndex = _objects.FindIndex(o =>
            string.Equals(o.InitialMotion.ObjectId, command.TargetObjectId, StringComparison.Ordinal));
        if (targetIndex < 0)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);

        var target = _objects[targetIndex];
        if (!string.Equals(target.ObjectType, SpaceObjectType.Station, StringComparison.OrdinalIgnoreCase))
            return CommandStartOutcome.Rejected(CommandReasonCodes.DockTargetNotStation);

        long shipElapsedMs = Math.Max(0, gameTimeMs - obj.StartGameTimeMs);
        var shipMotion = _motion.Predict(obj.InitialMotion, shipElapsedMs);
        long targetElapsedMs = Math.Max(0, gameTimeMs - target.StartGameTimeMs);
        var targetMotion = _motion.Predict(target.InitialMotion, targetElapsedMs);

        double dx = targetMotion.X - shipMotion.X;
        double dy = targetMotion.Y - shipMotion.Y;
        double distanceWorldUnits = Math.Sqrt(dx * dx + dy * dy);

        int commandIndex = _registry.CommandDefinitions.GetIndex(command.CommandType);
        var commandDef = _registry.CommandDefinitions.GetDefinition(commandIndex);
        double rangeWorldUnits = (commandDef.RangeKm ?? DefaultDockRangeKm) * WorldUnitsPerKm;
        if (distanceWorldUnits > rangeWorldUnits)
            return CommandStartOutcome.Rejected(CommandReasonCodes.DockOutOfRange);

        // Synchronization tolerance: floating-point safety margin only, not a gameplay
        // allowance — SpeedSynchronization/DirectionSynchronization capture and apply the
        // target's exact value, so a genuinely synchronized ship matches almost exactly.
        // No direction wraparound handling (e.g. 359.999 vs 0.001): stations are always
        // Stationary in the current content, so this does not arise in practice.
        const double speedEpsilonKmS = 1e-6;
        const double directionEpsilonDeg = 1e-6;
        if (Math.Abs(shipMotion.SpeedKmS - targetMotion.SpeedKmS) > speedEpsilonKmS ||
            Math.Abs(shipMotion.Direction - targetMotion.Direction) > directionEpsilonDeg)
        {
            return CommandStartOutcome.Rejected(CommandReasonCodes.DockNotSynchronized);
        }

        // Physically synchronize with the station: local offset (1, 1) world units per the
        // documented old synchronization model (Docking.md). Re-baseline StartGameTimeMs to
        // the dock moment so future BuildSnapshot/CaptureSaveState predictions compute
        // elapsed time from here, not from session start.
        var dockedMotion = obj.InitialMotion with
        {
            X = targetMotion.X + 1.0,
            Y = targetMotion.Y + 1.0,
            SpeedKmS = targetMotion.SpeedKmS,
            Direction = targetMotion.Direction
        };

        _objects[objectIndex] = obj with
        {
            InitialMotion = dockedMotion,
            StartGameTimeMs = gameTimeMs,
            IsDocked = true,
            DockedStationObjectId = target.InitialMotion.ObjectId
        };

        RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs);
        return CommandStartOutcome.Started;
    }

    private static bool IsNavigationCommandType(ModuleTypeDefinition moduleType, string commandType)
    {
        return string.Equals(moduleType.TypeId, "module.bridge.navigation.computer.basic", StringComparison.Ordinal) &&
               moduleType.CommandTypeIds.Contains(commandType, StringComparer.Ordinal);
    }

    /// <summary>
    /// Generic module-state gate for non-Engine module commands: power/operational/structure
    /// only. Unlike <see cref="CanExecuteEngineCommand"/>, this does NOT check propulsion
    /// parameters (MaxSpeedMps/TurnStepDegrees/inertia) — NavigationComputer (and other
    /// non-Engine module types) never define those.
    /// </summary>
    private static bool CanExecuteModuleCommand(InstalledModuleRuntime module)
    {
        return string.Equals(module.PowerState, "On", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(module.OperationalState, "Ready", StringComparison.OrdinalIgnoreCase) &&
               module.StructurePoints > 0;
    }

    /// <summary>
    /// Handles trade.buy / trade.sell / trade.refuel — station trading (requirements
    /// Docs\FirstRelease\Mechanics\{Money,StationInventory,Trading}.md). Like navigation.dock,
    /// these are immediate one-shot authoritative actions (no ActiveCycle/duration) validated
    /// only by <see cref="CanExecuteModuleCommand"/> (power/operational/structure) — a trade
    /// module is never "busy", so trade commands are always either Started or Rejected, never
    /// Deferred. Buy and Refuel are all-or-nothing (CP-2); only Sell may execute partially, when
    /// the station's hidden Credits balance cannot afford the full request (Money.md) — see
    /// <see cref="CommandResult.ExecutedQuantity"/>.
    /// </summary>
    private CommandStartOutcome TryStartTradeCommand(PlayerCommand command, long gameTimeMs)
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
        // Whether the addressed module supports this trade command type at all — this is also
        // the "right kind of module" check: Buy/Sell land on module.container.basic, Refuel on
        // module.engine.basic, purely through content wiring (Data\Commands\Container,
        // module-types.json), no separate hardcoded module-type check needed.
        if (!moduleType.CommandTypeIds.Contains(command.CommandType, StringComparer.Ordinal))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownCommandType);

        if (!CanExecuteModuleCommand(module))
            return CommandStartOutcome.Rejected(CommandReasonCodes.ModuleUnavailable);

        if (!obj.IsDocked)
            return CommandStartOutcome.Rejected(CommandReasonCodes.NotDocked);

        int stationIndex = _objects.FindIndex(o =>
            string.Equals(o.InitialMotion.ObjectId, obj.DockedStationObjectId, StringComparison.Ordinal));
        if (stationIndex < 0)
            return CommandStartOutcome.Rejected(CommandReasonCodes.NotDocked);

        if (command.Quantity is not { } qty || qty <= 0)
            return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidQuantity);

        if (string.IsNullOrWhiteSpace(command.ItemTypeId) || !_registry.ItemTypes.Contains(command.ItemTypeId))
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownItemType);

        int itemTypeIndex = _registry.ItemTypes.GetIndex(command.ItemTypeId);
        var itemType = _registry.ItemTypes.GetDefinition(itemTypeIndex);

        var station = _objects[stationIndex];
        int stationInventoryIndex = FindInventoryIndex(station.Inventory, itemTypeIndex);
        if (stationInventoryIndex < 0)
            return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownItemType);

        var stationInventoryItem = station.Inventory[stationInventoryIndex];
        // Story-20260825-084409 Batch 2 (U5/U7/U8) — see BuildDockedStationTradeProjection's
        // identical comment. Legacy station.PriceCoefficient no longer participates (§59).
        var priceFactors = ResolveStationPriceFactors(station, itemTypeIndex, itemType);
        long unitPriceCredits = StationPricing.ComputeUnitPriceCredits(itemType.BasePriceCredits ?? 0, priceFactors);

        if (command.CommandType == TradeCommandTypes.Buy)
        {
            long cargoCapacityKg = moduleType.CargoCapacityKg ?? 0;
            long cost = unitPriceCredits * qty;
            if (cost > PlayerCredits)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InsufficientPlayerCredits);

            if (qty > stationInventoryItem.StockQuantity)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InsufficientStationStock);

            long currentCargoMassKg = 0;
            foreach (var stack in module.Cargo)
                currentCargoMassKg += stack.Quantity * _registry.ItemTypes.GetDefinition(stack.ItemTypeIndex).UnitMassKg;

            long addedMassKg = qty * itemType.UnitMassKg;
            if (currentCargoMassKg + addedMassKg > cargoCapacityKg)
                return CommandStartOutcome.Rejected(CommandReasonCodes.CargoCapacityExceeded);

            PlayerCredits -= cost;

            var updatedInventory = station.Inventory.SetItem(stationInventoryIndex,
                stationInventoryItem with { StockQuantity = stationInventoryItem.StockQuantity - qty });
            _objects[stationIndex] = station with { Credits = station.Credits + cost, Inventory = updatedInventory };

            _objects[objectIndex] = UpdateModule(obj, moduleIndex, m =>
            {
                int stackIndex = FindCargoStackIndex(m.Cargo, itemTypeIndex);
                var updatedCargo = stackIndex >= 0
                    ? m.Cargo.SetItem(stackIndex, m.Cargo[stackIndex] with { Quantity = m.Cargo[stackIndex].Quantity + qty })
                    : m.Cargo.Add(new CargoStackRuntime(itemTypeIndex, qty));
                return m with { Cargo = updatedCargo };
            });

            RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs);
            return CommandStartOutcome.Started;
        }

        if (command.CommandType == TradeCommandTypes.Sell)
        {
            // Authoritative package-size validation (§59 "Продажа" / acceptance criteria):
            // Resource sells only in multiples of 100 kg, Good (incl. Fuel — story-20260825-084409
            // decision 3) only in multiples of 10 kg. Checked — and rejected — before any state
            // mutation or cargo-quantity check.
            long sellPackageSizeKg = SellPackageSizeKg(itemType.Category);
            if (qty % sellPackageSizeKg != 0)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InvalidPackageQuantity);

            int stackIndex = FindCargoStackIndex(module.Cargo, itemTypeIndex);
            long playerQty = stackIndex >= 0 ? module.Cargo[stackIndex].Quantity : 0;
            if (qty > playerQty)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InsufficientCargoQuantity);

            // Partial fill: the only direction where the station's hidden Credits balance can
            // limit the operation (CP-1/Money.md) — Buy/Refuel only ever add to it. A partial
            // fill must still land on a whole number of sell packages (§59) — qty itself is
            // already package-aligned (rejected above otherwise), but maxStationCanAfford
            // generally is not, so floor down after the Min.
            long maxStationCanAfford = unitPriceCredits > 0 ? station.Credits / unitPriceCredits : long.MaxValue;
            long executedQty = Math.Min(qty, maxStationCanAfford);
            executedQty = executedQty / sellPackageSizeKg * sellPackageSizeKg;
            if (executedQty <= 0)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InsufficientStationStock);

            long proceeds = unitPriceCredits * executedQty;
            PlayerCredits += proceeds;

            var updatedInventory = station.Inventory.SetItem(stationInventoryIndex,
                stationInventoryItem with { StockQuantity = stationInventoryItem.StockQuantity + executedQty });
            _objects[stationIndex] = station with { Credits = station.Credits - proceeds, Inventory = updatedInventory };

            _objects[objectIndex] = UpdateModule(obj, moduleIndex, m =>
            {
                int idx = FindCargoStackIndex(m.Cargo, itemTypeIndex);
                long remaining = m.Cargo[idx].Quantity - executedQty;
                var updatedCargo = remaining > 0
                    ? m.Cargo.SetItem(idx, m.Cargo[idx] with { Quantity = remaining })
                    : m.Cargo.RemoveAt(idx);
                return m with { Cargo = updatedCargo };
            });

            RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs,
                executedQuantity: executedQty < qty ? executedQty : null);
            return CommandStartOutcome.Started;
        }

        // TradeCommandTypes.Refuel — all-or-nothing, like Buy, but fills the module's fuel tank
        // (kg directly — Quantity is the mass) rather than a Cargo stack; item.fuel's
        // UnitMassKg is never consulted here regardless of its content value (story-20260825-
        // 084409 decision 3: Fuel is only special-cased for Buy/Refuel routing, not for how its
        // mass is measured on this branch).
        {
            long fuelCapacityKg = moduleType.FuelCapacityKg ?? 0;
            long cost = unitPriceCredits * qty;
            if (cost > PlayerCredits)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InsufficientPlayerCredits);

            if (qty > stationInventoryItem.StockQuantity)
                return CommandStartOutcome.Rejected(CommandReasonCodes.InsufficientStationStock);

            if (module.FuelAmountKg + qty > fuelCapacityKg)
                return CommandStartOutcome.Rejected(CommandReasonCodes.FuelCapacityExceeded);

            PlayerCredits -= cost;

            var updatedInventory = station.Inventory.SetItem(stationInventoryIndex,
                stationInventoryItem with { StockQuantity = stationInventoryItem.StockQuantity - qty });
            _objects[stationIndex] = station with { Credits = station.Credits + cost, Inventory = updatedInventory };

            _objects[objectIndex] = UpdateModule(obj, moduleIndex, m => m with { FuelAmountKg = m.FuelAmountKg + qty });

            RecordCommandResult(command, CommandResultStatus.Executed, gameTimeMs);
            return CommandStartOutcome.Started;
        }
    }

    /// <summary>
    /// Sell package size in kg by trade category (§59 "Продажа"): Resource sells only in
    /// multiples of 100 kg, Good (including Fuel — story-20260825-084409 decision 3) only in
    /// multiples of 10 kg. Module is out of scope entirely — no <see cref="ItemTypeDefinition"/>
    /// ever carries <see cref="TradeCategory"/> for a Module, so this switch never needs a
    /// Module case.
    /// </summary>
    private static long SellPackageSizeKg(TradeCategory category) => category switch
    {
        TradeCategory.Resource => 100,
        TradeCategory.Good => 10,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown TradeCategory.")
    };

    /// <summary>
    /// Maps the Engine-internal <see cref="TradeCategory"/> to the Contracts-side string mirror
    /// (<see cref="TradeItemCategories"/>) published on <see cref="StationInventoryItemSnapshot.
    /// Category"/> — see that field's doc comment for why it is a string, not a duplicate enum
    /// (story-20260825-084409 Batch 3, U10).
    /// </summary>
    private static string ToTradeItemCategory(TradeCategory category) => category switch
    {
        TradeCategory.Resource => TradeItemCategories.Resource,
        TradeCategory.Good => TradeItemCategories.Good,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown TradeCategory.")
    };

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

        // Match commands (engine.speedSynchronization / engine.directionSynchronization, §56.9)
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

        // Trailing-pursuit navigation (navigation.approach) carries an explicit,
        // authoritative target object like the match commands above, but — unlike
        // SpeedSynchronization/DirectionSynchronization — it never captures a scalar
        // speed/direction at start: the target's live state is re-read fresh on every
        // completed cycle (see ApplyApproachStep), because the aim point itself moves
        // as the target moves.
        int? approachTargetIndex = null;
        if (command.CommandType == NavigationComputerCommandTypes.Approach)
        {
            if (string.IsNullOrWhiteSpace(command.TargetObjectId))
                return CommandStartOutcome.Rejected(CommandReasonCodes.MissingTarget);

            approachTargetIndex = _objects.FindIndex(o =>
                string.Equals(o.InitialMotion.ObjectId, command.TargetObjectId, StringComparison.Ordinal));
            if (approachTargetIndex < 0)
                return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownTarget);
        }

        // Navigate-to-point (engine.orbit) carries an explicit, authoritative
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
        if (command.CommandType == ShipEngineCommandTypes.Orbit)
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
            // navigation.approach is a THIRD, distinct case: unlike SpeedSync/
            // DirectionSync (always idempotent) and Orbit (idempotent only same-target),
            // a re-sent Approach ALWAYS cancels-and-restarts — regardless of whether the
            // target is the same — because re-aiming should reset any state accumulated
            // by the in-flight pursuit. It is excluded from this idempotent check
            // entirely and always falls through to the cancel-and-replace branch below.
            if (string.Equals(command.CommandType, activeCycle.CommandType, StringComparison.Ordinal) &&
                command.CommandType != NavigationComputerCommandTypes.Approach)
            {
                bool sameNavigateTarget = command.CommandType != ShipEngineCommandTypes.Orbit ||
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
        // target disappearing do not affect the result. SpeedSynchronization captures speed
        // only, DirectionSynchronization captures course only; TargetObjectId is always stored
        // (diagnostics + restore after save/load, §1253-1298).
        string? targetObjectId = null;
        double? capturedTargetSpeedKmS = null;
        double? capturedTargetCourseDegrees = null;
        double? initialApproachTargetSpeedKmS = null;
        double? initialApproachTargetDirectionDegrees = null;
        if (matchTargetIndex is { } targetIndex)
        {
            var target = _objects[targetIndex];
            long targetElapsedMs = Math.Max(0, gameTimeMs - target.StartGameTimeMs);
            var targetMotion = _motion.Predict(target.InitialMotion, targetElapsedMs);
            targetObjectId = command.TargetObjectId;
            if (command.CommandType == ShipEngineCommandTypes.SpeedSynchronization)
                capturedTargetSpeedKmS = targetMotion.SpeedKmS;
            else
                capturedTargetCourseDegrees = targetMotion.Direction;
        }
        else if (approachTargetIndex is not null)
        {
            // Store the target reference AND bake an initial aim point + live target
            // speed/direction right now — not just on first cycle completion. Without
            // this, the snapshot sent out before the first ~1s cycle completes has
            // ActiveEngineCommandType == navigation.approach but null navigation fields;
            // the client's Approach-specific prediction guard (LinearMotionPredictor)
            // then fails and falls through to the generic turn-step fallback, spinning
            // the ship continuously in one direction. While paused this never
            // self-corrects, since game time — and therefore the first cycle — never
            // advances (this was a real, user-reported bug).
            var target = _objects[approachTargetIndex.Value];
            long targetElapsedMs = Math.Max(0, gameTimeMs - target.StartGameTimeMs);
            var targetMotion = _motion.Predict(target.InitialMotion, targetElapsedMs);

            var approachCommandDef = _registry.CommandDefinitions.GetDefinition(
                _registry.CommandDefinitions.GetIndex(command.CommandType));
            double trailDistanceWorldUnits = (approachCommandDef.TrailDistanceKm ?? 0) * WorldUnitsPerKm;
            (double aimX, double aimY) = DeepSpaceSaga.Motion.ApproachPursuitMath.ComputeAimPoint(
                targetMotion.X, targetMotion.Y, targetMotion.Direction, targetMotion.SpeedKmS, trailDistanceWorldUnits);

            targetObjectId = command.TargetObjectId;
            navigateTargetX = aimX;
            navigateTargetY = aimY;
            navPhase = targetMotion.SpeedKmS == 0 || trailDistanceWorldUnits <= 0
                ? ApproachPursuitMath.FinalPhase
                : ApproachPursuitMath.TrailPhase;
            initialApproachTargetSpeedKmS = targetMotion.SpeedKmS;
            initialApproachTargetDirectionDegrees = targetMotion.Direction;
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
                    navigationRequiredDepartureDistance: initialRequiredDistance,
                    navigationTargetSpeedKmS: initialApproachTargetSpeedKmS,
                    navigationTargetDirectionDegrees: initialApproachTargetDirectionDegrees)
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

    private static int FindCargoStackIndex(ImmutableArray<CargoStackRuntime> cargo, int itemTypeIndex)
    {
        for (int i = 0; i < cargo.Length; i++)
        {
            if (cargo[i].ItemTypeIndex == itemTypeIndex)
                return i;
        }

        return -1;
    }

    private static int FindInventoryIndex(ImmutableArray<StationInventoryItemRuntime> inventory, int itemTypeIndex)
    {
        if (inventory.IsDefaultOrEmpty)
            return -1;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].ItemTypeIndex == itemTypeIndex)
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
        double? navigationRequiredDepartureDistance = null,
        double? navigationTargetSpeedKmS = null,
        double? navigationTargetDirectionDegrees = null)
    {
        string cycleId = $"CYC-ENGINE-{++_nextEngineCycleId:D6}";
        // Approach now shares Orbit's faster MinTurnIntervalMs (~250 ms at 4°/s) turn
        // cadence instead of the default ComputeEffectiveCycleTimeMs (~1000 ms). This
        // supersedes the original design (which deliberately left Approach out of this
        // ternary to satisfy "recomputed every second" with zero new constants): with
        // only ~1000 ms between corrections, Approach's turnStepDegrees content value
        // (calibrated for a ~250 ms cadence, e.g. 1° per 250 ms = 4°/s matching
        // AngularInertiaDegPerSec) only delivered 1/4 of its intended turn rate, so each
        // straight-line segment between corrections was 4x longer than intended —
        // producing exactly the wide, slow, non-converging arc reported by the user
        // (story-20260827-083137.md, Post-implementation bug fix #2). Re-reading the
        // live target state at this faster cadence still satisfies "recomputed every
        // second" as a floor — strictly exceeded (fresher, not staler), not violated.
        long durationMs = (commandType == ShipEngineCommandTypes.Orbit ||
                           commandType == NavigationComputerCommandTypes.Approach) &&
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
            NavigationRequiredDepartureDistance: navigationRequiredDepartureDistance,
            NavigationTargetSpeedKmS: navigationTargetSpeedKmS,
            NavigationTargetDirectionDegrees: navigationTargetDirectionDegrees);
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
               commandType == ShipEngineCommandTypes.Orbit ||
               commandType == NavigationComputerCommandTypes.Approach;
    }

    private static bool IsMatchEngineCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.SpeedSynchronization ||
               commandType == ShipEngineCommandTypes.DirectionSynchronization;
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

            if (cycle.CommandType == ShipEngineCommandTypes.Orbit)
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

            if (cycle.CommandType == NavigationComputerCommandTypes.Approach)
            {
                // Trailing-pursuit cycles report their last-recomputed (baked) aim point
                // and the target's live speed/direction — the client-side trajectory
                // projector extrapolates the target's motion using these baked values
                // instead of a cross-object lookup (Checkpoint 1).
                long remainingMs = Math.Max(1, cycle.StartedGameTimeMs + cycle.DurationMs - gameTimeMs);
                return new ActiveEngineCycleMotion(
                    cycle.CommandType,
                    Math.Abs(moduleType.TurnStepDegrees ?? 0),
                    remainingMs,
                    cycle.DurationMs,
                    cycle.TargetWorldX,
                    cycle.TargetWorldY,
                    moduleType.AngularInertiaDegPerSec ?? 0,
                    // Reuses the Orbit-origin NavigationLockedCourseDegrees field,
                    // cycle-scoped for Approach — see ApplyApproachStep's doc-comment.
                    NavigationLockedCourseDegrees: cycle.NavigationLockedCourseDegrees,
                    NavigationPhase: cycle.NavigationPhase,
                    NavigationTargetSpeedKmS: cycle.NavigationTargetSpeedKmS,
                    NavigationTargetDirectionDegrees: cycle.NavigationTargetDirectionDegrees,
                    NavigationApproachTrailDistanceWorldUnits:
                        (_registry.CommandDefinitions.GetDefinition(
                            _registry.CommandDefinitions.GetIndex(cycle.CommandType)).TrailDistanceKm ?? 0) * WorldUnitsPerKm);
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

                    // Checkpoint 3: an Approach cycle whose target has disappeared from the
                    // world (destroyed, or otherwise no longer resolvable) cancels the same
                    // way the module-unavailable check above does — CommandResult(Cancelled)
                    // + ShipEvent(CycleInterrupted), reusing CommandReasonCodes.UnknownTarget
                    // (no new reason code invented for this).
                    if (cycle.CommandType == NavigationComputerCommandTypes.Approach &&
                        (cycle.TargetObjectId is null ||
                         !_objects.Exists(o => string.Equals(
                             o.InitialMotion.ObjectId, cycle.TargetObjectId, StringComparison.Ordinal))))
                    {
                        RecordCommandResultFromCycle(cycle, CommandResultStatus.Cancelled, gameTimeMs,
                            CommandReasonCodes.UnknownTarget);
                        _objects[objectIndex] = UpdateModule(obj, moduleIndex, current => current with { ActiveCycle = null });
                        obj = _objects[objectIndex];
                        RecordShipEvent(obj.InitialMotion.ObjectId, module.ModuleId,
                            ShipEventTypes.CycleInterrupted, CommandReasonCodes.UnknownTarget, gameTimeMs);
                        break;
                    }

                    long completionGameTimeMs = cycle.DurationMs == 0
                        ? gameTimeMs
                        : cycle.StartedGameTimeMs + cycle.DurationMs;
                    ActiveCycleData? nextCycle = cycle.IsAutoRepeat
                        ? CreateEngineCycle(cycle.CommandType, completionGameTimeMs, isAutoRepeat: true, moduleType,
                            targetObjectId: cycle.TargetObjectId,
                            commandId: cycle.CommandId, objectId: cycle.ObjectId, moduleId: cycle.ModuleId,
                            targetWorldX: cycle.TargetWorldX, targetWorldY: cycle.TargetWorldY,
                            navigationPhase: cycle.NavigationPhase,
                            navigationEscapeCourseDegrees: cycle.NavigationEscapeCourseDegrees,
                            navigationRequiredDepartureDistance: cycle.NavigationRequiredDepartureDistance,
                            navigationTargetSpeedKmS: cycle.NavigationTargetSpeedKmS,
                            navigationTargetDirectionDegrees: cycle.NavigationTargetDirectionDegrees)
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
            // SpeedSynchronization changes only the scalar speed (course untouched),
            // DirectionSynchronization changes only the course (speed untouched).
            // The captured value may exceed the ship's own MaxSpeedKmS (it came from a real
            // object), so speed is clamped the same way Accelerate clamps.
            // Legacy-save guard: a match cycle loaded from a save written by older code may
            // have a null captured field (match commands then passed without a target) —
            // such a cycle completes as a no-op instead of throwing.
            ShipEngineCommandTypes.SpeedSynchronization when cycle.CapturedTargetSpeedKmS is { } capturedSpeedKmS => UpdateEngineMotion(
                obj,
                moduleIndex,
                gameTimeMs,
                module => module with { ActiveCycle = nextCycle },
                motion => motion with
                {
                    SpeedKmS = Math.Min(moduleType.MaxSpeedMps!.Value / 1000.0, capturedSpeedKmS)
                }),

            ShipEngineCommandTypes.DirectionSynchronization when cycle.CapturedTargetCourseDegrees is { } capturedCourseDegrees => UpdateEngineMotion(
                obj,
                moduleIndex,
                gameTimeMs,
                module => module with { ActiveCycle = nextCycle },
                motion => motion with
                {
                    Direction = NormalizeDirection(capturedCourseDegrees)
                }),

            // Navigation (engine.orbit): one discrete turn step per cycle
            // (DurationMs = MinTurnIntervalMs, so ~250 ms per step at 4 deg/sec), heading
            // for the authoritative world target. On arrival the auto-repeat chain is cut
            // (nextCycle = null) — the cycle completes normally, writing CommandResult(Executed)
            // + ShipEvent(CommandCompleted) in CompleteActiveEngineCycles. Legacy-save guard:
            // a navigation cycle loaded from a save written by older code may have null
            // target coordinates — such a cycle completes as a no-op instead of throwing.
            ShipEngineCommandTypes.Orbit when cycle.TargetWorldX is { } targetX && cycle.TargetWorldY is { } targetY =>
                ApplyNavigationStep(obj, moduleIndex, moduleType, targetX, targetY, gameTimeMs, nextCycle),

            ShipEngineCommandTypes.Orbit => obj,

            // Trailing-pursuit navigation (navigation.approach): re-aims fresh from the
            // target's live state every completed cycle (never locks a permanent course,
            // unlike Orbit). Target-existence was already verified by
            // CompleteActiveEngineCycles right before calling this method, so
            // TargetObjectId is guaranteed resolvable here; the `when` guard is a
            // legacy-save no-op safeguard only (a cycle loaded from a save written by
            // older code may have a null TargetObjectId).
            NavigationComputerCommandTypes.Approach when cycle.TargetObjectId is { } approachTargetId =>
                ApplyApproachStep(obj, moduleIndex, moduleType, approachTargetId, cycle, gameTimeMs, nextCycle),

            NavigationComputerCommandTypes.Approach => obj,

            _ => obj
        };
    }

    /// <summary>
    /// Apply one completed navigation.approach pursuit cycle: read the target's live
    /// position/direction/speed fresh (never captured across cycles, unlike
    /// SpeedSynchronization/DirectionSynchronization), steer toward the freshly
    /// recomputed trailing aim point via <see cref="ApproachPursuitMath.Step"/>, and
    /// either complete (exact speed/direction match with the target, priming
    /// navigation.dock's strict epsilon check) or bake the new aim point and the
    /// target's live speed/direction into the next auto-repeat cycle so the client can
    /// extrapolate without a cross-object lookup (Checkpoint 1).
    /// </summary>
    private SpaceObjectRuntime ApplyApproachStep(
        SpaceObjectRuntime obj,
        int moduleIndex,
        ModuleTypeDefinition moduleType,
        string targetObjectId,
        ActiveCycleData cycle,
        long gameTimeMs,
        ActiveCycleData? nextCycle)
    {
        var target = _objects.Single(o =>
            string.Equals(o.InitialMotion.ObjectId, targetObjectId, StringComparison.Ordinal));
        long targetElapsedMs = Math.Max(0, gameTimeMs - target.StartGameTimeMs);
        var targetMotion = _motion.Predict(target.InitialMotion, targetElapsedMs);

        long shipElapsedMs = Math.Max(0, gameTimeMs - obj.StartGameTimeMs);
        var shipMotion = _motion.Predict(obj.InitialMotion, shipElapsedMs);

        var commandDef = _registry.CommandDefinitions.GetDefinition(
            _registry.CommandDefinitions.GetIndex(cycle.CommandType));
        double trailDistanceWorldUnits = (commandDef.TrailDistanceKm ?? 0) * WorldUnitsPerKm;
        bool isFinalApproach = cycle.NavigationPhase == ApproachPursuitMath.FinalPhase ||
                               targetMotion.SpeedKmS == 0 ||
                               trailDistanceWorldUnits <= 0;

        var result = ApproachPursuitMath.Step(
            shipMotion.X, shipMotion.Y, shipMotion.Direction, shipMotion.SpeedKmS,
            targetMotion.X, targetMotion.Y, targetMotion.Direction, targetMotion.SpeedKmS,
            isFinalApproach ? 0 : trailDistanceWorldUnits,
            moduleType.TurnStepDegrees ?? 0,
            moduleType.AngularInertiaDegPerSec ?? 0,
            cycle.DurationMs,
            cycle.NavigationLockedCourseDegrees);

        if (result.IsArrived)
        {
            if (!isFinalApproach && nextCycle is not null)
            {
                // The trailing point is a staging waypoint, not the destination.
                // Continue from behind the target to its exact live position.
                nextCycle = nextCycle with
                {
                    TargetWorldX = targetMotion.X,
                    TargetWorldY = targetMotion.Y,
                    NavigationPhase = ApproachPursuitMath.FinalPhase,
                    NavigationTargetSpeedKmS = targetMotion.SpeedKmS,
                    NavigationTargetDirectionDegrees = targetMotion.Direction,
                    NavigationLockedCourseDegrees = null
                };

                return UpdateEngineMotion(
                    obj, moduleIndex, gameTimeMs,
                    module => module with { ActiveCycle = nextCycle },
                    motion => motion with { Direction = NormalizeDirection(result.NewDirectionDegrees) });
            }

            // Exact scalar assignment, mirroring SpeedSynchronization/
            // DirectionSynchronization — not asymptotic — so navigation.dock's strict
            // epsilon check (~1e-6) passes immediately after this cycle completes.
            return UpdateEngineMotion(
                obj, moduleIndex, gameTimeMs,
                module => module with { ActiveCycle = null },
                motion => motion with
                {
                    SpeedKmS = targetMotion.SpeedKmS,
                    Direction = NormalizeDirection(targetMotion.Direction)
                });
        }

        if (nextCycle is not null)
        {
            // Bake the freshly recomputed aim point and the target's live speed/
            // direction into the next auto-repeat cycle — this is what lets the client
            // extrapolate the target's motion between server cycles without a
            // cross-object lookup (Checkpoint 1).
            nextCycle = nextCycle with
            {
                TargetWorldX = result.AimPointX,
                TargetWorldY = result.AimPointY,
                NavigationTargetSpeedKmS = targetMotion.SpeedKmS,
                NavigationTargetDirectionDegrees = targetMotion.Direction,
                NavigationPhase = isFinalApproach
                    ? ApproachPursuitMath.FinalPhase
                    : ApproachPursuitMath.TrailPhase,
                // Reuses the Orbit-origin NavigationLockedCourseDegrees field, cycle-scoped
                // (not permanent) for Approach — see ApproachPursuitMath's class doc-comment
                // and ActiveCycleData.NavigationLockedCourseDegrees for the dual-meaning
                // convention (story-20260827-083137.md, Post-implementation bug fix #2).
                NavigationLockedCourseDegrees = result.LockedCourseDegrees
            };
        }

        return UpdateEngineMotion(
            obj,
            moduleIndex,
            gameTimeMs,
            module => module with { ActiveCycle = nextCycle },
            motion => motion with { Direction = NormalizeDirection(result.NewDirectionDegrees) });
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
    /// (engine.orbit). Uses the same roll-then-apply shape as every other
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
    bool IsKnown = false,
    /// <summary>
    /// Hull grid geometry carried forward from the scenario/save (requirements §57).
    /// Required (non-null) whenever Modules is non-empty — see ValidateModulePlacement.
    /// </summary>
    HullLayoutData? HullLayout = null,
    /// <summary>Authoritative docking state (navigation.dock). Source of truth for
    /// <see cref="ObjectMotionSnapshot.IsDocked"/>, projected onto the outgoing snapshot
    /// row in BuildSnapshot the same way ObjectType/DisplayName are.</summary>
    bool IsDocked = false,
    /// <summary>ObjectId of the station this object is docked to. Null unless <see cref="IsDocked"/>.</summary>
    string? DockedStationObjectId = null,
    /// <summary>
    /// Station's Credits balance (Docs\FirstRelease\Mechanics\Money.md). Only meaningful for
    /// ObjectType == Station; 0 for every other object type (never RNG-resolved for them).
    /// </summary>
    long Credits = 0,
    /// <summary>
    /// Station's price coefficient, fixed-point where 1000 == 1.0x (Docs\FirstRelease\
    /// Mechanics\StationInventory.md's 0.5..2.0 range == 500..2000). Only meaningful for
    /// ObjectType == Station; 1000 (neutral) for every other object type.
    /// </summary>
    int PriceCoefficient = 1000,
    /// <summary>
    /// Station's tradeable stock, one entry per sellable item type. Only meaningful for
    /// ObjectType == Station; empty for every other object type.
    /// </summary>
    ImmutableArray<StationInventoryItemRuntime> Inventory = default,
    /// <summary>
    /// Station's size classification (§59), resolved once by
    /// <see cref="SimulationEngine.ResolveStationSize"/> and then persisted explicitly — same
    /// "resolved once, explicit from then on" shape as <see cref="Credits"/>, except this is
    /// never RNG-generated (story-20260825-084409 Batch 2 instruction). Only meaningful for
    /// ObjectType == Station; <see cref="StationSize.Medium"/> (neutral placeholder, never
    /// read) for every other object type.
    /// </summary>
    StationSize StationSize = StationSize.Medium,
    /// <summary>
    /// Station's producing-module instances (§59 "Производящие модули станции"). Fully
    /// explicit, never RNG-generated. Only meaningful for ObjectType == Station; empty for
    /// every other object type.
    /// </summary>
    ImmutableArray<StationProducingModuleRuntime> ProducingModules = default,
    /// <summary>
    /// Station's active events/buffs/debuffs (§59, story-20260825-084409 CP-3 — schema +
    /// persistence only). Only meaningful for ObjectType == Station; empty for every other
    /// object type and for every station no scenario/save ever populates one for.
    /// </summary>
    ImmutableArray<StationEventRuntime> Events = default);

/// <summary>
/// One producing-module instance installed on a station (see <see cref="StationProducingModuleData"/>).
/// <see cref="FactoryTypeIndex"/> indexes <see cref="GameDataRegistry.FactoryTypes"/>.
/// </summary>
internal sealed record StationProducingModuleRuntime(int FactoryTypeIndex, bool Active);

/// <summary>
/// One station event/buff/debuff (see <see cref="StationEventData"/>) — schema + persistence
/// only, story-20260825-084409 CP-3.
/// </summary>
internal sealed record StationEventRuntime(
    string EventId,
    string DisplayName,
    string? Description,
    long StartedGameTimeMs,
    long? DurationMs,
    ImmutableArray<StationEventPriceFactorRuntime> PriceFactors);

/// <summary>
/// One multiplicative price factor contributed by a <see cref="StationEventRuntime"/>. See
/// <see cref="StationEventPriceFactorData"/> for the addressing-mode rules (specific item,
/// category-wide, or station-wide when both are null).
/// </summary>
internal sealed record StationEventPriceFactorRuntime(
    TradeCategory? Category,
    int? ItemTypeIndex,
    int Factor);

internal sealed record InstalledModuleRuntime(
    string ModuleId,
    int ModuleTypeIndex,
    ImmutableArray<(int X, int Y)> OccupiedCells,
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

/// <summary>One tradeable item's stock on a station (see StationInventoryItemData).</summary>
internal sealed record StationInventoryItemRuntime(
    int ItemTypeIndex,
    long StockQuantity);

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
    double? NavigationRequiredDepartureDistance = null,
    double? NavigationTargetSpeedKmS = null,
    double? NavigationTargetDirectionDegrees = null,
    double? NavigationApproachTrailDistanceWorldUnits = null);

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
