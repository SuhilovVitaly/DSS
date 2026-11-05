using System.Globalization;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class SetupScreenTests
{
    [Fact]
    public async Task RealDefaultFormUsesScenarioOwnedRowsLoadoutsTacticsAndProbabilityGoal()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var setup = await Open(fixture);
        Render(setup);
        var options = setup.Flow.Options!;
        var scenario = options.Scenarios[0];
        var request = setup.BuildStartRequest();
        Assert.Equal(scenario.Id, request.ScenarioId);
        Assert.Equal(scenario.OwnTacticId, request.OwnTacticId);
        Assert.Equal(scenario.EnemyTacticId, request.EnemyTacticId);
        Assert.Equal(1, request.Seed);
        var expected = scenario.Drones.Where(d => d.Side == scenario.PlayerSide).Select(d =>
            new TacticalLoadoutChoice(d.Id, d.WeaponId, d.ArmorId, d.ShieldId, d.ShieldCount));
        Assert.Equal(expected, request.Loadout);
        Assert.Contains("80%", setup.ScenarioSummary);
        Assert.Contains("60%", setup.ScenarioSummary);
        Assert.Contains("3%", setup.ScenarioSummary);
        Assert.Contains(scenario.Description, setup.ScenarioSummary);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
    }

    [Fact]
    public async Task SelectedModulesShieldSlotsTacticsAndExactLongSeedReachRealEngine()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var setup = await Open(fixture);
        Render(setup);
        var layout = setup.CurrentLayout!;
        var options = setup.Flow.Options!;
        var first = options.Scenarios[0].Drones.First(d => d.Side == options.Scenarios[0].PlayerSide);
        Choose(setup, layout.RowCell(0, 2), Key.Home);
        Choose(setup, layout.RowCell(0, 3), Key.Home);
        Choose(setup, layout.RowCell(0, 4), Key.End);
        var withShield = setup.BuildStartRequest().Loadout[0];
        Assert.Equal(options.Shields[^1].Id, withShield.ShieldId);
        Assert.Equal(first.ShieldSlots, withShield.ShieldCount);
        Choose(setup, layout.RowCell(0, 4), Key.Home);
        Choose(setup, layout.OwnTactic, Key.Home);
        Choose(setup, layout.EnemyTactic, Key.End);
        Click(setup, layout.Seed);
        WriteSeed(setup, "−9223372036854775808");
        var wanted = setup.BuildStartRequest();
        Assert.Equal(long.MinValue, wanted.Seed);
        Assert.Equal(options.Weapons[0].Id, wanted.Loadout[0].GunId);
        Assert.Equal(options.Armor[0].Id, wanted.Loadout[0].PlateId);
        Assert.Null(wanted.Loadout[0].ShieldId);
        Assert.Equal(0, wanted.Loadout[0].ShieldCount);
        Assert.Equal(options.Tactics[0].Id, wanted.OwnTacticId);
        Assert.Equal(options.Tactics[^1].Id, wanted.EnemyTacticId);
        var action = setup.OnKeyDown(Key.Enter);
        Assert.Equal(ScreenEvent.StartTacticalBattle, action);
        await fixture.Route(action);
        var state = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(long.MinValue, state.Seed);
        Assert.Equal(wanted.OwnTacticId, state.Tactics[0].TacticId);
        Assert.Equal(wanted.EnemyTacticId, state.Tactics[1].TacticId);
        var unit = state.Units.First(u => u.Id == first.Id);
        Assert.Equal(wanted.Loadout[0].GunId, unit.Build!.WeaponId);
        Assert.Equal(wanted.Loadout[0].PlateId, unit.Build.ArmorId);
        Assert.Null(unit.Build.ShieldId);
        Assert.Equal(1, fixture.Proxy.Starts);
    }

    [Fact]
    public async Task SeedBlankZeroMinusOverflowFilterAndRandomAreHandledBeforeStart()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var setup = await Open(fixture);
        Render(setup);
        Click(setup, setup.CurrentLayout!.Seed);
        foreach (string input in new[] { "", "0", "−0", "-0" })
        {
            WriteSeed(setup, input);
            Assert.Equal(1, setup.BuildStartRequest().Seed);
        }
        WriteSeed(setup, "9223372036854775807"); Assert.Equal(long.MaxValue, setup.BuildStartRequest().Seed);
        WriteSeed(setup, "9223372036854775808"); Assert.False(setup.CanStart);
        Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.Enter));
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        Assert.Equal(0, fixture.Proxy.Starts);
        WriteSeed(setup, "Ж+−1"); Assert.Equal("−1", setup.SeedText);
        Assert.Equal(-1, setup.BuildStartRequest().Seed);
        Click(setup, setup.CurrentLayout.RandomSeed);
        Assert.InRange(setup.BuildStartRequest().Seed, 1, 99999);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
    }

    [Fact]
    public async Task KeyboardWrapsFocusAndRevealsOffscreenFieldsAtIncreasedScale()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(uiScale: 1.5f);
        var setup = await Open(fixture);
        Render(setup);
        Assert.True(setup.CurrentLayout!.MaximumScroll > 0);
        setup.OnKeyDown(Key.Tab, KeyModifiers.Shift, false);
        Assert.Equal(ScreenEvent.CloseTacticalBattle, setup.OnKeyDown(Key.Enter));
        setup.OnKeyDown(Key.Tab, KeyModifiers.None, false);
        Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.Enter)); // scenario popup
        Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.Escape)); // close popup, keep setup
        int toSeed = 1 + setup.CurrentLayout.DroneCount * 3 + 2;
        for (int i = 0; i < toSeed; i++) setup.OnKeyDown(Key.Tab, KeyModifiers.None, false);
        Assert.True(setup.CurrentLayout.Scroll > 0);
        Assert.True(setup.CurrentLayout.Content.Contains(setup.CurrentLayout.Seed));
        WriteSeed(setup, "42"); Assert.Equal(42, setup.BuildStartRequest().Seed);
        setup.OnKeyDown(Key.Tab, KeyModifiers.Shift, false);
        Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.KeypadEnter)); // enemy tactic popup
        setup.OnInputReset();
        Assert.Equal(ScreenEvent.CloseTacticalBattle, setup.OnKeyDown(Key.Escape));
    }

    [Fact]
    public async Task SetupUsesPlayerSideBAndKeepsNpcRangesInItsOwnedRows()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var options = fixture.Engine.GetTacticalBattleOptions();
        var scenario = options.Scenarios[0];
        var sideB = scenario with
        {
            PlayerSide = "B",
            Goal = scenario.Goal! with { PlayerSide = "B" },
            OwnTacticId = scenario.EnemyTacticId,
            EnemyTacticId = scenario.OwnTacticId
        };
        fixture.Proxy.OptionsOverride = options with { Scenarios = [sideB] };
        var setup = await Open(fixture);
        Render(setup);
        var owned = sideB.Drones.Where(d => d.Side == "B").ToArray();
        Assert.Contains(owned, d => d.Accuracy.IsRandom || d.Maneuver.IsRandom);
        Assert.Equal(owned.Select(d => d.Id), setup.BuildStartRequest().Loadout.Select(d => d.DroneId));
        Assert.Equal(owned.Length, setup.CurrentLayout!.DroneCount);
        Assert.Equal(sideB.OwnTacticId, setup.BuildStartRequest().OwnTacticId);
    }

    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1.5f)]
    public async Task ActualFormRasterFitsSupportedSizesAndStickyActionsRemainVisible(int width, int height, float scale)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(uiScale: scale);
        var setup = await Open(fixture);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        setup.Render(canvas, width, height);
        var layout = setup.CurrentLayout!;
        Assert.True(layout.Viewport.Contains(layout.Close)); Assert.True(layout.Viewport.Contains(layout.Start));
        Assert.True(layout.Content.Bottom < layout.Start.Top);
        Assert.True(layout.Close.Right < layout.Start.Left);
        if (width == 1280 && scale > 1) Assert.True(layout.MaximumScroll > 0);
        else { Assert.Equal(0, layout.MaximumScroll); Assert.True(layout.Content.Contains(layout.Help)); }
        Assert.NotEqual(bitmap.GetPixel(0, 0), bitmap.GetPixel((int)(layout.Start.MidX * scale), (int)(layout.Start.MidY * scale)));
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { } root)
        {
            string directory = Path.Combine(root, "renders"); Directory.CreateDirectory(directory);
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, $"setup-{width}x{height}-scale{scale.ToString(CultureInfo.InvariantCulture)}.png"));
            data.SaveTo(file);
        }
    }

    private static async Task<TacticalBattleSetupScreen> Open(BattleNavigationTests.Fixture fixture)
    {
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        return setup;
    }
    private static void Render(TacticalBattleSetupScreen setup)
    {
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        setup.Render(canvas, 1280, 720);
    }
    private static void Click(TacticalBattleSetupScreen setup, SKRect rect) => setup.OnMouseDown(rect.MidX * setup.UiScale, rect.MidY * setup.UiScale, MouseButton.Left);
    private static void Choose(TacticalBattleSetupScreen setup, SKRect rect, Key navigation)
    {
        Click(setup, rect); setup.OnKeyDown(navigation); Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.Enter));
    }
    private static void WriteSeed(TacticalBattleSetupScreen setup, string text)
    {
        for (int i = 0; i < 20; i++) setup.OnKeyDown(Key.Backspace);
        foreach (char c in text) setup.OnTextInput(c);
    }
}
