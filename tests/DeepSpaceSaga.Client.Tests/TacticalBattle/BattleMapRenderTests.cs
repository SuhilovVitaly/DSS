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
    public async Task WreckMarkerAndNameAppearAtLethalArrivalBeforeSalvoEnds()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var enemy = battle.Units.First(u => u.Side != battle.PlayerSide && u.Kind == TacticalUnitKind.Drone) with
        {
            Id = "Tag-7",
            Pose = new(50, 0, 180)
        };
        var destroyed = enemy with { State = TacticalUnitState.Destroyed, Hull = 0 };
        var shot = new TacticalShotSnapshot(battle.Turn, "shooter", enemy.Id, new(-200, 0, 0), enemy.Pose,
            1, true, 40, false, false, true)
        { LethalImpact = true };
        var frame = new TacticalBattleFrame(battle with { Units = [destroyed], BeforeSalvoUnits = [enemy], Paths = [] },
            new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", TacticalBattlePresentation.SalvoFlight, EnemyForecast: false);
        var miss = shot with { Hit = false, Killed = false, LethalImpact = false };
        var state = frame.Battle with
        {
            Shots = [shot, miss],
            FireSteps = [new(0, shot.ShooterId, enemy.Id, TacticalFireSkipReason.None, shot, [destroyed], [], null),
                new(1, miss.ShooterId, enemy.Id, TacticalFireSkipReason.None, miss, [destroyed], [], null)]
        };
        var results = new TacticalBattleResults(state, state.Shots, [], 0);
        long arrivalMs = (long)(TacticalBattleResults.InitialDelayMs + TacticalBattleResults.FlightMs);
        var liveLayout = new BattleLayout();
        var wreckLayout = new BattleLayout();
        using var flying = Record(frame with { Battle = results.Project(arrivalMs - 1), Salvo = results.Capture(arrivalMs - 1) }, liveLayout);
        var arrival = results.Capture(arrivalMs);
        Assert.True(arrival.InFlight);
        using var impact = Record(frame with { Battle = results.Project(arrivalMs), Salvo = arrival }, wreckLayout);
        using var movement = Raster(frame with { Presentation = TacticalBattlePresentation.Current });
        Assert.NotEmpty(MarkerPixels(flying, new SKPoint(490, 360), TacticalBattleMapRenderer.Enemy));
        Assert.NotEqual(flying.GetPixel(490, 360), impact.GetPixel(490, 360));
        Assert.Equal(impact.GetPixel(490, 360), movement.GetPixel(490, 360));
        string name = PilotCardModel.Name(enemy);
        Assert.Contains(liveLayout.Texts, text => text.Text == name && text.Color == BattleDesign.Text);
        Assert.Contains(wreckLayout.Texts, text => text.Text == name && text.Color == BattleDesign.Muted);
        var label = wreckLayout.Rects["label:" + enemy.Id];
        Assert.InRange(label.Height, 11, 13);
        Assert.False(label.IntersectsWith(new SKRect(483, 353, 497, 367)));

        static SKBitmap Record(TacticalBattleFrame current, BattleLayout layout)
        {
            BattleDesign.Recording = layout;
            try { return Raster(current); }
            finally { BattleDesign.Recording = null; }
        }

    }

    [Fact]
    public async Task TraversedTrailIsVisibleOverFiringSectorsAndThroughoutSalvoPresentation()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var own = battle.Units.First(u => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone);
        var drone = own with { Pose = new(200, 0, 180), Build = own.Build! with { Range = 500, ArcDegrees = 45 } };
        var state = battle with
        {
            Phase = TacticalBattlePhase.Fire,
            Units = [drone],
            BeforeSalvoUnits = [drone],
            Paths = [new(battle.Turn, drone.Id, [new(-220, 0, 180), drone.Pose])]
        };
        var plan = new TacticalFirePlan([new(drone, new(drone.Id, [], null), null)], drone.Id);
        var frame = new TacticalBattleFrame(state, new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "",
            EnemyForecast: false, FirePlan: plan);

        AssertTrailVisible(frame);
        var afterFire = state with { Phase = TacticalBattlePhase.Move };
        AssertTrailVisible(frame with
        {
            Battle = afterFire,
            Presentation = TacticalBattlePresentation.SalvoFlight,
            FirePlan = null,
            Salvo = new(true, false, [], [])
        });
        AssertTrailVisible(frame with
        {
            Battle = afterFire,
            Presentation = TacticalBattlePresentation.SalvoResults,
            FirePlan = null,
            Salvo = new(false, false, [], [])
        });

        static void AssertTrailVisible(TacticalBattleFrame frame)
        {
            using var withTrail = Raster(frame);
            using var withoutTrail = Raster(frame with { Battle = frame.Battle with { Paths = [] } });
            Assert.Contains(Enumerable.Range(221, 24), x =>
                withTrail.GetPixel(x, 360).Green > withoutTrail.GetPixel(x, 360).Green + 15);
            Assert.Contains(Enumerable.Range(221, 24), x => withTrail.GetPixel(x, 360) == withoutTrail.GetPixel(x, 360));
        }
    }











    [Fact]
    public async Task Reference_skin_changes_field_units_and_paths_on_the_same_snapshot()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var state = screen.Flow.State!;
        var own = state.Units.First(unit => unit.Side == state.PlayerSide && unit.Kind == TacticalUnitKind.Drone) with
        {
            Pose = new(-100, 20, 0),
            Speed = 0
        };
        var enemy = state.Units.First(unit => unit.Side != state.PlayerSide && unit.Kind == TacticalUnitKind.Drone) with
        {
            Pose = new(90, -60, 0),
            Speed = 100
        };
        var snapshot = state with
        {
            Units = [own, enemy],
            Paths = [new(state.Turn, own.Id, [new(-175, 20, 0), own.Pose])]
        };
        var frame = new TacticalBattleFrame(snapshot, new(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "", EnemySectors: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var skin = catalog.GetOrClassic("contrast");
        Assert.Equal("contrast", skin.Id);
        using var classic = Raster(frame);
        using var contrast = Raster(frame, skin);

        Assert.Equal(TacticalBattleMapRenderer.Own, classic.GetPixel(340, 382));
        Assert.Equal(new SKColor(117, 244, 209), contrast.GetPixel(340, 382));
        Assert.NotEqual(classic.GetPixel(25, 25), contrast.GetPixel(25, 25));
        Assert.NotEqual(classic.GetPixel(300, 380), contrast.GetPixel(300, 380));
        Assert.NotEqual(classic.GetPixel(630, 300), contrast.GetPixel(630, 300));
        Assert.Same(state, screen.Flow.State);
        Save(contrast, "contrast-field.png");
    }

    [Fact]
    public async Task DroneSilhouettesFollowFrameAndCourse()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var own = battle.Units.First(unit => unit.Kind == TacticalUnitKind.Drone && unit.Side == battle.PlayerSide);
        var enemy = battle.Units.First(unit => unit.Kind == TacticalUnitKind.Drone && unit.Side != battle.PlayerSide);
        var units = new[]
        {
            own with { Id = "light", FrameId = "light", Pose = new(-19, 0, 270) },
            own with { Id = "heavy", FrameId = "heavy", Pose = new(0, 0, 270) },
            enemy with { Id = "thug", FrameId = "thug", Pose = new(19, 0, 270) }
        };
        var view = new TacticalBattleView(880, 720, 0, 0, 1);
        var frame = new TacticalBattleFrame(battle with { Units = [.. units], Paths = [], Anomalies = [] },
            view, SKRect.Empty, 1, false, "", EnemyForecast: false);
        var rotated = frame with
        {
            Battle = frame.Battle with { Units = [.. units.Select(u => u with { Pose = u.Pose with { Course = 0 } })] }
        };
        var upLayout = new BattleLayout();
        var rightLayout = new BattleLayout();
        BattleDesign.Recording = upLayout;
        try
        {
            using var up = Raster(frame);
            BattleDesign.Recording = rightLayout;
            using var right = Raster(rotated);
            BattleDesign.Recording = null;
            var masks = new List<HashSet<(int X, int Y)>>();
            var upBounds = new List<SKRect>();
            var rightBounds = new List<SKRect>();
            foreach (var unit in units)
            {
                var center = view.Project(unit.Pose);
                Assert.Equal(center, TacticalBattleMapRenderer.HoverPoint(frame, unit));
                var color = unit.Side == battle.PlayerSide ? TacticalBattleMapRenderer.Own : TacticalBattleMapRenderer.Enemy;
                var upPixels = MarkerPixels(up, center, color);
                var rightPixels = MarkerPixels(right, center, color);
                Assert.InRange(upPixels.Count, 10, 50);
                Assert.InRange(upPixels.Count(p => rightPixels.Contains((-p.Y - 1, p.X))), upPixels.Count - 4, upPixels.Count);
                Assert.All(upPixels, pixel => Assert.InRange(pixel.X, -5, 4));
                Assert.Contains(upPixels, pixel => Math.Abs(pixel.X) <= 1 && Math.Abs(pixel.Y) <= 1);
                masks.Add(upPixels);
                upBounds.Add(PixelBounds(center, upPixels));
                rightBounds.Add(PixelBounds(center, rightPixels));
            }
            Assert.False(masks[0].SetEquals(masks[1]));
            Assert.False(masks[0].SetEquals(masks[2]));
            Assert.False(masks[1].SetEquals(masks[2]));
            AssertLabelsClear(upLayout, upBounds);
            AssertLabelsClear(rightLayout, rightBounds);
            using var selected = Raster(rotated with { SelectedDroneId = "heavy", HoverUnitId = "heavy" });
            Save(selected, "modular-drone-silhouettes.png");
        }
        finally
        {
            BattleDesign.Recording = null;
        }

        static SKRect PixelBounds(SKPoint center, HashSet<(int X, int Y)> pixels) =>
            new(center.X + pixels.Min(p => p.X), center.Y + pixels.Min(p => p.Y),
                center.X + pixels.Max(p => p.X) + 1, center.Y + pixels.Max(p => p.Y) + 1);

        void AssertLabelsClear(BattleLayout layout, List<SKRect> markerBounds)
        {
            var labels = units.Select(unit => layout.Rects["label:" + unit.Id]).ToArray();
            for (int i = 0; i < labels.Length; i++)
            {
                Assert.DoesNotContain(markerBounds, bounds => labels[i].IntersectsWith(bounds));
                Assert.DoesNotContain(labels.Take(i), label => labels[i].IntersectsWith(label));
            }
        }
    }

    [Fact]
    public async Task UnknownDroneFrameKeepsSideMarker()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var battle = (await Start(fixture)).Flow.State!;
        var own = battle.Units.First(unit => unit.Kind == TacticalUnitKind.Drone && unit.Side == battle.PlayerSide) with
        {
            FrameId = "unknown",
            Pose = new(-35, 0, 0)
        };
        var enemy = battle.Units.First(unit => unit.Kind == TacticalUnitKind.Drone && unit.Side != battle.PlayerSide) with
        {
            FrameId = "unknown",
            Pose = new(35, 0, 0)
        };
        var view = new TacticalBattleView(880, 720, 0, 0, 1);
        var frame = new TacticalBattleFrame(battle with { Units = [own, enemy], Paths = [], Anomalies = [] },
            view, SKRect.Empty, 1, false, "", EnemyForecast: false);
        using var raster = Raster(frame);
        var diamond = MarkerPixels(raster, view.Project(own.Pose), TacticalBattleMapRenderer.Own);
        Assert.Contains((0, -3), diamond);
        Assert.Contains((0, 2), diamond);
        Assert.DoesNotContain((5, 5), diamond);
        var triangle = MarkerPixels(raster, view.Project(enemy.Pose), TacticalBattleMapRenderer.Enemy);
        Assert.Contains(triangle, pixel => pixel.X > 0 && Math.Abs(pixel.Y) <= 1);
        Assert.Contains(triangle, pixel => pixel.X < 0 && pixel.Y >= 1);
        Assert.DoesNotContain((5, 4), triangle);
    }

    private static HashSet<(int X, int Y)> MarkerPixels(SKBitmap bitmap, SKPoint center, SKColor color)
    {
        var pixels = new HashSet<(int, int)>();
        for (int y = -9; y < 9; y++)
        {
            for (int x = -9; x < 9; x++)
            {
                var actual = bitmap.GetPixel((int)center.X + x, (int)center.Y + y);
                if (Math.Abs(actual.Red - color.Red) <= 45 && Math.Abs(actual.Green - color.Green) <= 45 &&
                    Math.Abs(actual.Blue - color.Blue) <= 45)
                    pixels.Add((x, y));
            }
        }
        return pixels;
    }

    [Fact]
    public void Torpedo_visual_roles_resolve_without_reclassifying_current_shots()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var skin = catalog.GetOrClassic("contrast");
        Assert.Equal("triangle", skin.Shape("torpedo.projectile.hit"));
        Assert.Equal("diamond", skin.Shape("shot.projectile.hit"));
        Assert.Equal(new[] { 7f, 3f }, skin.Dash("torpedo.trail").ToArray());
        Assert.Equal(new[] { 3f, 2f }, skin.Dash("shot.trail").ToArray());
    }

    internal static SKBitmap Raster(TacticalBattleFrame frame, TacticalBattleSkin? skin = null)
    {
        using var scope = TacticalBattleSkinRenderScope.Enter(skin ?? TacticalBattleSkin.Classic);
        var bitmap = new SKBitmap(frame.View.PixelWidth, frame.View.PixelHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(TacticalBattleMapRenderer.Background);
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
