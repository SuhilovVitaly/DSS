using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal sealed class EngineerBonusDebugCommand : IDebugCommand
{
    public DebugCommandDescriptor Descriptor { get; } = new("engineer_bonus", "engineer_bonus [auto|0|10..15]", "Бонус инженера: навык или временная подмена", "активная сессия, до перезапуска процесса");
    public Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken token)
    {
        string value = arguments.Trim();
        if (value.Length == 0)
        {
            if (context.Session?.DemoCampaign is not { } campaign) return Task.FromResult(DebugCommandResult.Error("Нет активной сессии"));
            int persisted = context.Session.CarrierOperations?.Crew.FirstOrDefault(c => c.Role == CrewRole.Engineer)?.SalvageBonusPercentagePoints ?? 0;
            return Task.FromResult(DebugCommandResult.Ok(Describe(campaign.DebugSettings ?? new(), persisted)));
        }
        int? bonus = null;
        if (value != "auto" && (!int.TryParse(value, out int parsed) || parsed is not (0 or >= 10 and <= 15)))
            return Task.FromResult(DebugCommandResult.Error("Синтаксис: " + Descriptor.Syntax));
        if (value != "auto") bonus = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        return Apply(context, settings => settings with { EngineerBonusPp = bonus }, token);
    }
    internal static string Describe(DemoDebugSettings settings, int persisted) => "engineer_bonus = " +
        (settings.EngineerBonusPp ?? persisted) + " п.п. · " + (settings.EngineerBonusPp is null ? "навык" : "override");
    internal static Task<DebugCommandResult> Apply(DebugCommandContext context, Func<DemoDebugSettings, DemoDebugSettings> change, CancellationToken token) =>
        context.Host.RunExclusiveAsync(async ct =>
        {
            if (!context.IsCurrent || context.Session?.DemoCampaign is not { } campaign) return DebugCommandResult.Error("Нет активной сессии");
            if (context.Host.ActiveBattle is { } battle && (battle.Flow.Pending || battle.TurnBusy || battle.HasSalvoPresentation)) return DebugCommandResult.Busy();
            var settings = change(campaign.DebugSettings ?? new());
            var response = await context.Session.ExecuteDemoCampaignAsync(new SetLootDebugRequest(Guid.NewGuid().ToString("N"), campaign.Revision,
                settings.EngineerBonusPp, settings.DestructionRolls, settings.LootAttempts), ct);
            if (!context.IsCurrent) return DebugCommandResult.Error("Сессия изменилась");
            return response.Applied ? DebugCommandResult.Ok("Применено: engineer_bonus=" + (settings.EngineerBonusPp?.ToString() ?? "auto") +
                "; rolls=" + settings.DestructionRolls + "; attempts=" + settings.LootAttempts) : DebugCommandResult.Error("Отказ: " + response.Reason);
        }, token);
}
