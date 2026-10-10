using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class AuthoritativeHostileAccessTests
{
    private static void DockFixture(SimulationEngine engine, string stationId)
    {
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != save.GameState.PlayerShipObjectId ? o :
                    o with { IsDocked = true, DockedStationObjectId = stationId, FirstPortFeeGameTimeMs = 0, NextPortFeeDueGameTimeMs = 86400000 }).ToArray()
            }
        }, isSave: true);
    }

    private static string Module(AuthoritativeSnapshot s, string command) => s.InstalledModules
        .First(m => m.CommandTypeIds.Contains(command)).ModuleId;

    [Fact]
    public void RawDockAndDialogueCannotBypassAiGate()
    {
        using (var engine = SeededAiBasesTests.Create())
        {
            var before = engine.CaptureSnapshot();
            engine.ReceiveCommand(new("ai-dock", 1, before.PlayerShipObjectId!, Module(before, NavigationComputerCommandTypes.Dock),
                NavigationComputerCommandTypes.Dock, TargetObjectId: before.AiMap!.Bases[0].ObjectId));
            var after = engine.CaptureSnapshot();
            Assert.Equal("station_access_denied", after.CommandResults.Single(r => r.CommandId == "ai-dock").ReasonCode);
            Assert.Null(after.ActiveDialogue);
            Assert.Equal(before.PlayerCredits, after.PlayerCredits);
        }
        var (dialogueEngine, _) = DialogueEffectTests.Create(
            [new("GrantStationAccess"), new("RemoveCredits", Amount: 10), new("DockPlayerToStation")]);
        using (dialogueEngine)
        {
            var save = dialogueEngine.CaptureSaveState();
            var orbit = new OrbitalElements(10000, 10000, 864000000, 90, 0, 0, "clockwise");
            dialogueEngine.LoadScenario(save with
            {
                GameState = save.GameState with
                {
                    AiMap = new(1, [new("STATION-01", "Orbital", "Ai", null, orbit, 0, 0)]),
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != "STATION-01" ? o : o with
                    { Orbit = orbit, IsKnown = true, Inventory = [], ProducingModules = [] }).ToArray()
                }
            }, isSave: true);
            var before = dialogueEngine.CaptureSnapshot();
            Assert.Equal("station_access_denied", Assert.Single(before.ActiveDialogue!.Choices).DisabledReasonKey);
            var after = DialogueTests.Choose(dialogueEngine, "choose");
            Assert.Equal(before.PlayerCredits, after.PlayerCredits);
            Assert.False(after.Objects.Single(o => o.ObjectId == after.PlayerShipObjectId).IsDocked);
        }
    }

    [Fact]
    public void AllTradeEntrypointsRejectAi()
    {
        using var engine = SeededAiBasesTests.Create();
        DockFixture(engine, engine.CaptureSnapshot().AiMap!.Bases[0].ObjectId);
        var before = engine.CaptureSaveState().GameState;
        var snapshot = engine.CaptureSnapshot();
        Assert.Null(snapshot.DockedStationTrade);
        foreach (var command in new[] { TradeCommandTypes.Buy, TradeCommandTypes.Sell, TradeCommandTypes.Refuel })
        {
            string module = Module(snapshot, command);
            string item = command == TradeCommandTypes.Refuel ? "item.fuel" : "item.water";
            var quote = engine.GetTradeQuote(new(command, snapshot.PlayerShipObjectId!, module, command, item, 1));
            Assert.Equal("station_access_denied", quote.DisabledReason);
            foreach (bool quoted in new[] { false, true })
            {
                string id = command + quoted;
                engine.ReceiveCommand(new(id, quoted ? 2UL : 1UL, snapshot.PlayerShipObjectId!, module, command,
                    ItemTypeId: item, Quantity: 1, QuoteId: quoted ? "stale-human-quote" : null, MarketRevision: quoted ? 1 : null));
                var after = engine.CaptureSnapshot();
                Assert.Equal("station_access_denied", after.CommandResults.Single(r => r.CommandId == id).ReasonCode);
                var state = engine.CaptureSaveState().GameState;
                Assert.Equal(before.PlayerTokens, state.PlayerTokens);
                Assert.Equal(JsonSerializer.Serialize(before.SpaceObjects), JsonSerializer.Serialize(state.SpaceObjects));
                Assert.Equal(JsonSerializer.Serialize(before.VoyageLedgers), JsonSerializer.Serialize(state.VoyageLedgers));
            }
        }
    }

    [Fact]
    public void HumanScientificMilitaryStillAccessible()
    {
        using var engine = SeededAiBasesTests.Create();
        var human = engine.CaptureSnapshot().ClusterMap!.Stations.First(s => s.MarketProfileId.Contains("scientific", StringComparison.Ordinal));
        DockFixture(engine, human.ObjectId);
        var snapshot = engine.CaptureSnapshot();
        Assert.NotNull(snapshot.DockedStationTrade);
        var quote = engine.GetTradeQuote(new("human", snapshot.PlayerShipObjectId!, Module(snapshot, TradeCommandTypes.Buy),
            TradeCommandTypes.Buy, "item.water", 1));
        Assert.DoesNotContain("station_access_denied", quote.LimitReasons);
        Assert.True(quote.ExecutableQuantity > 0);
    }
}
