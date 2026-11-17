using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0008Tk0005Tests
{
    [Theory]
    [InlineData("Russian", "Инженер", "п.п.")]
    [InlineData("English", "Engineer", "percentage points")]
    public void RealShipScreen_ShowsPersistedEngineerBonus(string language, string role, string units)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"),
            Path.Combine(AppContext.BaseDirectory, "Scenarios/Demo/scenario.json"));
        var snapshot = engine.CaptureSnapshot();
        var member = Assert.Single(snapshot.CarrierOperations!.Crew.Where(c => c.Role == CrewRole.Engineer));
        var screen = new ShipScreen(ShipViewModel.From(snapshot.CarrierOperations));
        string label = Assert.Single(screen.CrewLabels.Where(l => l.Contains(role, StringComparison.Ordinal)));
        Assert.Contains(member.DisplayName, label);
        Assert.Contains(member.SalvageBonusPercentagePoints.ToString(), label);
        Assert.Contains(units, label);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true), true);
        Assert.Equal(screen.CrewLabels, new ShipScreen(ShipViewModel.From(engine.CaptureSnapshot().CarrierOperations)).CrewLabels);
        Assert.Null(ShipCrewPanel.EngineerLabel(new("pilot", "Pilot", CrewRole.Pilot)));
    }
}
