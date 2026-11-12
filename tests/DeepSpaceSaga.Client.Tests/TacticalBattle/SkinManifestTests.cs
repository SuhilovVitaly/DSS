using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;
using Xunit;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class SkinManifestTests
{
    [Fact]
    public void Tabletop_scope_reads_shipped_sheets_and_restores_classic()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using (TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop")))
        {
            Assert.True(TacticalBattleSkinRenderScope.Tabletop);
            var sheet = TacticalBattleSkinRenderScope.ShipSheet("heavy", "own")!.Value;
            Assert.Equal((8, 32, 64), (sheet.Columns, sheet.Frames, sheet.Cell));
            var strip = TacticalBattleSkinRenderScope.EffectStrip("boom-kill")!.Value;
            Assert.Equal((16, 80, 55f), (strip.Frames, strip.Cell, strip.FrameMs));
            Assert.NotNull(TacticalBattleSkinRenderScope.EffectStrip("trail"));
            Assert.Null(TacticalBattleSkinRenderScope.ShipSheet("carrier", "disabled"));
            Assert.Null(TacticalBattleSkinRenderScope.EffectStrip("missing"));
            using (TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic))
            {
                Assert.False(TacticalBattleSkinRenderScope.Tabletop);
                Assert.Null(TacticalBattleSkinRenderScope.ShipSheet("heavy", "own"));
            }
            Assert.True(TacticalBattleSkinRenderScope.Tabletop);
        }
        foreach (var id in new[] { "classic", "contrast", "xenon-star" })
        {
            using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic(id));
            Assert.False(TacticalBattleSkinRenderScope.Tabletop);
            Assert.Null(TacticalBattleSkinRenderScope.EffectStrip("puff"));
        }
    }

    [Theory]
    [InlineData(0, 32, 512, 256)]
    [InlineData(8.5f, 32, 512, 256)]
    [InlineData(8, 31, 512, 256)]
    [InlineData(8, 32, 511, 256)]
    [InlineData(8, 32, 512, 255)]
    public void Invalid_sheet_geometry_is_ignored(float columns, int frames, int width, int height)
    {
        string json = System.Text.Json.JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            id = "test",
            displayName = "Test",
            roles = new Dictionary<string, object>
            {
                ["field.style"] = new { metrics = new { tabletop = 1 } },
                ["field.unit.sprite.heavy.own"] = new { image = "assets/sheet.png", metrics = new { columns, frames } },
            },
        });
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        var skin = TacticalBattleSkin.Parse(json).WithResources(new() { ["assets/sheet.png"] = image });
        using var scope = TacticalBattleSkinRenderScope.Enter(skin);
        Assert.Null(TacticalBattleSkinRenderScope.ShipSheet("heavy", "own"));
    }

    [Fact]
    public void V1_reads_all_property_types_and_keeps_classic_fallbacks()
    {
        var skin = TacticalBattleSkin.Parse("""
            {
              "schemaVersion": 1,
              "id": "test-skin2",
              "displayName": "  Скин  ",
              "roles": {
                "field.unit.drone": {
                  "fill": "#123456", "stroke": "#ABCDEF80", "text": "#F1F2F3", "opacity": 0.75,
                  "strokeWidth": 1.25, "size": 13, "fontSize": 15, "radius": 4,
                  "dash": [1, 2.5], "shape": "diamond", "image": "assets/icons/Drone.PNG",
                  "font": "assets/test.OTF", "metrics": { "heading-length": 6, "marker-offset": -2 }
                },
                "field.unit.drone.own": { "fill": "#10203040", "dash": [], "metrics": { "marker-offset": 0 } },
                "torpedo.trail": { "stroke": "#ABCDEF" }
              }
            }
            """);

        const string parent = "field.unit.drone";
        const string own = "field.unit.drone.own.selected";
        Assert.Equal("test-skin2", skin.Id);
        Assert.Equal("Скин", skin.DisplayName);
        Assert.Equal(new SKColor(0x10, 0x20, 0x30, 0x40), skin.Color(own, "fill", SKColors.Black));
        Assert.Equal(new SKColor(0xAB, 0xCD, 0xEF, 0x80), skin.Color(own, "stroke", SKColors.Black));
        Assert.Equal(new SKColor(0xF1, 0xF2, 0xF3), skin.Color(own, "text", SKColors.Black));
        Assert.Equal(0.75f, skin.Number(own, "opacity", 1));
        Assert.Equal(1.25f, skin.Number(own, "strokeWidth", 2));
        Assert.Equal(13, skin.Number(own, "size", 10));
        Assert.Equal(15, skin.Number(own, "fontSize", 10));
        Assert.Equal(4, skin.Number(own, "radius", 0));
        Assert.Equal(new[] { 1f, 2.5f }, skin.Dash(parent).ToArray());
        Assert.True(skin.Dash(own).IsEmpty);
        Assert.True(skin.Dash("field.unit.carrier").IsDefault);
        Assert.Equal("diamond", skin.Shape(own));
        Assert.Equal("assets/icons/Drone.PNG", skin.Resource(own, "image"));
        Assert.Equal("assets/test.OTF", skin.Resource(own, "font"));
        Assert.Equal(6, skin.Metric(own, "heading-length", 2));
        Assert.Equal(0, skin.Metric(own, "marker-offset", -2));
        Assert.Equal(2, skin.ResourceReferences.Count());
        Assert.Equal(SKColors.Gold, skin.Color("field.ruler", "fill", SKColors.Gold));
        Assert.Equal(SKColors.Gold, TacticalBattleSkin.Classic.Color(parent, "fill", SKColors.Gold));
        Assert.Equal("classic", TacticalBattleSkin.Classic.Id);
    }

    [Theory]
    [InlineData("""{"id":"example","displayName":"Test","roles":{}}""", "schemaVersion")]
    [InlineData("""{"schemaVersion":1.0,"id":"example","displayName":"Test","roles":{}}""", "schemaVersion")]
    [InlineData("""{"schemaVersion":2,"id":"example","displayName":"Test","roles":{}}""", "schemaVersion")]
    [InlineData("""{"schemaVersion":1,"id":"classic","displayName":"Test","roles":{}}""", "id")]
    [InlineData("""{"schemaVersion":1,"id":"Example","displayName":"Test","roles":{}}""", "id")]
    [InlineData("""{"schemaVersion":1,"id":"example\n","displayName":"Test","roles":{}}""", "id")]
    [InlineData("""{"schemaVersion":1,"id":"example","displayName":"  ","roles":{}}""", "displayName")]
    [InlineData("""{"schemaVersion":1,"id":"example","displayName":"Test","roles":[] }""", "roles")]
    [InlineData("""{"schemaVersion":1,"id":"example","id":"other","displayName":"Test","roles":{}}""", "id")]
    [InlineData("""{"schemaVersion":1,"id":"example","displayName":"Test","roles":{},"extra":0}""", "extra")]
    [InlineData("{", "JSON")]
    public void Invalid_manifest_is_rejected_as_a_unit(string json, string field)
    {
        Assert.Contains(field, Assert.Throws<FormatException>(() => TacticalBattleSkin.Parse(json)).Message);
    }

    [Theory]
    [InlineData("field..grid", """{"stroke":"#123456"}""", "field..grid")]
    [InlineData("field.grid", """{"stroke":"#12345G"}""", "stroke")]
    [InlineData("field.grid", """{"stroke":123}""", "stroke")]
    [InlineData("field.grid", """{"stroke-width":1}""", "stroke-width")]
    [InlineData("field.grid", """{"opacity":1.00000001}""", "opacity")]
    [InlineData("field.grid", """{"opacity":"1"}""", "opacity")]
    [InlineData("field.grid", """{"size":1e1000}""", "size")]
    [InlineData("field.grid", """{"size":513}""", "size")]
    [InlineData("field.grid", """{"fontSize":5}""", "fontSize")]
    [InlineData("field.grid", """{"radius":-1}""", "radius")]
    [InlineData("field.grid", """{"strokeWidth":33}""", "strokeWidth")]
    [InlineData("field.grid", """{"dash":[1]}""", "dash")]
    [InlineData("field.grid", """{"dash":[1,0]}""", "dash[1]")]
    [InlineData("field.grid", """{"shape":"star"}""", "shape")]
    [InlineData("field.grid", """{"metrics":{"offset":2,"offset":3}}""", "metrics.offset")]
    [InlineData("field.grid", """{"metrics":{"offset":-257}}""", "metrics.offset")]
    [InlineData("field.grid", """{"fill":"#123456","fill":"#FFFFFF"}""", "fill")]
    [InlineData("field.grid", """{"image":"assets/../escape.png"}""", "image")]
    [InlineData("field.grid", """{"image":"assets//empty.png"}""", "image")]
    [InlineData("field.grid", """{"image":"C:/assets/root.png"}""", "image")]
    [InlineData("field.grid", """{"image":"assets\\icon.png"}""", "image")]
    [InlineData("field.grid", """{"font":"assets/icon.png"}""", "font")]
    public void Invalid_role_rejects_the_whole_manifest(string role, string properties, string field)
    {
        string json = $$"""
            {"schemaVersion":1,"id":"test","displayName":"Test","roles":{"{{role}}":{{properties}} } }
            """;
        Assert.Contains($"roles.{role}", Assert.Throws<FormatException>(() => TacticalBattleSkin.Parse(json)).Message);
        Assert.Contains(field, Assert.Throws<FormatException>(() => TacticalBattleSkin.Parse(json)).Message);
    }
}
