using System.Runtime.CompilerServices;
using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

public sealed partial class Ep0019ViewTests
{
    private static string SourceDirectory([CallerFilePath] string source = "") => Path.GetDirectoryName(source)!;
    [Theory, MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public Task Scene_matches_regression_snapshot(string name) => CompareSnapshot(name);
    [Theory, MemberData(nameof(Ep0020AnomalyScenesTests.Scenes), MemberType = typeof(Ep0020AnomalyScenesTests)), Trait("Category", "Ep0020View")]
    public Task Anomaly_scene_matches_regression_snapshot(string name, int seed, int turn) => CompareSnapshot(name, seed, turn);
    private static async Task CompareSnapshot(string name, int? seed = null, int? turn = null)
    {
        bool verifyM3Layout = Environment.GetEnvironmentVariable("DSS_EP0020_VERIFY_M3_LAYOUT") == "1";
        bool verifyM2Masks = Environment.GetEnvironmentVariable("DSS_EP0020_VERIFY_M2_MASKS") == "1";
        await using var s = await Ep0019Scenes.Create(name, withoutModules: verifyM2Masks, withoutAnomalies: verifyM3Layout); using var locale = Localization.UseLocaleForTests("Russian"); using var current = s.Render();
        if (seed is not null) Assert.Equal(seed.Value, s.Screen.Flow.State!.Seed);
        if (turn is not null) Assert.Equal(turn.Value, s.Screen.Flow.State!.Turn);
        if (Environment.GetEnvironmentVariable("DSS_EP0019_ACCEPT_SNAPSHOTS") == "1")
        {
            string dir = Path.Combine(SourceDirectory(), "Snapshots"); Directory.CreateDirectory(dir);
            using var png = current.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(dir, name + ".png"), png.ToArray()); return;
        }
        using var expected = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "TacticalBattle", "View", "Snapshots", name + ".png"));
        Assert.NotNull(expected); Assert.Equal((1920, 1080), (expected.Width, expected.Height));
        var one = expected.Pixels; var two = current.Pixels; int[] tiles = new int[120 * 68]; int different = 0;
        bool verifyM1Masks = Environment.GetEnvironmentVariable("DSS_EP0020_VERIFY_M1_MASKS") == "1";
        for (int i = 0; i < one.Length; i++)
        {
            int px = i % 1920, py = i / 1920;
            // Transitional comparison against EP-0019: only the two UiSpec §7 rectangles.
            if (verifyM1Masks && name.StartsWith("2-", StringComparison.Ordinal) && px >= 1560 && px < 1911
                && ((py >= 357 && py < 402) || (py >= 417 && py < 456))) continue;
            if (verifyM2Masks && name.StartsWith("2-", StringComparison.Ordinal) && px >= 1560 && px < 1911
                && ((py >= 285 && py < 405) || (py >= 417 && py < 456))) continue;
            if (Math.Abs(one[i].Red - two[i].Red) <= 40 && Math.Abs(one[i].Green - two[i].Green) <= 40 && Math.Abs(one[i].Blue - two[i].Blue) <= 40) continue;
            different++; int x = i % 1920, y = i / 1920; tiles[y / 16 * 120 + x / 16]++;
        }
        double maxTile = tiles.Select((count, index) => count / (double)(16 * (index / 120 == 67 ? 8 : 16))).Max();
        File.WriteAllText(Path.Combine(Output(name), "regression.json"), JsonSerializer.Serialize(new { verifyM1Masks, verifyM2Masks, verifyM3Layout, different, fraction = different / (double)one.Length, maxTile }));
        Assert.InRange(different / (double)one.Length, 0, .005); Assert.InRange(maxTile, 0, .25);
    }
}
