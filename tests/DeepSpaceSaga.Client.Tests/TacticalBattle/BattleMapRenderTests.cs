using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleMapRenderTests
{
    [Fact]
    public async Task MarkerColorsFollowPlayerAndInactiveAndJammedStatesRenderDifferently()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var source = screen.Flow.State!;
        var carrierA = source.Units.First(u => u.Kind == TacticalUnitKind.Carrier && u.Side == "A");
        var carrierB = source.Units.First(u => u.Kind == TacticalUnitKind.Carrier && u.Side == "B");
        var droneA = source.Units.First(u => u.Kind == TacticalUnitKind.Drone && u.Side == "A");
        var droneB = source.Units.First(u => u.Kind == TacticalUnitKind.Drone && u.Side == "B");
        var inactive = source.Units.Where(u => u.Kind == TacticalUnitKind.Drone && u.Side == "A").Skip(1).Take(2).ToArray();
        ImmutableArray<TacticalUnitSnapshot> units =
        [
            carrierA with { Pose = new(-280, -150, 0) },
            carrierB with { Pose = new(280, -150, 30), State = TacticalUnitState.Destroyed, Hull = 0 },
            droneA with { Pose = new(-230, 0, 0), Jammed = true },
            droneB with { Pose = new(230, 0, 180) },
            inactive[0] with { Pose = new(-120, 135, 0), State = TacticalUnitState.Disabled, Hull = 10 },
            inactive[1] with { Pose = new(120, 135, 0), State = TacticalUnitState.Destroyed, Hull = 0 }
        ];
        var state = source with { Units = units, Paths = [] };
        var frame = new TacticalBattleFrame(state, new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "",
            EnemyForecast: false, SelectedDroneId: droneA.Id);
        using var a = Raster(frame);
        using var b = Raster(frame with { Battle = state with { PlayerSide = "B" } });
        Assert.Equal(TacticalBattleMapRenderer.Own, a.GetPixel(160, 210));
        Assert.Equal(TacticalBattleMapRenderer.Enemy, b.GetPixel(160, 210));
        Assert.Equal(TacticalBattleMapRenderer.Inactive, a.GetPixel(720, 210));
        Assert.Equal(TacticalBattleMapRenderer.Own, a.GetPixel(210, 360));
        Assert.Equal(TacticalBattleMapRenderer.Enemy, b.GetPixel(210, 360));
        Assert.Equal(TacticalBattleMapRenderer.Enemy, a.GetPixel(670, 360));
        Assert.Equal(TacticalBattleMapRenderer.Own, b.GetPixel(670, 360));
        var ring = a.GetPixel(210, 350);
        Assert.True(ring.Red > 170 && ring.Green > 120 && ring.Blue < 110);
        var selection = a.GetPixel(210, 348);
        Assert.True(selection.Red > 140 && selection.Blue > 140);
        var destroyed = a.GetPixel(560, 495);
        Assert.True(destroyed.Red >= 60 && Math.Abs(destroyed.Red - destroyed.Green) < 16);
        Assert.NotEqual(TacticalBattleMapRenderer.Inactive, a.GetPixel(320, 495)); // open, not filled
        Save(a, "battle-map-states.png");
    }

    [Fact]
    public async Task StraightForecastShowsEndpointAndFilledRearSector()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var battle = screen.Flow.State!;
        var enemy = battle.Units.First(u => u.Side != battle.PlayerSide && u.Kind == TacticalUnitKind.Drone) with
        {
            Pose = new(-100, 0, 0), Speed = 150
        };
        var frame = new TacticalBattleFrame(battle with { Units = [enemy], Paths = [], Rules = battle.Rules with { RearArc = 45 } },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "");
        using var on = Raster(frame);
        using var off = Raster(frame with { EnemyForecast = false });
        Assert.True(on.GetPixel(490, 360).Red > 160); // endpoint at x = 50
        Assert.True(on.GetPixel(463, 369).Red > off.GetPixel(463, 369).Red + 25); // inside sector
        Assert.True(on.GetPixel(470, 379).Red > on.GetPixel(470, 376).Red); // radial edge
        Assert.Equal(off.GetPixel(470, 384), on.GetPixel(470, 384)); // outside sector
    }

    [Fact]
    public async Task GridRulerAndActualPanelTogglesChangeOnlyPresentation()
    {
        Assert.Equal(40, TacticalBattleMapRenderer.GridStep(1800));
        Assert.Equal(80, TacticalBattleMapRenderer.GridStep(3600));
        Assert.Equal(20, TacticalBattleMapRenderer.GridStep(900));
        Assert.Equal(100, TacticalBattleMapRenderer.RulerLength(1, 1));
        Assert.Equal(20, TacticalBattleMapRenderer.RulerLength(5, 1));
        Assert.Equal(10, TacticalBattleMapRenderer.RulerLength(20, 2));
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State;
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Save(bitmap, "battle-map-initial.png");
        using var defaultMap = bitmap.Copy();
        var (forecast, _) = screen.OverlayControls;
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(forecast.MidX, forecast.MidY, MouseButton.Left));
        screen.Render(canvas, 1280, 720);
        Assert.True(ChangedPixels(defaultMap, bitmap) > 25);
        using var noForecast = bitmap.Copy();
        var (_, sectors) = screen.OverlayControls;
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(sectors.MidX, sectors.MidY, MouseButton.Left));
        screen.Render(canvas, 1280, 720);
        Assert.True(ChangedPixels(noForecast, bitmap) > 100);
        Assert.Same(state, screen.Flow.State);
        Assert.NotEqual(bitmap.GetPixel(20, 702), bitmap.GetPixel(20, 706));

        static int ChangedPixels(SKBitmap before, SKBitmap after)
        {
            int changed = 0;
            for (int y = 0; y < before.Height; y += 2)
                for (int x = 0; x < 880; x += 2)
                    if (before.GetPixel(x, y) != after.GetPixel(x, y)) changed++;
            return changed;
        }
    }

    private static SKBitmap Raster(TacticalBattleFrame frame)
    {
        var bitmap = new SKBitmap(frame.View.PixelWidth, frame.View.PixelHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(2, 12, 22));
        TacticalBattleMapRenderer.Draw(canvas, frame);
        return bitmap;
    }

    private static void Save(SKBitmap bitmap, string name)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, name));
        png.SaveTo(file);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }
}
