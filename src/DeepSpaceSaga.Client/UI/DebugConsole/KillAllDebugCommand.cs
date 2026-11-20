using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal sealed class KillAllDebugCommand : IDebugCommand
{
    public DebugCommandDescriptor Descriptor { get; } =
        new("kill", "kill all", "Уничтожить вражеские носители и отключить их дроны", "текущий бой");

    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        if (!string.Equals(arguments.Trim(), "all", StringComparison.Ordinal))
            return Task.FromResult(DebugCommandResult.Error("Синтаксис: kill all"));
        return context.Host.RunExclusiveAsync(_ => KillAsync(context), cancellationToken);
    }

    private static async Task<DebugCommandResult> KillAsync(DebugCommandContext context)
    {
        var battle = context.Host.ActiveBattle;
        if (battle is null || battle.IsSetup || context.Session is null ||
            !ReferenceEquals(battle.Flow.Session, context.Session) || battle.Flow.State is not { } state)
            return DebugCommandResult.Error(KillDroneDebugCommand.NoBattleText);
        if (battle.Flow.Pending || battle.TurnBusy || battle.HasSalvoPresentation) return DebugCommandResult.Busy();
        if (state.Phase == TacticalBattlePhase.Over) return DebugCommandResult.Error(KillDroneDebugCommand.BattleOverText);

        var response = await battle.Flow.DebugDestroyAsync(
            new(TacticalBattleFlow.NewIdentity("debug-kill-all"), state.BattleId, state.Revision, "") { AllEnemies = true });
        if (response is null) return DebugCommandResult.Busy();
        if (!response.Accepted) return DebugCommandResult.Error("Отказ: " + GameSessionHandle.TacticalBattleRefusalText(response.Code));

        battle.ShowConfirmedState();
        return DebugCommandResult.Ok(KillDroneDebugCommand.BattleEndedText);
    }
}
