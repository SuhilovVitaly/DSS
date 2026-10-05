using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class EconomyBalanceSeamTests
{
    private static string State(SimulationEngine e, long time, long motion = 0) => JsonSerializer.Serialize(
        SimulationEngine.NormalizeTradingContinuationForTests(e.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, motion).GameState));

    [Fact]
    public void Hourly_explicit_time_run_applies_each_economy_boundary_once()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        for (int hour = 0; hour <= 240; hour++)
        {
            long time = hour * GameCalendar.HourMs;
            var snapshot = f.Engine.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, 0);
            Assert.Equal(time, snapshot.GameTimeMs); Assert.Equal(0, snapshot.SimulationTimeMs);
            string before = State(f.Engine, time);
            f.Engine.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, 0);
            Assert.Equal(before, State(f.Engine, time));
            var manifest = f.Engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, 0).GameState.TradingEconomyContinuation!;
            Assert.Equal(time, manifest.LastProcessedMarketGameTimeMs); Assert.Equal(hour + 1, manifest.NextMarketEventSequence);
        }
    }

    [Fact]
    public void Repeated_target_is_idempotent_and_does_not_advance_motion()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        f.Send(QuotedTradeExecutionTests.BridgeModuleId, NavigationComputerCommandTypes.Undock, target: f.Destination);
        f.Send(QuotedTradeExecutionTests.EngineModuleId, ShipEngineCommandTypes.Accelerate); f.Advance(1000);
        f.Send(QuotedTradeExecutionTests.EngineModuleId, NavigationComputerCommandTypes.Approach, target: f.Destination);
        long motion = f.MotionTime;
        var before = f.Save().GameState;
        var snapshot = f.Engine.CaptureSnapshotForTests(GameCalendar.DayMs, SimulationSpeed.Speed0, motion);
        Assert.Equal(motion, snapshot.SimulationTimeMs);
        var after = f.Engine.CaptureSaveStateForTests(GameCalendar.DayMs, SimulationSpeed.Speed0, motion).GameState;
        Assert.Equal(before.VoyageState, after.VoyageState);
        Assert.Equal(JsonSerializer.Serialize(before.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules),
            JsonSerializer.Serialize(after.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules));
        string saved = State(f.Engine, GameCalendar.DayMs, motion);
        f.Engine.CaptureSnapshotForTests(GameCalendar.DayMs, SimulationSpeed.Speed0, motion);
        Assert.Equal(saved, State(f.Engine, GameCalendar.DayMs, motion));
        // Existing lower calendar target does not rewind the owning cursor or apply economic effects;
        // diagnostics forbid descending schedules. Compare the owner again at its processed target.
        f.Engine.CaptureSnapshotForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, motion);
        Assert.Equal(saved, State(f.Engine, GameCalendar.DayMs, motion));
    }

    [Fact]
    public void Ten_day_continuous_and_midpoint_save_load_states_are_canonical_equal()
    {
        using var f = TradingVoyageFixture.Create(calendarRatio: 1);
        SimulationEngine? loaded = null;
        try
        {
            for (int hour = 1; hour <= 240; hour++)
            {
                long time = hour * GameCalendar.HourMs;
                f.Engine.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, 0);
                if (hour == 120)
                {
                    var save = f.Engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, 0);
                    loaded = new(QuotedTradeExecutionTests.RealRegistry());
                    loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
                    Assert.Equal(State(f.Engine, time), State(loaded, time));
                }
                if (loaded is null) continue;
                loaded.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, 0);
                Assert.Equal(State(f.Engine, time), State(loaded, time));
            }
            Assert.NotNull(loaded);
        }
        finally { loaded?.Dispose(); }
    }

    [Fact]
    public void Friend_tool_uses_existing_seams_without_public_time_api()
    {
        var friends = typeof(SimulationEngine).Assembly.GetCustomAttributes<InternalsVisibleToAttribute>();
        Assert.Single(friends, f => f.AssemblyName == "DeepSpaceSaga.EconomyBalance");
        foreach (string name in new[] { "CaptureSnapshotForTests", "CaptureSaveStateForTests" })
        {
            var method = typeof(SimulationEngine).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method); Assert.False(method.IsPublic);
        }
    }
}
