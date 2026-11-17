using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0008Tk0002Tests
{
    private static ShipCrewMemberData Engineer(int? bonus = null, string id = "engineer") =>
        new(id, "Alex Morgan", Gender: "Male", Role: "Engineer", SalvageBonusPercentagePoints: bonus);

    internal static ScenarioFile WithEngineer(ScenarioFile save, params ShipCrewMemberData[] engineers) =>
        save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
            { Crew = (s.Crew ?? []).Where(c => !string.Equals(c.Role, "Engineer", StringComparison.OrdinalIgnoreCase)).Concat(engineers).ToArray() }).ToArray()
            }
        };

    [Fact]
    public void NewEngineer_GeneratesWithinTenToFifteen()
    {
        var values = Enumerable.Range(0, 32).Select(seed => SimulationEngine.ResolveEngineerBonus((ulong)seed, "ship", Engineer())).ToArray();
        Assert.All(values, value => Assert.InRange(value, 10, 15));
        Assert.True(values.Distinct().Count() > 1);
        Assert.Equal(values[3], SimulationEngine.ResolveEngineerBonus(3, "ship", Engineer()));
    }

    [Fact]
    public void SaveLoad_PreservesBonusAndIdentity()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(WithEngineer(engine.CaptureSaveState(), Engineer()), true);
        var saved = engine.CaptureSaveState();
        var first = saved.GameState.SpaceObjects.Single(s => s.ObjectId == saved.GameState.PlayerShipObjectId).Crew!.Single(c => c.Role == "Engineer");
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true, true), true);
        Assert.Equal(first.SalvageBonusPercentagePoints!.Value / 100.0, engine.GetAssignedEngineerBonusFraction());
        var second = engine.CaptureSaveState().GameState.SpaceObjects.Single(s => s.ObjectId == saved.GameState.PlayerShipObjectId).Crew!.Single(c => c.Role == "Engineer");
        Assert.Equal(first, second);
    }

    [Fact]
    public void MissingEngineer_ReturnsZero()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(WithEngineer(engine.CaptureSaveState()), true);
        Assert.Equal(0, engine.GetAssignedEngineerBonusFraction());
    }

    [Fact]
    public void DestroyedCarrier_ReturnsZero()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = WithEngineer(engine.CaptureSaveState(), Engineer(12));
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
            {
                IsDestroyed = true,
                CarrierState = s.CarrierState! with { HullCurrent = 0 }
            }).ToArray()
            }
        };
        engine.LoadScenario(save, true);
        Assert.Equal(0, engine.GetAssignedEngineerBonusFraction());
    }

    [Fact]
    public void InvalidOrMultipleEngineers_Rejected()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        foreach (var entries in new[] { new[] { Engineer(9) }, new[] { Engineer(16) },
            new[] { Engineer(12), Engineer(13, "second") }, new[] { Engineer(12) with { Role = "Pilot" } } })
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(WithEngineer(save, entries), true));
    }
}
