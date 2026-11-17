using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

internal static class AppDebugOverrides
{
    internal static async Task<DebugCommandResult> FogAsync(bool? enabled,
        Func<bool?, DemoDebugSettings> configure, long? activeRevision,
        Func<DemoCampaignCommand, CancellationToken, Task<DemoCampaignResponse>>? execute,
        CancellationToken cancellationToken)
    {
        try
        {
            if (enabled is { } value && execute is not null)
            {
                if (activeRevision is null) return DebugCommandResult.Error("Состояние кампании ещё недоступно");
                var response = await execute(new SetFogOfWarRequest(Guid.NewGuid().ToString("N"), activeRevision.Value, value), cancellationToken);
                if (!response.Applied) return DebugCommandResult.Error("fog_of_war: " + response.Reason);
                // Engine has committed the process setting. Never acknowledge a rejected request.
                return DebugCommandResult.Ok("fog_of_war = " + value.ToString().ToLowerInvariant());
            }
            return DebugCommandResult.Ok("fog_of_war = " + configure(enabled).FogOfWar.ToString().ToLowerInvariant());
        }
        catch (NotSupportedException) { return DebugCommandResult.Error("Настройка демо недоступна"); }
    }
}
