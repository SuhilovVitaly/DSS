using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0010Tk0001Tests
{
    [Fact]
    public void SelfDestructWritesSingleLogEntry_RepeatCaptureAndLoadDoNotDuplicate()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.GetTacticalBattleState()!;
        var entries = state.Log.Where(e => e.Kind == TacticalBattleLogKind.SelfDestructed).ToArray();
        Assert.NotEmpty(entries);
        Assert.Equal(entries.Length, entries.Select(e => e.TargetId).Distinct().Count());
        Assert.All(entries, e => Assert.Equal(TacticalUnitKind.Carrier, e.Unit!.Kind));
        Assert.Equal(entries.Length, engine.GetTacticalBattleState()!.Log.Count(e => e.Kind == TacticalBattleLogKind.SelfDestructed));
        var restored = JsonSerializer.Deserialize<TacticalBattleSnapshot>(JsonSerializer.Serialize(state))!;
        Assert.Equal(entries.Length, restored.Log.Count(e => e.Kind == TacticalBattleLogKind.SelfDestructed));
        Assert.Contains(state.Log, e => e.Kind == TacticalBattleLogKind.Destroyed && e.Unit?.Kind == TacticalUnitKind.Drone);
    }
    [Fact]
    public void EnumValuesAppendOnly()
    {
        Assert.Equal(7, (int)TacticalBattleLogKind.Destroyed);
        Assert.Equal(17, (int)TacticalBattleLogKind.Moved);
        Assert.Equal(18, (int)TacticalBattleLogKind.SelfDestructed);
    }
}
