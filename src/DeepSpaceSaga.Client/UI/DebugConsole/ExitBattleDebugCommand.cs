namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary><c>exit_battle</c>: aborts the tutorial battle of the session the command was dispatched in and opens its map.</summary>
internal sealed class ExitBattleDebugCommand : IDebugCommand
{
    internal const string NoTutorialBattleText = "Нет активного обучающего боя";
    internal const string SyntaxText = "Синтаксис: exit_battle";

    public DebugCommandDescriptor Descriptor { get; } =
        new("exit_battle", "exit_battle", "Прервать обучающий бой и открыть карту", "обучающий бой");

    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.IsNullOrWhiteSpace(arguments)) return Task.FromResult(DebugCommandResult.Error(SyntaxText));
        return context.Host.ExitTutorialBattleAsync(context.Session, cancellationToken);
    }
}
