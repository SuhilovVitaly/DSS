using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0008Tk0003Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0008-tk0003-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));
    private string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0008Tk0003Tests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private static JsonObject HullLayout()
    {
        var cells = new JsonArray();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject { ["width"] = 5, ["height"] = 5, ["cells"] = cells };
    }

    private static JsonObject Bay(string moduleId, int ax)
    {
        var cells = new JsonArray();
        foreach (var (x, y) in new[] { (ax, 0), (ax + 1, 0), (ax, 1), (ax + 1, 1) })
            cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.drone-bay.medium",
            ["occupiedCells"] = cells,
            ["structurePoints"] = 250,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(),
        };
    }

    private static JsonObject Cargo(string moduleId, int quantity)
    {
        var cells = new JsonArray { new JsonObject { ["x"] = 4, ["y"] = 2 } };
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.container.basic",
            ["occupiedCells"] = cells,
            ["structurePoints"] = 150,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(new JsonObject { ["itemTypeId"] = "item.drone-torpedo.t1", ["quantity"] = quantity }),
        };
    }

    private static JsonObject Crew(string id, string role) => new()
    {
        ["crewId"] = id,
        ["displayName"] = id,
        ["role"] = role,
        ["accuracySkill"] = 10,
        ["maneuverabilitySkill"] = 10,
        ["initiative"] = 55,
    };

    private SimulationEngine CreateEngine()
    {
        var ship = new JsonObject
        {
            ["objectId"] = "SPC-0001",
            ["objectType"] = "PlayerShip",
            ["shipClassId"] = "ship.tetrarch",
            ["persistenceType"] = "Permanent",
            ["name"] = "Tetrarch",
            ["positionX"] = 0,
            ["positionY"] = 0,
            ["speedMps"] = 0,
            ["directionDegrees"] = 0,
            ["movementType"] = "Stationary",
            ["isKnown"] = true,
            ["hullLayout"] = HullLayout(),
            ["modules"] = new JsonArray(Bay("BAY-01", 0), Bay("BAY-02", 2), Cargo("CARGO-01", 42)),
            ["crew"] = new JsonArray(
                Crew("CHR-0001", "Captain"), Crew("CHR-PLAYER-DEFENSE", "Officer"),
                Crew("CHR-PILOT-1", "Pilot"), Crew("CHR-PILOT-2", "Pilot"),
                Crew("CHR-PILOT-3", "Pilot"), Crew("CHR-PILOT-4", "Pilot")),
        };
        var scenario = new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "tk0003", ["name"] = "tk0003" },
            ["gameState"] = new JsonObject
            {
                ["masterSeed"] = 42,
                ["gameTimeMs"] = 0,
                ["currentSpeed"] = "Speed0",
                ["playerShipObjectId"] = "SPC-0001",
                ["spaceObjects"] = new JsonArray(ship),
            },
        };
        File.WriteAllText(ScenarioPath, scenario.ToJsonString());

        var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath);
        engine.SetOwnedWingForTests("SPC-0001", new[]
        {
            Drone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0, 1, 6),
            Drone("DRN-PLAYER-02", "CHR-PILOT-2", "BAY-01", 1, 0, 12),
            Drone("DRN-PLAYER-03", "CHR-PILOT-3", "BAY-02", 0, 0, 12),
            Drone("DRN-PLAYER-04", "CHR-PILOT-4", "BAY-02", 1, 0, 6),
        });
        return engine;
    }

    private static OwnedDroneData Drone(string id, string crewId, string bay, int slot, int ammo, int capacity) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: "heavy.sword.shield", WeaponId: "gun.sword.t1", ArmorId: "plate.medium",
        ShieldId: "shield.t1", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: ammo, AmmoCapacity: capacity, EngineIds: ["drive.standard", "drive.standard"]);

    [Fact]
    public async Task LocalRearmMatchesEngineReceipt()
    {
        using var directEngine = CreateEngine();
        using var connectionEngine = CreateEngine();
        await using var connection = new LocalGameSessionConnection(connectionEngine);

        var request = new RearmDroneRequest("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0);
        var direct = directEngine.RearmDrone(request);
        var viaConnection = await connection.RearmDroneAsync(request);

        Assert.Equal(direct.Accepted, viaConnection.Accepted);
        Assert.Equal(direct.Transferred, viaConnection.Transferred);
        Assert.Equal(direct.Revision, viaConnection.Revision);
        Assert.Equal(direct.Reason, viaConnection.Reason);
    }

    [Fact]
    public async Task CancelledBeforeSendDoesNotSpend()
    {
        using var engine = CreateEngine();
        await using var connection = new LocalGameSessionConnection(engine);

        var request = new RearmDroneRequest("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0);
        var cancelled = new CancellationToken(canceled: true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await connection.RearmDroneAsync(request, cancelled));

        Assert.Equal(1, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
        Assert.Equal(0, engine.GetCarrierRevision("SPC-0001"));
    }

    [Fact]
    public async Task RetryAfterLostReplyKeepsOneTransfer()
    {
        using var engine = CreateEngine();
        await using var connection = new LocalGameSessionConnection(engine);

        var request = new RearmDroneRequest("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0);
        var first = await connection.RearmDroneAsync(request);
        Assert.True(first.Accepted);

        // The reply was lost; the client retries the same CommandId — one transfer, not two.
        var retry = await connection.RearmDroneAsync(request);
        Assert.False(retry.Accepted);
        Assert.Equal("duplicate_command", retry.Reason);
        Assert.Equal(4, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
    }
}
