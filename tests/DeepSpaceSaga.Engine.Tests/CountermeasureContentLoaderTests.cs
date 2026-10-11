using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CountermeasureContentLoaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dss-defense-content-" + Guid.NewGuid().ToString("N"));
    public CountermeasureContentLoaderTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, true);
    private const string Valid = """
        {"moduleImplementations":[{"typeId":"defense","displayName":"Defense","type":"module.countermeasure.launcher",
        "massKg":2000,"structurePointsMax":60,"powerConsumptionW":0,"baseCycleTimeMs":123,
        "countermeasureAccuracy":55,"countermeasureManualRangeKm":200,"countermeasureMaxFlightTimeMs":60000,
        "countermeasureSpeedKmS":12,"countermeasureTurnRateDegPerSec":90,
        "countermeasureRangeKm":100,"countermeasureReloadMs":10000}]}
        """;
    private ModuleTypeDefinition Load(JsonNode root)
    {
        string path = Path.Combine(_directory, "modules.json");
        File.WriteAllText(path, root.ToJsonString());
        return Assert.Single(EngineContentLoader.LoadModuleImplementations(path,
            [new ModuleCategoryDefinition("module.countermeasure.launcher", "Defense", 1, ImmutableArray<string>.Empty)]));
    }

    [Fact]
    public void Countermeasure_parameters_load_and_validate()
    {
        var module = Load(JsonNode.Parse(Valid)!);
        Assert.Equal(55m, module.CountermeasureAccuracy);
        Assert.Equal(200, module.CountermeasureManualRangeKm);
        Assert.Equal(60000, module.CountermeasureMaxFlightTimeMs);
        Assert.Equal(12, module.CountermeasureSpeedKmS);
        Assert.Equal(90, module.CountermeasureTurnRateDegPerSec);
        Assert.Equal(100, module.CountermeasureRangeKm);
        Assert.Equal(10000, module.CountermeasureReloadMs);
        Assert.Equal(123, module.BaseCycleTimeMs);
        Assert.Equal("module.countermeasure.launcher", module.CategoryTypeId);
    }

    [Theory]
    [InlineData("countermeasureAccuracy")]
    [InlineData("countermeasureManualRangeKm")]
    [InlineData("countermeasureMaxFlightTimeMs")]
    [InlineData("countermeasureSpeedKmS")]
    [InlineData("countermeasureTurnRateDegPerSec")]
    [InlineData("countermeasureRangeKm")]
    [InlineData("countermeasureReloadMs")]
    public void Partial_countermeasure_parameters_rejected(string field)
    {
        var root = JsonNode.Parse(Valid)!;
        root["moduleImplementations"]![0]!.AsObject().Remove(field);
        Assert.Throws<ContentException>(() => Load(root));
    }

    [Theory]
    [InlineData("countermeasureAccuracy", "-1")]
    [InlineData("countermeasureAccuracy", "100.1")]
    [InlineData("countermeasureAccuracy", "1e999")]
    [InlineData("countermeasureManualRangeKm", "0")]
    [InlineData("countermeasureManualRangeKm", "-1")]
    [InlineData("countermeasureManualRangeKm", "1e999")]
    [InlineData("countermeasureMaxFlightTimeMs", "0")]
    [InlineData("countermeasureMaxFlightTimeMs", "-1")]
    [InlineData("countermeasureMaxFlightTimeMs", "1.5")]
    [InlineData("countermeasureMaxFlightTimeMs", "9223372036854775808")]
    [InlineData("countermeasureSpeedKmS", "0")]
    [InlineData("countermeasureSpeedKmS", "1e999")]
    [InlineData("countermeasureTurnRateDegPerSec", "-1")]
    [InlineData("countermeasureRangeKm", "0")]
    [InlineData("countermeasureReloadMs", "0")]
    [InlineData("countermeasureReloadMs", "1.5")]
    public void Invalid_countermeasure_parameters_rejected(string field, string value)
    {
        var root = JsonNode.Parse(Valid)!;
        root["moduleImplementations"]![0]![field] = JsonNode.Parse(value);
        Assert.Throws<ContentException>(() => Load(root));
    }

    [Fact]
    public void Legacy_content_without_defense_is_valid()
    {
        var legacy = new ModuleTypeDefinition("legacy", "Legacy", 1, 1, 1, 0, ImmutableArray<string>.Empty);
        GameDataRegistry.ValidateWeaponRatings(legacy);
        Assert.Null(legacy.CountermeasureBaseRating);
        Assert.Null(legacy.TorpedoBaseRating);
    }

    [Fact]
    public void Ratings_require_matching_category()
    {
        var module = Load(JsonNode.Parse(Valid)!);
        Assert.Throws<ContentException>(() => GameDataRegistry.ValidateWeaponRatings(module with { CategoryTypeId = "module.torpedo.launcher" }));
        Assert.Throws<ContentException>(() => GameDataRegistry.ValidateWeaponRatings(module with { TorpedoBaseRating = 30 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Manual_and_auto_ranges_are_independent(int accuracy)
    {
        var root = JsonNode.Parse(Valid)!;
        root["moduleImplementations"]![0]!["countermeasureAccuracy"] = accuracy;
        root["moduleImplementations"]![0]!["countermeasureManualRangeKm"] = 25.5;
        var module = Load(root);
        Assert.Equal((decimal)accuracy, module.CountermeasureAccuracy);
        Assert.Equal(100, module.CountermeasureRangeKm);
        Assert.Equal(25.5, module.CountermeasureManualRangeKm);
        Assert.Null(module.CountermeasureBaseRating);
    }

    [Fact]
    public void Weapon_schema_rejects_invalid_values_through_direct_registry()
    {
        var module = Load(JsonNode.Parse(Valid)!);
        foreach (var invalid in new[]
        {
            module with { CountermeasureAccuracy = null },
            module with { CountermeasureAccuracy = 100.1m },
            module with { CountermeasureManualRangeKm = double.NaN },
            module with { CountermeasureMaxFlightTimeMs = 0 },
            module with { CountermeasureRangeKm = double.PositiveInfinity },
            module with { CategoryTypeId = null }
        })
            Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [invalid], [], []));
    }
}
