using System.Text.Json;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0009Tk0003Tests
{
    [Fact]
    public void DemoStart_UsesOneExistingOfficerAndInstalledSensor()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var snapshot = engine.CaptureSnapshot();
        var sensor = snapshot.DemoCampaign!.SensorOperator!;
        var crew = Assert.Single(snapshot.CarrierOperations!.Crew.Where(c => c.Id == sensor.CrewId));
        Assert.Equal("CHR-PLAYER-DEFENSE", crew.Id);
        Assert.Equal(10, crew.SensorRangeBonusPercent);
        Assert.Equal("MOD-PLAYER-SENSOR-01", sensor.ModuleId);
        Assert.Equal(2640, sensor.EffectiveRadiusWorldUnits);
        Assert.Equal(2400, sensor.BaseRadiusWorldUnits);
        Assert.Null(sensor.AvailabilityReason);
    }

    [Fact]
    public void Labels_RuEnParity()
    {
        var root = DemoEp0033Us0001Tk0003Tests.Root();
        using var ru = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Data", "Locale", "Russian.json")));
        using var en = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Data", "Locale", "English.json")));
        var keys = ru.RootElement.EnumerateObject().Where(p => p.Name.StartsWith("Ship.Sensor.", StringComparison.Ordinal)).Select(p => p.Name).Order().ToArray();
        Assert.Equal(6, keys.Length);
        Assert.Equal(keys, en.RootElement.EnumerateObject().Where(p => p.Name.StartsWith("Ship.Sensor.", StringComparison.Ordinal)).Select(p => p.Name).Order().ToArray());
        Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(en.RootElement.GetProperty(key).GetString())));
    }
}
