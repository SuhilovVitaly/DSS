using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportsSaveSchemaTests
{
    [Fact]
    public void NewReportStateRoundTrips()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState();
        save = save with { GameState = save.GameState with { CombatReportsState = new(1, long.MaxValue, [Target]) } };
        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true);
        Assert.Equal(19, loaded.SaveFormatVersion);
        Assert.Equal(long.MaxValue, loaded.GameState.CombatReportsState!.Sequence);
        Assert.Equal(new[] { Target }, loaded.GameState.CombatReportsState.ReportedRangeTargetIds);
    }

    [Fact]
    public void PreFeatureSave17HasExplicitBaseline()
    {
        using var engine = Create();
        var root = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveState()))!;
        root["gameState"]!.AsObject().Remove("combatReportsState");
        Assert.Null(ScenarioLoader.LoadFromJson(root.ToJsonString(), true).GameState.CombatReportsState);
    }

    [Theory]
    [InlineData("{\"version\":2,\"sequence\":0,\"reportedRangeTargetIds\":[]}")]
    [InlineData("{\"version\":1,\"sequence\":-1,\"reportedRangeTargetIds\":[]}")]
    [InlineData("{\"version\":1,\"sequence\":0,\"reportedRangeTargetIds\":null}")]
    [InlineData("{\"version\":1,\"sequence\":0,\"reportedRangeTargetIds\":[\"x\",\"X\"]}")]
    [InlineData("{\"version\":1,\"sequence\":0,\"reportedRangeTargetIds\":[\" x\"]}")]
    [InlineData("{\"version\":1,\"sequence\":0,\"reportedRangeTargetIds\":[null]}")]
    [InlineData("{\"version\":1,\"reportedRangeTargetIds\":[]}")]
    [InlineData("{\"sequence\":0,\"reportedRangeTargetIds\":[]}")]
    [InlineData("{\"version\":1,\"sequence\":0}")]
    public void MalformedReportStateIsRejectedAtomically(string state)
    {
        using var engine = Create();
        var before = engine.CaptureSaveState();
        var root = JsonNode.Parse(ScenarioLoader.Serialize(before))!;
        root["gameState"]!["combatReportsState"] = JsonNode.Parse(state);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(ScenarioLoader.LoadFromJson(root.ToJsonString(), true), true));
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void DestroyedTargetIdsRemainValid()
    {
        CombatReportsSaveValidation.Validate(new(1, 17, ["no-longer-in-world"]));
    }

    [Fact]
    public void Save16StillRejected()
    {
        using var engine = Create();
        var save = engine.CaptureSaveState() with { SaveFormatVersion = 16 };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save, true));
    }
}
