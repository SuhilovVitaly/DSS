using System.Collections.Immutable;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>Speaker of the battle tutorial; every text is a Locale key.</summary>
internal sealed record TutorialOfficer(string NameKey, string RoleKey, string Portrait, string PortraitKey);

/// <summary>Pacing limits for the comms panel (applied by the quiet rules).</summary>
internal sealed record TutorialLimits(int MinGapMs = 2500, int AutoCollapseMs = 15000, int MaxLinesPerPhase = 2);

/// <summary>Player action that completes a shown step (kebab-case in JSON).</summary>
internal enum TutorialCompletion { PilotSelected, PathEdited, PhaseLeft, TargetChosen, PilotCycled, EvadeOrDecoy, Acknowledged }

/// <summary>Battle situation computed from the visible snapshot (kebab-case in JSON; None = no condition).</summary>
internal enum TutorialSignal { None, UnderFire, OwnDamaged, LowConsciousness, AnomalyPresent, DroneLost, OutcomeVictory, OutcomeDefeat, OutcomeOther }

/// <summary>When a step may be shown. Phase uses the PhasePanel vocabulary: Move, Fire, Salvo, Results, Outcome.</summary>
internal sealed record TutorialCondition(string Phase, int? Turn, int? MinTurn, string? After, TutorialSignal Signal);

internal sealed record TutorialStep(string Id, string LineKey, TutorialCondition When, TutorialCompletion Complete, string? Anchor, int Priority);

/// <summary>Immutable battle tutorial loaded from Data/Tutorial/battle-tutorial.json.</summary>
internal sealed record TutorialScript(int SchemaVersion, TutorialOfficer? Officer, TutorialLimits Limits, ImmutableArray<TutorialStep> Steps)
{
    internal static TutorialScript Empty { get; } = new(1, null, new TutorialLimits(), []);
}
