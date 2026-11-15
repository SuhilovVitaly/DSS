using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0012 TK-0004 (AC-02..AC-04): RU/EN lifecycle text keeps "battle ended" distinct
/// from "result saved", offers an explicit checkpoint-write failure + Retry, explains a missing
/// ready wing, and never promises immunity from a forced process kill or calls a manual Load a
/// free repair.
/// </summary>
public sealed class Ep0029Us0012Tk0004Tests
{
    private static readonly string[] LifecycleKeys =
    [
        "TacticalBattle.ResultUnsaved",
        "TacticalBattle.ResultSaved",
        "TacticalBattle.CheckpointSaveFailed",
        "TacticalBattle.CheckpointRetry",
        "TacticalBattle.CheckpointRetryHelp",
        "TacticalBattle.RealLosses",
        "TacticalBattle.ContinuationSaved",
        "TacticalBattle.NoReadyWing",
        "TacticalBattle.UnsupportedWing",
        "TacticalBattle.ManualLoadHint",
        "ShipDestroyed.Defeat",
        "TacticalBattle.Refusal.NoReadyDrones",
        "TacticalBattle.Refusal.CarrierNotReady",
        "TacticalBattle.Refusal.StaleCarrierState",
        "TacticalBattle.Refusal.WrongShip",
    ];

    private static readonly string[] TerminalActionKeys =
    [
        "ShipDestroyed.Title",
        "ShipDestroyed.Defeat",
        "ShipDestroyed.Load",
        "ShipDestroyed.MainMenu",
        "TacticalBattle.CheckpointRetry",
        "TacticalBattle.NoReadyWing",
        "TacticalBattle.UnsupportedWing",
    ];

    [Fact]
    public void LifecycleLocaleParity()
    {
        var russian = ReadLocale("Russian.json");
        var english = ReadLocale("English.json");

        Assert.Equal(russian.Keys.Order(StringComparer.Ordinal), english.Keys.Order(StringComparer.Ordinal));
        foreach (string key in LifecycleKeys)
        {
            Assert.True(russian.TryGetValue(key, out string? ru), $"Russian.json is missing '{key}'.");
            Assert.True(english.TryGetValue(key, out string? en), $"English.json is missing '{key}'.");
            Assert.False(string.IsNullOrWhiteSpace(ru));
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.NotEqual(key, ru);
            Assert.NotEqual(key, en);
        }
    }

    [Fact]
    public void SaveFailureIsNotSuccess()
    {
        var russian = ReadLocale("Russian.json");
        var english = ReadLocale("English.json");

        foreach (var locale in new[] { russian, english })
        {
            // "Battle ended" and "result saved" are independent states, and a write failure is
            // never rendered with the success text.
            Assert.NotEqual(locale["TacticalBattle.ResultSaved"], locale["TacticalBattle.ResultUnsaved"]);
            Assert.NotEqual(locale["TacticalBattle.ResultSaved"], locale["TacticalBattle.CheckpointSaveFailed"]);
            Assert.False(string.IsNullOrWhiteSpace(locale["TacticalBattle.CheckpointRetry"]));
            Assert.False(string.IsNullOrWhiteSpace(locale["TacticalBattle.ContinuationSaved"]));
        }

        Assert.Contains("не сохран", russian["TacticalBattle.ResultUnsaved"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("сохран", russian["TacticalBattle.ResultSaved"], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("не сохран", russian["TacticalBattle.ResultSaved"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not saved", english["TacticalBattle.ResultUnsaved"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("saved", english["TacticalBattle.ResultSaved"], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("not saved", english["TacticalBattle.ResultSaved"], StringComparison.OrdinalIgnoreCase);

        // The Retry hint warns about the real risk instead of promising protection from a kill.
        Assert.Contains("потерян", russian["TacticalBattle.CheckpointRetryHelp"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lost", english["TacticalBattle.CheckpointRetryHelp"], StringComparison.OrdinalIgnoreCase);
        foreach (string key in LifecycleKeys)
        {
            Assert.DoesNotContain("гарантирован", russian[key], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("guaranteed", english[key], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("не будет потерян", russian[key], StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("will not be lost", english[key], StringComparison.OrdinalIgnoreCase);
        }

        // A manual Load is allowed but is not described as free repair.
        Assert.DoesNotContain("бесплатн", russian["TacticalBattle.ManualLoadHint"], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("free", english["TacticalBattle.ManualLoadHint"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("не чинит", russian["TacticalBattle.ManualLoadHint"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not repair", english["TacticalBattle.ManualLoadHint"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TerminalActionsHaveTranslations()
    {
        var russian = ReadLocale("Russian.json");
        var english = ReadLocale("English.json");

        foreach (string key in TerminalActionKeys)
        {
            Assert.True(russian.TryGetValue(key, out string? ru), $"Russian.json is missing '{key}'.");
            Assert.True(english.TryGetValue(key, out string? en), $"English.json is missing '{key}'.");
            Assert.False(string.IsNullOrWhiteSpace(ru));
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.NotEqual(key, ru);
            Assert.NotEqual(key, en);
            Assert.NotEqual(ru, en);
        }
    }

    private static Dictionary<string, string> ReadLocale(string file)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Data", "Locale", file);
        Assert.True(File.Exists(path), $"Locale file not found: {path}");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("Locale file is empty: " + path);
    }
}
