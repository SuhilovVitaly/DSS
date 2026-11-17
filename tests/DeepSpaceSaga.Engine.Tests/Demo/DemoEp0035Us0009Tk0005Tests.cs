using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0009Tk0005Tests
{
    [Theory]
    [InlineData(10, true, 2640)]
    [InlineData(0, true, 2400)]
    [InlineData(10, false, 1920)]
    public void SkillAndAssignment_AppliedOnceAndSaved(int bonus, bool assigned, double expected)
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = DemoEp0035Us0009Tk0002Tests.Assign(engine.CaptureSaveState(), assigned ? "CHR-PLAYER-DEFENSE" : null);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
                {
                    Crew = s.Crew!.Select(c => c.CrewId == "CHR-PLAYER-DEFENSE" ? c with { SensorRangeBonusPercent = bonus } : c).ToArray()
                }).ToArray()
            }
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        for (int i = 0; i < 3; i++)
        {
            var snapshot = engine.CaptureSnapshot();
            Assert.Equal(expected, snapshot.DemoCampaign!.Sensor!.RadiusWorldUnits);
            Assert.Equal(expected, snapshot.DemoCampaign.SensorOperator!.EffectiveRadiusWorldUnits);
            Assert.Equal(2400, snapshot.DemoCampaign.SensorOperator.BaseRadiusWorldUnits);
            Assert.Equal(expected - 2400, snapshot.DemoCampaign.SensorOperator.OperatorContributionWorldUnits);
            Assert.Equal(bonus, snapshot.CarrierOperations!.Crew.Single(c => c.Id == "CHR-PLAYER-DEFENSE").SensorRangeBonusPercent);
            engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true), true);
        }
    }

    [Fact]
    public void LegacyCrew_DefaultsToZero()
    {
        var crew = System.Text.Json.JsonSerializer.Deserialize<ShipCrewMemberData>("{\"crewId\":\"legacy\",\"displayName\":\"Legacy\"}");
        Assert.Equal(0, crew!.SensorRangeBonusPercent);
    }
}
