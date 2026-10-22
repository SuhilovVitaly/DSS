using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0001TK0005Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-point-profiles-{Guid.NewGuid():N}");
    private string ModulesPath => Path.Combine(_directory, "launchers.json");
    private static JsonObject Module(bool counter = false) => JsonNode.Parse(counter ? """
        {"typeId":"module.countermeasure.launcher.test","displayName":"Defense","type":"module.countermeasure.launcher",
         "massKg":2000,"structurePointsMax":150,"powerConsumptionW":0,"baseCycleTimeMs":1000,
         "rangeKm":30,"reloadMs":12500,"scatterPercent":0.08,
         "profiles":[{"id":"wide","speedKmS":15,"blastRadiusKm":1},{"id":"fast","speedKmS":25,"blastRadiusKm":0.5}]}
        """ : """
        {"typeId":"module.torpedo.launcher.test","displayName":"Torpedo","type":"module.torpedo.launcher",
         "massKg":2000,"structurePointsMax":150,"powerConsumptionW":0,"baseCycleTimeMs":1000,
         "rangeKm":70,"reloadMs":10000,"scatterPercent":0.02,"torpedoDamage":200,"armorPiercingPercent":20,
         "profiles":[{"id":"slow","speedKmS":6,"blastRadiusKm":9},{"id":"medium","speedKmS":10,"blastRadiusKm":5}]}
        """)!.AsObject();

    public PointCombatUS0001TK0005Tests() => Directory.CreateDirectory(_directory);

    private ModuleTypeDefinition Load(JsonObject module)
    {
        File.WriteAllText(ModulesPath, new JsonObject { ["moduleImplementations"] = new JsonArray(module) }.ToJsonString());
        string categoryId = module["type"]!.GetValue<string>();
        var category = new ModuleCategoryDefinition(categoryId, "Launcher", 1, []);
        return Assert.Single(EngineContentLoader.LoadModuleImplementations(ModulesPath, [category]));
    }

    private void Reject(JsonObject module, string field)
    {
        var error = Assert.Throws<ContentException>(() => Load(module));
        Assert.Contains(ModulesPath, error.Message);
        Assert.Contains(field, error.Message);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("blank")]
    [InlineData("speed")]
    [InlineData("radius")]
    [InlineData("range")]
    [InlineData("scatter")]
    [InlineData("reload")]
    [InlineData("null-list")]
    [InlineData("null-member")]
    public void Launcher_profiles_require_unique_ids_positive_speed_radius_and_range(string invalid)
    {
        var module = Module();
        string field;
        switch (invalid)
        {
            case "empty": module["profiles"] = new JsonArray(); field = "profiles"; break;
            case "missing": module.Remove("profiles"); field = "profiles"; break;
            case "duplicate": module["profiles"]![1]!["id"] = "slow"; field = "id"; break;
            case "blank": module["profiles"]![0]!["id"] = " "; field = "id"; break;
            case "speed": module["profiles"]![0]!["speedKmS"] = 0; field = "speedKmS"; break;
            case "radius": module["profiles"]![0]!["blastRadiusKm"] = -1; field = "blastRadiusKm"; break;
            case "range": module["rangeKm"] = 0; field = "rangeKm"; break;
            case "scatter": module["scatterPercent"] = -0.01; field = "scatterPercent"; break;
            case "null-list": module["profiles"] = null; field = "profiles"; break;
            case "null-member": module["profiles"]![0] = null; field = "profiles"; break;
            default: module["reloadMs"] = -1; field = "reloadMs"; break;
        }
        Reject(module, field);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("speedKmS")]
    [InlineData("blastRadiusKm")]
    public void Missing_profile_members_name_file_and_field(string field)
    {
        var module = Module();
        module["profiles"]![0]!.AsObject().Remove(field);
        Reject(module, field);
    }

    [Fact]
    public void Launcher_profile_schema_distinguishes_damage_from_countermissile()
    {
        var torpedo = Load(Module());
        Assert.True(torpedo.IsPointLauncher);
        Assert.Equal(200, torpedo.TorpedoDamage);
        Assert.Equal(20m, torpedo.ArmorPiercingPercent);
        Assert.Equal(70, torpedo.RangeKm);
        Assert.Equal(10000, torpedo.ReloadMs);
        Assert.Equal(0.02, torpedo.ScatterPercent);
        Assert.Equal("slow", torpedo.Profiles[0].ProfileId);
        Assert.Equal(6, torpedo.Profiles[0].SpeedKmS);
        Assert.Equal(9, torpedo.Profiles[0].BlastRadiusKm);
        Assert.Equal(10, torpedo.Profiles[1].SpeedKmS);
        Assert.Equal(5, torpedo.Profiles[1].BlastRadiusKm);
        var counter = Load(Module(true));
        Assert.Null(counter.TorpedoDamage);
        Assert.Null(counter.ArmorPiercingPercent);
        Assert.Equal(30, counter.RangeKm);
        Assert.Equal(12500, counter.ReloadMs);
        Assert.Equal(0.08, counter.ScatterPercent);
        Assert.Equal(25, counter.Profiles[1].SpeedKmS);
        var invalid = Module(true);
        invalid["torpedoDamage"] = 200;
        Reject(invalid, "torpedoDamage");
        var invalidPiercing = Module(true);
        invalidPiercing["armorPiercingPercent"] = 20;
        Reject(invalidPiercing, "armorPiercingPercent");
    }

    [Theory]
    [InlineData("torpedoDamage")]
    [InlineData("armorPiercingPercent")]
    [InlineData("rangeKm")]
    [InlineData("reloadMs")]
    [InlineData("scatterPercent")]
    public void Point_launcher_fields_are_required_without_defaults(string field)
    {
        var module = Module();
        module.Remove(field);
        Reject(module, field);
    }

    [Theory]
    [InlineData("speedKmS", "NaN")]
    [InlineData("speedKmS", "Infinity")]
    [InlineData("speedKmS", "1e309")]
    [InlineData("blastRadiusKm", "NaN")]
    [InlineData("blastRadiusKm", "Infinity")]
    [InlineData("blastRadiusKm", "1e309")]
    [InlineData("rangeKm", "NaN")]
    [InlineData("rangeKm", "Infinity")]
    [InlineData("rangeKm", "1e309")]
    [InlineData("scatterPercent", "NaN")]
    [InlineData("scatterPercent", "Infinity")]
    [InlineData("scatterPercent", "1e309")]
    public void Nonfinite_profile_numbers_are_rejected(string field, string value)
    {
        var root = new JsonObject { ["moduleImplementations"] = new JsonArray(Module()) };
        string original = field switch { "speedKmS" => "6", "blastRadiusKm" => "9", "rangeKm" => "70", _ => "0.02" };
        File.WriteAllText(ModulesPath, root.ToJsonString().Replace($"\"{field}\":{original}", $"\"{field}\":{value}", StringComparison.Ordinal));
        var category = new ModuleCategoryDefinition("module.torpedo.launcher", "Launcher", 1, []);
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadModuleImplementations(ModulesPath, [category]));
        Assert.Contains(ModulesPath, error.Message);
        Assert.Contains(field, error.Message);
    }

    [Theory]
    [InlineData("torpedoSpeedKmS")]
    [InlineData("torpedoTurnRateDegPerSec")]
    [InlineData("accuracy")]
    [InlineData("barrels")]
    public void Point_launchers_reject_legacy_fields(string field)
    {
        var module = Module();
        module[field] = 1;
        Reject(module, field);
    }

    [Fact]
    public void Zero_reload_and_scatter_are_valid_and_registry_revalidates_profiles()
    {
        var module = Module();
        module["reloadMs"] = 0;
        module["scatterPercent"] = 0;
        var definition = Load(module);
        Assert.Equal(0, definition.ReloadMs);
        Assert.Equal(0, definition.ScatterPercent);
        Assert.Contains("profiles", Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [definition with { Profiles = [] }], [], [])).Message);
        Assert.Contains("speedKmS", Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [definition with
        {
            Profiles = [new("bad", double.NaN, 9)]
        }], [], [])).Message);
    }

    [Theory]
    [InlineData("module.engine")]
    [InlineData("module.cargo")]
    public void Profiles_on_nonlaunchers_name_file_and_field(string category)
    {
        var module = Module();
        module["type"] = category;
        Reject(module, "profiles");
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
