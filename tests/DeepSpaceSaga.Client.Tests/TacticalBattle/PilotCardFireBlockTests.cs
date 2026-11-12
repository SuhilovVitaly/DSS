using System.Collections.Immutable;
using Silk.NET.Input;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardFireBlockTests
{
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

    [Fact]
    public async Task Target_wheel_scrolls_visible_commands_and_resets_with_the_group()
    {
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
