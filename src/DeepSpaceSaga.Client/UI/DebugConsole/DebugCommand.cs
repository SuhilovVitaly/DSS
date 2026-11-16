using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>Registry entry shown by help: unique Ordinal name, syntax, description and applicability.</summary>
internal sealed record DebugCommandDescriptor(string Name, string Syntax, string Description, string Scope);

/// <summary>Output lines of one console command; <see cref="CloseConsole"/> closes the console after a published result.</summary>
internal sealed record DebugCommandResult(IReadOnlyList<string> Lines, bool IsError = false, bool CloseConsole = false)
{
    internal const string BusyText = "Выполняется переход или ход боя — повторите позже";

    public static DebugCommandResult Ok(params string[] lines) => new(lines);
    public static DebugCommandResult Error(string line) => new([line], IsError: true);
    public static DebugCommandResult Busy() => Error(BusyText);
}

/// <summary>One console command. Mutations go through the session/flow, never directly to Engine.</summary>
internal interface IDebugCommand
{
    DebugCommandDescriptor Descriptor { get; }
    Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken);
}

/// <summary>Window services available to console commands (implemented explicitly by <see cref="SkiaWindow"/>).</summary>
internal interface IDebugConsoleHost
{
    GameSessionHandle? Session { get; }
    /// <summary>True while a screen transition holds the window transition lock.</summary>
    bool IsTransitionBusy { get; }
    TacticalBattleScreen? ActiveBattle { get; }
    /// <summary>Runs the operation under the transition lock without waiting; a held lock yields <see cref="DebugCommandResult.Busy"/>.</summary>
    Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken);
    /// <summary>
    /// Aborts the active tutorial battle of <paramref name="expectedSession"/> through the regular exit/settlement path
    /// and shows its map (or the ship-destroyed screen); answers busy while a transition or a battle request is in flight.
    /// </summary>
    Task<DebugCommandResult> ExitTutorialBattleAsync(GameSessionHandle? expectedSession, CancellationToken cancellationToken);
}

/// <summary>Per-submission context; the session is captured at dispatch so a result after a session change is detected as stale.</summary>
internal sealed class DebugCommandContext(DebugCommandRegistry registry, IDebugConsoleHost host)
{
    public DebugCommandRegistry Registry { get; } = registry;
    public IDebugConsoleHost Host { get; } = host;
    public GameSessionHandle? Session { get; } = host.Session;
    public bool IsCurrent => ReferenceEquals(Session, Host.Session);
}
