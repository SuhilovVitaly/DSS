using System.Globalization;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal sealed class PirateSquadChanceDebugCommand : IDebugCommand
{
    public DebugCommandDescriptor Descriptor { get; } = new("pirate_squad_chance", "pirate_squad_chance [0..100]",
        "Вероятность эскадры в следующей новой кампании", "везде, до перезапуска процесса");

    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        int? percent = null;
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            if (!int.TryParse(arguments.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value is < 0 or > 100)
                return Task.FromResult(DebugCommandResult.Error("Синтаксис: pirate_squad_chance [целое 0..100]"));
            percent = value;
        }
        return context.Host.ConfigurePirateSquadChanceAsync(percent, cancellationToken);
    }
}
