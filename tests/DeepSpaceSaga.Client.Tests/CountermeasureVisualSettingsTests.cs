using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class CountermeasureVisualSettingsTests
{
    [Fact]
    public void Custom_palette_loads_and_invalid_optional_values_use_fallback()
    {
        string path = Path.GetTempFileName();
        try
        {
            string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CombatVisualSettings.RelativePath));
            var node = JsonNode.Parse(json)!;
            node["countermeasure"] = "#12345678";
            File.WriteAllText(path, node.ToJsonString());
            Assert.Equal(new SKColor(0x12, 0x34, 0x56, 0x78), CombatVisualSettings.Load(path).Countermeasure);
            node["countermeasure"] = "invalid";
            File.WriteAllText(path, node.ToJsonString());
            Assert.Equal(CombatVisualSettings.Default.Countermeasure, CombatVisualSettings.Load(path).Countermeasure);
        }
        finally { File.Delete(path); }
    }
}
