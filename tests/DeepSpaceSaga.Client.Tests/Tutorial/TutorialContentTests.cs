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
