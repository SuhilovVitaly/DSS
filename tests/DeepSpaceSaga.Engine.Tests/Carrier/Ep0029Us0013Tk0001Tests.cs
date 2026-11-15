using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// EP-0029 US-0013 TK-0001: end-to-end engine acceptance over the shipped starter carrier.
/// The oracle is literal (Documentation.md «Стартовый корабль» / «Крыло и торпеды»):
/// 78 torpedoes = 12+12+6+6 loaded magazines + 42 in the hold. Settlement charges each torpedo
/// exactly once, a destroyed magazine is lost with the drone (never returned to cargo), a terminal
/// destroyed hull survives a save/load, and a Save18 world is never granted starter carrier assets.
/// </summary>
public sealed class Ep0029Us0013Tk0001Tests
{
    private const string PlayerShipId = "SPC-0001";
    private const string TorpedoItemTypeId = "item.drone-torpedo.t1";
    private const string ReducedDroneId = "DRN-PLAYER-01";

    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private static string TutorialScenarioPath => Path.Combine(ClientRoot, "Scenarios", "Tutorial", "scenario.json");

    private static string SaveFixtures => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "Saves"));

    private static SimulationEngine LoadTutorial()
    {
        var engine = SimulationEngine.CreateFromScenarioFile(SettingsPath, TutorialScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    private static GameDataRegistry Registry() =>
        EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

    private static SpaceObjectData PlayerShip(ScenarioFile scenario) =>
        scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);

    private static int HoldTorpedoes(AuthoritativeSnapshot snapshot) =>
        snapshot.InstalledModules
            .SelectMany(module => module.Cargo)
            .Where(stack => stack.ItemTypeId == TorpedoItemTypeId)
            .Sum(stack => (int)stack.Quantity);

    private static int HoldTorpedoes(ScenarioFile save) =>
        (PlayerShip(save).Modules ?? [])
            .SelectMany(module => module.Cargo ?? [])
            .Where(stack => stack.ItemTypeId == TorpedoItemTypeId)
            .Sum(stack => (int)stack.Quantity);

    private static string Economy(GameStateData state) => JsonSerializer.Serialize(new
    {
        state.PlayerTokens,
        state.EconomyTime,
        state.CatalogCompatibility,
        state.MarketEventCatalogFingerprint,
        state.TradingMap,
        state.TradingEconomyContinuation,
        state.MarketKnowledge,
        state.VoyageState,
        state.VoyageLedgers,
        state.VoyageFuelSettlements,
    });

    private static TacticalBattleSnapshot AdvanceToFire(SimulationEngine engine, string battleId,
        TacticalBattleSnapshot state)
    {
        // The tutorial opens in Move. Keep issuing the engine's own AI move until the salvo phase
        // is offered; a turn with no candidate on either side auto-resolves and returns to Move.
        for (int step = 0; step < 30 && state.Outcome is null && state.Phase != TacticalBattlePhase.Fire; step++)
        {
            Assert.Equal(TacticalBattlePhase.Move, state.Phase);
            var response = engine.MoveTacticalBattle(
                new MoveTacticalBattleRequest($"acceptance-move-{step}", battleId, state.Revision));
            Assert.True(response.Accepted, "move rejected: " + response.Code);
            state = response.State!;
        }
        Assert.Equal(TacticalBattlePhase.Fire, state.Phase);
        return state;
    }

    [Fact]
    public void CampaignRoundTripConservesSeventyEight()
    {
        using var engine = LoadTutorial();
        var initialCarrier = engine.CaptureSnapshot().CarrierOperations!;
        var initialAmmo = initialCarrier.OwnedDrones.ToDictionary(d => d.Id, d => d.AmmoCurrent, StringComparer.Ordinal);
        Assert.Equal(36, initialAmmo.Values.Sum());
        int initialHold = HoldTorpedoes(engine.CaptureSnapshot());
        Assert.Equal(42, initialHold);
        Assert.Equal(78, initialAmmo.Values.Sum() + initialHold);

        var start = engine.StartTacticalBattle(new StartTacticalBattleRequest("acceptance-start", 1, "initial")
        {
            SourceShipId = initialCarrier.ShipId,
            ExpectedCarrierRevision = initialCarrier.Revision,
        });
        Assert.True(start.Accepted, "owned tutorial start rejected: " + start.Code);
        string battleId = start.State!.BattleId;

        var fireState = AdvanceToFire(engine, battleId, start.State);
        var session = engine.TacticalBattleSessionForTests!;
        // Deterministic fixture: enemy batteries are empty (no return fire can destroy a player
        // drone) and one magazine is already partly spent, so the post-sortie rearm always has a gap.
        foreach (var enemy in session.Battle.Drones.Where(d => d.Side != session.PlayerSide))
            enemy.AmmoCurrent = 0;
        var reduced = session.Battle.Drones.Single(d => d.Side == session.PlayerSide && d.Id == ReducedDroneId);
        reduced.AmmoCurrent = Math.Max(0, reduced.AmmoCapacity - 3);

        var fire = engine.FireTacticalBattle(new FireTacticalBattleRequest("acceptance-fire", battleId, fireState.Revision,
            [.. fireState.Targets.Select(target => new TacticalFireTarget(target.DroneId, target.DefaultTargetId))]));
        Assert.True(fire.Accepted, "fire rejected: " + fire.Code);

        var exit = engine.ExitTacticalBattle(new ExitTacticalBattleRequest("acceptance-exit", battleId));
        Assert.True(exit.Accepted, "exit rejected: " + exit.Code);
        var settlement = Assert.IsType<SortieSettlementSnapshot>(exit.Settlement);
        Assert.Equal(battleId, settlement.BattleId);
        Assert.Equal(PlayerShipId, settlement.ShipId);
        Assert.False(settlement.ShipDestroyed);
        Assert.Equal(initialCarrier.Revision + 1, settlement.CarrierRevision);

        // Independent conservation oracle: world magazines + hold + fired + magazines lost with a
        // destroyed hull must still add up to the literal 78. Settlement consumed is the only delta.
        var afterSortie = engine.CaptureSnapshot();
        Assert.Equal(settlement.CarrierRevision, afterSortie.CarrierOperations!.Revision);
        var saved = engine.CaptureSaveState();
        var wing = PlayerShip(saved).CarrierState!.OwnedDrones!;
        int hold = HoldTorpedoes(saved);
        int magazines = wing.Where(drone => !drone.Destroyed).Sum(drone => drone.AmmoCurrent);
        int lostWithDestroyed = wing.Where(drone => drone.Destroyed).Sum(drone => initialAmmo[drone.DroneId]);
        Assert.Equal(78, magazines + hold + lostWithDestroyed + settlement.ConsumedTorpedoes);
        Assert.Equal(initialAmmo.Values.Sum() + initialHold,
            wing.Sum(drone => drone.AmmoCurrent) + hold + settlement.ConsumedTorpedoes);

        // One-successful-rearm: the transfer moves exactly one torpedo from cargo into the magazine.
        var reducedDrone = wing.First(drone => !drone.Destroyed && drone.AmmoCurrent < drone.AmmoCapacity);
        var rearm = engine.RearmDrone(new RearmDroneRequest("acceptance-rearm", PlayerShipId,
            reducedDrone.DroneId, 1, settlement.CarrierRevision));
        Assert.True(rearm.Accepted, "rearm rejected: " + rearm.Reason);
        Assert.Equal(1, rearm.Transferred);
        Assert.Equal(settlement.CarrierRevision + 1, rearm.Revision);

        var rearmed = engine.CaptureSaveState();
        Assert.Equal(78, wing.Sum(drone => drone.AmmoCurrent) + hold + settlement.ConsumedTorpedoes);
        Assert.Equal(78, PlayerShip(rearmed).CarrierState!.OwnedDrones!
            .Sum(drone => drone.AmmoCurrent) + HoldTorpedoes(rearmed) + settlement.ConsumedTorpedoes);

        // Production save round-trip: Save21 stores expanded state and never re-applies the manifest.
        var reloaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(rearmed),
            allowNonZeroGameTime: true, isSave: true);
        Assert.Null(reloaded.Metadata.StarterCarrierManifest);
        using var restored = new SimulationEngine(Registry());
        restored.LoadScenario(reloaded, isSave: true);
        var restoredCarrier = restored.CaptureSnapshot().CarrierOperations!;
        Assert.Equal(rearm.Revision, restoredCarrier.Revision);
        Assert.Equal(rearm.Snapshot!.OwnedDrones.ToDictionary(d => d.Id, d => d.AmmoCurrent),
            restoredCarrier.OwnedDrones.ToDictionary(d => d.Id, d => d.AmmoCurrent));
        Assert.Equal(HoldTorpedoes(rearmed), HoldTorpedoes(restored.CaptureSnapshot()));

        // A duplicate exit after the reload is a plain NoBattle: it never writes a second settlement.
        string beforeDuplicate = ScenarioLoader.Serialize(restored.CaptureSaveState());
        var duplicate = restored.ExitTacticalBattle(new ExitTacticalBattleRequest("acceptance-duplicate-exit", battleId));
        Assert.False(duplicate.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, duplicate.Code);
        Assert.Null(duplicate.Settlement);
        Assert.Equal(beforeDuplicate, ScenarioLoader.Serialize(restored.CaptureSaveState()));
    }

    [Fact]
    public void DestroyedHullCannotBeRecreated()
    {
        using var engine = LoadTutorial();
        var carrier = engine.CaptureSnapshot().CarrierOperations!;
        var start = engine.StartTacticalBattle(new StartTacticalBattleRequest("destroyed-start", 1, "initial")
        {
            SourceShipId = carrier.ShipId,
            ExpectedCarrierRevision = carrier.Revision,
        });
        Assert.True(start.Accepted, "owned tutorial start rejected: " + start.Code);

        // Terminal fixture: the carrier hull reaches zero; production settlement must persist it.
        engine.TacticalBattleSessionForTests!.Battle.Carriers.First(c => c.Side == "A").Hull = 0;
        var exit = engine.ExitTacticalBattle(new ExitTacticalBattleRequest("destroyed-exit", start.State!.BattleId));
        Assert.True(exit.Accepted);
        var settlement = Assert.IsType<SortieSettlementSnapshot>(exit.Settlement);
        Assert.True(settlement.ShipDestroyed);

        var destroyed = engine.CaptureSnapshot();
        Assert.True(destroyed.CarrierOperations!.IsDestroyed);
        Assert.Equal(0, destroyed.CarrierOperations.HullCurrent);
        Assert.Equal(settlement.CarrierRevision, destroyed.CarrierOperations.Revision);

        var saved = engine.CaptureSaveState();
        var reloaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved),
            allowNonZeroGameTime: true, isSave: true);
        using var restored = new SimulationEngine(Registry());
        restored.LoadScenario(reloaded, isSave: true);
        var restoredCarrier = restored.CaptureSnapshot().CarrierOperations!;
        Assert.True(restoredCarrier.IsDestroyed);
        Assert.Equal(0, restoredCarrier.HullCurrent);
        Assert.Equal(settlement.CarrierRevision, restoredCarrier.Revision);
        Assert.Equal(78, restoredCarrier.OwnedDrones.Sum(drone => drone.AmmoCurrent) + HoldTorpedoes(restored.CaptureSnapshot()));

        var retry = restored.StartTacticalBattle(new StartTacticalBattleRequest("destroyed-retry", 1, "initial")
        {
            SourceShipId = restoredCarrier.ShipId,
            ExpectedCarrierRevision = restoredCarrier.Revision,
        });
        Assert.False(retry.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.CarrierNotReady, retry.Code);
    }

    [Fact]
    public void ZeroAmmoDrawAndDuplicateSettlement()
    {
        using var engine = LoadTutorial();
        var carrier = engine.CaptureSnapshot().CarrierOperations!;
        var start = engine.StartTacticalBattle(new StartTacticalBattleRequest("stalemate-start", 1, "initial")
        {
            SourceShipId = carrier.ShipId,
            ExpectedCarrierRevision = carrier.Revision,
        });
        Assert.True(start.Accepted, "owned tutorial start rejected: " + start.Code);
        string battleId = start.State!.BattleId;

        // Both sides are out of torpedoes before any shot: the engine must resolve a stalemate
        // immediately instead of leaving the battle waiting for an impossible shot.
        var session = engine.TacticalBattleSessionForTests!;
        foreach (var drone in session.Battle.Drones)
            drone.AmmoCurrent = 0;
        var move = engine.MoveTacticalBattle(new MoveTacticalBattleRequest("stalemate-move", battleId, start.State.Revision));
        Assert.True(move.Accepted, "move rejected: " + move.Code);
        var state = move.State!;
        Assert.Equal(TacticalBattlePhase.Over, state.Phase);
        Assert.Equal(TacticalBattleOutcomeCode.Draw, state.Outcome!.Kind);
        Assert.Contains(state.Outcome.Reasons, reason => reason.Kind == TacticalOutcomeReasonCode.NoCombatCapability);

        var exit = engine.ExitTacticalBattle(new ExitTacticalBattleRequest("stalemate-exit", battleId));
        Assert.True(exit.Accepted);
        var settlement = Assert.IsType<SortieSettlementSnapshot>(exit.Settlement);
        Assert.Equal(SortieExitKind.Stalemate, settlement.ExitKind);
        var saved = engine.CaptureSaveState();
        var wing = PlayerShip(saved).CarrierState!.OwnedDrones!;
        int hold = HoldTorpedoes(saved);
        Assert.Equal(78, wing.Sum(drone => drone.AmmoCurrent) + hold + settlement.ConsumedTorpedoes);
        Assert.Equal(36, settlement.ConsumedTorpedoes);

        // Duplicate exit after a successful settlement: no battle exists, so nothing re-settles.
        string beforeDuplicate = ScenarioLoader.Serialize(saved);
        var duplicate = engine.ExitTacticalBattle(new ExitTacticalBattleRequest("stalemate-duplicate-exit", battleId));
        Assert.False(duplicate.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, duplicate.Code);
        Assert.Null(duplicate.Settlement);
        Assert.Equal(beforeDuplicate, ScenarioLoader.Serialize(engine.CaptureSaveState()));

        var reloaded = ScenarioLoader.LoadFromJson(beforeDuplicate, allowNonZeroGameTime: true, isSave: true);
        using var restored = new SimulationEngine(Registry());
        restored.LoadScenario(reloaded, isSave: true);
        var restoredCarrier = restored.CaptureSnapshot().CarrierOperations!;
        Assert.Equal(settlement.CarrierRevision, restoredCarrier.Revision);
        Assert.Equal(0, restoredCarrier.OwnedDrones.Sum(drone => drone.AmmoCurrent));
        Assert.Equal(42, HoldTorpedoes(restored.CaptureSnapshot()));
    }

    [Theory]
    [InlineData("Default-Save18.json")]
    [InlineData("Default-Save18-combat.json")]
    public void Save18To21PreservesEconomy(string fixture)
    {
        var saved = ScenarioLoader.LoadFromFile(Path.Combine(SaveFixtures, fixture),
            allowNonZeroGameTime: true, isSave: true);
        Assert.Equal(SaveFormat.CurrentSaveFormatVersion, saved.SaveFormatVersion);

        var registry = Registry();
        Assert.NotNull(saved.GameState.CatalogCompatibility);
        Assert.True(CarrierCatalogCompatibility.Accepts(registry, saved.GameState.CatalogCompatibility));
        var savedShip = PlayerShip(saved);
        int savedHullCells = savedShip.HullLayout?.Cells.Count ?? 0;
        string[] savedCrew = (savedShip.Crew ?? []).Select(member => member.CrewId).ToArray();

        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(saved, isSave: true);
        var snapshot = engine.CaptureSnapshot();

        // Legacy world keeps its authored identity: no 24-cell starter hull, no starter wing,
        // no 42 gifted torpedoes and no starter six-person crew.
        var carrier = Assert.IsType<CarrierOperationsSnapshot>(snapshot.CarrierOperations);
        Assert.True(carrier.OwnedDrones.IsDefaultOrEmpty);
        Assert.True(carrier.Hangars.IsDefaultOrEmpty);
        Assert.Equal(savedCrew, carrier.Crew.Select(member => member.Id));
        Assert.DoesNotContain(carrier.Crew, member => member.Id is "CHR-PLAYER-EVELYN" or "CHR-PLAYER-AUDREY"
            or "CHR-PLAYER-CLAIRE" or "CHR-PLAYER-RACHEL");
        Assert.Equal(12, savedHullCells);
        Assert.Equal(savedHullCells, PlayerShip(engine.CaptureSaveState()).HullLayout?.Cells.Count ?? 0);
        Assert.Equal(0, HoldTorpedoes(snapshot));
        Assert.DoesNotContain(snapshot.InstalledModules,
            module => module.ModuleTypeId.StartsWith("module.drone-bay.", StringComparison.Ordinal));

        // Economy continuation survives the Save18 → 21 → save → load → save cycle unchanged.
        var captured = engine.CaptureSaveState();
        Assert.Equal(2000L, captured.GameState.PlayerTokens!.Value);
        var reloaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(captured),
            allowNonZeroGameTime: true, isSave: true);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(reloaded, isSave: true);
        Assert.Equal(Economy(captured.GameState), Economy(restored.CaptureSaveState().GameState));

        // A tampered legacy fingerprint is still rejected: Save18 migration is not a bypass.
        using var rejecting = new SimulationEngine(registry);
        var tampered = reloaded with
        {
            GameState = reloaded.GameState with
            {
                CatalogCompatibility = reloaded.GameState.CatalogCompatibility! with { Fingerprint = new string('A', 64) },
            },
        };
        var error = Assert.Throws<ScenarioException>(() => rejecting.LoadScenario(tampered, isSave: true));
        Assert.Contains("catalog fingerprint", error.Message, StringComparison.Ordinal);
    }
}
