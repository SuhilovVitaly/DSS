using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.Tests.Tutorial;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0013 TK-0002 (AC-01..AC-04): the real client session path over the owned starter
/// carrier. Tutorial exit → rearm through <see cref="GameSessionHandle"/> → second battle keeps
/// the same ShipId/DroneId/CrewId identities; seed-1 Enter-only and hint-following outcomes are
/// reported without forcing a win; the replay counter never reads the final/future magazine; and
/// RU/EN resolve the same six crew names, portraits and the two reserved cabins.
/// </summary>
[Collection("InterfaceLog")]
public sealed class Ep0029Us0013Tk0002Tests
{
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");
    private const string ReducedDroneId = "DRN-PLAYER-01";

    private static async Task<CarrierOperationsSnapshot> WaitCarrierAsync(GameSessionHandle handle, int revision)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (handle.CarrierOperations is { } carrier && carrier.Revision >= revision)
                return carrier;
            Assert.Null(handle.Failure);
            await Task.Delay(20);
        }
        throw new TimeoutException($"carrier revision {revision} was not published");
    }

    private static int HoldTorpedoes(SnapshotBuffer buffer) =>
        (buffer.Presented?.Snapshot.InstalledModules ?? [])
            .SelectMany(module => module.Cargo)
            .Where(stack => stack.ItemTypeId == "item.drone-torpedo.t1")
            .Sum(stack => (int)stack.Quantity);

    private static string[] OwnDroneIds(TacticalBattleSnapshot state) => state.Units
        .Where(unit => unit.Side == state.PlayerSide && unit.Kind == TacticalUnitKind.Drone)
        .Select(unit => unit.Id)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [Fact]
    public async Task OwnedTutorialRearmSecondBattle()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0013-tk0002-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(SettingsPath, TutorialPath);
            engine.SetSpeed(SimulationSpeed.Speed0);
            await using var connection = new LocalGameSessionConnection(engine, directory);
            await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);

            var carrier = await WaitCarrierAsync(handle, revision: 0);
            Assert.Equal("SPC-0001", carrier.ShipId);
            var firstIds = carrier.OwnedDrones.Select(drone => drone.Id).Order(StringComparer.Ordinal).ToArray();
            var firstCrew = carrier.OwnedDrones.Select(drone => drone.CrewId).Order(StringComparer.Ordinal).ToArray();
            Assert.Equal(4, firstIds.Length);
            Assert.Equal(36, carrier.OwnedDrones.Sum(drone => drone.AmmoCurrent));
            Assert.Equal(42, HoldTorpedoes(handle.Buffer));

            var start = await handle.StartTacticalBattleAsync(new StartTacticalBattleRequest("tk2-start-1", 1, "initial")
            {
                SourceShipId = carrier.ShipId,
                ExpectedCarrierRevision = carrier.Revision,
            });
            Assert.True(start.Accepted, "owned tutorial start rejected: " + start.Code);
            string battleId = start.State!.BattleId;
            Assert.Equal(firstIds, OwnDroneIds(start.State));
            Assert.Equal(carrier.ShipId,
                start.State.Units.Single(unit => unit.Kind == TacticalUnitKind.Carrier && unit.Side == start.State.PlayerSide).Id);

            var move = await handle.MoveTacticalBattleAsync(
                new MoveTacticalBattleRequest("tk2-move-1", battleId, start.State.Revision));
            Assert.True(move.Accepted, "move rejected: " + move.Code);

            // Deterministic fixture: one magazine is spent mid-sortie so the settlement persists a
            // real gap and the next rearm transfers torpedoes from the hold through the handle.
            var session = engine.TacticalBattleSessionForTests!;
            var reduced = session.Battle.Drones.Single(drone => drone.Side == session.PlayerSide && drone.Id == ReducedDroneId);
            reduced.AmmoCurrent = reduced.AmmoCapacity - 3;

            var exit = await handle.ExitTacticalBattleAsync(new ExitTacticalBattleRequest("tk2-exit-1", battleId));
            Assert.True(exit.Accepted, "exit rejected: " + exit.Code);
            var settlement = Assert.IsType<SortieSettlementSnapshot>(exit.Settlement);
            Assert.Equal(3, settlement.ConsumedTorpedoes);
            Assert.Equal(1, settlement.CarrierRevision);

            var afterSortie = await WaitCarrierAsync(handle, revision: settlement.CarrierRevision);
            Assert.Equal(firstIds, afterSortie.OwnedDrones.Select(drone => drone.Id).Order(StringComparer.Ordinal));
            Assert.Equal(reduced.AmmoCapacity - 3,
                afterSortie.OwnedDrones.Single(drone => drone.Id == ReducedDroneId).AmmoCurrent);
            Assert.Equal(42, HoldTorpedoes(handle.Buffer));

            var rearmRequest = new RearmDroneRequest("tk2-rearm-1", carrier.ShipId, ReducedDroneId, 3, afterSortie.Revision);
            Assert.False(handle.IsRearmPending(rearmRequest.CommandId));
            var rearm = await handle.RearmDroneAsync(rearmRequest);
            Assert.True(rearm.Accepted, "rearm rejected: " + rearm.Reason);
            Assert.Equal(3, rearm.Transferred);
            Assert.Equal(afterSortie.Revision + 1, rearm.Revision);

            var afterRearm = await WaitCarrierAsync(handle, revision: rearm.Revision);
            Assert.Equal(reduced.AmmoCapacity,
                afterRearm.OwnedDrones.Single(drone => drone.Id == ReducedDroneId).AmmoCurrent);
            Assert.Equal(39, HoldTorpedoes(handle.Buffer));

            var second = await handle.StartTacticalBattleAsync(new StartTacticalBattleRequest("tk2-start-2", 2, "initial")
            {
                SourceShipId = carrier.ShipId,
                ExpectedCarrierRevision = afterRearm.Revision,
            });
            Assert.True(second.Accepted, "second owned start rejected: " + second.Code);
            var secondState = second.State!;
            Assert.Equal(firstIds, OwnDroneIds(secondState));
            Assert.Equal(firstCrew, secondState.Units
                .Where(unit => unit.Side == secondState.PlayerSide && unit.Kind == TacticalUnitKind.Drone)
                .Select(unit => unit.Build!.PilotId)
                .Order(StringComparer.Ordinal));
            Assert.Equal(reduced.AmmoCapacity, secondState.Units.Single(unit => unit.Id == ReducedDroneId).AmmoCurrent!.Value);
            Assert.Equal(carrier.ShipId,
                secondState.Units.Single(unit => unit.Kind == TacticalUnitKind.Carrier && unit.Side == secondState.PlayerSide).Id);

            var secondExit = await handle.ExitTacticalBattleAsync(
                new ExitTacticalBattleRequest("tk2-exit-2", secondState.BattleId));
            Assert.True(secondExit.Accepted);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    [Fact]
    public void TutorialEnterAndHintsReportActualOutcome()
    {
        // Seed 1 is reported, never forced: the existing winnability suite owns the real win check.
        var enter = TutorialBattleWinnabilityTests.Play(SkiaWindow.TutorialBattleSeed, followHints: false);
        var hints = TutorialBattleWinnabilityTests.Play(SkiaWindow.TutorialBattleSeed, followHints: true);

        Assert.Equal("initial", enter.Scenario);
        Assert.Equal("initial", hints.Scenario);
        Assert.NotNull(enter.Outcome);
        Assert.NotNull(hints.Outcome);

        string directory = ReportDirectory("US-0013-TK-0002");
        string path = Path.Combine(directory, "tutorial-seed1-outcome.txt");
        File.WriteAllLines(path, new[]
        {
            $"enter: seed={enter.Seed} outcome={enter.Outcome} turn={enter.Turn} losses=[{enter.Losses}] consumed={enter.ConsumedTorpedoes}",
            $"hints: seed={hints.Seed} outcome={hints.Outcome} turn={hints.Turn} losses=[{hints.Losses}] consumed={hints.ConsumedTorpedoes}",
        });

        string report = File.ReadAllText(path);
        Assert.Contains("enter:", report, StringComparison.Ordinal);
        Assert.Contains("hints:", report, StringComparison.Ordinal);
        Assert.Contains("consumed=", report, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AmmoReplayDoesNotShowFutureState()
    {
        // Frame-consistent counter: during the salvo replay every frame reads its own step's
        // magazine, so the first frame shows the pre-salvo ammo and mid frames the historical
        // step value — never the final (future) state from TacticalBattleSnapshot.Units.
        await using var flowFixture = await OwnedSetupFixture.CreateAsync();
        var state = await flowFixture.StartOwnedBattleAsync();
        var own = state.Units.First(unit => unit.Side == state.PlayerSide &&
            unit.Kind == TacticalUnitKind.Drone && unit.AmmoCurrent is > 0);
        int capacity = own.AmmoCapacity!.Value;
        Assert.True(capacity >= 3, "a starter magazine must hold at least three torpedoes");
        var enemy = state.Units.First(unit => unit.Side != state.PlayerSide && unit.Kind == TacticalUnitKind.Drone);
        var shot = new TacticalShotSnapshot(state.Turn, own.Id, enemy.Id, own.Pose, enemy.Pose, 1, false, 0, false, false, false);
        var before = own with { AmmoCurrent = capacity };
        var afterFirst = own with { AmmoCurrent = capacity - 1 };
        var afterSecond = own with { AmmoCurrent = capacity - 2 };
        var battle = state with
        {
            Phase = TacticalBattlePhase.Fire,
            Units = [afterSecond],
            BeforeSalvoUnits = [before],
            Shots = [shot, shot],
            FireSteps =
            [
                new TacticalFireStepSnapshot(0, own.Id, enemy.Id, TacticalFireSkipReason.None, shot, [afterFirst], [], null),
                new TacticalFireStepSnapshot(1, own.Id, enemy.Id, TacticalFireSkipReason.None, shot, [afterSecond], [], null),
            ],
            FireSkipped = false,
        };
        var results = new TacticalBattleResults(battle, battle.Shots, [], 0);
        long firstStepMs = (long)TacticalBattleResults.InitialDelayMs + (long)results.DurationMs + 1;
        long secondStepMs = (long)TacticalBattleResults.InitialDelayMs + (long)(results.DurationMs * 2) + 1;

        int AmmoAt(long nowMs) => results.Project(nowMs).Units.Single(unit => unit.Id == own.Id).AmmoCurrent!.Value;

        Assert.Equal(capacity, AmmoAt(0));
        Assert.Equal(capacity - 1, AmmoAt(firstStepMs));
        Assert.Equal(capacity - 2, AmmoAt(secondStepMs));
        Assert.NotEqual(capacity - 2, AmmoAt(firstStepMs));
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void RussianEnglishNamesAndReservedSeats(string language)
    {
        using var engine = SimulationEngine.CreateFromScenarioFile(SettingsPath, TutorialPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var carrier = engine.CaptureSnapshot().CarrierOperations!;
        using (Localization.UseLocaleForTests(language))
        {
            Assert.Equal(6, carrier.Crew.Length);
            var cabins = carrier.CabinReservation!;
            Assert.Equal(8, cabins.TotalCabins);
            Assert.Equal(6, cabins.OccupiedCabins);
            Assert.Equal(2, cabins.ReservedCabins);
            Assert.Equal(0, cabins.FreeCabins);
            Assert.Equal(0, cabins.PrisonerCount);

            // Stable identifiers and display names are locale-independent technical facts.
            Assert.Equal(
                ["Dunkan Su", "Irina Volskaya", "Evelyn Ward", "Audrey Bennett", "Claire Morgan", "Rachel Hayes"],
                carrier.Crew.Select(member => member.DisplayName));
            foreach (var pilot in carrier.Crew.Where(member => member.Role == CrewRole.Pilot))
            {
                Assert.False(string.IsNullOrWhiteSpace(pilot.PortraitAssetId));
                Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory,
                    pilot.PortraitAssetId!.Replace('/', Path.DirectorySeparatorChar))));
            }

            var start = engine.StartTacticalBattle(new StartTacticalBattleRequest("tk2-names-" + language, 1, "initial")
            {
                SourceShipId = carrier.ShipId,
                ExpectedCarrierRevision = carrier.Revision,
            });
            Assert.True(start.Accepted, "owned tutorial start rejected: " + start.Code);
            var state = start.State!;
            foreach (var unit in state.Units.Where(unit =>
                unit.Side == state.PlayerSide && unit.Kind == TacticalUnitKind.Drone))
            {
                var member = carrier.Crew.Single(crew => crew.Id == unit.Build!.PilotId);
                Assert.Equal(member.DisplayName, unit.Build.PilotName);
                Assert.Equal(member.PortraitAssetId, unit.Build.PilotPortraitAssetId);
                Assert.Equal<CrewGender?>(member.Gender, unit.Build.PilotGender);
                Assert.Equal(member.Accuracy, unit.Accuracy);
                Assert.Equal(member.Maneuver, unit.Maneuver);
            }
            Assert.True(engine.ExitTacticalBattle(
                new ExitTacticalBattleRequest("tk2-names-exit-" + language, state.BattleId)).Accepted);
        }
    }

    private static string ReportDirectory(string ticket)
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? "";
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(Path.GetTempPath(), "dss-validation-ep0029");
        string directory = Path.Combine(root, ticket);
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Minimal real owned setup: the production F8 screen started through its own flow.</summary>
    private sealed class OwnedSetupFixture : IAsyncDisposable
    {
        private readonly BattleNavigationTests.Fixture _fixture;
        private OwnedSetupFixture(BattleNavigationTests.Fixture fixture) => _fixture = fixture;

        internal static async Task<OwnedSetupFixture> CreateAsync()
        {
            var fixture = await BattleNavigationTests.Fixture.Create();
            return new OwnedSetupFixture(fixture);
        }

        internal async Task<TacticalBattleSnapshot> StartOwnedBattleAsync()
        {
            await _fixture.PressKey(Key.F8);
            var setup = Assert.IsType<TacticalBattleSetupScreen>(_fixture.Screens.Current);
            await setup.Flow.OptionsTask!;
            var response = await setup.Flow.StartAsync(setup.BuildStartRequest());
            Assert.True(response!.Accepted, "owned setup start rejected: " + response.Code);
            return setup.Flow.State!;
        }

        public ValueTask DisposeAsync() => _fixture.DisposeAsync();
    }
}
