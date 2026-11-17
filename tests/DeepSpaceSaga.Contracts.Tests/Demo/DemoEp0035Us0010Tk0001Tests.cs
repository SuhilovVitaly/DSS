using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0010Tk0001Tests
{
    [Fact]
    public void FrozenBatchRoundtripPreservesUnknownEtaAndSingleEstimate()
    {
        var batch = new SensorContactReportBatch("batch:4", [new("report:pirate:01", "pirate:01", null,
            1250, 90, 2100, null, "no_intercept", 12, ["lair", "lair"])]);
        var json = JsonSerializer.Serialize(batch);
        Assert.Equal(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<SensorContactReportBatch>(json)));
        Assert.DoesNotContain("Layout", json);
        Assert.DoesNotContain("Weapon", json);
        Assert.Null(batch.Reports[0].EtaRealSecondsAt1X);
        Assert.Equal(12, batch.Reports[0].ApproximateDroneCount);
    }
}
