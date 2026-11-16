using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>Outcome of matching a <c>kill_drone</c> name against the confirmed battle state.</summary>
internal enum KillTargetStatus { Found, NotFound, Ambiguous, Carrier, AlreadyDestroyed }

/// <summary>Resolved target (for Found/Carrier/AlreadyDestroyed) or the distinct matching unit ids (Ambiguous).</summary>
internal sealed record KillTargetResolution(KillTargetStatus Status, TacticalUnitSnapshot? Unit, IReadOnlyList<string> MatchIds);

/// <summary>
/// <c>kill_drone &lt;имя&gt;</c>: authoritatively destroys one drone of either side in the current battle (EP-0030 US-0003).
/// The name is the whole trimmed argument and matches, Ordinal and case-sensitive, a unit Id or its map label
/// (<c>Build?.PilotName ?? Id</c>, G-04). Ambiguous names and carriers never send a request.
/// </summary>
internal sealed class KillDroneDebugCommand : IDebugCommand
{
    internal const string SyntaxText = "Синтаксис: kill_drone <имя>";
    internal const string NoBattleText = "Нет активного боя";
    internal const string BattleOverText = "Бой уже завершён";
    internal const string CarrierText = "kill_drone не применяется к носителям";
    internal const string AlreadyDestroyedText = "Цель уже уничтожена";
    internal const string BattleEndedText = "Бой завершён";

    public DebugCommandDescriptor Descriptor { get; } =
        new("kill_drone", "kill_drone <имя>", "Уничтожить дрона любой стороны по Id или имени", "текущий бой");

    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        string name = arguments?.Trim() ?? "";
        if (name.Length == 0) return Task.FromResult(DebugCommandResult.Error(SyntaxText));
        return context.Host.RunExclusiveAsync(_ => KillAsync(context, name), cancellationToken);
    }

    private static async Task<DebugCommandResult> KillAsync(DebugCommandContext context, string name)
    {
        var battle = context.Host.ActiveBattle;
        if (battle is null || battle.IsSetup || context.Session is null || !ReferenceEquals(battle.Flow.Session, context.Session))
            return DebugCommandResult.Error(NoBattleText);
        // A request in flight, a movement animation or a displayed salvo would otherwise be overwritten by a stale state.
        if (battle.Flow.Pending || battle.TurnBusy || battle.HasSalvoPresentation) return DebugCommandResult.Busy();
        if (battle.Flow.State is not { } state) return DebugCommandResult.Error(NoBattleText);
        if (state.Phase == TacticalBattlePhase.Over) return DebugCommandResult.Error(BattleOverText);

        var resolution = Resolve(state, name);
        switch (resolution.Status)
        {
            case KillTargetStatus.Ambiguous:
                return DebugCommandResult.Error("Неоднозначное имя: " + name + " — совпадения: " + string.Join(", ", resolution.MatchIds));
            case KillTargetStatus.NotFound:
                return DebugCommandResult.Error("Цель не найдена: " + name);
            case KillTargetStatus.Carrier:
                return DebugCommandResult.Error(CarrierText);
            case KillTargetStatus.AlreadyDestroyed:
                return DebugCommandResult.Error(AlreadyDestroyedText);
        }

        var target = resolution.Unit!;
        var response = await battle.Flow.DebugDestroyAsync(
            new(TacticalBattleFlow.NewIdentity("debug-kill"), state.BattleId, state.Revision, target.Id));
        if (response is null) return DebugCommandResult.Busy();
        if (!response.Accepted) return DebugCommandResult.Error("Отказ: " + GameSessionHandle.TacticalBattleRefusalText(response.Code));

        battle.ShowConfirmedState();
        string destroyed = "Уничтожен: " + PilotCardModel.Name(target) + " (" + target.Id + ")";
        return response.State?.Phase == TacticalBattlePhase.Over || battle.Flow.State?.Phase == TacticalBattlePhase.Over
            ? DebugCommandResult.Ok(destroyed, BattleEndedText)
            : DebugCommandResult.Ok(destroyed);
    }

    /// <summary>Exact Ordinal match of the trimmed name on unit Id or map label; several distinct units are ambiguous.</summary>
    internal static KillTargetResolution Resolve(TacticalBattleSnapshot state, string name)
    {
        ArgumentNullException.ThrowIfNull(state);
        string trimmed = name?.Trim() ?? "";
        List<TacticalUnitSnapshot> matches = trimmed.Length == 0 ? [] : state.Units
            .Where(unit => string.Equals(unit.Id, trimmed, StringComparison.Ordinal) ||
                string.Equals(PilotCardModel.Name(unit), trimmed, StringComparison.Ordinal))
            .DistinctBy(unit => unit.Id, StringComparer.Ordinal)
            .ToList();
        if (matches.Count == 0) return new(KillTargetStatus.NotFound, null, []);
        if (matches.Count > 1) return new(KillTargetStatus.Ambiguous, null, matches.Select(unit => unit.Id).ToList());
        var unit = matches[0];
        var status = unit.Kind == TacticalUnitKind.Carrier ? KillTargetStatus.Carrier
            : unit.State == TacticalUnitState.Destroyed ? KillTargetStatus.AlreadyDestroyed
            : KillTargetStatus.Found;
        return new(status, unit, [unit.Id]);
    }
}
