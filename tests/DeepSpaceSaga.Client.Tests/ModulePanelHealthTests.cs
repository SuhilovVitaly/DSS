using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelHealthTests
{
    [Fact]
    public async Task Render_uses_authoritative_limit_and_explains_health_and_criticality()
    {
        await using var f = await PointCombatUiFixture.Create();
        var panel = f.Screen.CommandsPanel;
        var nav = Assert.Single(panel.CommandPanelRows, r => r.Name == "Navigation");
        panel.OnMouseDown(nav.CaptionRect.MidX, nav.CaptionRect.MidY);
        f.Render();
        nav = Assert.Single(panel.CommandPanelRows, r => r.Name == "Navigation");
        Assert.True(nav.Opened);
        Assert.Contains(nav.DetailLines, l => l.Contains(" / 3"));
        panel.OnMouseMove(nav.BodyRect.MidX, nav.BodyRect.Top + 20);
        f.Render();
        Assert.Contains("/", panel.CurrentHealthTooltip);
        Assert.Contains(Localization.Combat("Panel.CriticalTooltip"), panel.CurrentHealthTooltip);
    }

    [Fact]
    public async Task Warm_actual_panel_render_allocates_nothing_and_invalidates_on_data_change()
    {
        await using var f = await PointCombatUiFixture.Create();
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        IReadOnlyList<InstalledModuleSnapshot> modules = f.Presented.InstalledModules;
        var panel = f.Screen.CommandsPanel;
        for (int i = 0; i < 30; i++) panel.Render(canvas, modules, 1080, PointCombatUiFixture.Player);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) panel.Render(canvas, modules, 1080, PointCombatUiFixture.Player);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        panel.DestroyedModuleLimit = 7;
        panel.Render(canvas, modules, 1080, PointCombatUiFixture.Player);
        Assert.Contains(panel.CommandPanelRows.SelectMany(r => r.DetailLines), l => l.Contains(" / 7"));
        panel.ReleasePresentationCache();
    }
}
