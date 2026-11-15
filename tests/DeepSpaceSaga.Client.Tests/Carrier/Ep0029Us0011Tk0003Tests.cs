using System.Text.Json;
using DeepSpaceSaga.Client.Tests.Tutorial;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0011 TK-0003: the tutorial speaks about a real battle with real losses and finite
/// magazines, points to the ship screen and the 42-torpedo hold after a victory, and never promises
/// a revival/return to the map after a terminal defeat. Irina stays the only speaker.
/// </summary>
[Collection("Battle localization")]
public sealed class Ep0029Us0011Tk0003Tests
{
    private static readonly string[] Languages = ["Russian", "English"];

    private static string BaseDir => AppContext.BaseDirectory;
    private static string ScriptPath => Path.Combine(BaseDir, "Data", "Tutorial", "battle-tutorial.json");
    private static string TutorialScenarioPath => Path.Combine(BaseDir, "Scenarios", "Tutorial", "scenario.json");
    private static string ManifestPath => Path.Combine(BaseDir, "Data", "Ships", "starter-carrier.json");

    private static string Line(string id)
    {
        var script = TutorialContentTests.Shipped();
        return Localization.Get(Assert.Single(script.Steps, step => step.Id == id).LineKey);
    }

    private static void AssertFitsTwoPanelRows(string text, string context) =>
        Assert.True(TutorialCommsPanel.Wrap(text).Length <= 2,
            $"{context}: {TutorialCommsPanel.Wrap(text).Length} rows: {string.Join(" | ", TutorialCommsPanel.Wrap(text))}");

    [Theory]
    [InlineData("Russian", "Командир", "настоящ", "потер", "торпед")]
    [InlineData("English", "Commander", "real", "loss", "torpedo")]
    public void TutorialMentionsRealLosses(string language, string commander, string real, string loss, string torpedo)
    {
        using var locale = Localization.UseLocaleForTests(language);

        string intro = Line("intro");
        Assert.Contains(commander, intro, StringComparison.Ordinal);
        Assert.Contains(real, intro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(loss, intro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(torpedo, intro, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("симуляц", intro, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("трениров", intro, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("simulation", intro, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("training", intro, StringComparison.OrdinalIgnoreCase);
        AssertFitsTwoPanelRows(intro, $"{language} intro");

        // The fire lesson names the finite magazine and the skip control.
        string target = Line("fire.target");
        string finite = language == "Russian" ? "конечн" : "finite";
        Assert.Contains(finite, target, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(Localization.Get("TacticalBattle.Ui.HoldFire"), target, StringComparison.Ordinal);
        Assert.DoesNotContain("бесконечн", target, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unlimited", target, StringComparison.OrdinalIgnoreCase);
        AssertFitsTwoPanelRows(target, $"{language} fire.target");

        // The existing strict phrases stay intact.
        string submit = Line("fire.submit");
        Assert.Contains("Enter", submit, StringComparison.Ordinal);
        Assert.Contains(language == "Russian" ? "на карточке выбранного пилота" : "on the selected pilot's card",
            submit, StringComparison.Ordinal);
        string replay = Localization.Get("TacticalBattle.Ui.Replay").Replace("↺", "", StringComparison.Ordinal).Trim();
        Assert.Contains(replay, Line("results.read"), StringComparison.Ordinal);
    }

    [Fact]
    public void DisabledHintsStillRealBattle()
    {
        // Hints are a client display preference; the shipped script carries no mode flag that could
        // turn the real battle into a sandbox when hints are off.
        using var script = JsonDocument.Parse(File.ReadAllText(ScriptPath));
        Assert.Equal(
            new[] { "limits", "officer", "schemaVersion", "steps" },
            script.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(
            new[] { "nameKey", "portrait", "portraitKey", "roleKey" },
            script.RootElement.GetProperty("officer").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.False(script.RootElement.TryGetProperty("mode", out _));
        Assert.DoesNotContain("sandbox", File.ReadAllText(ScriptPath), StringComparison.OrdinalIgnoreCase);

        // The hint toggle only labels the panel; it never claims the battle is harmless.
        foreach (string language in Languages)
        {
            var locale = Localization.LoadLocaleFile(language);
            Assert.NotNull(locale);
            foreach (string key in new[] { "Settings.TutorialHints", "Settings.TutorialHints.On", "Settings.TutorialHints.Off" })
            {
                Assert.True(locale!.TryGetValue(key, out string? value), $"{language}: missing {key}");
                Assert.DoesNotContain("симуляц", value!, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("training", value!, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("sandbox", value!, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Hints off cannot change the world: the tutorial starts the real owned wing (36 loaded +
        // 42 in the hold), so its losses and spent torpedoes are economical consequences.
        var scenario = ScenarioLoader.LoadFromFile(TutorialScenarioPath);
        var ship = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Equal(4, ship.CarrierState!.OwnedDrones!.Count);
        Assert.Equal(36, ship.CarrierState.OwnedDrones.Sum(d => d.AmmoCurrent));
        Assert.Equal(42, ship.Modules!.SelectMany(m => m.Cargo ?? [])
            .Single(c => c.ItemTypeId == "item.drone-torpedo.t1").Quantity);
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void OutcomeDoesNotPromiseRevival(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);

        string toMap = Localization.Get("TacticalBattle.ToMap");
        string defeat = Line("over.defeat");
        Assert.Contains(toMap, defeat, StringComparison.Ordinal);
        foreach (string banned in new[] { "заново", "снова", "повтор", "again", "restart", "retry", "replay" })
            Assert.DoesNotContain(banned, defeat, StringComparison.OrdinalIgnoreCase);

        // Terminal wording: the carrier is lost and the run is over; no promised return.
        if (language == "Russian")
        {
            Assert.Contains("погиб", defeat, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("окончен", defeat, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("продолжим", defeat, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("верн", defeat, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Contains("gone", defeat, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("over", defeat, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("carry on", defeat, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("return", defeat, StringComparison.OrdinalIgnoreCase);
        }

        // The victory line points at the ship screen and the 42-torpedo hold for rearming.
        string victory = Line("over.victory");
        Assert.Contains(toMap, victory, StringComparison.Ordinal);
        if (language == "Russian")
        {
            Assert.Contains("корабл", victory, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("трюм", victory, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("42", victory, StringComparison.Ordinal);
            Assert.Contains("пополн", victory, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Contains("ship", victory, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("hold", victory, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("42", victory, StringComparison.Ordinal);
            Assert.Contains("rearm", victory, StringComparison.OrdinalIgnoreCase);
        }

        AssertFitsTwoPanelRows(defeat, $"{language} over.defeat");
        AssertFitsTwoPanelRows(victory, $"{language} over.victory");
        AssertFitsTwoPanelRows(Line("over.other"), $"{language} over.other");
    }

    [Fact]
    public void OfficerPortraitRemainsDistinct()
    {
        using var script = JsonDocument.Parse(File.ReadAllText(ScriptPath));
        var officer = script.RootElement.GetProperty("officer");
        Assert.Equal("Tutorial.Officer.Name", officer.GetProperty("nameKey").GetString());
        Assert.Equal("Tutorial.Officer.Role", officer.GetProperty("roleKey").GetString());
        Assert.Equal("tutorial.officer", officer.GetProperty("portraitKey").GetString());
        string portrait = officer.GetProperty("portrait").GetString()!;
        Assert.Equal("Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png", portrait);

        // Irina is the same crew member in the shared starter manifest and stays distinct from the
        // four pilot portraits (she is not a fifth pilot).
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var crew = manifest.RootElement.GetProperty("crew").EnumerateArray().ToArray();
        var irina = Assert.Single(crew, c => c.GetProperty("crewId").GetString() == "CHR-PLAYER-DEFENSE");
        Assert.Equal(portrait, irina.GetProperty("portraitAssetId").GetString());
        var pilotPortraits = crew
            .Where(c => c.GetProperty("role").GetString() == "Pilot")
            .Select(c => c.GetProperty("portraitAssetId").GetString()).ToArray();
        Assert.Equal(4, pilotPortraits.Length);
        Assert.DoesNotContain(portrait, pilotPortraits);

        using (Localization.UseLocaleForTests("Russian"))
        {
            Assert.Contains("Вольская", Localization.Get("Tutorial.Officer.Name"), StringComparison.Ordinal);
            Assert.Contains("Скитал", Localization.Get("Tutorial.Officer.Role"), StringComparison.Ordinal);
        }

        using (Localization.UseLocaleForTests("English"))
        {
            Assert.Contains("Volskaya", Localization.Get("Tutorial.Officer.Name"), StringComparison.Ordinal);
            Assert.Contains("Skitalets", Localization.Get("Tutorial.Officer.Role"), StringComparison.Ordinal);
        }
    }
}
