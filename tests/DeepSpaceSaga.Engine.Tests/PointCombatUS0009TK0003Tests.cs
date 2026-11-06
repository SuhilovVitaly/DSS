using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class LegacyCombatReaderTests
{
    [Theory]
    [InlineData("missing-envelope")]
    [InlineData("duplicate-flight")]
    [InlineData("position")]
    [InlineData("deadline")]
    [InlineData("profile")]
    [InlineData("radius")]
    [InlineData("missing-armor-piercing")]
    [InlineData("missing-core")]
    [InlineData("missing-launch-time")]
    [InlineData("missing-rng")]
    [InlineData("rng-version")]
    [InlineData("rng-counter")]
    [InlineData("flight-sequence")]
    [InlineData("journal-sequence")]
    [InlineData("encounter-timeline")]
    [InlineData("encounter-player")]
    [InlineData("negative-statistic")]
    [InlineData("unknown-dodge-flight")]
    public void Malformed_archived_combat_is_refused_before_world_replacement(string defect)
    {
        string path = Path.Combine(SaveFixturesTests.Fixtures, "Default-Save19-combat.json");
        string original = File.ReadAllText(path);
        var root = JsonNode.Parse(original)!;
        var state = root["gameState"]!;
        var point = state["pointCombatState"]!;
        var flights = point["Projectiles"]!.AsArray();
        var flight = flights[0]!["Flight"]!.AsObject();
        switch (defect)
        {
            case "missing-envelope": state.AsObject().Remove("pointCombatState"); break;
            case "duplicate-flight": flights.Add(flights[0]!.DeepClone()); break;
            case "position": flight["X"] = flight["X"]!.GetValue<double>() + 1; break;
            case "deadline": flight["DetonationMotionTimeMs"] = 99; break;
            case "profile": flight["ProfileId"] = ""; break;
            case "radius": flight["BlastRadiusKm"] = 0; break;
            case "missing-armor-piercing": flight.Remove("ArmorPiercingPercent"); break;
            case "missing-core": flight.Remove("CoreFraction"); break;
            case "missing-launch-time": flight.Remove("LaunchMotionTimeMs"); break;
            case "missing-rng": point.AsObject().Remove("NpcDodge"); break;
            case "rng-version": point["LaunchScatter"]!["AlgorithmVersion"] = 2; break;
            case "rng-counter": point["LaunchScatter"]!["Counter"] = 1; break;
            case "flight-sequence": state["defenseState"]!["ProjectileSequence"] = 0; break;
            case "journal-sequence": state["defenseState"]!["JournalSequence"] = 2; break;
            case "encounter-timeline": point["Encounter"]!["State"] = 2; break;
            case "encounter-player": point["Encounter"]!["EnemyIds"] = new JsonArray("SPC-0001"); break;
            case "negative-statistic": point["Encounter"]!["Stats"]!["ArmorDamageTaken"] = -1; break;
            case "unknown-dodge-flight": point["DodgeThreats"]![0]!["ProjectileId"] = "torpedo-999"; break;
            default: throw new InvalidOperationException(defect);
        }

        using var engine = new SimulationEngine(SaveFixturesTests.Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(ScenarioLoader.LoadFromJson(original, true, true), true);
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(ScenarioLoader.LoadFromJson(root.ToJsonString(), true, true), true));
        Assert.Contains("Invalid legacy combat save", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        Assert.Equal(original, File.ReadAllText(path));
    }
}
