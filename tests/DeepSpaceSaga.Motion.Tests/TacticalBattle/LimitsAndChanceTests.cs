using System.Text.Json;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Motion.Tests.TacticalBattle;

public sealed class LimitsAndChanceTests
{
    private static readonly TacticalHitRules Rules = new(50, 5, 95, 15, 12, 10, 30);

    [Theory]
    [InlineData(.47, .205)]
    [InlineData(.56, .34)]
    [InlineData(.30, .05)]
    [InlineData(.95, .925)]
    public void Decoy_multiplies_miss_chance_after_clamp(double chance, double expected)
    {
        Assert.Equal(expected, TacticalHitChance.WithDecoy(chance, 1.5, .05), 12);
        Assert.Equal(chance, TacticalHitChance.WithDecoy(chance, 1, .05), 12);
    }

    [Fact]
    public void Decoy_never_goes_below_min_chance()
    {
        Assert.Equal(.05, TacticalHitChance.WithDecoy(.05, 1.5, .05));
        Assert.Equal(.1, TacticalHitChance.WithDecoy(.3, 100, .1));
        Assert.Equal(1, TacticalHitChance.WithDecoy(1, 1.5, .05));
    }

    [Fact]
    public void Module_mass_enters_limits_like_shield_mass()
    {
        var frame = new TacticalFrameLimits(460, 760, 50, 110, 70);
        TacticalDrive[] engines = [new(100, 25, 10, 32.5, 1), new(100, 25, 10, 32.5, .5)];
        Assert.Equal(TacticalLimits.Of(frame, 120, 70, 10, 2, engines, 10),
            TacticalLimits.Of(frame, 120, 70, 15, 2, engines));
        Assert.Equal(TacticalLimits.Of(frame, 120, 70, 10, 2, engines),
            TacticalLimits.Of(frame, 120, 70, 10, 2, engines, 0));
    }

    [Fact]
    public void CombatSectionThreeExamplesAndDamagedEngineLimitsMatch()
    {
        var heavy = new TacticalFrameLimits(460, 760, 50, 110, 70);
        var drive = new TacticalDrive(100, 25, 10, 32.5, 1);
        var limits = TacticalLimits.Of(heavy, 120, 70, 10, 2, [drive, drive]);
        Assert.Equal(870, limits.Mass);
        Assert.Equal(760.0 / 870, limits.MassFactor);
        Assert.Equal(50 + 50 * (760.0 / 870), limits.MaxSpeed);
        Assert.Equal(20 * (760.0 / 870), limits.Change);
        Assert.Equal(65 * (760.0 / 870), limits.Turn);
        Assert.Equal(limits.Turn * 0.5, limits.TurnLimit(limits.MaxSpeed, 0.5));
        Assert.Equal(limits.Turn, limits.TurnLimit(limits.MinSpeed, 0.5));
        Assert.Equal(60 + limits.Change, limits.ClampSpeed(200, 60));
        var damaged = TacticalLimits.Of(heavy, 120, 70, 10, 2, [drive with { Factor = 0.5 }, drive]);
        Assert.Equal(limits.Mass, damaged.Mass);
        Assert.Equal(15 * (760.0 / 870), damaged.Change);
        var stopped = TacticalLimits.Of(heavy, 120, 70, 10, 2, [drive with { Factor = 0 }, drive with { Factor = 0 }]);
        Assert.Equal(50, stopped.ClampSpeed(93, 93));
        Assert.Equal(0, stopped.TurnLimit(50, 0.5));
        var thug = TacticalLimits.Of(new(260, 760, 45, 105, 70), 30, 50, 0, 0, [new(70, 20, 9, 30, 1)]);
        Assert.Equal(410, thug.Mass);
        Assert.InRange(thug.MaxSpeed, 82.07, 82.08);
        Assert.InRange(thug.Change, 16.68, 16.69);
        Assert.InRange(thug.Turn, 55.60, 55.61);
    }

    [Fact]
    public void EngineMassIsSummedSeparatelyBeforeAddingToOtherMasses()
    {
        var limits = TacticalLimits.Of(new(1e16, 1e16, 0, 100, 100), 1, 1, 0, 0, [new(1, 1, 1, 1, 1), new(1, 1, 1, 1, 1)]);
        Assert.Equal(10000000000000002.0, limits.Mass);
        var capped = TacticalLimits.Of(new(1, 1, 10, 20, 30), 0, 0, 0, 0, [new(0, 100, 100, 100, 1)]);
        Assert.Equal(20, capped.MaxSpeed);
        Assert.Equal(30, capped.Turn);
        Assert.Throws<ArgumentOutOfRangeException>(() => TacticalLimits.Of(new(0, 1, 0, 0, 0), 0, 0, 0, 0, []));
    }

    [Fact]
    public void HitTermsFollowCombatSectionSixExamplesAndDistinctRearArcs()
    {
        var front = new TacticalShotInput(59, 47, 0, 125, 50, 50, 0, 180, true, 10, 1, 45);
        var terms = TacticalHitChance.Terms(Rules, front);
        Assert.Equal(59, terms.Accuracy);
        Assert.Equal(-47, terms.Maneuver);
        Assert.Equal(9, terms.Range);
        Assert.Equal(-10, terms.Shield);
        Assert.Equal(61, terms.Raw);
        Assert.Equal(0.61, terms.Chance);
        var rear = TacticalHitChance.Terms(Rules, front with { RearAngle = 0 });
        Assert.True(rear.IsRear);
        Assert.True(rear.HasRearBonus);
        Assert.Equal(0.81, rear.Chance);
        Assert.Equal(0.52, TacticalHitChance.Chance(Rules, front with { Distance = 125 }));
        Assert.Equal(0.72, TacticalHitChance.Chance(Rules, front with { Distance = 125, RearAngle = 0 }));
        var wideRear = TacticalHitChance.Terms(Rules, front with { RearAngle = 40 });
        Assert.True(wideRear.IsRear);
        Assert.False(wideRear.HasRearBonus);
        Assert.Equal(0, wideRear.Shield);
        Assert.Equal(0.71, wideRear.Chance);
        var carrier = TacticalHitChance.Terms(Rules, front with { TargetIsDrone = false, RearAngle = 0 });
        Assert.False(carrier.IsRear);
        Assert.False(carrier.HasRearBonus);
        Assert.Equal(0, carrier.Shield);
        Assert.Equal(0.71, carrier.Chance);
        Assert.Equal(18, TacticalHitChance.Terms(Rules, front with { WeaponRangeBonus = 30 }).Range);
        Assert.Equal(-12, TacticalHitChance.Terms(Rules, front with { Angle = -50 }).Angle);
        Assert.Equal(0, TacticalHitChance.Terms(Rules, front with { Distance = 200 }).Range);
        Assert.Equal(0.95, TacticalHitChance.Chance(Rules, front with { Accuracy = 1000 }));
        Assert.Equal(0.05, TacticalHitChance.Chance(Rules, front with { Accuracy = -1000 }));
        Assert.Equal(0, TacticalHitChance.Terms(Rules, front with { ShieldCount = 0 }).Shield);
    }

    [Fact]
    public void Evasion_term_subtracts_penalty_before_clamp()
    {
        var shot = new TacticalShotInput(59, 47, 0, 125, 50, 50, 0, 180, true, 10, 1, 45);
        var rules = Rules with { EvasionPenalty = 20 };
        Assert.Equal(0.61, TacticalHitChance.Chance(rules, shot));
        var terms = TacticalHitChance.Terms(rules, shot with { TargetEvading = true });
        Assert.Equal(-20, terms.Evasion);
        Assert.Equal(41, terms.Raw);
        Assert.Equal(0.41, terms.Chance);
        Assert.Equal(0.9, TacticalHitChance.Chance(rules, shot with { Accuracy = 108, TargetEvading = true }));
        Assert.Equal(0.05, TacticalHitChance.Chance(rules, shot with { Accuracy = 0, TargetEvading = true }));
        Assert.Equal(0.95, TacticalHitChance.Chance(rules, shot with { Accuracy = 1000, TargetEvading = true }));
        Assert.Equal(0, TacticalHitChance.Terms(rules, shot with { TargetIsDrone = false, TargetEvading = true }).Evasion);
    }

    [Fact]
    public void Evasion_applies_to_rear_shots()
    {
        var shot = new TacticalShotInput(59, 47, 0, 125, 50, 50, 0, 0, true, 10, 1, 45, TargetEvading: true);
        var terms = TacticalHitChance.Terms(Rules with { EvasionPenalty = 20 }, shot);
        Assert.True(terms.IsRear);
        Assert.Equal(10, terms.Rear);
        Assert.Equal(0, terms.Shield);
        Assert.Equal(-20, terms.Evasion);
        Assert.Equal(0.61, terms.Chance);
    }

    [Fact]
    public void AllPublishedSeedOneShotChancesMatchAtOriginalSixDecimalPrecision()
    {
        string repo = Repository();
        string content = Path.Combine(repo, "src/DeepSpaceSaga.Client/Data/TacticalBattle");
        var cache = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        JsonElement Read(string file)
        {
            if (cache.TryGetValue(file, out var value)) return value;
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(content, file)));
            return cache[file] = doc.RootElement.Clone();
        }
        using var trace = JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "VData/Planning/V4.0/Reference/trace-seed-1.json")));
        var data = trace.RootElement.GetProperty("data");
        var units = data.GetProperty("assembly").GetProperty("units").EnumerateArray().ToDictionary(e => e.GetProperty("id").GetString()!, StringComparer.Ordinal);
        var scenario = Read("scenarios.json").GetProperty("initial");
        var starts = scenario.GetProperty("sides").EnumerateArray().SelectMany(s => s.GetProperty("drones").EnumerateArray())
            .ToDictionary(e => e.GetProperty("id").GetString()!, StringComparer.Ordinal);
        var carriers = scenario.GetProperty("sides").EnumerateArray().Select(s => s.GetProperty("carrier"))
            .ToDictionary(e => e.GetProperty("id").GetString()!, StringComparer.Ordinal);
        var rules = Read("rules.json");
        var hit = rules.GetProperty("hit");
        var tuning = new TacticalHitRules(N(hit, "base"), N(hit, "minChance"), N(hit, "maxChance"), N(hit, "rangeBonus"),
            N(hit, "anglePenalty"), N(hit, "rearBonus"), N(hit, "rearBonusArcDeg"));
        int shots = 0;
        foreach (var turn in data.GetProperty("turns").EnumerateArray())
        {
            var positions = turn.GetProperty("positions").EnumerateArray().ToDictionary(e => e.GetProperty("id").GetString()!, Pose, StringComparer.Ordinal);
            foreach (var carrier in carriers) positions[carrier.Key] = Pose(carrier.Value);
            foreach (var shot in turn.GetProperty("shots").EnumerateArray())
            {
                string shooter = shot.GetProperty("shooter").GetString()!, target = shot.GetProperty("target").GetString()!;
                bool drone = units.ContainsKey(target);
                var layout = Read("layouts.json").GetProperty(starts[shooter].GetProperty("layout").GetString()!);
                var weapon = Read("Modules/Weapons/" + layout.GetProperty("weapon").GetString() + ".json");
                int defense = 0, count = 0;
                if (drone)
                {
                    var targetLayout = Read("layouts.json").GetProperty(starts[target].GetProperty("layout").GetString()!);
                    if (targetLayout.GetProperty("shield").ValueKind == JsonValueKind.String)
                    {
                        defense = Read("Modules/Shields/" + targetLayout.GetProperty("shield").GetString() + ".json").GetProperty("defense").GetInt32();
                        count = targetLayout.TryGetProperty("shieldCount", out var c) ? c.GetInt32() : 1;
                    }
                }
                var input = new TacticalShotInput(units[shooter].GetProperty("accuracy").GetInt32(),
                    drone ? units[target].GetProperty("maneuver").GetInt32() : Read("Frames/carrier.json").GetProperty("maneuver").GetInt32(),
                    weapon.GetProperty("hitBonus").GetInt32(), N(weapon, "range"), N(weapon, "arcDeg"), N(shot, "distance"), N(shot, "angle"),
                    TacticalHitChance.RearAngle(positions[shooter], positions[target]), drone, defense, count, N(rules.GetProperty("rear"), "arcHalfDeg"),
                    weapon.TryGetProperty("rangeBonus", out var rangeBonus) ? rangeBonus.GetDouble() : null);
                Assert.InRange(Math.Abs(TacticalHitChance.Chance(tuning, input) - N(shot, "chance")), 0, 0.0000006);
                shots++;
            }
        }
        Assert.Equal(34, shots);
    }

    private static TacticalPose Pose(JsonElement element) => new(N(element, "x"), N(element, "y"), N(element, "course"));
    private static double N(JsonElement element, string name) => element.GetProperty(name).GetDouble();
    private static string Repository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("DSS repository not found.");
    }
}
