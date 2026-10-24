using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ModulePanelHealthTests
{
    [Fact]
    public async Task Native_regressions_keep_all_captions_reachable_and_panel_symbols_supported()
    {
        await using var fixture = await PointCombatUiFixture.Create(1280, 720, 1.5f);
        var panel = fixture.Screen.CommandsPanel;
        Assert.All(panel.CommandPanelRows, row => Assert.True(row.CaptionRect.Bottom <= 480, row.Name));
        var paint = (SKPaint)typeof(CommandsPanel).GetField("_detailsPaint",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(panel)!;
        string glyphs = string.Concat(new[] { "BarLeft", "BarFull", "BarEmpty", "BarRight", "Selected", "CriticalIcon" }
            .Select(key => Localization.Combat("Panel." + key)));
        foreach (char symbol in glyphs + "Точность Броня")
            Assert.True(paint.GetGlyphs(symbol.ToString()).All(g => g != 0), $"Missing U+{(int)symbol:X4} in {paint.Typeface.FamilyName}");
    }

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
