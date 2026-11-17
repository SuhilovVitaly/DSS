using DeepSpaceSaga.Contracts;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0003Tk0001Tests
{
    [Fact]
    public void BindingAndExplicitContinueRoundtripPreserveAllIdentities()
    {
        var snapshot = new WorldEncounterSnapshot(new("battle:1", "encounter:1", "pirate:01"), WorldEncounterPhases.Captured, false, "sensor_report_pending");
        Assert.Equal(snapshot, JsonSerializer.Deserialize<WorldEncounterSnapshot>(JsonSerializer.Serialize(snapshot)));
        var command = new BeginEncounterBattleCommand("continue:1", 4, snapshot.Binding.EncounterId);
        Assert.Equal(command, JsonSerializer.Deserialize<BeginEncounterBattleCommand>(JsonSerializer.Serialize(command)));
    }
}
