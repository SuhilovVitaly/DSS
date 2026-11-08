using System.Runtime.CompilerServices;
using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

public sealed partial class Ep0019ViewTests
{
    private static string SourceDirectory([CallerFilePath] string source = "") => Path.GetDirectoryName(source)!;
    [Theory, MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_matches_regression_snapshot(string name)
    {
        await using var s = await Ep0019Scenes.Create(name); using var locale = Localization.UseLocaleForTests("Russian"); using var current = s.Render();
        if (Environment.GetEnvironmentVariable("DSS_EP0019_ACCEPT_SNAPSHOTS") == "1")
        {
            string dir = Path.Combine(SourceDirectory(), "Snapshots"); Directory.CreateDirectory(dir);
            using var png = current.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(dir, name + ".png"), png.ToArray()); return;
        }
        using var expected = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "TacticalBattle", "View", "Snapshots", name + ".png"));
        Assert.NotNull(expected); Assert.Equal((1920, 1080), (expected.Width, expected.Height));
        var one = expected.Pixels; var two = current.Pixels; int[] tiles = new int[120 * 68]; int different = 0;
        for (int i = 0; i < one.Length; i++)
        {
            if (Math.Abs(one[i].Red - two[i].Red) <= 40 && Math.Abs(one[i].Green - two[i].Green) <= 40 && Math.Abs(one[i].Blue - two[i].Blue) <= 40) continue;
            different++; int x = i % 1920, y = i / 1920; tiles[y / 16 * 120 + x / 16]++;
        }
        double maxTile = tiles.Select((count, index) => count / (double)(16 * (index / 120 == 67 ? 8 : 16))).Max();
        File.WriteAllText(Path.Combine(Output(name), "regression.json"), JsonSerializer.Serialize(new { different, fraction = different / (double)one.Length, maxTile }));
        Assert.InRange(different / (double)one.Length, 0, .005); Assert.InRange(maxTile, 0, .25);
    }
}
