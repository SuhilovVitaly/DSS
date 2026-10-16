using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Fixture = DeepSpaceSaga.Client.Tests.TorpedoCommandPanelTests.Fixture;

namespace DeepSpaceSaga.Client.Tests;

public class ManualDefenseUiFlowTests
{
    private static InstalledModuleSnapshot Module(string id, string target = "target") => new(id,
        "module.countermeasure.launcher.basic", "Defense", id == "defense-1" ? 2 : 3,
        [DefenseCommandTypes.Fire, DefenseCommandTypes.Enable, DefenseCommandTypes.Disable], "On", "Ready", 60,
        Commands: [new(DefenseCommandTypes.Fire, "Fire", "object"), new(DefenseCommandTypes.Enable, "Enable", "none"),
            new(DefenseCommandTypes.Disable, "Disable", "none")],
        Defense: new(false, new("crew-" + id, id, WeaponSkillType.CountermeasureDefense, 50, 55, 55), DefenseState.Ready),
        DefensePreview: new("player", id, target, 0, true, Accuracy: 0, TargetManeuverability: 5));

    private static void Configure(Fixture f)
    {
        f.Update(s => s with
        {
            InstalledModules = s.InstalledModules.Add(Module("defense-1")).Add(Module("defense-2")),
            Objects = s.Objects.Select(o => o.ObjectId == "player" ? o : o with
            {
                ObjectType = SpaceObjectType.Missile,
                RenderObjectType = SpaceObjectType.Missile,
                Torpedo = new("enemy", "launcher", "player", 0, 3, 90, 150, 0,
                    new(0, 1, TorpedoRoutePhase.Straight, true, [new(o.X, o.Y, 0, 3, 0, 10000)]), [],
                    CapturedManeuverability: 5, OwnerPlayerRelationAtLaunch: PlayerRelation.Enemy)
            }).ToImmutableArray()
        });
        TorpedoCommandPanelTests.SelectTarget(f);
    }

    private static CommandButtonGeometry Button(GameSessionScreen screen, string module, string type = DefenseCommandTypes.Fire, float scale = 1)
    {
        TorpedoCommandPanelTests.Render(screen);
        var row = screen.CommandsPanel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == module);
        if (!row.Opened)
        {
            screen.OnMouseDown(row.CaptionRect.MidX * scale, row.CaptionRect.MidY * scale);
            TorpedoCommandPanelTests.Render(screen);
        }
        return screen.CommandsPanel.AllCommandButtons.Single(b => b.Weapon?.ModuleId == module && b.CommandTypeId == type);
    }

    private static void Click(GameSessionScreen screen, CommandButtonGeometry button, float scale = 1) => screen.OnMouseDown(button.Rect.MidX * scale, button.Rect.MidY * scale);
    private static CommandResult Result(PlayerCommand c, CommandResultStatus status) => new(c.CommandId, c.ObjectId, c.ModuleId, c.CommandType, status, 0);

    [Fact]
    public async Task Double_click_only_blocks_its_module_and_results_match_command_identity()
    {
        await using var f = new Fixture(); Configure(f);
        var one = Button(f.Screen, "defense-1");
        Assert.True(one.Enabled); // Authoritative zero chance is still allowed.
        Click(f.Screen, one); Click(f.Screen, one);
        var two = Button(f.Screen, "defense-2");
        Assert.True(two.Enabled);
        Click(f.Screen, two); Click(f.Screen, two);
        Assert.Equal(2, f.Connection.Commands.Count);
        Assert.Equal(new[] { "defense-1", "defense-2" }, f.Connection.Commands.Select(c => c.ModuleId));
        var first = f.Connection.Commands[0]; var second = f.Connection.Commands[1];
        f.Update(s => s with { CommandResults = [Result(first with { CommandId = "unrelated" }, CommandResultStatus.Executed)] });
        Assert.False(Button(f.Screen, "defense-1").Enabled);
        f.Update(s => s with { CommandResults = [Result(first, CommandResultStatus.Deferred), Result(second, CommandResultStatus.Rejected)] });
        Assert.False(Button(f.Screen, "defense-1").Enabled);
        Assert.True(Button(f.Screen, "defense-2").Enabled);
        f.Update(s => s with { CommandResults = [Result(first, CommandResultStatus.Executed)] });
        f.Update(s => s with { CommandResults = [] }); // Receipt remains in SnapshotBuffer after coalescing.
        Assert.True(Button(f.Screen, "defense-1").Enabled);
        Assert.All(f.Connection.Commands, c => Assert.Equal("target", c.TargetObjectId));
    }

    [Fact]
    public async Task Selection_change_preserves_sent_target_and_stale_preview_disables_fire()
    {
        await using var f = new Fixture(); Configure(f);
        Click(f.Screen, Button(f.Screen, "defense-1"));
        f.Screen.OnMouseDown(740, 420);
        TorpedoCommandPanelTests.Render(f.Screen);
        Assert.Equal("hover", f.Screen.SelectedObjectId);
        Assert.Equal("target", Assert.Single(f.Connection.Commands).TargetObjectId);
        Assert.False(Button(f.Screen, "defense-2").Enabled);
        f.Update(s => s with
        {
            InstalledModules = s.InstalledModules.Select(m => m.DefensePreview is null ? m : m with
            { DefensePreview = m.DefensePreview with { TargetTorpedoId = "hover" } }).ToImmutableArray()
        });
        Assert.True(Button(f.Screen, "defense-2").Enabled);
        Click(f.Screen, Button(f.Screen, "defense-2"));
        Assert.Equal("hover", f.Connection.Commands[1].TargetObjectId);
    }

    [Theory]
    [InlineData("sync")]
    [InlineData("async")]
    [InlineData("cancel")]
    public async Task Transport_failure_releases_only_the_affected_module(string failure)
    {
        await using var f = new Fixture(); Configure(f);
        var transport = new TaskCompletionSource();
        f.Connection.OnSend = c => c.ModuleId == "defense-1"
            ? failure == "sync" ? throw new IOException("offline") : new(transport.Task) : ValueTask.CompletedTask;
        Click(f.Screen, Button(f.Screen, "defense-1"));
        Click(f.Screen, Button(f.Screen, "defense-2"));
        if (failure == "async") transport.SetException(new IOException("offline"));
        if (failure == "cancel") transport.SetCanceled();
        Assert.True(Button(f.Screen, "defense-1").Enabled);
        Assert.False(Button(f.Screen, "defense-2").Enabled);
    }

    [Fact]
    public async Task Toggle_addresses_second_module_without_target_or_first_module_pending()
    {
        await using var f = new Fixture(); Configure(f);
        Click(f.Screen, Button(f.Screen, "defense-2", DefenseCommandTypes.Enable));
        var command = Assert.Single(f.Connection.Commands);
        Assert.Equal("defense-2", command.ModuleId);
        Assert.Null(command.TargetObjectId);
        Assert.True(Button(f.Screen, "defense-1").Enabled);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("denied")]
    [InlineData("owner")]
    [InlineData("module")]
    [InlineData("target")]
    [InlineData("power")]
    public async Task Stale_enabled_button_rechecks_authoritative_preview_and_module(string reason)
    {
        await using var f = new Fixture(); Configure(f);
        var oldButton = Button(f.Screen, "defense-1");
        Assert.True(oldButton.Enabled);
        f.Update(s => s with
        {
            InstalledModules = s.InstalledModules.Select(m => m.ModuleId != "defense-1" ? m : m with
            {
                PowerState = reason == "power" ? "Off" : m.PowerState,
                DefensePreview = reason switch
                {
                    "missing" => null,
                    "denied" => m.DefensePreview! with { CanFire = false, Reason = "target_out_of_range" },
                    "owner" => m.DefensePreview! with { OwnerObjectId = "enemy" },
                    "module" => m.DefensePreview! with { ModuleId = "defense-2" },
                    "target" => m.DefensePreview! with { TargetTorpedoId = "hover" },
                    _ => m.DefensePreview
                }
            }).ToImmutableArray()
        });
        Click(f.Screen, oldButton);
        Assert.Empty(f.Connection.Commands);
        Assert.False(Button(f.Screen, "defense-1").Enabled);
        Assert.True(Button(f.Screen, "defense-2").Enabled);
    }

    [Fact]
    public async Task Panel_click_preserves_map_selection_and_foreground_overlay_consumes_it()
    {
        await using var f = new Fixture(); Configure(f);
        var button = Button(f.Screen, "defense-1");
        typeof(GameSessionScreen).GetField("_lastPanelRect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(f.Screen, button.Rect);
        Click(f.Screen, button);
        Assert.Empty(f.Connection.Commands);
        Assert.Equal("target", f.Screen.SelectedObjectId);
        button = Button(f.Screen, "defense-1");
        Click(f.Screen, button);
        Assert.Single(f.Connection.Commands);
        Assert.Equal("target", f.Screen.SelectedObjectId);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public async Task Two_paused_clicks_launch_two_modules_through_real_local_session(float scale)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        string directory = Path.Combine(Path.GetTempPath(), "dss-manual-defense-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        long now = 0;
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = 10000,
                    PositionY = o.ObjectId == "SPC-0001" ? 10000 : 10060,
                    SpeedMps = 0,
                    DirectionDegrees = 0,
                    MovementType = "Stationary",
                    Modules = o.Modules!.Select(m => m with
                    {
                        AutoDefenseEnabled = false,
                        PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
                    }).ToArray()
                }).ToArray()
            }
        };
        var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref now)));
        engine.LoadScenario(scenario);
        var connection = new LocalGameSessionConnection(engine, directory);
        var handle = new GameSessionHandle(connection);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle, uiScale: scale);
        try
        {
            await Wait(s => s.Objects.Length == 2);
            screen.OnActivated();
            await connection.SetSimulationSpeedAsync(SimulationSpeed.Speed1);
            Interlocked.Exchange(ref now, 4);
            await connection.SetSimulationSpeedAsync(SimulationSpeed.Speed0);
            var launched = await Wait(s => s.Objects.Any(o => o.Torpedo is not null));
            string target = launched.Objects.Single(o => o.Torpedo is not null).ObjectId;
            await connection.SetObjectInteractionStateAsync(null, target);
            await Wait(s => s.SelectedObjectId == target && s.InstalledModules.Count(m => m.DefensePreview?.CanFire == true) == 2);
            TorpedoCommandPanelTests.Render(screen);
            if (screen.IsPanelVisible)
            {
                var close = screen.LastCloseRect;
                screen.OnMouseDown(close.MidX * scale, close.MidY * scale);
            }
            foreach (string module in new[] { "MOD-PLAYER-COUNTERMEASURE-01", "MOD-PLAYER-COUNTERMEASURE-02" })
            {
                var button = Button(screen, module, scale: scale);
                Assert.True(button.Enabled);
                Click(screen, button, scale); Click(screen, button, scale);
            }
            var after = await Wait(s => s.Objects.Count(o => o.Countermeasure is not null) == 2);
            Assert.Equal(1, after.MotionTimeMs);
            Assert.Equal(SimulationSpeed.Speed0, after.CurrentSpeed);
            Assert.All(after.Objects.Where(o => o.Countermeasure is not null), o =>
            {
                Assert.Equal(target, o.Countermeasure!.TargetTorpedoId);
                Assert.Equal(LaunchMode.Manual, o.Countermeasure.Mode);
                Assert.Equal(1, o.Countermeasure.LaunchMotionTimeMs);
            });
            Assert.Equal(2, after.InstalledModules.Count(m => m.Defense?.State == DefenseState.Guiding));
            Assert.Empty(after.CombatJournal.Where(e => e.Roll is not null));
            Assert.Equal(target, screen.SelectedObjectId);
        }
        finally
        {
            screen.OnDeactivated();
            await handle.DisposeAsync();
            Directory.Delete(directory, true);
        }

        async Task<AuthoritativeSnapshot> Wait(Func<AuthoritativeSnapshot, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (handle.Buffer.Latest is not { } buffered || !predicate(buffered.Snapshot))
            {
                Assert.Null(handle.Failure);
                await Task.Delay(10, timeout.Token);
            }
            return handle.Buffer.Latest!.Snapshot;
        }
    }
}
