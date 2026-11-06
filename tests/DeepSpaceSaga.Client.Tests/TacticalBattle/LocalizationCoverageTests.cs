using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class LocalizationCoverageTests
{
    private const string Prefix = "TacticalBattle.";
    private static readonly string Source = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));

    [Fact]
    public void RequiredScreenKeysResolveWithMatchingFormatsInBothPackagedLanguages()
    {
        var ru = ReadLocale("Russian");
        var en = ReadLocale("English");
        Assert.NotEmpty(ru);
        Assert.Equal(ru.Keys.Order(StringComparer.Ordinal), en.Keys.Order(StringComparer.Ordinal));
        foreach (string key in ru.Keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(ru[key]));
            Assert.False(string.IsNullOrWhiteSpace(en[key]));
            Assert.NotEqual(key, ru[key]);
            Assert.NotEqual(key, en[key]);
            Assert.Equal(CompositeFormat.Parse(ru[key]).MinimumArgumentCount, CompositeFormat.Parse(en[key]).MinimumArgumentCount);
        }

        var sources = Directory.EnumerateFiles(Path.Combine(Source, "UI/Screens/TacticalBattle"), "*.cs");
        var direct = sources.SelectMany(path => Regex.Matches(File.ReadAllText(path),
                """\b(?:L|F|Format|Localize)\(\s*"([A-Z][A-Za-z0-9.]*)"\s*[,)]""")
            .Select(m => m.Groups[1].Value));
        string[] conditional =
        [
            "SetupTitle", "BattleTitle", "Start", "Fire", "Move", "Drone", "FrameSkills", "Weapon", "Armor", "Shield",
            "MoveEdited", "MoveSuggested", "Sides.Own", "Sides.Enemy", "Log.HitRear", "Log.HitHull",
            "Log.CarrierDestroyed", "Log.Destroyed", "SalvoCarrierDestroyed", "SalvoDestroyed",
            "Outcome.Victory", "Outcome.Defeat", "Outcome.Draw", "Outcome.Timeout", "Phase.Results"
        ];
        var dynamic = Enum.GetNames<TacticalBattlePhase>().Select(n => "Phase." + n)
            .Concat(Enum.GetNames<TacticalUnitState>().Select(n => "UnitState." + n))
            .Concat(Enum.GetNames<TacticalOutcomeReasonCode>().Select(n => "OutcomeReason." + n))
            .Concat(Enum.GetNames<TacticalBattleRejectionCode>().Where(n => n != "None").Select(n => "Refusal." + n))
            .Concat(Enumerable.Range(1, 8).Select(i => "Forecast.Legend" + i));
        foreach (string key in direct.Concat(conditional).Concat(dynamic).Distinct(StringComparer.Ordinal))
            Assert.True(ru.ContainsKey(Prefix + key), "Missing screen key: " + key);
    }

    [Theory]
    [InlineData("Russian", "Движение")]
    [InlineData("English", "Movement")]
    public async Task RealSetupBattleCardsAndJournalUseCatalogNamesInEitherLocale(string language, string movement)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.PressKey(Silk.NET.Input.Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        var scenario = setup.Flow.Options!.Scenarios.Single(s => s.Id == "initial");

        StartTacticalBattleRequest start;
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        using (Localization.UseLocaleForTests(language))
        {
            var localizedSetup = new TacticalBattleSetupScreen(setup.Flow, 1);
            localizedSetup.Render(canvas, 1280, 720);
            Assert.Contains(scenario.Description, localizedSetup.ScenarioSummary);
            start = localizedSetup.BuildStartRequest() with { OwnTacticId = "raid" };
        }
        Assert.True((await setup.Flow.StartAsync(start))!.Accepted);
        using (Localization.UseLocaleForTests(language))
        {
            var battle = new TacticalBattleScreen(setup.Flow, 1);
            battle.Render(canvas, 1280, 720);
            var frame = battle.CaptureFrame(1280, 720)!;
            Assert.Equal(scenario.Name, frame.Battle.ScenarioName);
            Assert.Equal(movement, TacticalBattlePanelHeader.Capture(frame).Phase);
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source, "Data/TacticalBattle/ai.json")));
            var roles = catalog.RootElement.GetProperty("roles");
            var cards = TacticalBattleSidesTab.Cards(frame).Where(c => c.Unit.Kind == TacticalUnitKind.Drone).ToArray();
            Assert.Contains(cards, c => c.Unit.RoleId == "striker");
            foreach (var card in cards)
            {
                var unit = card.Unit;
                var row = scenario.Drones.Single(d => d.Id == unit.Id);
                Assert.Equal(row.FrameName, unit.Build!.FrameName);
                Assert.Equal(row.PilotName, unit.Build.PilotName);
                string role = roles.GetProperty(unit.RoleId!).GetProperty("name").GetString()!;
                Assert.Equal(role, unit.RoleName);
                Assert.Contains(role, card.Details);
                Assert.Contains(unit.Build.PilotName, card.Details);
            }
        }

        var state = setup.Flow.State!;
        for (int i = 0; i < 100 && state.Phase != TacticalBattlePhase.Over &&
            !state.Log.Any(e => e.Kind == TacticalBattleLogKind.RoleChanged && !e.NoAttackers); i++)
        {
            var response = state.Phase == TacticalBattlePhase.Move
                ? await setup.Flow.MoveAsync(new($"locale-move-{i}", state.BattleId, state.Revision))
                : await setup.Flow.FireAsync(new($"locale-fire-{i}", state.BattleId, state.Revision,
                    [.. state.Targets.Select(t => new TacticalFireTarget(t.DroneId, t.DefaultTargetId))]));
            Assert.True(response!.Accepted);
            state = response.State!;
        }
        using (Localization.UseLocaleForTests(language))
        {
            using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(Source, "Data/TacticalBattle/ai.json")));
            var roles = catalog.RootElement.GetProperty("roles");
            var events = state.Log.Where(e => e.Kind == TacticalBattleLogKind.RoleChanged && !e.NoAttackers).ToArray();
            Assert.NotEmpty(events);
            foreach (var entry in events)
            {
                string role = roles.GetProperty(entry.RoleId!).GetProperty("name").GetString()!;
                Assert.Equal(role, entry.RoleName);
                Assert.Contains(role, BattleLogFormatter.Format(entry));
            }
        }
        await setup.Flow.ExitAsync();
    }

    private static Dictionary<string, string> ReadLocale(string language)
    {
        string file = Path.Combine(AppContext.BaseDirectory, "Data", "Locale", language + ".json");
        using var doc = JsonDocument.Parse(File.ReadAllText(file));
        var entries = doc.RootElement.EnumerateObject().ToArray();
        Assert.DoesNotContain(entries, p => p.Name.StartsWith("Combat.", StringComparison.Ordinal) ||
            p.Name.StartsWith("CombatReports.", StringComparison.Ordinal) || p.Name.StartsWith("Defeat.", StringComparison.Ordinal));
        return entries.Where(p => p.Name.StartsWith(Prefix, StringComparison.Ordinal))
            .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
    }
}
