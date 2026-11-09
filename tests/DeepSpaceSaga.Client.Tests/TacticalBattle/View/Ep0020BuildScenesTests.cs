using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("InterfaceLog")]
public sealed class Ep0020BuildScenesTests
{
    public static TheoryData<string> Scenes => new() { "2-build-selected", "2-build-hover", "2-build-damaged" };
    [Theory, MemberData(nameof(Scenes))]
    public async Task Build_scenes_use_real_selection_and_explicit_manual_damage(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name); using var locale = Localization.UseLocaleForTests("Russian");
        using var bitmap = scene.Render(); var f = scene.Frame; var model = BuildCardModel.From(f)!;
        Assert.Equal(4, f.Battle.Turn); Assert.Equal("Марта", f.SelectedDroneId);
        Assert.Equal(name == "2-build-hover" ? "Вера" : "Марта", model.Unit.Id);
        Assert.Equal(name == "2-build-hover", model.Hovered);
        if (name == "2-build-damaged")
        {
            Assert.Equal("заклинило", model.Cells.Single(c => c.Kind == TacticalSlotKind.Weapon).Detail);
            Assert.Equal("повреждён ×0,5", model.Cells.Single(c => c.Kind == TacticalSlotKind.Engine && c.Index == 0).Detail);
            // This scene explicitly changes captured presentation data, not combat RNG/state.
            Assert.False(scene.Screen.Flow.State!.Units.Single(u => u.Id == "Марта").Jammed);
        }
    }
}
