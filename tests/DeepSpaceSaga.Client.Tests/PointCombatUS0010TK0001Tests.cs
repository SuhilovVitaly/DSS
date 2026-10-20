using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0010TK0001Tests
{
    private static string Source => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", CombatVisualSettings.RelativePath));
    private static void WithPalette(Action<string, JsonObject> action)
    {
        var file = Path.Combine(Path.GetTempPath(), "dss-point-palette-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.Copy(Source, file);
            action(file, JsonNode.Parse(File.ReadAllText(file))!.AsObject());
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void Combat_visuals_are_data_driven()
    {
        var shipped = CombatVisualSettings.Load(Source);
        Assert.Equal(PointCombatVisualSettings.Default, shipped.PointCombat);
        Assert.Equal(File.ReadAllText(Source), File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CombatVisualSettings.RelativePath)));
        WithPalette((file, json) =>
        {
            var group = json["pointCombat"]!.AsObject();
            var properties = typeof(PointCombatVisualSettings).GetProperties().Where(p => p.GetMethod?.IsStatic == false).ToArray();
            Assert.Equal(24, properties.Length);
            Assert.Equal(properties.Length, group.Count);
            foreach (var property in properties)
            {
                string key = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
                group[key] = property.PropertyType == typeof(SKColor) ? JsonValue.Create("#12345678") : JsonValue.Create(25.5);
            }
            File.WriteAllText(file, json.ToJsonString());
            var restarted = CombatVisualSettings.Load(file).PointCombat;
            foreach (var property in properties)
                Assert.Equal(property.PropertyType == typeof(SKColor) ? (object)new SKColor(0x12, 0x34, 0x56, 0x78) : 25.5, property.GetValue(restarted));
            Assert.Equal(shipped.PointCombat, PointCombatVisualSettings.Default); // loading never mutates defaults
        });
    }

    [Theory]
    [InlineData("blastFill", "bad")]
    [InlineData("blastStroke", "missing")]
    [InlineData("lineWidthPx", "zero")]
    [InlineData("dashLengthPx", "negative")]
    [InlineData("reticleRadiusPx", "tooLarge")]
    [InlineData("minimumBlastMarkerDiameterPx", "tooSmall")]
    [InlineData("blastDurationRealMs", "string")]
    [InlineData("unknownColor", "unknown")]
    public void Invalid_point_styles_report_file_and_exact_key(string key, string mutation)
    {
        WithPalette((file, json) =>
        {
            var group = json["pointCombat"]!.AsObject();
            if (mutation == "missing") group.Remove(key);
            else group[key] = mutation switch
            {
                "zero" => JsonValue.Create(0),
                "negative" => JsonValue.Create(-1),
                "tooLarge" => JsonValue.Create(10001),
                "tooSmall" => JsonValue.Create(5),
                _ => JsonValue.Create("invalid")
            };
            File.WriteAllText(file, json.ToJsonString());
            var error = Assert.Throws<InvalidDataException>(() => CombatVisualSettings.Load(file));
            Assert.Contains(file, error.Message); Assert.Contains("pointCombat." + key, error.Message);
        });
    }

    [Fact]
    public void Duplicate_point_style_key_is_rejected_and_absent_group_preserves_legacy_palette()
    {
        WithPalette((file, json) =>
        {
            var text = json.ToJsonString();
            text = text.Replace("\"blastFill\":", "\"blastFill\":\"#FFFFFFFF\",\"blastFill\":", StringComparison.Ordinal);
            File.WriteAllText(file, text);
            var error = Assert.Throws<InvalidDataException>(() => CombatVisualSettings.Load(file));
            Assert.Contains("pointCombat.blastFill", error.Message); Assert.Contains("duplicate", error.Message);
            json.Remove("pointCombat"); File.WriteAllText(file, json.ToJsonString());
            Assert.Equal(CombatVisualSettings.Default, CombatVisualSettings.Load(file));
        });
    }
}
