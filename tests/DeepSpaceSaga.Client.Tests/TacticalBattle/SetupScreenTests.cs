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
    public async Task RealDefaultFormUsesOwnedWingRowsScenarioTacticsAndProbabilityGoal()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var setup = await Open(fixture);
        Render(setup);
        var options = setup.Flow.Options!;
        var scenario = options.Scenarios[0];
        var carrier = fixture.Handle.CarrierOperations!;
        var request = setup.BuildStartRequest();
        Assert.Equal(scenario.Id, request.ScenarioId);
        Assert.Equal(scenario.OwnTacticId, request.OwnTacticId);
        Assert.Equal(scenario.EnemyTacticId, request.EnemyTacticId);
        Assert.Equal(1, request.Seed);
        // EP-0029: the F8 form renders the persistent owned wing read-only; it never submits a
        // public loadout and always carries the latest authoritative ShipId/revision.
        Assert.True(setup.OwnedWingMode);
        Assert.Equal(0, setup.EditableLoadoutControls);
        Assert.Equal(carrier.OwnedDrones.Select(drone => drone.Id), setup.WingComposition.Select(drone => drone.Id));
        Assert.True(request.Loadout.IsDefaultOrEmpty);
        Assert.Equal(carrier.ShipId, request.SourceShipId);
        Assert.Equal(carrier.Revision, request.ExpectedCarrierRevision);
        Assert.Contains("80%", setup.ScenarioSummary);
        Assert.Contains("60%", setup.ScenarioSummary);
        Assert.Contains("3%", setup.ScenarioSummary);
        Assert.Contains(scenario.Description, setup.ScenarioSummary);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
    }

    [Fact]
    public async Task OwnedReadOnlyWingTacticsAndExactLongSeedReachRealEngine()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var setup = await Open(fixture);
        Render(setup);
        var layout = setup.CurrentLayout!;
        var options = setup.Flow.Options!;
        var carrier = fixture.Handle.CarrierOperations!;
        Assert.True(setup.OwnedWingMode);
        Assert.Equal(0, setup.EditableLoadoutControls);
        Choose(setup, layout.OwnTactic, Key.Home);
        Choose(setup, layout.EnemyTactic, Key.End);
        Click(setup, layout.Seed);
        WriteSeed(setup, "−9223372036854775808");
        var wanted = setup.BuildStartRequest();
        Assert.Equal(long.MinValue, wanted.Seed);
        Assert.True(wanted.Loadout.IsDefaultOrEmpty);
        Assert.Equal(carrier.ShipId, wanted.SourceShipId);
        Assert.Equal(carrier.Revision, wanted.ExpectedCarrierRevision);
        Assert.Equal(options.Tactics[0].Id, wanted.OwnTacticId);
        Assert.Equal(options.Tactics[^1].Id, wanted.EnemyTacticId);
        var action = setup.OnKeyDown(Key.Enter);
        Assert.Equal(ScreenEvent.StartTacticalBattle, action);
        await fixture.Route(action);
        var state = fixture.Engine.GetTacticalBattleState()!;
        Assert.Equal(long.MinValue, state.Seed);
        Assert.Equal(wanted.OwnTacticId, state.Tactics[0].TacticId);
        Assert.Equal(wanted.EnemyTacticId, state.Tactics[1].TacticId);
        // The launched units are the owned drones with their owned equipment, not catalog defaults.
        var ownUnits = state.Units
            .Where(unit => unit.Side == state.PlayerSide && unit.Kind == TacticalUnitKind.Drone).ToArray();
        Assert.Equal(carrier.OwnedDrones.Select(drone => drone.Id).Order(StringComparer.Ordinal),
            ownUnits.Select(unit => unit.Id).Order(StringComparer.Ordinal));
        foreach (var unit in ownUnits)
        {
            var drone = carrier.OwnedDrones.Single(candidate => candidate.Id == unit.Id);
            Assert.Equal(drone.WeaponId, unit.Build!.WeaponId);
            Assert.Equal(drone.ArmorId, unit.Build.ArmorId);
            Assert.Equal(drone.ShieldId, unit.Build.ShieldId);
            Assert.Equal(drone.ShieldCount, unit.Build.ShieldCount);
            Assert.Equal(drone.AmmoCapacity, unit.AmmoCapacity!.Value);
        }
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
        // Owned rows are read-only: focus order is scenario, own tactic, enemy tactic, then seed.
        int toSeed = 1 + 2 + 1;
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
    public async Task OwnedSetupKeepsScenarioPlayerSideAndNpcRangesInItsRows()
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
        // The encounter's side-B drones still describe NPC ranges, but the F8 form shows and
        // launches only the persistent owned wing — scenario/player-side/tactics stay selectable.
        var encounterRows = sideB.Drones.Where(d => d.Side == "B").ToArray();
        Assert.Contains(encounterRows, d => d.Accuracy.IsRandom || d.Maneuver.IsRandom);
        Assert.True(setup.OwnedWingMode);
        Assert.Equal(fixture.Handle.CarrierOperations!.OwnedDrones.Length, setup.CurrentLayout!.DroneCount);
        var request = setup.BuildStartRequest();
        Assert.True(request.Loadout.IsDefaultOrEmpty);
        Assert.Equal(sideB.Id, request.ScenarioId);
        Assert.Equal(sideB.OwnTacticId, request.OwnTacticId);
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
