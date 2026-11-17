namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal sealed class FogOfWarDebugCommand : IDebugCommand
{
    public DebugCommandDescriptor Descriptor { get; } = new("fog_of_war", "fog_of_war [true|false]",
        "Туман войны на карте", "везде, до перезапуска процесса");

    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        bool? enabled = null;
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            if (!bool.TryParse(arguments.Trim(), out bool value))
                return Task.FromResult(DebugCommandResult.Error("Синтаксис: fog_of_war [true|false]"));
            enabled = value;
        }
        return context.Host.ConfigureFogOfWarAsync(enabled, cancellationToken);
    }
}
