using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Settings;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("Battle localization")]
public sealed class SkinBattleIntegrationTests
{
    private const int Width = 1280, Height = 720;
    private static readonly SKColor FirstColor = new(93, 24, 128);
    private static readonly SKColor EditedColor = new(23, 113, 72);

    [Fact]
    public async Task ClassicAndContrastShareIdenticalPlaybackFacts()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var classic = await Start(fixture);
        for (int turn = 0; turn < 30 && classic.Flow.State?.Phase == TacticalBattlePhase.Move; turn++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        var before = classic.Flow.State!;
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var contrast = new TacticalBattleScreen(classic.Flow, 1, skins.GetOrClassic("contrast"))
        { ClockMilliseconds = classic.ClockMilliseconds };
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var accepted = classic.Flow.State!;
        contrast.OnBattleUpdated(before, new("same-presentation", true, TacticalBattleRejectionCode.None, accepted.Phase, accepted, []));
        foreach (long elapsed in new long[] { 0, 300, 975, 975, 10000 })
        {
            fixture.AdvanceAnimation(elapsed);
            var one = classic.CaptureFrame(1280, 720)!;
            var two = contrast.CaptureFrame(1280, 720)!;
            Assert.Equal(JsonSerializer.Serialize(one.Battle), JsonSerializer.Serialize(two.Battle));
            Assert.Equal(one.Presentation, two.Presentation);
            Assert.Equal(one.Salvo!.Flights.Select(f => (f.LaunchMs, f.DurationMs, f.Arrived)),
                two.Salvo!.Flights.Select(f => (f.LaunchMs, f.DurationMs, f.Arrived)));
        }
        Assert.Equal(1, fixture.Proxy.Fires);
        Assert.Same(accepted, classic.Flow.State);
    }

    [Fact]
    public async Task Installed_pack_flows_from_settings_to_real_battle_and_survives_restart()
    {
        using var profile = new Profile();
        profile.Install(FirstColor);
        string previousLanguage = Localization.ReadLanguageSetting();
        try
        {
            await using (var fixture = await BattleNavigationTests.Fixture.Create(settingsPath: profile.Settings))
            {
                var settings = await OpenSettings(fixture);
                Assert.Equal("classic", settings.SelectedSkinId);
                SelectSkin(settings, Key.End);
                Assert.Equal("workbench", settings.SelectedSkinId);
                Assert.Equal("workbench", TacticalBattleSkinPreference.Read(profile.Settings));
                await fixture.Route(ScreenEvent.CloseSettings);
                var battle = await Start(fixture);
                Assert.Equal("workbench", battle.SkinId);
                using var first = Draw(battle);
                Assert.Contains(FirstColor, first.Pixels);

                await fixture.Route(ScreenEvent.NewTacticalBattle);
                var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
                await setup.Flow.OptionsTask!;
                await fixture.Route(ScreenEvent.StartTacticalBattle);
                var again = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
                Assert.Equal("workbench", again.SkinId);
                Assert.Equal(battle.Flow.State!.Seed, again.Flow.State!.Seed);
                await Exit(fixture, again);
                Assert.Equal("workbench", (await Start(fixture)).SkinId);
            }

            profile.Install(EditedColor);
            await using var restarted = await BattleNavigationTests.Fixture.Create(settingsPath: profile.Settings);
            var restored = await OpenSettings(restarted);
            Assert.Equal("workbench", restored.SelectedSkinId);
            await restarted.Route(ScreenEvent.CloseSettings);
            var themed = await Start(restarted);
            using var updated = Draw(themed);
            Assert.Contains(EditedColor, updated.Pixels);
            Assert.DoesNotContain(FirstColor, updated.Pixels);
            await Exit(restarted, themed);
            var selectClassic = await OpenSettings(restarted);
            SelectSkin(selectClassic, Key.Home);
            Assert.Equal("classic", selectClassic.SelectedSkinId);
            await restarted.Route(ScreenEvent.CloseSettings);
            Assert.Equal("classic", (await Start(restarted)).SkinId);
        }
        finally { Localization.ApplyLanguage(previousLanguage); }
    }

    [Fact]
    public async Task Missing_pack_falls_back_and_battle_state_is_skin_independent()
    {
        using var profile = new Profile();
        profile.Install(FirstColor);
        Assert.True(TacticalBattleSkinPreference.TryWrite(profile.Settings, "workbench"));
        string previousLanguage = Localization.ReadLanguageSetting();
        try
        {
            string styled = await Play("workbench");
            File.Delete(profile.Manifest);
            string classic = await Play("classic");
            Assert.Equal(styled, classic);
            Assert.Equal("workbench", TacticalBattleSkinPreference.Read(profile.Settings));
        }
        finally { Localization.ApplyLanguage(previousLanguage); }

        async Task<string> Play(string expected)
        {
            await using var fixture = await BattleNavigationTests.Fixture.Create(settingsPath: profile.Settings);
            var settings = await OpenSettings(fixture);
            Assert.Equal(expected, settings.SelectedSkinId);
            await fixture.Route(ScreenEvent.CloseSettings);
            var screen = await Start(fixture);
            Assert.Equal(expected, screen.SkinId);
            for (int turn = 0; turn < 100 && screen.Flow.State!.Phase != TacticalBattlePhase.Over; turn++)
            {
                var evt = screen.Flow.State.Phase == TacticalBattlePhase.Move
                    ? ScreenEvent.MoveTacticalBattle : ScreenEvent.FireTacticalBattle;
                await fixture.Route(evt);
            }
            var state = screen.Flow.State!;
            Assert.NotNull(state.Outcome);
            string outcome = JsonSerializer.Serialize(new { state.Units, state.Losses, state.Outcome, state.Turn });
            await Exit(fixture, screen);
            Assert.Same(fixture.Game, fixture.Screens.Current);
            Assert.False(fixture.Engine.IsTacticalBattleActive);
            return outcome;
        }
    }

    [Fact]
    public async Task Language_and_skin_survive_settings_changes_and_real_f8_start()
    {
        using var profile = new Profile();
        profile.Install(FirstColor);
        string previousLanguage = Localization.ReadLanguageSetting();
        try
        {
            await using var fixture = await BattleNavigationTests.Fixture.Create(settingsPath: profile.Settings);
            var settings = await OpenSettings(fixture);
            SelectSkin(settings, Key.End);
            SelectLanguage(settings, 1);
            Assert.Equal("Russian", Localization.ReadLanguageSetting(profile.Settings));
            Assert.Equal("workbench", TacticalBattleSkinPreference.Read(profile.Settings));
            await fixture.Route(ScreenEvent.CloseSettings);
            Localization.ApplyLanguage("English");
            var russian = await Start(fixture, "Дронов противника:");
            Assert.Equal("workbench", russian.SkinId);
            Assert.Equal("Журнал", russian.PanelTabs.Items.Single(tab => tab.Id == "log").Label);
            await Exit(fixture, russian);

            settings = await OpenSettings(fixture);
            SelectLanguage(settings, 0);
            SelectSkin(settings, Key.Home);
            Assert.Equal("English", Localization.ReadLanguageSetting(profile.Settings));
            Assert.Equal("classic", TacticalBattleSkinPreference.Read(profile.Settings));
            await fixture.Route(ScreenEvent.CloseSettings);
            Localization.ApplyLanguage("Russian");
            var english = await Start(fixture, "Enemy drones:");
            Assert.Equal("classic", english.SkinId);
            Assert.Equal("Log", english.PanelTabs.Items.Single(tab => tab.Id == "log").Label);
            using var doc = JsonDocument.Parse(File.ReadAllText(profile.Settings));
            Assert.Equal(17, doc.RootElement.GetProperty("unrelated").GetInt32());
        }
        finally { Localization.ApplyLanguage(previousLanguage); }
    }

    private static async Task<SettingsScreen> OpenSettings(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenSettings);
        var settings = Assert.IsType<SettingsScreen>(fixture.Screens.Current);
        using var bitmap = Draw(settings);
        return settings;
    }

    private static void SelectSkin(SettingsScreen settings, Key target)
    {
        var field = SettingsLayout.SkinField(Width, Height);
        settings.OnMouseDown(field.MidX, field.MidY, MouseButton.Left);
        settings.OnKeyDown(target);
        settings.OnKeyDown(Key.Enter);
        Assert.False(settings.SkinSaveFailed);
    }

    private static void SelectLanguage(SettingsScreen settings, int option)
    {
        float y = SettingsLayout.PanelTop(Height) + SettingsLayout.LanguageRowY;
        settings.OnMouseDown(Width / 2f, y + 10, MouseButton.Left);
        settings.OnMouseDown(Width / 2f, y + SettingsLayout.LanguageComboHeight + (option + 0.5f) * SettingsLayout.LanguageOptionHeight,
            MouseButton.Left);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture, string? expectedSetupText = null)
    {
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        if (expectedSetupText is not null)
        {
            using var bitmap = Draw(setup);
            Assert.Contains(expectedSetupText, setup.ScenarioSummary);
        }
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }

    private static async Task Exit(BattleNavigationTests.Fixture fixture, TacticalBattleScreen screen)
    {
        fixture.FinishPresentation();
        var evt = screen.OnKeyDown(Key.Escape);
        if (evt == ScreenEvent.None)
        {
            Assert.True(screen.ConfirmExit);
            evt = screen.OnKeyDown(Key.Enter);
        }
        Assert.Equal(ScreenEvent.CloseTacticalBattle, evt);
        await fixture.Route(evt);
    }

    private static SKBitmap Draw(IScreen screen)
    {
        var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, Width, Height);
        return bitmap;
    }

    private sealed class Profile : IDisposable
    {
        private readonly string _root = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath(),
            "skin-integration-" + Guid.NewGuid().ToString("N"));
        private string Pack => Path.Combine(_root, "Skins", "workbench");
        internal string Settings => Path.Combine(_root, "Settings.json");
        internal string Manifest => Path.Combine(Pack, "skin.json");

        internal Profile()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(Settings, """{"gameSettings":{"language":"English"},"unrelated":17}""");
        }

        internal void Install(SKColor color)
        {
            var assets = Path.Combine(Pack, "assets");
            Directory.CreateDirectory(assets);
            using var bitmap = new SKBitmap(8, 8);
            bitmap.Erase(color);
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            string path = color == FirstColor ? "initial.png" : "edited.png";
            using (var stream = File.Create(Path.Combine(assets, path)))
                png.SaveTo(stream);
            File.WriteAllText(Manifest, JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                id = "workbench",
                displayName = "Workbench",
                roles = new Dictionary<string, object> { ["field.background"] = new { image = "assets/" + path } },
            }));
        }

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
