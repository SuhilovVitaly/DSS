namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal sealed class LootForceDebugCommand : IDebugCommand
{
    public DebugCommandDescriptor Descriptor { get; } = new("loot_force", "loot_force rolls|attempts auto|keep|destroy|success|fail", "Подменить будущие броски лута", "активная сессия, до перезапуска процесса");
    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken token)
    {
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !(parts[0] == "rolls" && parts[1] is "auto" or "keep" or "destroy" || parts[0] == "attempts" && parts[1] is "auto" or "success" or "fail"))
            return Task.FromResult(DebugCommandResult.Error("Синтаксис: " + Descriptor.Syntax));
        return EngineerBonusDebugCommand.Apply(context, settings => parts[0] == "rolls"
            ? settings with { DestructionRolls = parts[1] } : settings with { LootAttempts = parts[1] }, token);
    }
}
