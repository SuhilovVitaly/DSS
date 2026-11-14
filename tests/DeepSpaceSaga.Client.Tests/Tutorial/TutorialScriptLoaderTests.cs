using DeepSpaceSaga.Client.UI.Screens.Tutorial;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

public sealed class TutorialScriptLoaderTests
{
    private const string Officer = """
        "officer": { "nameKey": "Tutorial.Officer.Name", "roleKey": "Tutorial.Officer.Role",
                     "portrait": "Images/Persons/W4/Portraits/head-1.png", "portraitKey": "tutorial.officer" }
        """;

    private static string Script(string steps, string? extraRoot = null) =>
        "{ \"schemaVersion\": 1, " + (extraRoot is null ? "" : extraRoot + ", ") + "\"steps\": [" + steps + "] }";

    private const string IntroStep = """
        { "id": "intro", "lineKey": "Tutorial.Line.Intro", "when": { "phase": "Move", "turn": 1 }, "complete": "acknowledged", "priority": 100 }
        """;

    [Fact]
    public void ParsesValidScriptPreservingStepOrder()
    {
        string json = """
            { "schemaVersion": 1,
              "officer": { "nameKey": "Tutorial.Officer.Name", "roleKey": "Tutorial.Officer.Role",
                           "portrait": "Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png", "portraitKey": "tutorial.officer" },
              "limits": { "minGapMs": 3000, "autoCollapseMs": 20000, "maxLinesPerPhase": 3 },
              "steps": [
                { "id": "intro", "lineKey": "Tutorial.Line.Intro", "when": { "phase": "Move", "turn": 1 }, "complete": "acknowledged", "priority": 100 },
                { "id": "move.select-pilot", "lineKey": "Tutorial.Line.MoveSelectPilot",
                  "when": { "phase": "Move", "turn": 1, "after": "intro" }, "complete": "pilot-selected", "anchor": "pilot:{own}", "priority": 90 },
                { "id": "fire.under-fire", "lineKey": "Tutorial.Line.UnderFire",
                  "when": { "phase": "Fire", "minTurn": 2, "signal": "under-fire" }, "complete": "evade-or-decoy", "anchor": "evade-ring:{signal}", "priority": 10 },
                { "id": "outcome", "lineKey": "Tutorial.Line.Outcome", "when": { "phase": "Outcome" }, "complete": "phase-left", "anchor": "button:primary", "priority": 0 }
              ] }
            """;

        var script = TutorialScriptLoader.Parse(json);

        Assert.Equal(1, script.SchemaVersion);
        Assert.Equal(new TutorialOfficer("Tutorial.Officer.Name", "Tutorial.Officer.Role",
            "Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png", "tutorial.officer"), script.Officer);
        Assert.Equal(new TutorialLimits(3000, 20000, 3), script.Limits);
        Assert.Equal(["intro", "move.select-pilot", "fire.under-fire", "outcome"], script.Steps.Select(s => s.Id));
        var select = script.Steps[1];
        Assert.Equal("Tutorial.Line.MoveSelectPilot", select.LineKey);
        Assert.Equal(new TutorialCondition("Move", 1, null, "intro", TutorialSignal.None), select.When);
        Assert.Equal(TutorialCompletion.PilotSelected, select.Complete);
        Assert.Equal("pilot:{own}", select.Anchor);
        Assert.Equal(90, select.Priority);
        var underFire = script.Steps[2];
        Assert.Equal(new TutorialCondition("Fire", null, 2, null, TutorialSignal.UnderFire), underFire.When);
        Assert.Equal(TutorialCompletion.EvadeOrDecoy, underFire.Complete);
        Assert.Null(script.Steps[3].When.Turn);
        Assert.Equal(TutorialCompletion.PhaseLeft, script.Steps[3].Complete);
    }

    [Fact]
    public void OptionalSectionsUseDefaults()
    {
        var script = TutorialScriptLoader.Parse(Script(IntroStep));

        Assert.Null(script.Officer);
        Assert.Equal(new TutorialLimits(), script.Limits);
        Assert.Equal(new TutorialLimits(2500, 15000, 2), script.Limits);
        Assert.Null(script.Steps[0].Anchor);
    }

    [Fact]
    public void EveryCompletionAndSignalNameParses()
    {
        string[] completions = ["pilot-selected", "path-edited", "phase-left", "target-chosen", "pilot-cycled", "evade-or-decoy", "acknowledged"];
        string[] signals = ["under-fire", "own-damaged", "low-consciousness", "anomaly-present", "drone-lost", "outcome-victory", "outcome-defeat", "outcome-other"];
        string steps = string.Join(",", signals.Select((signal, i) =>
            $$"""{ "id": "c{{i}}", "lineKey": "Tutorial.Line.C{{i}}", "when": { "phase": "Fire", "signal": "{{signal}}" }, "complete": "{{completions[i % completions.Length]}}" }"""));

        var script = TutorialScriptLoader.Parse(Script(steps));

        Assert.Equal(Enum.GetValues<TutorialCompletion>().Append(TutorialCompletion.PilotSelected), script.Steps.Select(s => s.Complete));
        Assert.Equal(Enum.GetValues<TutorialSignal>().Skip(1), script.Steps.Select(s => s.When.Signal));
    }

    public static TheoryData<string, string> InvalidScripts()
    {
        static string Step(string id = "a", string lineKey = "Tutorial.Line.A", string when = "{ \"phase\": \"Move\" }",
            string complete = "\"pilot-selected\"", string extra = "") =>
            $"{{ \"id\": \"{id}\", \"lineKey\": \"{lineKey}\", \"when\": {when}, \"complete\": {complete}{extra} }}";
        static string Officer(string name = "\"Tutorial.Officer.Name\"", string role = "\"Tutorial.Officer.Role\"",
            string portrait = "\"Images/Persons/W4/Portraits/head.png\"", string key = "\"tutorial.officer\"") =>
            $"\"officer\": {{ \"nameKey\": {name}, \"roleKey\": {role}, \"portrait\": {portrait}, \"portraitKey\": {key} }}";
        return new()
        {
            // root
            { "{ \"schemaVersion\": 2, \"steps\": [] }", "schemaVersion" },
            { "{ \"steps\": [] }", "schemaVersion" },
            { "{ \"schemaVersion\": \"1\", \"steps\": [] }", "schemaVersion" },
            { "{ \"schemaVersion\": 1 }", "steps" },
            { "{ \"schemaVersion\": 1, \"steps\": {} }", "steps" },
            { "{ \"schemaVersion\": 1, \"steps\": [], \"extra\": true }", "extra" },
            { "[]", "$" },
            // officer
            { Script(IntroStep, Officer(name: "\"\"")), "officer.nameKey" },
            { Script(IntroStep, Officer(role: "null")), "officer.roleKey" },
            { Script(IntroStep, Officer(key: "\" \"")), "officer.portraitKey" },
            { Script(IntroStep, "\"officer\": { \"nameKey\": \"N\", \"roleKey\": \"R\", \"portraitKey\": \"k\" }"), "officer.portrait" },
            { Script(IntroStep, Officer(portrait: "\"Images/../secret.png\"")), "officer.portrait" },
            { Script(IntroStep, Officer(portrait: "\"Images/head.jpg\"")), "officer.portrait" },
            { Script(IntroStep, Officer(portrait: "\"/Images/head.png\"")), "officer.portrait" },
            { Script(IntroStep, Officer(portrait: "\"C:/Images/head.png\"")), "officer.portrait" },
            { Script(IntroStep, Officer(portrait: "\"Images\\\\head.png\"")), "officer.portrait" },
            { Script(IntroStep, Officer(portrait: "\"Images/he ad.png\"")), "officer.portrait" },
            { Script(IntroStep, "\"officer\": { \"nameKey\": \"N\", \"roleKey\": \"R\", \"portrait\": \"a.png\", \"portraitKey\": \"k\", \"x\": 1 }"), "officer.x" },
            // limits
            { Script(IntroStep, "\"limits\": { \"minGapMs\": -1 }"), "limits.minGapMs" },
            { Script(IntroStep, "\"limits\": { \"minGapMs\": 60001 }"), "limits.minGapMs" },
            { Script(IntroStep, "\"limits\": { \"autoCollapseMs\": 999 }"), "limits.autoCollapseMs" },
            { Script(IntroStep, "\"limits\": { \"autoCollapseMs\": 120001 }"), "limits.autoCollapseMs" },
            { Script(IntroStep, "\"limits\": { \"maxLinesPerPhase\": 0 }"), "limits.maxLinesPerPhase" },
            { Script(IntroStep, "\"limits\": { \"maxLinesPerPhase\": 6 }"), "limits.maxLinesPerPhase" },
            { Script(IntroStep, "\"limits\": { \"maxLinesPerPhase\": 2.5 }"), "limits.maxLinesPerPhase" },
            { Script(IntroStep, "\"limits\": { \"gap\": 1 }"), "limits.gap" },
            // steps
            { Script(Step(id: "Bad")), "steps[0].id" },
            { Script(Step(id: "a..b")), "steps[0].id" },
            { Script(Step(id: "a-")), "steps[0].id" },
            { Script(Step() + "," + Step()), "steps[1].id" },
            { Script(Step(lineKey: "Tutorial.Officer.Name")), "steps[0].lineKey" },
            { Script(Step(lineKey: "Tutorial.Line.")), "steps[0].lineKey" },
            { Script(Step(when: "{ \"phase\": \"Over\" }")), "steps[0].when.phase" },
            { Script(Step(when: "{ }")), "steps[0].when.phase" },
            { Script(Step(when: "{ \"phase\": \"Move\", \"turn\": 0 }")), "steps[0].when.turn" },
            { Script(Step(when: "{ \"phase\": \"Move\", \"minTurn\": 0 }")), "steps[0].when.minTurn" },
            { Script(Step(when: "{ \"phase\": \"Move\", \"turn\": 1, \"minTurn\": 2 }")), "steps[0].when.minTurn" },
            { Script(Step(id: "a") + "," + Step(id: "b", when: "{ \"phase\": \"Move\", \"after\": \"c\" }") + "," + Step(id: "c")), "steps[1].when.after" },
            { Script(Step(id: "a", when: "{ \"phase\": \"Move\", \"after\": \"a\" }")), "steps[0].when.after" },
            { Script(Step(when: "{ \"phase\": \"Move\", \"signal\": \"none\" }")), "steps[0].when.signal" },
            { Script(Step(when: "{ \"phase\": \"Move\", \"signal\": \"UnderFire\" }")), "steps[0].when.signal" },
            { Script(Step(when: "{ \"phase\": \"Move\", \"phases\": 1 }")), "steps[0].when.phases" },
            { Script(Step(complete: "\"pilotSelected\"")), "steps[0].complete" },
            { Script(Step(complete: "\"PilotSelected\"")), "steps[0].complete" },
            { Script(Step(complete: "1")), "steps[0].complete" },
            { Script(Step(extra: ", \"anchor\": \"Pilot:x\"")), "steps[0].anchor" },
            { Script(Step(extra: ", \"anchor\": \"pilot:{mine}\"")), "steps[0].anchor" },
            { Script(Step(extra: ", \"anchor\": \"pilot:\"")), "steps[0].anchor" },
            { Script(Step(extra: ", \"anchor\": \"pilot:a b\"")), "steps[0].anchor" },
            { Script(Step(extra: ", \"priority\": -1")), "steps[0].priority" },
            { Script(Step(extra: ", \"priority\": 101")), "steps[0].priority" },
            { Script(Step(extra: ", \"once\": true")), "steps[0].once" },
            { Script(Step(extra: ", \"lineKey2\": \"x\"")), "steps[0].lineKey2" },
        };
    }

    [Theory]
    [MemberData(nameof(InvalidScripts))]
    public void RejectsInvalidScript(string json, string fieldPath)
    {
        var error = Assert.Throws<FormatException>(() => TutorialScriptLoader.Parse(json));
        Assert.StartsWith(fieldPath + ":", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadReturnsEmptyAndWarnsOnMissingOrBrokenFile()
    {
        string root = Path.GetFullPath(Path.Combine(TempRoot(), "tutorial-loader-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        try
        {
            string file = Path.Combine(root, "Data", "Tutorial", "battle-tutorial.json");
            Assert.Equal(file, Path.GetFullPath(Path.Combine(root, TutorialScriptLoader.RelativePath)));

            var warnings = new List<string>();
            var missing = TutorialScriptLoader.Load(root, warnings.Add);
            Assert.Same(TutorialScript.Empty, missing);
            Assert.Equal($"TUTORIAL: {file} not found", Assert.Single(warnings));

            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "{ not json");
            warnings.Clear();
            Assert.Same(TutorialScript.Empty, TutorialScriptLoader.Load(root, warnings.Add));
            Assert.StartsWith($"TUTORIAL: {file}: ", Assert.Single(warnings), StringComparison.Ordinal);

            File.WriteAllText(file, "{ \"schemaVersion\": 1, \"steps\": [ { \"id\": \"Bad\" } ] }");
            warnings.Clear();
            Assert.Same(TutorialScript.Empty, TutorialScriptLoader.Load(root, warnings.Add));
            Assert.Equal($"TUTORIAL: {file}: steps[0].id: must match ^[a-z0-9]+([.-][a-z0-9]+)*$", Assert.Single(warnings));

            File.WriteAllText(file, Script(IntroStep));
            warnings.Clear();
            var loaded = TutorialScriptLoader.Load(root, warnings.Add);
            Assert.Empty(warnings);
            Assert.Equal("intro", Assert.Single(loaded.Steps).Id);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void EmptyScriptHasNoStepsAndDefaultLimits()
    {
        Assert.Empty(TutorialScript.Empty.Steps);
        Assert.Null(TutorialScript.Empty.Officer);
        Assert.Equal(new TutorialLimits(), TutorialScript.Empty.Limits);
        Assert.Equal(1, TutorialScript.Empty.SchemaVersion);
    }

    private static string TempRoot() =>
        Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root && Path.IsPathFullyQualified(root)
            ? root : Path.GetTempPath();
}
