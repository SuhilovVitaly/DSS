using System.Collections.Immutable;
using Silk.NET.Input;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardFireBlockTests
{
    [Theory]
    [InlineData("2-fire", 153)]
    [InlineData("2-no-target", 106)]
    [InlineData("2-evasion-plan", 106)]
    [InlineData("2-anomaly-dust", 106)]
    public async Task Slim_fire_panel_sizes_content_and_preserves_command_eligibility(string sceneName, float height)
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create(sceneName);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1920, 1080); using var c = new SKCanvas(bitmap);
        try
        {
            for (int i = 0; i < 16 && screen.CaptureFrame(1920, 1080)!.SelectedDroneId != scene.Frame.SelectedDroneId; i++)
                screen.OnKeyDown(Key.Tab);
            Assert.Equal(scene.Frame.SelectedDroneId, screen.CaptureFrame(1920, 1080)!.SelectedDroneId);
            if (sceneName == "2-evasion-plan") screen.OnKeyDown(Key.E);
            screen.Render(c, 1920, 1080);
            var row = screen.CaptureFrame(1920, 1080)!.FirePlan!.Selected!;
            var panel = screen.Layout.Rects["pilot-panel:" + row.Drone.Id];
            Assert.Equal(height, panel.Height);
            foreach (var rect in screen.Layout.Rects.Where(kv => kv.Key.StartsWith("card-command:", StringComparison.Ordinal)))
                Assert.True(panel.Contains(rect.Value), rect.Key);
            Assert.Equal(row.CanEvade, !screen.CardCommandBounds("evade").IsEmpty);
            Assert.Equal(row.CanDecoy, !screen.CardCommandBounds("decoy").IsEmpty);
            if (sceneName == "2-fire")
            {
                Assert.False(screen.CardCommandBounds("target:Таг-5").IsEmpty);
                Assert.False(screen.CardCommandBounds("target:Таг-6").IsEmpty);
                var target = screen.CardCommandBounds("target:Таг-5");
                screen.OnMouseDown(target.MidX, target.MidY, MouseButton.Left);
                Assert.Contains(screen.BuildFireRequest().Targets, t => t.DroneId == row.Drone.Id && t.TargetId == "Таг-5");
            }
            else Assert.DoesNotContain(screen.Layout.Rects.Keys, k => k.StartsWith("card-command:target:", StringComparison.Ordinal));
            if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } folder)
            {
                using var crop = new SKBitmap(630, 1440); using var cc = new SKCanvas(crop); cc.Scale(2);
                screen.Render(cc, 1920, 1080);
                using var png = crop.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(Path.Combine(folder, "fire-panel-" + sceneName + "-column-x2.png")); png.SaveTo(file);
            }
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public async Task Fire_commands_and_defense_are_in_the_expanded_drone_card()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        using var image = scene.Render();
        var screen = scene.Screen;
        Assert.Equal(new SKRect(6, 93, 196, 317), screen.Layout.Rects["pilot:Юн"]);
        Assert.Equal(new SKRect(18, 292.5f, 285, 319.5f), screen.CardCommandBounds("target:Таг-5"));
        Assert.Equal(new SKRect(18, 322.5f, 285, 349.5f), screen.CardCommandBounds("target:Таг-6"));
        Assert.Equal(new SKRect(18, 391.5f, 285, 418.5f), screen.CardCommandBounds("decoy"));
        Assert.Equal(new SKRect(154.5f, 424.5f, 285, 450), screen.CardCommandBounds("evade"));
        Assert.Equal(5, screen.Layout.Rects.Keys.Count(key => key.StartsWith("card-command:", StringComparison.Ordinal)));
        Assert.Contains(screen.Layout.Texts, item => item.Text == "Таг-6: броня 20/20, затем корпус 40/40");
    }

    [Theory]
    [InlineData("2-evasion-plan", "EvadeNoFire", false)]
    [InlineData("2-no-target", "NoTargetsInArc", true)]
    [InlineData("2-anomaly-dust", "DroneInDust", true)]
    [InlineData("2-decoy-empty", "DecoyEmpty", false)]
    public async Task Fire_state_exposes_eligible_commands(string name, string text, bool canDecoy)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var image = scene.Render();
        Assert.Contains(scene.Screen.Layout.Texts, item => item.Text.Contains(BattleDesign.M(text), StringComparison.Ordinal));
        Assert.Equal(canDecoy, !scene.Screen.CardCommandBounds("decoy").IsEmpty);
    }

    [Fact]
    public async Task Absorption_is_displayed_in_the_target_row()
    {
        await using var scene = await Ep0019Scenes.Create("2-anomaly-absorption");
        using var image = scene.Render();
        Assert.Contains(scene.Screen.Layout.Texts, item => item.Text == BattleDesign.M("Card.Absorption", 36) && item.Color == BattleDesign.Heat);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("tabletop")]
    public async Task Target_wheel_scrolls_visible_commands_and_resets_with_the_group(string skinId)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        await using var scene = await Ep0019Scenes.Create("2-fire");
        var frame = scene.Frame;
        var row = frame.FirePlan!.Selected!;
        var first = row.Group.List[0];
        var candidates = Enumerable.Range(0, 5)
            .Select(i => first with { TargetId = "target" + i }).ToImmutableArray();
        row = row with { Group = row.Group with { List = candidates } };
        frame = frame with { FirePlan = new([row], row.Drone.Id) };
        var column = new PilotCardColumn();
        using var bitmap = new SKBitmap(196, 720);
        using var canvas = new SKCanvas(bitmap);
        Draw();
        var visible = column.CommandBounds("target:target0");
        Assert.False(visible.IsEmpty);
        Assert.True(column.CommandBounds("target:target3").IsEmpty);
        Assert.False(column.ScrollTargets("other", new(visible.MidX, visible.MidY), -1));
        Assert.True(column.ScrollTargets(row.Drone.Id, new(visible.MidX, visible.MidY), -1));
        Draw();
        Assert.True(column.CommandBounds("target:target0").IsEmpty);
        Assert.Equal(visible, column.CommandBounds("target:target1"));
        Assert.False(column.CommandBounds("target:target3").IsEmpty);

        row = row with { Group = row.Group with { List = candidates.Reverse().ToImmutableArray() } };
        frame = frame with { FirePlan = new([row], row.Drone.Id) };
        Draw();
        Assert.Equal(visible, column.CommandBounds("target:target4"));

        void Draw()
        {
            using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic(skinId));
            column.Arrange(frame, PilotCardModel.Project(frame));
            column.Draw(canvas, PilotPortraitCatalog.Default);
        }
    }

    [Fact]
    public async Task Card_and_map_or_key_actions_produce_identical_fire_requests()
    {
        await using var card = await Ep0019Scenes.Create("2-fire");
        await using var keys = await Ep0019Scenes.Create("2-fire");
        using var cardImage = card.Render();
        using var keyImage = keys.Render();
        var row = card.Frame.FirePlan!.Selected!;
        var alternative = row.Group.List.First(target => target.TargetId != row.TargetId);
        var target = keys.Frame.Battle.Units.Single(unit => unit.Id == alternative.TargetId);
        var point = keys.Frame.View.Project(target.Pose);
        Click("target:" + alternative.TargetId);
        keys.Screen.OnMouseDown(point.X * 1.5f, point.Y * 1.5f, MouseButton.Left);
        AssertPlansEqual();
        Click("decoy");
        keys.Screen.OnKeyDown(Key.D);
        AssertPlansEqual();
        Click("evade");
        keys.Screen.OnKeyDown(Key.E);
        AssertPlansEqual();

        void AssertPlansEqual() => Assert.Equal(keys.Screen.BuildFireRequest().Targets.ToArray(),
            card.Screen.BuildFireRequest().Targets.ToArray());
        void Click(string action)
        {
            var r = card.Screen.CardCommandBounds(action);
            Assert.False(r.IsEmpty);
            card.Screen.OnMouseDown(r.MidX, r.MidY, MouseButton.Left);
        }
    }
}
