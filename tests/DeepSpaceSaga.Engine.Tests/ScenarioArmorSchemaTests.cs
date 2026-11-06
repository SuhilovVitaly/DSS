using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ScenarioArmorSchemaTests
{
    [Fact]
    public void ArchivedArmorIsValidatedAndDroppedWhileStructureSurvives()
    {
        var archive = JsonNode.Parse(File.ReadAllText(Path.Combine(SaveFixturesTests.Fixtures, "Default-Save19-combat.json")))!;
        var player = archive["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == "SPC-0001")!;
        var engine = player["modules"]!.AsArray().Single(m => (string?)m!["moduleId"] == "MOD-PLAYER-ENGINE-01")!;
        engine["armorClass"] = "light";
        engine["armor"] = 17;
        engine["structurePoints"] = 123;
        engine["disabledUntilMotionTimeMs"] = 1200;
        var upgraded = Load(archive);
        Assert.Equal(123, upgraded.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001")
            .Modules!.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01").StructurePoints);
        string saved = ScenarioLoader.Serialize(upgraded);
        foreach (string field in new[] { "armor", "armorClass", "hullHitPoints", "hullHitPointsMax",
            "ai", "autoDefenseEnabled", "disabledUntilMotionTimeMs" })
            Assert.DoesNotContain($"\"{field}\"", saved);
        var current = JsonNode.Parse(saved)!;
        current["gameState"]!["spaceObjects"]![0]!["hullHitPoints"] = 1;
        Assert.Throws<ScenarioException>(() => Load(current));
        engine["armor"] = -1;
        Assert.Contains("invalid saved armor", Assert.Throws<ScenarioException>(() => Load(archive)).Message);
    }

    private static ScenarioFile Load(JsonNode node) => ScenarioLoader.LoadFromJson(node.ToJsonString(),
        allowNonZeroGameTime: true, isSave: true);
}
