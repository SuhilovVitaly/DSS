using System.Runtime.CompilerServices;
using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("InterfaceLog")]
public sealed class TabletopSnapshots
{
    public static TheoryData<string> Scenes => new()
    {
        "1-move-no-hover", "1-anomaly-move-dust", "2-fire-hover-own", "2-anomaly-absorption",
        "3-salvo-flight", "4-salvo-results", "4-decoy-results", "5-outcome"
    };

    [Theory, MemberData(nameof(Scenes)), Trait("Category", "Ep0024View")]
    public async Task Real_tabletop_screen_matches_reviewed_snapshot(string name)
    {
        using var profile = new TabletopTestProfile();
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create(name, settingsPath: profile.SettingsPath);
        Assert.Equal("tabletop", scene.Screen.SkinId);
        using var current = scene.Render();
        string output = Path.Combine(profile.ValidationRoot, "tabletop", name);
        Directory.CreateDirectory(output);
        using var png = current.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(output, "candidate.png"), png.ToArray());
        File.WriteAllText(Path.Combine(output, "layout.json"), JsonSerializer.Serialize(new
        {
            scene.Screen.Layout.Camera,
            Rects = scene.Screen.Layout.Rects.ToDictionary(p => p.Key, p => new { p.Value.Left, p.Value.Top, p.Value.Right, p.Value.Bottom }),
            scene.Screen.Layout.Texts
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (Environment.GetEnvironmentVariable("DSS_EP0024_PREVIEW_SNAPSHOTS") == "1") return;
        if (Environment.GetEnvironmentVariable("DSS_EP0024_ACCEPT_SNAPSHOTS") == "1")
        {
            string directory = Path.Combine(SourceDirectory(), "Snapshots", "tabletop");
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), png.ToArray());
            return;
        }
        using var expected = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "TacticalBattle", "View", "Snapshots", "tabletop", name + ".png"));
        Assert.NotNull(expected);
        Assert.Equal((1920, 1080), (expected.Width, expected.Height));
        int different = 0; int[] tiles = new int[120 * 68];
        var one = expected.Pixels; var two = current.Pixels;
        for (int i = 0; i < one.Length; i++)
        {
            if (Math.Abs(one[i].Red - two[i].Red) <= 40 && Math.Abs(one[i].Green - two[i].Green) <= 40 && Math.Abs(one[i].Blue - two[i].Blue) <= 40) continue;
            different++; tiles[i / 1920 / 16 * 120 + i % 1920 / 16]++;
        }
        double fraction = different / (double)one.Length;
        double maxTile = tiles.Select((count, index) => count / (double)(16 * (index / 120 == 67 ? 8 : 16))).Max();
        File.WriteAllText(Path.Combine(output, "regression.json"), JsonSerializer.Serialize(new { different, fraction, maxTile }));
        Assert.InRange(fraction, 0, .005); Assert.InRange(maxTile, 0, .25);
    }

    private static string SourceDirectory([CallerFilePath] string source = "") => Path.GetDirectoryName(source)!;
}

internal sealed class TabletopTestProfile : IDisposable
{
    internal string ValidationRoot { get; } = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
    private readonly string _directory;
    internal string SettingsPath => Path.Combine(_directory, "Settings.json");

    internal TabletopTestProfile()
    {
        _directory = Path.Combine(ValidationRoot, "tabletop-profile-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(SettingsPath, """{"gameSettings":{"tacticalBattleSkinId":"tabletop","language":"Russian"}}""");
            string source = Path.Combine(AppContext.BaseDirectory, "Skins", "tabletop");
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(_directory, "Skins", "tabletop", Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
            }
        }
        catch { Dispose(); throw; }
    }

    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
