using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatSessionRoundtripTests
{
    [Fact]
    public async Task Local_session_file_save_restores_world_without_legacy_combat()
    {
        await using var fixture = await CombatSessionFixture.Create();
        var rejected = await fixture.Launch("retired-session-launch");
        Assert.Equal(CommandReasonCodes.UnknownCommandType, Assert.Single(rejected.CommandResults).ReasonCode);
        var before = await fixture.Advance(1000);
        var restored = await fixture.SaveAndRestore();
        Assert.Equal(before.MotionTimeMs, restored.MotionTimeMs);
        Assert.Equal(before.GameTimeMs, restored.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, restored.CurrentSpeed);
        var player = Assert.Single(restored.Objects, o => o.ObjectId == CombatSessionFixture.Player);
        var previous = Assert.Single(before.Objects, o => o.ObjectId == CombatSessionFixture.Player);
        Assert.Equal((previous.X, previous.Y, previous.SpeedKmS, previous.Direction), (player.X, player.Y, player.SpeedKmS, player.Direction));
        Assert.NotEmpty(restored.InstalledModules);
        Assert.Equal(PlayerRelation.Neutral, Assert.Single(restored.Objects, o => o.ObjectId == CombatSessionFixture.Target).RelationToPlayer);
    }
}

/// <summary>Real content, local transport, file writer and loader, with a deterministic physical clock.</summary>
internal sealed class CombatSessionFixture : IAsyncDisposable
{
    internal const string Player = "SPC-0001", Target = "SPC-0002", EngineModule = "MOD-PLAYER-ENGINE-01";
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static string Settings => Path.Combine(Root, "Settings.json");
    internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "dss-combat-session-" + Guid.NewGuid().ToString("N"));
    internal SimulationEngine Engine { get; }
    internal LocalGameSessionConnection Connection { get; }
    internal LocalGameSessionConnection? Restored { get; private set; }
    private long _now;
    private CombatSessionFixture(bool defense)
    {
        Directory.CreateDirectory(DirectoryPath);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Settings, out _, out _);
        Engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _now)));
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json"));
        var scenario = source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    Ai = null,
                    PositionX = o.ObjectId == Player ? 0 : 600,
                    PositionY = 0,
                    SpeedMps = o.ObjectId == Player ? 1000 : 0,
                    DirectionDegrees = 90,
                    MovementType = o.ObjectId == Player ? "Linear" : "Stationary",
                    Modules = o.Modules!.Select(m => m with { PowerState = m.ModuleTypeId.StartsWith("module.engine.") || o.ObjectId != Player ? "Off" : "On" }).ToArray()
                }).ToArray()
            }
        };
        Engine.LoadScenario(scenario);
        Connection = new(Engine, DirectoryPath);
    }
    internal static async Task<CombatSessionFixture> Create(bool defense = false)
    {
        var fixture = new CombatSessionFixture(defense);
        await First(fixture.Connection);
        return fixture;
    }
    internal static PlayerCommand PointLaunch(string id) => new(id, 1, Player, EngineModule, "torpedo.launch", TargetWorldX: 600, TargetWorldY: 0);
    internal async Task<AuthoritativeSnapshot> Launch(string id)
    {
        await Connection.SendCommandAsync(PointLaunch(id));
        return Engine.CaptureSnapshot();
    }
    internal async Task<AuthoritativeSnapshot> Advance(long deltaMs)
    {
        await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
        Interlocked.Add(ref _now, deltaMs * 4);
        await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
        return Engine.CaptureSnapshot();
    }
    internal async Task<AuthoritativeSnapshot> SaveAndRestore()
    {
        await Connection.SaveAsync("combat");
        if (Restored is not null) await Restored.DisposeAsync();
        Restored = LocalGameSessionConnection.CreateFromSaveFile(Settings, Path.Combine(DirectoryPath, "combat.json"));
        return await First(Restored);
    }
    private static async Task<AuthoritativeSnapshot> First(LocalGameSessionConnection connection)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        return snapshots.Current;
    }
    public async ValueTask DisposeAsync()
    {
        if (Restored is not null) await Restored.DisposeAsync();
        await Connection.DisposeAsync();
        Directory.Delete(DirectoryPath, true);
    }
}
