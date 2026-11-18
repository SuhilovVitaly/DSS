using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0008Tk0004Tests
{
    [Fact]
    public void RealDemo_HasSeparateEngineerAndLegacyLoadDoesNotGrantOne()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"),
            Path.Combine(AppContext.BaseDirectory, "Scenarios/Demo/scenario.json"));
        var member = Assert.Single(engine.CaptureSnapshot().CarrierOperations!.Crew.Where(c => c.Role == CrewRole.Engineer));
        Assert.Equal("CHR-PLAYER-ENGINEER", member.Id);
        Assert.InRange(member.SalvageBonusPercentagePoints, 10, 15);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, member.PortraitAssetId!)));
        var save = engine.CaptureSaveState();
        var legacy = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with { Crew = s.Crew!.Where(c => c.Role != "Engineer").ToArray() }).ToArray()
            }
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(legacy), true, true), true);
        Assert.DoesNotContain(engine.CaptureSnapshot().CarrierOperations!.Crew, c => c.Role == CrewRole.Engineer);
    }

    [Theory]
    [InlineData("Russian", "Инженер", "п.п.")]
    [InlineData("English", "Engineer", "percentage points")]
    public void EngineerLocale_UsesPercentagePoints(string language, string role, string units)
    {
        using var locale = Localization.UseLocaleForTests(language);
        Assert.Equal(role, Localization.Get("Ship.Crew.Role.Engineer"));
        Assert.Contains(units, string.Format(Localization.Get("Ship.Crew.SalvageBonus"), 12));
    }
}
