using DeepSpaceSaga.Contracts;
using Fixture = DeepSpaceSaga.Client.Tests.BasicCombatUiFlowTests.Fixture;
namespace DeepSpaceSaga.Client.Tests;

public class CountermeasureUiFlowTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1.2f)]
    public async Task Defense_ui_dispatches_real_commands(int width, int height, float scale)
    {
        await using var f = await Fixture.Create(width, height, scale);
        async Task Toggle(string command, bool enabled)
        {
            var row = f.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Countermeasure Launcher");
            if (!row.Opened) { f.Screen.OnMouseDown(row.CaptionRect.MidX * scale, row.CaptionRect.MidY * scale); f.Render(); }
            var button = Assert.Single(f.Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == command);
            Assert.True(button.Enabled);
            f.Screen.OnMouseDown(button.Rect.MidX * scale, button.Rect.MidY * scale);
            await f.Wait(s => s.InstalledModules.Any(m => m.Defense?.AutoEnabled == enabled));
            f.Render();
        }
        await Toggle(DefenseCommandTypes.Disable, false);
        await Toggle(DefenseCommandTypes.Enable, true);
        var launcher = f.Screen.CommandsPanel.CommandPanelRows.Single(r => r.Name == "Torpedo Launcher");
        if (!launcher.Opened) { f.Screen.OnMouseDown(launcher.CaptionRect.MidX * scale, launcher.CaptionRect.MidY * scale); f.Render(); }
        f.Select(Fixture.Target);
        var fired = await f.Fire();
        string torpedo = Assert.Single(fired.Objects, o => o.Torpedo is not null).ObjectId;
        var destroy = Assert.Single(f.Screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == CombatCommandTypes.SelfDestruct);
        Assert.True(destroy.Enabled);
        f.Screen.OnMouseDown(destroy.Rect.MidX * scale, destroy.Rect.MidY * scale);
        var ended = await f.Wait(s => s.Objects.All(o => o.ObjectId != torpedo));
        Assert.Equal(SimulationSpeed.Speed0, ended.CurrentSpeed);
        Assert.Equal(fired.MotionTimeMs, ended.MotionTimeMs);
        Assert.Equal(fired.Objects.Single(o => o.ObjectId == Fixture.Target).HullCombat, ended.Objects.Single(o => o.ObjectId == Fixture.Target).HullCombat);
        Assert.Contains(ended.CombatJournal, e => e.Type == CombatEventType.SelfDestruct && e.ProjectileObjectId == torpedo);
    }
}
