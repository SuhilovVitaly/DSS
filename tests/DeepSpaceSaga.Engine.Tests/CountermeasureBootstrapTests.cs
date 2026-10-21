using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureBootstrapTests
{
    [Fact]
    public void Independent_launchers_resume_separate_launch_based_deadlines_through_real_file_load()
    {
        using var engine = Point.Create();
        var initial = Point.At(engine).InstalledModules.Where(m => m.Defense is not null).ToArray(); Assert.Equal(2, initial.Length);
        Assert.NotEqual(initial[0].Operator!.CrewId, initial[1].Operator!.CrewId);
        engine.ReceiveCommand(Point.Shot("first")); Point.At(engine); Point.At(engine, 1000);
        engine.ReceiveCommand(Point.Shot("second", Point.Counter2, y: -30)); Point.At(engine, 1000);
        var save = Point.Save(engine, 1100);
        string path = Path.Combine(Path.GetTempPath(), "point-dual-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            using var loaded = EngineContentLoader.CreateEngineFromSaveFile(Path.Combine(Point.Root, "Settings.json"), path);
            var first = Point.At(loaded, 12500).InstalledModules.Where(m => m.Defense is not null).ToDictionary(m => m.ModuleId);
            Assert.Equal(DefenseState.Ready, first[Point.Counter].Defense!.State); Assert.Null(first[Point.Counter].Defense!.ReloadDueMotionTimeMs);
            Assert.Equal(DefenseState.Reloading, first[Point.Counter2].Defense!.State); Assert.Equal(13500, first[Point.Counter2].Defense!.ReloadDueMotionTimeMs);
            Assert.All(Point.At(loaded, 13500).InstalledModules.Where(m => m.Defense is not null), m => Assert.Equal(DefenseState.Ready, m.Defense!.State));
            Assert.Equal(engine.CaptureLaunchScatterState(), loaded.CaptureLaunchScatterState());
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Generated_source_and_packaged_world_keep_point_loadout_operators_and_group_geometry(bool deployed)
    {
        var testOutput = new DirectoryInfo(AppContext.BaseDirectory);
        string root = deployed ? Path.Combine(Point.Root, "bin", testOutput.Parent!.Name, testOutput.Name) : Point.Root;
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
        var snapshot = Point.At(engine); Assert.Equal(3, snapshot.PlayerCrewCount); Assert.Equal(3, snapshot.InstalledModules.Sum(m => m.CabinesCount ?? 0));
        var counters = snapshot.InstalledModules.Where(m => m.Defense is not null).ToArray();
        Assert.Equal(new[] { Point.Counter, Point.Counter2 }, counters.Select(m => m.ModuleId));
        Assert.NotEqual(counters[0].Operator!.CrewId, counters[1].Operator!.CrewId);
        Assert.All(counters, m =>
        {
            Assert.False(m.Defense!.AutoEnabled); Assert.Equal(DefenseState.Ready, m.Defense.State); Assert.Equal(30, m.Defense.RangeKm);
            Assert.Equal(12500, m.Defense.ReloadDurationMs); Assert.Equal(new[] { "wide", "fast" }, m.Defense.Profiles!.Value.Select(p => p.ProfileId)); Assert.Null(m.DefensePreview);
        });
        var player = snapshot.Objects.Single(o => o.ObjectId == Point.Player); var enemy = snapshot.Objects.Single(o => o.ObjectId == Point.Enemy);
        Assert.Equal(-10000, enemy.X - player.X, 6); Assert.Equal(-5000, enemy.Y - player.Y, 6);
        var save = engine.CaptureSaveState(); Assert.Equal(3, save.GameState.DefenseState!.Launchers.Count);
        Assert.All(save.GameState.DefenseState.Launchers, l => Assert.False(l.State.AutoEnabled));
        Assert.Equal(3, save.GameState.SpaceObjects.Single(o => o.ObjectId == Point.Player).Crew!.Count);
        Assert.Equal(2, save.GameState.SpaceObjects.Single(o => o.ObjectId == Point.Enemy).Crew!.Count);
    }

    [Fact]
    public void Missing_point_operator_projects_no_operator_and_rejects_without_draws()
    {
        using var engine = Point.Create(configure: o => o.ObjectId != Point.Player ? o : o with
        { Modules = o.Modules!.Select(m => m.ModuleId is Point.Counter or Point.Counter2 ? m with { OperatorCrewId = null } : m).ToArray() });
        var initial = Point.At(engine); Assert.All(initial.InstalledModules.Where(m => m.Defense is not null), m =>
        { Assert.Equal(DefenseState.NoOperator, m.Defense!.State); Assert.Null(m.Defense.Operator); });
        engine.ReceiveCommand(Point.Shot()); Assert.Equal(CommandReasonCodes.NoWeaponOperator, Assert.Single(Point.At(engine).CommandResults).ReasonCode);
        Assert.Equal(0UL, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Npc_counter_capability_does_not_grant_player_authority_or_automatic_counter_fire()
    {
        using var engine = Point.Create();
        engine.ReceiveCommand(Point.Shot() with { ObjectId = Point.Enemy, ModuleId = "MOD-PIRATE-COUNTERMEASURE-01" });
        var state = Point.At(engine, 1000); Assert.Equal(CommandReasonCodes.UnknownObject, Assert.Single(state.CommandResults).ReasonCode);
        Assert.DoesNotContain(state.Objects, o => o.OwnProjectile is not null || o.ObservedProjectile is not null);
        Assert.Equal(0UL, engine.CaptureLaunchScatterState().Counter);
    }
}
