using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0016TK0001Tests
{
    private static string ClientRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Fact]
    public void Combat_locale_key_sets_match_and_have_no_fallback_literals()
    {
        var en = Localization.LoadLocaleFile("English")!; var ru = Localization.LoadLocaleFile("Russian")!;
        Assert.Equal(en.Keys.Where(k => k.StartsWith("Combat.", StringComparison.Ordinal)).Order(), ru.Keys.Where(k => k.StartsWith("Combat.", StringComparison.Ordinal)).Order());
        foreach (string language in new[] { "English", "Russian" })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(ClientRoot, "Data", "Locale", language + ".json")));
            var properties = document.RootElement.EnumerateObject().ToArray();
            Assert.Equal(properties.Length, properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count());
        }
        foreach (var file in Directory.EnumerateFiles(Path.Combine(ClientRoot, "UI", "Screens", "GameSession"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(file), "CombatText\\(\"([^\"\\n]+)\"\\s*,"))
            {
                string key = "Combat." + match.Groups[1].Value;
                Assert.True(en.TryGetValue(key, out var english) && !string.IsNullOrWhiteSpace(english), file + ": " + key);
                Assert.True(ru.TryGetValue(key, out var russian) && !string.IsNullOrWhiteSpace(russian), file + ": " + key);
            }
        }
        Assert.Equal(Localization.Get("Combat.Unknown"), GameSessionScreen.CombatText("future-ui-key", "CALLER LITERAL MUST NEVER BE DISPLAYED"));
        Assert.Equal(Localization.Get("Combat.CommandRejected"), GameSessionScreen.CombatText("Reason.future-code", "CALLER LITERAL"));
        Assert.Equal(Localization.Get("Combat.Forecast.Unknown"), GameSessionScreen.CombatText("Forecast.future-status", "CALLER LITERAL"));
        Assert.Equal("custom-profile-id", GameSessionScreen.CombatText("Profile.custom-profile-id", "CALLER LITERAL"));
        foreach (var status in Enum.GetValues<CombatForecastStatus>())
        {
            Assert.True(en.ContainsKey("Combat.Forecast." + status)); Assert.True(ru.ContainsKey("Combat.Forecast." + status));
        }
        foreach (var reason in new[] { "engine_disabled", "invalid_evade_side", "invalid_turn_payload", "unknown_threat", "combat_speed_locked" })
        { Assert.True(en.ContainsKey("Combat.Reason." + reason)); Assert.True(ru.ContainsKey("Combat.Reason." + reason)); }
    }

    [Theory]
    [InlineData("English", 1f)]
    [InlineData("English", 1.5f)]
    [InlineData("Russian", 1f)]
    [InlineData("Russian", 1.5f)]
    public async Task Actual_point_screen_uses_selected_locale_without_changing_identities(string language, float scale)
    {
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(.02));
        var scene = world.Engine.CaptureSnapshot(); var locale = Localization.LoadLocaleFile(language)!;
        using (Localization.UseLocaleForTests(language))
        {
            var buffer = new SnapshotBuffer(() => 0); buffer.Update(scene);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
            using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1920, 1080); screen.OnMouseMove(1000, 500); screen.OnKeyDown(Key.Q); screen.OnKeyDown(Key.Number2);
            screen.Render(canvas, 1920, 1080);
            Assert.Equal("medium", screen.PointAim!.ProfileId); Assert.NotNull(screen.CurrentTorpedoAimPreview);
            var row = Assert.Single(screen.CommandsPanel.CommandPanelRows, p => p.Weapon?.ModuleId == PointCombatSessionFixture.Torpedo);
            Assert.StartsWith("Torpedo Launcher", row.Name); Assert.Equal(row.Name, row.Weapon!.Caption); // internal identity stays in its domain
            Assert.Contains(row.DetailLines, line => line.StartsWith("> " + locale["Combat.Profile.medium"], StringComparison.Ordinal));
            Assert.Contains(row.DetailLines, line => line.Contains(locale["Combat.KilometersPerSecond"], StringComparison.Ordinal));
            if (!row.Opened)
            {
                float x = row.CaptionRect.MidX * scale, y = row.CaptionRect.MidY * scale;
                screen.OnMouseMove(x, y); screen.OnMouseDown(x, y); screen.OnMouseMove(1000, 500);
                screen.Render(canvas, 1920, 1080);
                row = Assert.Single(screen.CommandsPanel.CommandPanelRows, p => p.Weapon?.ModuleId == PointCombatSessionFixture.Torpedo);
            }
            Assert.Contains(row.Buttons, b => b.CommandTypeId == CombatCommandTypes.Launch && b.Label == locale["Combat.Aim"] + " [Q]");
            Assert.True(screen.AvailableMapRect().Contains(screen.TorpedoAimLabelBounds));
            foreach (var tile in screen.ObjectInfoPanel.OwnModuleDiagram!.Tiles)
            {
                var module = scene.InstalledModules.Single(m => m.ModuleId == tile.Id);
                Assert.Equal(locale["Combat.Module." + module.ModuleTypeId], tile.Name);
            }
            Assert.Equal(locale["Combat.UnknownObject"], ObjectLabelText.Build(SpaceObjectType.UnknownSpaceObject, "hidden-name", "hidden-id"));
            Assert.Equal(locale["Combat.Panel.TorpedoLauncher"] + " 2", Localization.CombatPanelCaption("Torpedo Launcher 2"));
            Assert.DoesNotContain("Combat.", string.Join('\n', row.DetailLines));
            if (scale == 1 && Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(Path.Combine(directory, "combat-locale-" + language + ".png")); png.SaveTo(file);
            }
            screen.OnDeactivated();
        }
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Report_profiles_and_generic_errors_use_the_requested_dictionary(string language)
    {
        var locale = Localization.LoadLocaleFile(language)!;
        var report = new BattleReportSnapshot(1, 1000, BattleReportKind.EnemyShipHit, "episode", new(BattleReportReporterRole.TorpedoOperator, "crew", "Captured Name"),
            TargetDisplayName: "Custom Ship Name", ProjectileKind: ProjectileKind.Torpedo, ProfileId: "fast", BlastRadiusKm: 2.5, HitResult: ProjectileHitResult.Core);
        var presentation = new CombatReportFormatter(key => locale[key]).Format(report);
        Assert.Contains(locale["Combat.Profile.fast"], presentation.Message); Assert.Contains(locale["Combat.Hit.Core"], presentation.Message);
        Assert.Contains("Custom Ship Name", presentation.Message); Assert.Contains("Captured Name", presentation.Reporter);
        Assert.Equal(locale["Combat.CommandRejected"], Localization.Combat("Reason.future", key => locale[key]));
        Assert.Equal("custom-profile", Localization.Combat("Profile.custom-profile", key => locale[key]));
    }

    [Fact]
    public void Locale_scope_restores_current_language_and_preserves_custom_data_names()
    {
        string before = Localization.Get("Combat.Aim");
        using (Localization.UseLocaleForTests("English"))
        {
            Assert.Equal("Aim", Localization.Combat("Aim"));
            using (Localization.UseLocaleForTests("Russian")) Assert.Equal("Прицел", Localization.Combat("Aim"));
            Assert.Equal("Aim", Localization.Combat("Aim"));
            Assert.Equal("User custom module name", Localization.CombatModuleName("custom.module", "User custom module name"));
        }
        Assert.Equal(before, Localization.Get("Combat.Aim"));
    }
}
