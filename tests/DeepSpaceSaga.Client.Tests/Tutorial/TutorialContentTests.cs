using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>
/// EP-0028-US-0003: the shipped battle tutorial (Data/Tutorial/battle-tutorial.json) and its officer lines in Russian and English.
/// Shared by TK-0001…TK-0005; the generic checks (load, keys, panel fit) cover every step the script ships.
/// </summary>
[Collection("Battle localization")]
public sealed class TutorialContentTests
{
    private static readonly string[] Languages = ["Russian", "English"];

    internal static TutorialScript Shipped()
    {
        var warnings = new List<string>();
        var script = TutorialScriptLoader.Load(AppContext.BaseDirectory, warnings.Add);
        Assert.Empty(warnings);
        return script;
    }

    private static TutorialStep StepOf(TutorialScript script, string id) =>
        Assert.Single(script.Steps, s => s.Id == id);

    private static void AssertStep(TutorialScript script, string id, string lineKey, string phase, int? turn, int? minTurn, string? after,
        TutorialSignal signal, TutorialCompletion complete, string anchor, int priority)
    {
        var step = StepOf(script, id);
        Assert.Equal(lineKey, step.LineKey);
        Assert.Equal(new TutorialCondition(phase, turn, minTurn, after, signal), step.When);
        Assert.Equal(complete, step.Complete);
        Assert.Equal(anchor, step.Anchor);
        Assert.Equal(priority, step.Priority);
    }

    private static IEnumerable<string> AllKeys(TutorialScript script) =>
        new[] { script.Officer!.NameKey, script.Officer.RoleKey }.Concat(script.Steps.Select(s => s.LineKey));

    [Fact]
    public void ShippedScriptLoadsWithoutWarnings()
    {
        var script = Shipped();
        Assert.NotNull(script.Officer);
        Assert.NotEmpty(script.Steps);
        Assert.Equal("Tutorial.Officer.Name", script.Officer!.NameKey);
        Assert.Equal("Tutorial.Officer.Role", script.Officer.RoleKey);
        Assert.Equal(TutorialCommsPanelTests.OfficerPortrait, script.Officer.Portrait);
        Assert.Equal("tutorial.officer", script.Officer.PortraitKey);
    }

    [Fact]
    public void EveryLineKeyExistsInBothLocales()
    {
        var script = Shipped();
        foreach (string language in Languages)
        {
            var locale = Localization.LoadLocaleFile(language);
            Assert.NotNull(locale);
            foreach (string key in AllKeys(script))
                Assert.True(locale!.TryGetValue(key, out var text) && !string.IsNullOrWhiteSpace(text), $"{language}: missing {key}");
        }
    }

    [Fact]
    public void EveryLineFitsTwoPanelLinesInBothLanguages()
    {
        var script = Shipped();
        foreach (string language in Languages)
        {
            using var locale = Localization.UseLocaleForTests(language);
            foreach (var step in script.Steps)
            {
                string text = Localization.Get(step.LineKey);
                Assert.NotEqual(step.LineKey, text);
                var rows = TutorialCommsPanel.Wrap(text);
                Assert.True(rows.Length <= 2, $"{language} {step.Id}: {rows.Length} rows: {string.Join(" | ", rows)}");
            }
        }
    }

    [Fact]
    public void OfficerHeaderFitsThePanelWithoutEllipsis()
    {
        var officer = Shipped().Officer!;
        foreach (string language in Languages)
        {
            using var locale = Localization.UseLocaleForTests(language);
            string header = Localization.Get(officer.NameKey) + " · " + Localization.Get(officer.RoleKey);
            Assert.True(header.Length <= 45, $"{language}: header '{header}' is {header.Length} chars");
            using var paint = BattleTypography.TextPaint(11, true);
            Assert.Equal(ControlText.SafeSymbols(header), ControlText.Ellipsize(header, TutorialCommsPanel.TextWidth, paint));
        }
    }

    [Fact]
    public void OfficerIsTheSkitaletsCommsOfficerInBothLanguages()
    {
        var officer = Shipped().Officer!;
        using (Localization.UseLocaleForTests("Russian"))
        {
            Assert.Contains("Вольская", Localization.Get(officer.NameKey), StringComparison.Ordinal);
            Assert.Contains("Скитал", Localization.Get(officer.RoleKey), StringComparison.Ordinal);
            Assert.Contains("Командир", Localization.Get(StepOf(Shipped(), "intro").LineKey), StringComparison.Ordinal);
        }
        using (Localization.UseLocaleForTests("English"))
        {
            Assert.Contains("Volskaya", Localization.Get(officer.NameKey), StringComparison.Ordinal);
            Assert.Contains("Skitalets", Localization.Get(officer.RoleKey), StringComparison.Ordinal);
            Assert.Contains("Commander", Localization.Get(StepOf(Shipped(), "intro").LineKey), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void IntroMatchesTheCatalog() =>
        AssertStep(Shipped(), "intro", "Tutorial.Line.Intro", "Move", 1, null, null, TutorialSignal.None,
            TutorialCompletion.PilotSelected, "pilot:{own}", 100);

    [Fact]
    public void MoveLessonsMatchTheCatalog()
    {
        var script = Shipped();
        AssertStep(script, "move.steer", "Tutorial.Line.MoveSteer", "Move", 1, null, "intro", TutorialSignal.None,
            TutorialCompletion.PathEdited, "endpoint:{selected}", 90);
        AssertStep(script, "move.submit", "Tutorial.Line.MoveSubmit", "Move", 1, null, "move.steer", TutorialSignal.None,
            TutorialCompletion.PhaseLeft, "button:primary", 80);
        AssertStep(script, "move.cycle", "Tutorial.Line.MoveCycle", "Move", 2, null, null, TutorialSignal.None,
            TutorialCompletion.PilotCycled, "pilot:{own}", 70);
        AssertStep(script, "move.reachable", "Tutorial.Line.MoveReachable", "Move", null, 2, "move.cycle", TutorialSignal.None,
            TutorialCompletion.PathEdited, "reachable", 60);
        // The first Move teaches three things (pick a pilot, steer, submit), each after the player's own action.
        Assert.True(script.Limits.MaxLinesPerPhase >= 3);
    }

    [Fact]
    public void MoveLessonsNameTheirControls()
    {
        var script = Shipped();
        foreach (string language in Languages)
        {
            using var locale = Localization.UseLocaleForTests(language);
            Assert.Contains("Enter", Localization.Get(StepOf(script, "move.submit").LineKey), StringComparison.Ordinal);
            Assert.Contains("Tab", Localization.Get(StepOf(script, "move.cycle").LineKey), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FireAndSalvoLessonsMatchTheCatalog()
    {
        var script = Shipped();
        // minTurn 1, not turn 1: the first turn may have no Fire phase when the sides are out of range.
        AssertStep(script, "fire.target", "Tutorial.Line.FireTarget", "Fire", null, 1, null, TutorialSignal.None,
            TutorialCompletion.TargetChosen, "candidate:{any}", 90);
        AssertStep(script, "fire.submit", "Tutorial.Line.FireSubmit", "Fire", null, 1, "fire.target", TutorialSignal.None,
            TutorialCompletion.PhaseLeft, "button:primary", 80);
        AssertStep(script, "salvo.watch", "Tutorial.Line.SalvoWatch", "Salvo", null, 1, null, TutorialSignal.None,
            TutorialCompletion.PhaseLeft, "button:primary", 90);
        AssertStep(script, "results.read", "Tutorial.Line.ResultsRead", "Results", null, 1, null, TutorialSignal.None,
            TutorialCompletion.PhaseLeft, "button:replay", 80);
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void QuotedUiLabelsMatchCurrentLocale(string language)
    {
        var script = Shipped();
        using var locale = Localization.UseLocaleForTests(language);
        string Line(string id) => Localization.Get(StepOf(script, id).LineKey);
        Assert.Contains(Localization.Get("TacticalBattle.Ui.HoldFire"), Line("fire.target"), StringComparison.Ordinal);
        string replay = Localization.Get("TacticalBattle.Ui.Replay").Replace("↺", "", StringComparison.Ordinal).Trim();
        Assert.Contains(replay, Line("results.read"), StringComparison.Ordinal);
        Assert.Contains("Enter", Line("fire.submit"), StringComparison.Ordinal);
        Assert.Contains("Enter", Line("salvo.watch"), StringComparison.Ordinal);
    }

    [Fact]
    public void ContextLessonsMatchTheCatalog()
    {
        var script = Shipped();
        // Priority 79, not 85: in the first Fire phase the basics (target 90, open fire 80) come first, the threat after them.
        AssertStep(script, "ctx.under-fire", "Tutorial.Line.CtxUnderFire", "Fire", null, 2, null, TutorialSignal.UnderFire,
            TutorialCompletion.EvadeOrDecoy, "pilot:{signal}", 79);
        Assert.True(StepOf(script, "ctx.under-fire").Priority < StepOf(script, "fire.submit").Priority);
        AssertStep(script, "ctx.damage", "Tutorial.Line.CtxDamage", "Results", null, 1, null, TutorialSignal.OwnDamaged,
            TutorialCompletion.Acknowledged, "pilot:{signal}", 70);
        AssertStep(script, "ctx.low-mind", "Tutorial.Line.CtxLowMind", "Fire", null, 2, null, TutorialSignal.LowConsciousness,
            TutorialCompletion.Acknowledged, "pilot:{signal}", 75);
        // minTurn 3, not 2: the second Move belongs to Tab and the reachable zone (one mechanic per turn).
        AssertStep(script, "ctx.anomaly", "Tutorial.Line.CtxAnomaly", "Move", null, 3, null, TutorialSignal.AnomalyPresent,
            TutorialCompletion.Acknowledged, "pilot:{signal}", 65);
        AssertStep(script, "ctx.drone-lost", "Tutorial.Line.CtxDroneLost", "Results", null, 1, null, TutorialSignal.DroneLost,
            TutorialCompletion.Acknowledged, "pilot:{signal}", 95);
    }

    /// <summary>
    /// E and D are polled natively since US-0002-TK-0005, so the under-fire lesson names them; they must be the keys the pilot card
    /// itself shows («Уклониться [E]», «Ловушка [D]»). No other line names a single-letter key except H (the comms log);
    /// the English words "A" and "I" are not keys.
    /// </summary>
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void HotkeysNamedInLinesAreTheBattleKeys(string language)
    {
        var script = Shipped();
        using var locale = Localization.UseLocaleForTests(language);
        Assert.Contains("[E]", Localization.Get("TacticalBattle.Evade"), StringComparison.Ordinal);
        Assert.Contains("[D]", Localization.Get("TacticalBattle.Decoy"), StringComparison.Ordinal);
        foreach (var step in script.Steps)
        {
            var keys = System.Text.RegularExpressions.Regex.Matches(Localization.Get(step.LineKey), @"(?<![\p{L}\d'])[B-HJ-Z](?![\p{L}\d'])")
                .Select(m => m.Value).ToArray();
            string[] allowed = step.Id == "ctx.under-fire" ? ["E", "D"] : ["H"];
            Assert.All(keys, key => Assert.Contains(key, allowed));
            if (step.Id == "ctx.under-fire") Assert.Equal(["D", "E"], keys.Order(StringComparer.Ordinal).ToArray());
        }
    }

    [Fact]
    public void OfficerPortraitComposesDeterministicallyFromExistingHead()
    {
        var officer = Shipped().Officer!;
        using var first = DialoguePortraitComposer.Compose(officer.Portrait, PersonSex.Female, officer.PortraitKey);
        using var second = DialoguePortraitComposer.Compose(officer.Portrait, PersonSex.Female, officer.PortraitKey);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(300, first!.Width);
        Assert.Equal(300, first.Height);
        Assert.Equal(first.Bytes, second!.Bytes);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, officer.Portrait)));
        Assert.NotEqual(CharacterPortraits.DefaultFemale, officer.Portrait);
        string pilots = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "UI", "tactical-battle-pilots.json"));
        Assert.DoesNotContain(Path.GetFileNameWithoutExtension(officer.Portrait), pilots, StringComparison.Ordinal);
    }
}
