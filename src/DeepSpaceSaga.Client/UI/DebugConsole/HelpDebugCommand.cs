namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>
/// <c>help</c>: lists every registered console command (name, syntax, description, scope) straight from the registry.
/// Works without a session and never touches the host or the game state.
/// </summary>
internal sealed class HelpDebugCommand : IDebugCommand
{
    internal const string SyntaxText = "Синтаксис: help";

    public DebugCommandDescriptor Descriptor { get; } =
        new("help", "help", "Показать все команды консоли", "везде");

    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.IsNullOrWhiteSpace(arguments)) return Task.FromResult(DebugCommandResult.Error(SyntaxText));
        return Task.FromResult(DebugCommandResult.Ok([.. Format(context.Registry.DescribeAll())]));
    }

    /// <summary>A header plus one untruncated line per descriptor, in the given (registry Ordinal) order.</summary>
    internal static IReadOnlyList<string> Format(IReadOnlyList<DebugCommandDescriptor> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var lines = new List<string>(descriptors.Count + 1) { $"Команды консоли: {descriptors.Count}" };
        foreach (var d in descriptors)
            lines.Add($"{d.Name} | синтаксис: {d.Syntax} | {d.Description} | область: {d.Scope}");
        return lines;
    }
}
