using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatPaletteContentTests
{
    private const string RelativePath = "Data/UI/combat-visuals.json";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Combat_palette_contains_all_semantic_color_roles()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(ClientRoot, RelativePath)));
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        var expected = new Dictionary<string, string>
        {
            ["torpedo"] = "#EDB86AFF",
            ["trail"] = "#EDB86AFF",
            ["prediction"] = "#EDB86A80",
            ["intercept"] = "#EDB86AFF",
            ["preview"] = "#B7ABDF8C",
            ["hullHp"] = "#8FBBA7D9",
            ["explosion"] = "#EEA77AFF",
            ["wreck"] = "#7C8998A6",
            ["countermeasure"] = "#76D5DCFF",
            ["countermeasureTrail"] = "#76D5DC66",
            ["countermeasurePrediction"] = "#76D5DC8C",
            ["countermeasureIntercept"] = "#76D5DCFF",
            ["defenseRange"] = "#76D5DC2E",
            ["defenseText"] = "#DCE6EEE6",
            ["torpedoRange"] = "#EDB86A2E",
            ["manualDefenseRange"] = "#76D5DC47"
        };
        Assert.Equal(expected.Count + 3, root.EnumerateObject().Count());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("pointCombat").ValueKind);
        Assert.Equal(200_000, root.GetProperty("forecastHorizonMs").GetInt64());
        foreach (var (key, value) in expected)
        {
            string actual = root.GetProperty(key).GetString()!;
            Assert.Matches("^#[0-9A-Fa-f]{8}$", actual);
            Assert.Equal(value, actual);
        }
    }

    [Fact]
    public void Combat_palette_is_in_output_data_directory()
    {
        string output = Path.Combine(AppContext.BaseDirectory, RelativePath);
        Assert.True(File.Exists(output), $"Missing packaged palette: {output}");
        Assert.Equal(File.ReadAllText(Path.Combine(ClientRoot, RelativePath)), File.ReadAllText(output));
    }
}
