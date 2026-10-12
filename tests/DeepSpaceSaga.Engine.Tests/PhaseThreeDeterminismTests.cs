using System.Text.Json;
using DeepSpaceSaga.Contracts;
using Battle = DeepSpaceSaga.Engine.Tests.PhaseThreeCombatEndToEndTests.Battle;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeDeterminismTests
{
    [Theory]
    [InlineData(1ul)]
    [InlineData(42ul)]
    [InlineData(99ul)]
    public async Task Single_step_and_partitioned_battle_match(ulong seed)
    {
        await using var whole = new Battle(seed);
        await using var split = new Battle(seed);
        foreach (var battle in new[] { whole, split })
        {
            await battle.Connection.SendCommandAsync(new("brake", 1, Battle.Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Brake));
            battle.At(0);
        }
        var expected = whole.At(1800000);
        foreach (long time in new long[] { 100, 1700, 9100, 55100, 100100, 500000, 990000, 1200000, 1500000, 1800000 })
        {
            var beforePause = split.At(time, SimulationSpeed.Speed4);
            var paused = split.Pause(17000);
            Assert.Equal(beforePause.MotionTimeMs, paused.MotionTimeMs);
            Assert.Equal(beforePause.GameTimeMs, paused.GameTimeMs);
            Assert.Equal(JsonSerializer.Serialize(beforePause.Objects), JsonSerializer.Serialize(paused.Objects));
        }
        var actual = split.Engine.CaptureSnapshot();
        Assert.NotEmpty(expected.CombatJournal);
        Assert.Contains(expected.CombatJournal, e => e.Roll is not null);
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.Equal(expected.EffectiveOutcome, actual.EffectiveOutcome);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
    }
}
