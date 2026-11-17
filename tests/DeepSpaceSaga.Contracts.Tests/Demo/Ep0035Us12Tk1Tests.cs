using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class Ep0035Us12Tk1Tests
{
    [Fact]
    public void Squadron_IsOneGroupWithTwoStableMembers()
    {
        var source = new PirateGroupSnapshot("group", 0, 0, 0, 0, 0, true, PirateGroupStatuses.Observed,
            Members: [new("group:carrier:1", "lair"), new("group:carrier:2", "lair")]);
        var restored = JsonSerializer.Deserialize<PirateGroupSnapshot>(JsonSerializer.Serialize(source))!;
        Assert.Equal(source.Id, restored.Id);
        Assert.Equal(source.Members, restored.Members);
        Assert.Equal(2, restored.Members.Select(m => m.CarrierId).Distinct().Count());
    }

    [Fact]
    public void DebugDefaults_AreNormalFogAndTenPercentSquads()
    {
        Assert.Equal(new DemoDebugSettings(true, 10), new DemoDebugSettings());
        var allSquads = new DemoDebugSettings(false, 100);
        Assert.Equal(allSquads, JsonSerializer.Deserialize<DemoDebugSettings>(JsonSerializer.Serialize(allSquads)));
    }
}
