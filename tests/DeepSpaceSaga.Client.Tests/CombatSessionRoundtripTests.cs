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
    public async Task Local_session_file_save_restores_combat_snapshots()
    {
        await using var fixture = await CombatSessionFixture.Create();
        await fixture.Launch("session-launch");
        var before = await fixture.Advance(12345);
        var restored = await fixture.SaveAndRestore();
        Assert.Equal(before.MotionTimeMs, restored.MotionTimeMs);
        Assert.Equal(before.GameTimeMs, restored.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, restored.CurrentSpeed);
        Assert.Equal(JsonSerializer.Serialize(before.Objects), JsonSerializer.Serialize(restored.Objects));
        Assert.Equal(JsonSerializer.Serialize(before.InstalledModules), JsonSerializer.Serialize(restored.InstalledModules));
        Assert.Empty(restored.CombatImpacts);
        Assert.NotEmpty(Assert.Single(restored.Objects.Where(o => o.Torpedo is not null)).Torpedo!.Trail);
        await fixture.Restored!.SendCommandAsync(CombatSessionFixture.Fire("session-launch"));
        await fixture.Restored.SaveAsync("again");
        var saved = ScenarioLoader.LoadFromFile(Path.Combine(fixture.DirectoryPath, "again.json"), true);
        Assert.Single(saved.GameState.CombatState!.Projectiles);
        Assert.Equal(1, saved.GameState.CombatState.ProjectileSequence);
    }
}

/// <summary>Real content, local transport, file writer and loader, with a deterministic physical clock.</summary>
internal sealed class CombatSessionFixture : IAsyncDisposable
{
    internal const string Player = "SPC-0001", Target = "SPC-0002", Launcher = "MOD-PLAYER-TORPEDO-01";
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
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        if (!defense) scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects
            .Select(o => o with { Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = false }).ToArray() }).ToArray()
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
    internal static PlayerCommand Fire(string id) => new(id, 1, Player, Launcher, CombatCommandTypes.Fire, Target);
    internal async Task<AuthoritativeSnapshot> Launch(string id)
    {
        await Connection.SendCommandAsync(Fire(id));
        return Engine.CaptureSnapshot();
    }
    internal async Task<AuthoritativeSnapshot> Advance(long deltaMs)
    {
        await Connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
        Interlocked.Add(ref _now, deltaMs);
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
