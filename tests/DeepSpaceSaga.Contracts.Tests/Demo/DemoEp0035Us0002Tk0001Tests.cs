using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0002Tk0001Tests
{
    [Fact]
    public void ObservedMotionRoundtripContainsNoPrivatePatrolState()
    {
        var observation = new PursuitSnapshot("pirate:01", "pirate:01:3", PursuitStatuses.Receding, 2000, -50, 1000);
        var json = JsonSerializer.Serialize(observation);
        Assert.Equal(observation, JsonSerializer.Deserialize<PursuitSnapshot>(json));
        Assert.DoesNotContain("LastKnown", json);
        Assert.DoesNotContain("Waypoint", json);
    }
}
