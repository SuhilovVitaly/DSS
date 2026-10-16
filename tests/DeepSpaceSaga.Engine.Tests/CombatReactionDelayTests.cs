using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReactionDelayTests
{
    [Fact]
    public void Hostile_waits_after_range_entry_and_again_after_cooldown()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        Assert.DoesNotContain(At(engine, 1).Objects, o => o.Torpedo is not null);
        var pending = Assert.Single(engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.CombatReactionDelays!);
        Assert.InRange(pending.DueAt - pending.EligibleAt, 500, 1250);
        Assert.DoesNotContain(At(engine, pending.DueAt - 1).Objects, o => o.Torpedo is not null);
        var first = Assert.Single(At(engine, pending.DueAt).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(pending.DueAt, first.LaunchMotionTimeMs);
        long ready = (long)engine.CaptureSaveStateForTests(pending.DueAt, SimulationSpeed.Speed0).GameState.CombatState!
            .Launchers.Single(l => l.OwnerObjectId == Target).State.ReadyAtMotionTimeMs!;
        At(engine, ready);
        var next = Assert.Single(engine.CaptureSaveStateForTests(ready, SimulationSpeed.Speed0).GameState.CombatReactionDelays!);
        Assert.InRange(next.DueAt - ready, 500, 1250);
        Assert.Single(At(engine, next.DueAt - 1).CombatJournal.Where(e => e.Type == CombatEventType.Launch));
        Assert.Equal(2, At(engine, next.DueAt).CombatJournal.Count(e => e.Type == CombatEventType.Launch));
    }

    [Fact]
    public void Player_automatic_defense_waits_but_manual_shot_does_not()
    {
        var s = HostileTorpedoFireTests.HostileScenario(600);
        s = s with
        {
            GameState = s.GameState with
            {
                SpaceObjects = s.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o :
            o with { Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = true }).ToArray() }).ToArray()
            }
        };
        using var auto = Create(s);
        ManualCountermeasureFireTests.LaunchEnemy(auto);
        Assert.DoesNotContain(At(auto, 2).Objects, o => o.Countermeasure is not null);
        var pending = auto.CaptureSaveStateForTests(2, SimulationSpeed.Speed0).GameState.CombatReactionDelays!
            .Where(r => r.Kind == "defense").MinBy(r => r.DueAt)!;
        Assert.InRange(pending.DueAt - pending.EligibleAt, 500, 1250);
        Assert.DoesNotContain(At(auto, pending.DueAt - 1).Objects, o => o.Countermeasure is not null);
        Assert.Contains(At(auto, pending.DueAt).Objects, o => o.Countermeasure?.OwnerObjectId == Player);
        using var manual = Create(HostileTorpedoFireTests.HostileScenario());
        string target = ManualCountermeasureFireTests.LaunchEnemy(manual);
        manual.ReceiveCommand(ManualCountermeasureFireTests.Manual("immediate", target));
        Assert.Equal(1, Assert.Single(At(manual, 1).Objects, o => o.Countermeasure is not null).Countermeasure!.LaunchMotionTimeMs);
    }

    [Fact]
    public void Pending_delay_survives_pause_file_roundtrip_and_partitioning()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario()); At(engine, 1);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        var due = Assert.Single(save.GameState.CombatReactionDelays!).DueAt;
        for (int i = 0; i < 5; i++) Assert.DoesNotContain(At(engine, 1, i * 1000).Objects, o => o.Torpedo is not null);
        using var loaded = Create(); loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        At(loaded, 100); At(loaded, 400); At(loaded, due - 1);
        Assert.Equal(JsonSerializer.Serialize(At(engine, 20000).CombatJournal), JsonSerializer.Serialize(At(loaded, 20000).CombatJournal));
    }

    [Fact]
    public void Player_defense_pending_deadline_resumes_identically()
    {
        var source = HostileTorpedoFireTests.HostileScenario(1000);
        source = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                { Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = true }).ToArray() }).ToArray()
            }
        };
        using var engine = Create(source);
        ManualCountermeasureFireTests.LaunchEnemy(engine);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        var pending = save.GameState.CombatReactionDelays!.Where(r => r.Kind == "defense").ToArray();
        Assert.NotEmpty(pending);
        using var restored = Create();
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        At(restored, 100); At(restored, pending.Min(r => r.DueAt) - 1);
        Assert.Equal(JsonSerializer.Serialize(At(engine, 20000).CombatJournal),
            JsonSerializer.Serialize(At(restored, 20000).CombatJournal));
    }

    [Fact]
    public void Different_seeds_sample_different_delays_and_bad_save_is_atomic()
    {
        var values = new HashSet<long>();
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var s = HostileTorpedoFireTests.HostileScenario();
            using var engine = Create(s with { GameState = s.GameState with { MasterSeed = seed } }); At(engine, 1);
            var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0); var pending = Assert.Single(save.GameState.CombatReactionDelays!);
            values.Add(pending.DueAt); Assert.InRange(pending.DueAt - 1, 500, 1250);
            string before = JsonSerializer.Serialize(save);
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
            {
                GameState = save.GameState with
                { CombatReactionDelays = [pending with { DueAt = pending.EligibleAt + 499 }] }
            }, isSave: true));
            Assert.Equal(before, JsonSerializer.Serialize(engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0)));
        }
        Assert.True(values.Count > 1);
        Assert.Equal(0.25, SimulationSpeed.Speed1.MotionTimeMultiplier());
    }
}
