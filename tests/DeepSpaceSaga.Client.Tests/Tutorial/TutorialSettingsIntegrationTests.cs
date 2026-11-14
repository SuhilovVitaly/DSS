using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0004-TK-0002: the main-menu Settings row for tutorial hints reads and writes <c>Settings.json</c>.</summary>
[Collection("Battle localization")]
public sealed class TutorialSettingsIntegrationTests
{
    private const int Width = 1280, Height = 720;

    [Fact]
    public async Task MainMenuSettingsTogglePersistsHintsAndKeepsOtherSettings()
    {
        string root = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath(),
            "tutorial-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string profile = Path.Combine(root, "Settings.json");
        const string before = """
            {"gameSettings":{"language":"English","uiScale":1.0,"tacticalBattleSkinId":"classic","tacticalMap":{"zoom":2}},"unrelated":17}
            """;
        File.WriteAllText(profile, before);
        try
        {
            await using (var fixture = await BattleNavigationTests.Fixture.Create(settingsPath: profile))
            {
                var settings = await OpenSettings(fixture);
                Assert.True(settings.TutorialHintsEnabled); // missing value = on
                Click(settings);
                Assert.False(settings.TutorialHintsEnabled);
                Assert.False(settings.TutorialHintsSaveFailed);
                Assert.False(TutorialHintsPreference.Read(profile));
                var saved = JsonNode.Parse(File.ReadAllText(profile))!;
                saved["gameSettings"]!.AsObject().Remove(TutorialHintsPreference.Property);
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(before), saved)); // nothing else changed

                await fixture.Route(settings.OnKeyDown(Key.Escape));
                var reopened = await OpenSettings(fixture);
                Assert.NotSame(settings, reopened);
                Assert.False(reopened.TutorialHintsEnabled); // the row reflects Settings.json
                using (Snapshot(reopened, "settings-hints-off")) { }
                Click(reopened);
                Assert.True(reopened.TutorialHintsEnabled);
                Assert.True(TutorialHintsPreference.Read(profile));
                Assert.Equal(17, JsonNode.Parse(File.ReadAllText(profile))!["unrelated"]!.GetValue<int>());
            }

            // Both languages draw the row's label inside the panel.
            foreach (string language in new[] { "Russian", "English" })
            {
                using var locale = Localization.UseLocaleForTests(language);
                var screen = new SettingsScreen(["Monitor 1"], 0, _ => { }, 1, _ => { }, language, _ => { },
                    [("classic", "Classic")], "classic", _ => true, tutorialHints: true, trySetTutorialHints: _ => true);
                using var bitmap = Snapshot(screen, $"settings-{language.ToLowerInvariant()}");
                var row = SettingsLayout.TutorialHintsField(Width, Height);
                Assert.True(Inked(bitmap, row), $"{language}: the hints row is empty");
                Assert.True(Localization.Get("Settings.TutorialHints") != "Settings.TutorialHints");
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static async Task<SettingsScreen> OpenSettings(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenSettings);
        var settings = Assert.IsType<SettingsScreen>(fixture.Screens.Current);
        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        settings.Render(canvas, Width, Height);
        return settings;
    }

    private static void Click(SettingsScreen settings)
    {
        var row = SettingsLayout.TutorialHintsField(Width, Height);
        Assert.Equal(ScreenEvent.None, settings.OnMouseDown(row.MidX, row.MidY, MouseButton.Left));
    }

    private static SKBitmap Snapshot(SettingsScreen screen, string name)
    {
        var bitmap = new SKBitmap(Width, Height);
        bitmap.Erase(SKColors.Black);
        using (var canvas = new SKCanvas(bitmap))
        {
            screen.Render(canvas, Width, Height);
            canvas.Flush();
        }
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
        {
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(Path.Combine(root, name + ".png"));
            png.SaveTo(stream);
        }
        return bitmap;
    }

    /// <summary>The row region holds bright (text or check box) pixels over the dark panel.</summary>
    private static bool Inked(SKBitmap bitmap, SKRect rect)
    {
        int bright = 0;
        for (int x = (int)rect.Left; x < (int)rect.Right; x++)
        for (int y = (int)rect.Top; y < (int)rect.Bottom; y++)
        {
            var c = bitmap.GetPixel(x, y);
            if (c.Red + c.Green + c.Blue > 360) bright++;
        }
        return bright > 20;
    }
}
