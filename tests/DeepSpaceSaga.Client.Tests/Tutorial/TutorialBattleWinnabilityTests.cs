using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>
/// EP-0028-US-0005-TK-0002: the tutorial battle (scenario «initial», default loadout, <see cref="SkiaWindow.TutorialBattleSeed"/>)
/// is won by the player's side both by pressing Enter only and by doing what the officer's lessons ask. Orders are built by the
/// same client classes the battle screen uses; the client never touches RNG, so seed + orders fully determine the outcome.
/// </summary>
public sealed class TutorialBattleWinnabilityTests
{
    private const int MaxTurns = 60;
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");

    [Fact]
    public void EnterOnlyPlayerWinsTutorialBattle()
    {
        var result = Play(SkiaWindow.TutorialBattleSeed, followHints: false);
        Report("enter", result);
        AssertWon(result);
        Assert.Equal(result, Play(SkiaWindow.TutorialBattleSeed, followHints: false)); // deterministic
    }

    [Fact]
    public void HintFollowingPlayerWinsTutorialBattle()
    {
        var result = Play(SkiaWindow.TutorialBattleSeed, followHints: true);
        Report("hints", result);
        AssertWon(result);
        // T1 steer and T2 Tab are always possible; retarget and evade/decoy only when the battle offers them (reported).
        Assert.Contains("steer", result.HintActions);
        Assert.Contains("cycle", result.HintActions);
        Assert.Equal(result, Play(SkiaWindow.TutorialBattleSeed, followHints: true)); // deterministic
    }

    internal sealed record Result(long Seed, string Scenario, TacticalBattleOutcomeCode? Outcome, string Reasons, int Turn,
        string Losses, string HintActions);

    private static void AssertWon(Result result)
    {
        Assert.Equal("initial", result.Scenario);
        Assert.True(result.Outcome is not null, $"seed {result.Seed}: no outcome within {MaxTurns} turns");
        Assert.True(result.Outcome == TacticalBattleOutcomeCode.A, $"seed {result.Seed}: outcome {result.Outcome} ({result.Reasons}) on turn {result.Turn}");
    }

    /// <summary>
    /// Plays the tutorial battle on <paramref name="seed"/>. Enter: unedited Move orders (AI tactic) and default Fire targets.
    /// Hints (the actions that close the US-0003 lessons, once each): T1 Move — select the first pilot, turn +5°; T2 Move — Tab
    /// twice (selection only); first Fire — the first pilot takes the first listed target other than the default; the first Fire
    /// with a shot incoming on an own drone — that drone evades, or drops a decoy when it cannot evade. Everything else as Enter.
    /// </summary>
    internal static Result Play(long seed, bool followHints)
    {
        using var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), TutorialPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var started = engine.StartTacticalBattle(new StartTacticalBattleRequest("winnability-start", seed));
        Assert.True(started.Accepted, "tutorial battle start rejected: " + started.Code);
        var state = started.State!;
        var orders = new TacticalBattleOrders();
        var fire = new TacticalBattleFireOrders();
        bool steered = false, cycled = false, retargeted = false, defended = false;
        var actions = new List<string>();
        int step = 0;
        while (state.Outcome is null && state.Turn <= MaxTurns)
        {
            TacticalBattleResponse response;
            if (state.Phase == TacticalBattlePhase.Move)
            {
                orders.Observe(state, selectOnEntry: false); // Tabletop: Move starts without a selected pilot
                if (followHints && !steered && state.Turn == 1)
                {
                    steered = orders.Cycle(false) && orders.Adjust(0, 5);
                    if (steered) actions.Add("steer");
                }
                else if (followHints && !cycled && state.Turn == 2)
                {
                    cycled = orders.Cycle(false) && orders.Cycle(false);
                    if (cycled) actions.Add("cycle");
                }
                response = engine.MoveTacticalBattle(new($"move-{step++}", state.BattleId, state.Revision, orders.EditedOrders()));
            }
            else
            {
                fire.Observe(state, null);
                if (followHints && !retargeted)
                {
                    retargeted = true; // the first Fire lesson, whether or not there is another target to pick
                    var row = fire.Capture().Rows.FirstOrDefault();
                    if (row?.Group.List.FirstOrDefault(c => c.TargetId != row.Group.DefaultTargetId) is { } other &&
                        fire.Select(row.Drone.Id) && fire.Choose(other.TargetId))
                        actions.Add($"retarget@T{state.Turn}");
                }
                if (followHints && !defended &&
                    fire.Capture().Rows.FirstOrDefault(r => state.Incoming.Any(s => s.TargetId == r.Drone.Id)) is { } threatened)
                {
                    defended = threatened.CanEvade ? fire.ToggleEvade(threatened.Drone.Id) : fire.ToggleDecoy(threatened.Drone.Id);
                    if (defended) actions.Add((threatened.CanEvade ? "evade" : "decoy") + $"@T{state.Turn}");
                }
                response = engine.FireTacticalBattle(new($"fire-{step++}", state.BattleId, state.Revision, fire.BuildTargets()));
            }
            Assert.True(response.Accepted, $"seed {seed}, turn {state.Turn} {state.Phase}: rejected {response.Code}");
            state = response.State!;
        }
        string losses = string.Join(", ", state.Losses.Select(l => $"{l.Side}:{l.Count}"));
        string reasons = state.Outcome is null ? "" : string.Join(", ", state.Outcome.Reasons.Select(r => $"{r.Kind}"));
        return new(seed, state.ScenarioId, state.Outcome?.Kind, reasons, state.Turn, losses, string.Join(" ", actions));
    }

    private static void Report(string policy, Result result)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        File.AppendAllText(Path.Combine(root, "winnability.txt"), string.Create(CultureInfo.InvariantCulture,
            $"{policy}: seed={result.Seed} scenario={result.Scenario} outcome={result.Outcome} turn={result.Turn} reasons=[{result.Reasons}] losses=[{result.Losses}] hintActions={result.HintActions}{Environment.NewLine}"));
    }
}
