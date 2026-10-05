using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatPaletteContentTests
{
    private const string RelativePath = "Data/UI/combat-visuals.json";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Combat_palette_contains_all_eight_color_roles()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(ClientRoot, RelativePath)));
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        var expected = new Dictionary<string, string>
        {
            ["torpedo"] = "#FFFF00FF",
            ["trail"] = "#FFFF00FF",
            ["prediction"] = "#FFFF0066",
            ["intercept"] = "#FFFF00FF",
            ["preview"] = "#808080FF",
            ["hullHp"] = "#00FF00FF",
            ["explosion"] = "#FF0000FF",
            ["wreck"] = "#808080FF",
            ["countermeasure"] = "#00CCFFFF",
            ["countermeasureTrail"] = "#00CCFFFF",
            ["countermeasurePrediction"] = "#00CCFF88",
            ["countermeasureIntercept"] = "#00CCFFFF",
            ["defenseRange"] = "#00CCFF55",
            ["defenseText"] = "#99EEFFFF"
        };
        Assert.Equal(expected.Count + 1, root.EnumerateObject().Count());
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
