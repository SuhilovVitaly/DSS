using System.Text.Json;
using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Contracts.Tests;

public class TorpedoTerminationTests
{
    [Theory]
    [InlineData(TorpedoTerminationKind.Impact)]
    [InlineData(TorpedoTerminationKind.SelfDestruct)]
    [InlineData(TorpedoTerminationKind.Intercept)]
    public void Termination_kind_roundtrips(TorpedoTerminationKind kind)
    {
        var impact = new CombatImpactSnapshot(1, "torpedo", "owner", "module", "target", "", 0, 0, 0, TerminationKind: kind);
        Assert.Equal(kind, JsonSerializer.Deserialize<CombatImpactSnapshot>(JsonSerializer.Serialize(impact))!.TerminationKind);
    }
    [Fact]
    public void Legacy_impact_remains_impact()
    {
        var impact = JsonSerializer.Deserialize<CombatImpactSnapshot>("""{"EventId":1,"TorpedoObjectId":"t","OwnerObjectId":"o","LauncherModuleId":"m","TargetObjectId":"x","HitObjectId":"x","MotionTimeMs":1,"X":0,"Y":0}""")!;
        Assert.Equal(TorpedoTerminationKind.Impact, impact.TerminationKind);
    }
}
