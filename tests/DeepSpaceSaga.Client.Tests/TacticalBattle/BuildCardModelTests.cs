using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BuildCardModelTests
{
    [Fact]
    public async Task Shows_hovered_own_then_selected()
    {
        await using var s = await Ep0019Scenes.Create("2-fire");
        var f = s.Screen.CaptureFrame(1920, 1080)!;
        Assert.Equal("Вера", BuildCardModel.From(f.Battle, "Вера", "Марта")!.Unit.Id);
        Assert.Equal("Марта", BuildCardModel.From(f.Battle, null, "Марта")!.Unit.Id);
        Assert.True(BuildCardModel.From(f.Battle, "Вера", "Марта")!.Hovered);
        Assert.Null(BuildCardModel.From(f.Battle with { Phase = TacticalBattlePhase.Over }, "Вера", "Марта"));
    }
    [Fact]
    public async Task Enemy_hover_keeps_old_card()
    {
        await using var s = await Ep0019Scenes.Create("2-fire"); var b = s.Screen.Flow.State!;
        foreach (var u in b.Units.Where(u => u.Side != b.PlayerSide || u.Kind == TacticalUnitKind.Carrier))
            Assert.Null(BuildCardModel.From(b, u.Id, "Марта"));
    }
    [Fact]
    public async Task Cells_and_texts_follow_slots()
    {
        await using var s = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian"); var b = s.Screen.Flow.State!;
        var model = BuildCardModel.From(b, null, "Марта")!;
        Assert.Equal("ПИЛОТ И СБОРКА", model.Heading);
        Assert.Contains("50–75", model.Cells.Single(c => c.Kind == TacticalSlotKind.Weapon).Detail);
        Assert.True(model.Cells.Single(c => c.Kind == TacticalSlotKind.Shield && c.Index == 1).NoSlot);
        Assert.Contains("заряды 2/2", model.Cells.Single(c => c.Kind == TacticalSlotKind.Module).Detail);
        var u = model.Unit with { Slots = [] };
        var empty = BuildCardModel.From(b with { Units = b.Units.Replace(model.Unit, u) }, null, u.Id)!;
        Assert.All(empty.Cells, c => Assert.True(c.NoSlot)); // Build cannot invent installed modules.
        Assert.Equal("32,5", BuildCardModel.Number(32.5)); Assert.Equal("0,5", BuildCardModel.Number(.5, 2));
        Assert.Contains("поворот " + BuildCardModel.Number(model.Unit.Limits!.Turn) + "°", model.Motion);
    }
    [Fact]
    public async Task Damaged_slots_are_alerts()
    {
        await using var s = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests("Russian"); var b = s.Screen.Flow.State!; var u = b.Units.Single(u => u.Id == "Марта");
        var changed = u with
        {
            Slots = u.Slots.Select(slot => slot.Kind == TacticalSlotKind.Weapon ? slot with { State = TacticalSlotState.Jammed }
            : slot.Kind == TacticalSlotKind.Engine && slot.Index == 0 ? slot with { State = TacticalSlotState.Damaged, Factor = .5 } : slot).ToImmutableArray()
        };
        var model = BuildCardModel.From(b with { Units = b.Units.Replace(u, changed) }, null, u.Id)!;
        Assert.Equal(new[] { "заклинило", "повреждён ×0,5" }, model.Cells.Where(c => c.Alert).Select(c => c.Detail));
    }
    [Fact]
    public void Names_are_ellipsized_to_cell_width()
    {
        var text = BuildCardModel.Ellipsize("Очень длинное название двигателя номер двести", 52);
        Assert.EndsWith("…", text); Assert.True(BattleDesign.Measure(text, 9.5f, true) <= 52);
    }
    [Fact]
    public async Task Flight_enemy_falls_back_and_results_select_own_shooter()
    {
        await using var s = await Ep0019Scenes.Create("3-salvo-flight");
        var f = s.Screen.CaptureFrame(1920, 1080)! with { HoverUnitId = null, HoverPoint = null, LastSelectedDroneId = "Марта" };
        var enemy = f.Battle.Units.First(u => u.Side != f.Battle.PlayerSide && u.Kind == TacticalUnitKind.Drone);
        var shot = f.Salvo!.Flights[0].Shot with { ShooterId = enemy.Id };
        var enemyFlight = f with { Salvo = f.Salvo with { Flights = [new(shot, 0, 0, 100)] } };
        Assert.Equal("Марта", BuildCardModel.From(enemyFlight)!.Unit.Id);
        var ownShot = shot with { ShooterId = "Олег" };
        Assert.Equal("Олег", BuildCardModel.From(f with { Salvo = f.Salvo with { InFlight = false, Flights = [new(ownShot, 0, 0, 2000)] } })!.Unit.Id);
        Assert.Equal("Марта", f.LastSelectedDroneId);
    }
    [Theory]
    [InlineData("Russian", "32,5", "ПИЛОТ И СБОРКА")]
    [InlineData("English", "32.5", "PILOT AND BUILD")]
    public async Task Locale_is_complete(string language, string number, string heading)
    {
        await using var s = await Ep0019Scenes.Create("2-fire"); using var locale = Localization.UseLocaleForTests(language);
        var model = BuildCardModel.From(s.Screen.Flow.State!, null, "Вера")!;
        Assert.Equal(heading, model.Heading); Assert.Equal(number, BuildCardModel.Number(32.5));
        Assert.DoesNotContain("TacticalBattle.", string.Join(" ", model.Cells.Select(c => c.Detail + c.Name)) + model.Frame + model.Mass + model.Motion);
    }
}
