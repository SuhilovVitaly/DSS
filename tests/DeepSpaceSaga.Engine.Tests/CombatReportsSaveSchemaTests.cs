using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatReportsSaveSchemaTests
{
    [Fact]
    public void LegacyReportStateIsValidatedAndNotReemitted()
    {
        var root = Fixture();
        root["gameState"]!["combatReportsState"] = JsonNode.Parse(
            """{"version":1,"sequence":17,"reportedRangeTargetIds":["no-longer-in-world"]}""");
        var world = ScenarioLoader.LoadFromJson(root.ToJsonString(), allowNonZeroGameTime: true, isSave: true);
        Assert.Equal(20, world.SaveFormatVersion);
        Assert.Null(world.GameState.CombatReportsState);
    }

    private static JsonNode Fixture() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(SaveFixturesTests.Fixtures, "Default-Save19-combat.json")))!;

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
        var root = Fixture();
        root["gameState"]!["combatReportsState"] = JsonNode.Parse(state);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(ScenarioLoader.LoadFromJson(root.ToJsonString(), true, isSave: true), true));
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
