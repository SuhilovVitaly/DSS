using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal sealed class LootStateDebugCommand : IDebugCommand
{
    public DebugCommandDescriptor Descriptor { get; } = new("loot_state", "loot_state", "Источники, остатки, попытки и свободная масса", "только чтение");
    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken token)
    {
        if (!string.IsNullOrWhiteSpace(arguments)) return Task.FromResult(DebugCommandResult.Error("Синтаксис: loot_state"));
        var loot = context.Session?.DemoCampaign?.Rewards?.Loot;
        if (loot is not { Phase: LootPhases.Open }) return Task.FromResult(DebugCommandResult.Ok("Нет открытого сбора"));
        var lines = new List<string> { loot.BattleId + " · свободно " + loot.FreeCargoKg + " кг" };
        foreach (var source in loot.Objects)
        {
            lines.Add(source.ObjectId + " · " + source.Kind);
            lines.AddRange(source.Consumables.Select(c => "  " + c.ItemTypeId + " × " + c.Remaining));
            if (source.Scrap is { } scrap) lines.Add("  лом: " + scrap.State + " · " + scrap.Quantity);
            lines.AddRange(source.Modules.Select(m => "  " + m.InstanceId + " · " + m.State));
            if (source.Pilot is { } pilot) lines.Add("  пилот: " + pilot.SourcePilotId + " · captured=" + pilot.Captured);
        }
        return Task.FromResult(DebugCommandResult.Ok(lines.ToArray()));
    }
}
