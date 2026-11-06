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
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", EnemySectors: true);
        using var on = Raster(frame);
        using var off = Raster(frame with { EnemyForecast = false, EnemySectors = false });
        Assert.True(on.GetPixel(490, 360).Red > 160); // endpoint at x = 50
        Assert.True(on.GetPixel(463, 369).Red > off.GetPixel(463, 369).Red + 25); // inside sector
        Assert.True(on.GetPixel(470, 379).Red > on.GetPixel(470, 376).Red); // radial edge
        Assert.Equal(off.GetPixel(470, 384), on.GetPixel(470, 384)); // outside sector
    }

    [Fact]
    public async Task SelectedOnlyFiringSectorHidesOtherDronesArcs()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var own = battle.Units.Where(u => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone).Take(2).ToArray();
        var selected = own[0] with { Pose = new(-100, -100, 0), Build = own[0].Build! with { Range = 90, ArcDegrees = 30 } };
        var other = own[1] with { Pose = new(-100, 100, 0), Build = own[1].Build! with { Range = 90, ArcDegrees = 30 } };
        var plan = new TacticalFirePlan([Row(selected), Row(other)], selected.Id);
        var frame = new TacticalBattleFrame(battle with { Units = [selected, other], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", FirePlan: plan);
        using var all = Raster(frame);
        using var one = Raster(frame with { AllFireSectors = false });
        Assert.Equal(all.GetPixel(408, 260), one.GetPixel(408, 260));
        Assert.True(all.GetPixel(408, 460).Green > one.GetPixel(408, 460).Green);
        using var switched = Raster(frame with { AllFireSectors = false, FirePlan = plan with { SelectedDroneId = other.Id } });
        Assert.True(switched.GetPixel(408, 460).Green > one.GetPixel(408, 460).Green);
        Assert.True(switched.GetPixel(408, 260).Green < one.GetPixel(408, 260).Green);

        static TacticalFirePlanRow Row(TacticalUnitSnapshot unit) => new(unit, new(unit.Id, [], null), null);
    }

    [Fact]
    public async Task FirePlanShowsCandidateBadgeAndDistinctAlternativeArrow()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var shooter = battle.Units.First(u => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone) with
        {
            Pose = new(-200, 0, 0)
        };
        var enemies = battle.Units.Where(u => u.Side != battle.PlayerSide && u.Kind == TacticalUnitKind.Drone).Take(2).ToArray();
        var chosen = enemies[0] with { Pose = new(100, 0, 180) };
        var other = enemies[1] with { Pose = new(-200, 200, 180) };
        var group = new TacticalTargetGroupSnapshot(shooter.Id,
            [new(chosen.Id, 300, 0, false, 0.7, 0), new(other.Id, 200, 0, false, 0.5, 0)], chosen.Id);
        var frame = new TacticalBattleFrame(battle with { Units = [shooter, chosen, other], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", EnemyForecast: false,
            FirePlan: new([new(shooter, group, chosen.Id)], shooter.Id));
        using var planned = Raster(frame);
        using var noPlan = Raster(frame with { FirePlan = null });
        var alternative = planned.GetPixel(240, 445);
        Assert.InRange(alternative.Red, 30, 74);
        Assert.True(planned.GetPixel(370, 360).Blue > alternative.Blue + 50);
        Assert.NotEqual(noPlan.GetPixel(240, 445), alternative);
        var orange = new SKColor(243, 146, 49);
        Assert.Contains(orange, planned.Pixels);
        Assert.DoesNotContain(orange, noPlan.Pixels);
    }

    [Fact]
    public async Task WreckMarkerAndNameAppearAtLethalArrivalBeforeSalvoEnds()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var enemy = battle.Units.First(u => u.Side != battle.PlayerSide && u.Kind == TacticalUnitKind.Drone) with
        {
            Id = "Tag-7", Pose = new(50, 0, 180)
        };
        var destroyed = enemy with { State = TacticalUnitState.Destroyed, Hull = 0 };
        var shot = new TacticalShotSnapshot(battle.Turn, "shooter", enemy.Id, new(-200, 0, 0), enemy.Pose,
            1, true, 40, false, false, true) { LethalImpact = true };
        var frame = new TacticalBattleFrame(battle with { Units = [destroyed], BeforeSalvoUnits = [enemy], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", TacticalBattlePresentation.SalvoFlight, EnemyForecast: false);
        var results = new TacticalBattleResults(frame.Battle, [shot, shot with { Hit = false, LethalImpact = false }], [], 0);
        using var flying = Raster(frame with { Salvo = results.Capture(949) });
        var arrival = results.Capture(950);
        Assert.True(arrival.InFlight);
        using var impact = Raster(frame with { Salvo = arrival });
        using var movement = Raster(frame with { Presentation = TacticalBattlePresentation.Current });
        Assert.Equal(TacticalBattleMapRenderer.Enemy, flying.GetPixel(490, 360));
        foreach (var marker in new[] { impact.GetPixel(490, 360), movement.GetPixel(490, 360) })
        {
            Assert.InRange(marker.Red, 83, 85);
            Assert.Equal(marker.Red, marker.Green);
        }

        // The name has the same footprint below the marker during playback and movement.
        using var nameless = Raster(frame with
        {
            Battle = frame.Battle with { Units = [destroyed with { Id = "" }] },
            Presentation = TacticalBattlePresentation.Current
        });
        int textPixels = 0;
        for (int y = 374; y < 389; y++)
            for (int x = 465; x < 515; x++)
                if (impact.GetPixel(x, y) == movement.GetPixel(x, y) &&
                    movement.GetPixel(x, y) != nameless.GetPixel(x, y)) textPixels++;
        Assert.True(textPixels > 30);
    }

    [Fact]
    public async Task SalvoDamageSummaryDoesNotChangeTheMap()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var wreck = battle.Units.First(u => u.Kind == TacticalUnitKind.Drone) with
        {
            Pose = new(0, 0, 0), State = TacticalUnitState.Destroyed, Hull = 0
        };
        var salvo = new TacticalSalvoFrame(false, false, [], [new(wreck, 20, 40, true)]);
        var frame = new TacticalBattleFrame(battle with { Units = [wreck], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", TacticalBattlePresentation.SalvoResults, Salvo: salvo);
        using var withDamage = Raster(frame);
        using var withoutDamage = Raster(frame with { Salvo = salvo with { Damage = [] } });
        Assert.True(withDamage.Pixels.AsSpan().SequenceEqual(withoutDamage.Pixels));
    }

    [Fact]
    public async Task OffscreenDronesAndCarrierGetSeparateMarkersOnTheirMapEdges()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var own = battle.Units.Where(u => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone).Take(2).ToArray();
        var enemy = battle.Units.First(u => u.Side != battle.PlayerSide && u.Kind == TacticalUnitKind.Drone);
        var carrier = battle.Units.First(u => u.Side != battle.PlayerSide && u.Kind == TacticalUnitKind.Carrier);
        var units = new[]
        {
            own[0] with { Id = "Own-1", Pose = new(520, 0, 0) },
            own[1] with { Id = "Own-2", Pose = new(650, 0, 0) },
            enemy with { Id = "Enemy", Pose = new(-510, -100, 0) },
            carrier with { Id = "Carrier", Pose = new(0, -460, 0) }
        };
        var frame = new TacticalBattleFrame(battle with { Units = [.. units], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", EnemyForecast: false);
        using var outside = Raster(frame);
        Assert.True(HasColor(outside, new(790, 349, 875, 371), TacticalBattleMapRenderer.Own));
        Assert.True(HasColor(outside, new(790, 374, 875, 396), TacticalBattleMapRenderer.Own));
        Assert.True(HasColor(outside, new(5, 250, 90, 300), TacticalBattleMapRenderer.Enemy));
        Assert.True(HasColor(outside, new(390, 5, 490, 28), TacticalBattleMapRenderer.Enemy));

        units[0] = units[0] with { Pose = new(0, 0, 0) };
        units[1] = units[1] with { State = TacticalUnitState.Destroyed };
        using var returned = Raster(frame with { Battle = frame.Battle with { Units = [.. units] } });
        Assert.False(HasColor(returned, new(790, 349, 875, 396), TacticalBattleMapRenderer.Own));
        Assert.Equal(TacticalBattleMapRenderer.Own, returned.GetPixel(440, 360));

        static bool HasColor(SKBitmap bitmap, SKRectI bounds, SKColor color)
        {
            for (int y = bounds.Top; y < bounds.Bottom; y++)
                for (int x = bounds.Left; x < bounds.Right; x++)
                    if (bitmap.GetPixel(x, y) == color) return true;
            return false;
        }
    }

    [Fact]
    public async Task DisabledDroneShowsRedCrossInTheLabelInMoveAndFire()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var drone = battle.Units.First(u => u.Kind == TacticalUnitKind.Drone) with
        {
            Id = "Drone", Pose = new(0, 0, 0), State = TacticalUnitState.Disabled
        };
        var frame = new TacticalBattleFrame(battle with { Units = [drone], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", EnemyForecast: false);
        using var movement = Raster(frame);
        using var firing = Raster(frame with
        {
            Battle = frame.Battle with { Phase = TacticalBattlePhase.Fire },
            FirePlan = new([], null)
        });
        Assert.Equal(FindCross(movement), FindCross(firing));
        var cross = FindCross(movement);
        Assert.InRange(cross.Width, 14, 16);
        Assert.InRange(cross.Height, 14, 16);
        Assert.True(cross.Right < 440 && cross.Top >= 374);

        var wreck = drone with { State = TacticalUnitState.Destroyed, Hull = 0 };
        using var destroyed = Raster(frame with { Battle = frame.Battle with { Units = [wreck] } });
        Assert.True(FindCross(destroyed).IsEmpty);

        static SKRectI FindCross(SKBitmap bitmap)
        {
            var bounds = SKRectI.Empty;
            for (int y = 374; y < 400; y++)
                for (int x = 350; x < 470; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    if (color.Red < 150 || color.Green > 75 || color.Blue > 75) continue;
                    var pixel = SKRectI.Create(x, y, 1, 1);
                    if (bounds.IsEmpty) bounds = pixel;
                    else bounds.Union(pixel);
                }
            return bounds;
        }
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
        var (forecast, sectors, allFire) = screen.OverlayControls;
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(forecast.MidX, forecast.MidY, MouseButton.Left));
        screen.Render(canvas, 1280, 720);
        Assert.True(ChangedPixels(defaultMap, bitmap) > 25);
        using var noForecast = bitmap.Copy();
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(sectors.MidX, sectors.MidY, MouseButton.Left));
        screen.Render(canvas, 1280, 720);
        Assert.True(ChangedPixels(noForecast, bitmap) > 25);
        Assert.True(screen.CaptureFrame(1280, 720)!.AllFireSectors);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(allFire.MidX, allFire.MidY, MouseButton.Left));
        var visibility = screen.CaptureFrame(1280, 720)!;
        Assert.False(visibility.AllFireSectors);
        Assert.False(visibility.EnemyForecast);
        Assert.False(visibility.EnemySectors);
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
