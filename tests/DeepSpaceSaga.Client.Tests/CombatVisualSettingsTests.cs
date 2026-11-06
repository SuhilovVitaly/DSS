using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatVisualSettingsTests
{
    [Fact]
    public void Restart_reads_edited_palette_without_build()
    {
        WithPalette(path =>
        {
            var first = CombatVisualSettings.Load(path);
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            json["torpedo"] = "#12345678";
            File.WriteAllText(path, json.ToJsonString());
            var restarted = CombatVisualSettings.Load(path);
            Assert.Equal(new SKColor(237, 184, 106), first.Torpedo);
            Assert.Equal(new SKColor(0x12, 0x34, 0x56, 0x78), restarted.Torpedo);
            Assert.Equal(first.HullHp, restarted.HullHp);
        });
    }

    [Theory]
    [InlineData("torpedo", "#FFFF00")]
    [InlineData("wreck", "#GG0000FF")]
    [InlineData("hullHp", null)]
    [InlineData("schemaVersion", "2")]
    public void Malformed_palette_reports_file_and_key(string key, string? value)
    {
        WithPalette(path =>
        {
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            if (value is null) json.Remove(key);
            else json[key] = value;
            File.WriteAllText(path, json.ToJsonString());
            var error = Assert.Throws<InvalidDataException>(() => CombatVisualSettings.Load(path));
            Assert.Contains(path, error.Message);
            Assert.Contains(key, error.Message);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_or_invalid_json_reports_path(bool missing)
    {
        WithPalette(path =>
        {
            if (missing) File.Delete(path);
            else File.WriteAllText(path, "{");
            Assert.Contains(path, Assert.Throws<InvalidDataException>(() => CombatVisualSettings.Load(path)).Message);
        });
    }

    [Fact]
    public void PaletteRgbaDefaults_match_production_and_optional_v1_fallback()
    {
        WithPalette(path =>
        {
            var loaded = CombatVisualSettings.Load(path);
            Assert.Equal(CombatVisualSettings.Default, loaded);
            Assert.Equal(new SKColor(237, 184, 106, 46), loaded.TorpedoRange);
            Assert.Equal(new SKColor(183, 171, 223, 140), loaded.Preview);
            var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            json.Remove("torpedoRange");
            json["manualDefenseRange"] = "invalid";
            File.WriteAllText(path, json.ToJsonString());
            Assert.Equal(loaded, CombatVisualSettings.Load(path));
        });
    }

    private static void WithPalette(Action<string> action)
    {
        string path = Path.Combine(Path.GetTempPath(), "dss-palette-" + Guid.NewGuid() + ".json");
        try
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, CombatVisualSettings.RelativePath), path);
            action(path);
        }
        finally { File.Delete(path); }
    }
}
