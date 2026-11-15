using DeepSpaceSaga.Client.UI.Portraits;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ModularCrewPortraitTests
{
    [Theory]
    [InlineData("violet")]
    [InlineData("crew-f07")]
    [InlineData("crew-f18")]
    [InlineData("crew-f12")]
    [InlineData("crew-f19")]
    [InlineData("crew-f25")]
    [InlineData("crew-f05")]
    [InlineData("crew-f10")]
    [InlineData("crew-f13")]
    public void Renders_every_outfit_of_long_haired_characters(string head)
    {
        foreach (string outfit in new[] { "eva", "engineer", "medic", "security", "explorer", "pilot",
                                          "casual:01", "casual:02", "casual:03", "casual:04", "casual:05" })
        {
            using var bitmap = ModularCrewPortraits.Compose(AppContext.BaseDirectory, head, outfit);
            Assert.NotNull(bitmap);
            Assert.Equal(300, bitmap.Width);
            Assert.Equal(300, bitmap.Height);
            Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
            Assert.Equal(255, bitmap.GetPixel(150, 123).Alpha);
        }
    }

    [Fact]
    public void Separates_back_hair_body_head_and_front_hair()
    {
        string folder = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath(),
                                     "hair-stack-" + Guid.NewGuid().ToString("N"));
        string pack = Path.Combine(folder, "Images", "Persons", "ModularCrew");
        Directory.CreateDirectory(pack);
        var layers = new Dictionary<string, SKColor>
        {
            ["back"] = SKColors.Red, ["body"] = SKColors.Blue,
            ["head"] = SKColors.Green, ["front"] = SKColors.Orange
        };
        foreach (var ((name, color), index) in layers.Select((layer, index) => (layer, index)))
        {
            using var image = new SKBitmap(1024, 1536);
            using var canvas = new SKCanvas(image);
            using var paint = new SKPaint { Color = color };
            canvas.Clear(SKColors.Transparent);
            canvas.DrawRect(SKRect.Create(359 + 66 * index, 400, 266 - 66 * index, 40), paint);
            canvas.Flush();
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(pack, name + ".png"));
            png.SaveTo(file);
        }
        File.WriteAllText(Path.Combine(pack, "composition-pack.json"), """
            {"schemaVersion":1,"heads":{"crew":{"casual":"head.png","hairLayers":{"back":"back.png","front":"front.png"}}},
             "suits":{},"casualBodies":{"01":{"crew":"body.png"}}}
            """);
        try
        {
            using var bitmap = ModularCrewPortraits.Compose(folder, "crew", "casual:01");
            Assert.NotNull(bitmap);
            Assert.Equal(SKColors.Red, bitmap.GetPixel(56, 127));
            Assert.Equal(SKColors.Blue, bitmap.GetPixel(103, 127));
            Assert.Equal(SKColors.Green, bitmap.GetPixel(150, 127));
            Assert.Equal(SKColors.Orange, bitmap.GetPixel(198, 127));
            File.Delete(Path.Combine(pack, "front.png"));
            Assert.Null(ModularCrewPortraits.Compose(folder, "crew", "casual:01"));
        }
        finally
        {
            foreach (string name in layers.Keys)
                File.Delete(Path.Combine(pack, name + ".png"));
            File.Delete(Path.Combine(pack, "composition-pack.json"));
            Directory.Delete(pack);
            Directory.Delete(Path.GetDirectoryName(pack)!);
            Directory.Delete(Path.Combine(folder, "Images"));
            Directory.Delete(folder);
        }
    }
}
