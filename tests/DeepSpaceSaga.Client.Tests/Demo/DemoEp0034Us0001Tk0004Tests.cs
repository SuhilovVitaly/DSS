using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0001-TK-0004: the Ship screen shows the authoritative passenger capacity of a real demo New
/// Game (crew, reserve, positive free cabins), explains every blocker in RU/EN and only reads state.
/// </summary>
public sealed class DemoEp0034Us0001Tk0004Tests
{
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Demo", "scenario.json");

    private static void Render(ShipScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.OnActivated();
        screen.Render(canvas, 1920, 1080);
    }

    [Theory]
    [InlineData("English", "Cabins 12: crew 7, passengers 0, prisoners 0, reserve 0, free 5", "Free cabins: 5.")]
    [InlineData("Russian", "Каюты 12: экипаж 7, пассажиры 0, пленные 0, резерв 0, свободно 5", "Свободных кают: 5.")]
    public void ConfirmedSnapshot_RendersOutcome(string language, string summary, string ready)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var snapshot = engine.CaptureSnapshot();
        var screen = new ShipScreen(ShipViewModel.From(snapshot.CarrierOperations), 0, null,
            campaign: () => engine.CaptureSnapshot().DemoCampaign);

        Render(screen);
        var lines = screen.PassengerLines;
        Assert.Equal(3, lines.Count);
        Assert.Equal(Localization.Get("Demo.Passengers.Title"), lines[0]);
        Assert.Equal(summary, lines[1]);
        Assert.StartsWith(ready, lines[2]);
        Assert.Equal(7, snapshot.CarrierOperations!.Crew.Length);
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void RejectedCommand_ShowsReason(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (var code in DemoEp0034Us0001Tk0005Tests.ReasonCodes)
        {
            var capacity = code == PassengerCapacityReasons.NoFreeCabins
                ? new PassengerCapacitySnapshot(12, 6, 4, 2, 0)
                : code == DemoCampaignReasons.Unavailable ? null
                : new PassengerCapacitySnapshot(12, 6, 0, 2, 4, code);
            var state = new DemoCampaignSnapshot(1, 1, Transport: new DemoTransportSnapshot { PassengerCapacity = capacity });
            var screen = new ShipScreen(ShipViewModel.From(null), 0, null, campaign: () => state);
            Render(screen);
            string expected = capacity is null ? Localization.Get("Demo.Passengers.Empty") : Localization.Get("Demo.Passengers." + code);
            Assert.Equal(expected, screen.PassengerLines[^1]);
            Assert.DoesNotContain(code, string.Join("\n", screen.PassengerLines), StringComparison.Ordinal);
        }

        // An unknown future code is explained generically, never shown raw.
        Assert.Equal(Localization.Get("Demo.Passengers.Blocked"), PassengerCapacityPresentation.Reason("future_code"));

        // Long explanations wrap inside the crew panel width.
        var (left, _, right, _) = ShipLayout.CrewPanelLocalRect();
        foreach (var row in ShipScreen.WrapToWidth(Localization.Get("Demo.Passengers.cabins_overcommitted"), right - left, MenuStyle.TextStatus))
            Assert.True(MenuStyle.TextStatus.MeasureText(row) <= right - left, row);
    }

    [Fact]
    public void Reopen_DoesNotResubmit()
    {
        using var locale = Localization.UseLocaleForTests("English");
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var campaign = engine.CaptureSnapshot().DemoCampaign!;

        IReadOnlyList<string>? first = null;
        for (int open = 0; open < 2; open++)
        {
            var screen = new ShipScreen(ShipViewModel.From(engine.CaptureSnapshot().CarrierOperations), 0, null,
                campaign: () => engine.CaptureSnapshot().DemoCampaign);
            Render(screen);
            screen.OnMouseDown(960, 540, MouseButton.Left);
            Render(screen);
            first ??= screen.PassengerLines;
            Assert.Equal(first, screen.PassengerLines);
            Assert.Equal(ScreenEvent.CloseShip, screen.OnKeyDown(Key.Escape));
            screen.OnDeactivated();
        }

        // Viewing the capacity sends nothing: no campaign revision, receipt or world change.
        var after = engine.CaptureSnapshot().DemoCampaign!;
        Assert.Equal((campaign.Revision, campaign.CommitSequence), (after.Revision, after.CommitSequence));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }
}
