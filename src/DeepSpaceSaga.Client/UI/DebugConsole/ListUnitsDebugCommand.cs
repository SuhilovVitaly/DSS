using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>
/// <c>list_units</c>: reads one authoritative tactical snapshot through the session and lists every unit of both sides.
/// Read-only: no battle request, speed change, transition or Engine access.
/// </summary>
internal sealed class ListUnitsDebugCommand : IDebugCommand
{
    internal const string SyntaxText = "Синтаксис: list_units";
    internal const string NoSessionText = "Нет активной сессии";
    internal const string NoBattleText = "Нет активного боя или данные недоступны";
    internal const string NoUnitsText = "Участников нет";
    internal const string NoPilotText = "—";

    public DebugCommandDescriptor Descriptor { get; } =
        new("list_units", "list_units", "Показать всех участников текущего боя", "текущий бой");

    public async Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.IsNullOrWhiteSpace(arguments)) return DebugCommandResult.Error(SyntaxText);
        if (context.Session is not { } session) return DebugCommandResult.Error(NoSessionText);

        // The handle maps read failures to null, so null is "no battle or unavailable", never a proven absence.
        var snapshot = await session.GetTacticalBattleStateAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.IsCurrent) return DebugCommandResult.Error(DebugConsoleController.StaleResultText);
        if (snapshot is null) return DebugCommandResult.Error(NoBattleText);
        return DebugCommandResult.Ok([.. Format(snapshot)]);
    }

    /// <summary>Snapshot identity header, then every confirmed unit (<see cref="TacticalBattleSnapshot.Units"/>) sorted by Side, then Id (Ordinal).</summary>
    internal static IReadOnlyList<string> Format(TacticalBattleSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var lines = new List<string>(snapshot.Units.Length + 2)
        {
            $"Бой: {snapshot.BattleId} | ревизия: {snapshot.Revision} | ход: {snapshot.Turn} | фаза: {snapshot.Phase}",
        };
        if (snapshot.Units.IsDefaultOrEmpty)
        {
            lines.Add(NoUnitsText);
            return lines;
        }
        lines.Add($"Участников: {snapshot.Units.Length}");
        foreach (var unit in snapshot.Units.OrderBy(u => u.Side, StringComparer.Ordinal).ThenBy(u => u.Id, StringComparer.Ordinal))
        {
            string pilot = unit.Build?.PilotName is { } name && !string.IsNullOrWhiteSpace(name) ? name : NoPilotText;
            lines.Add($"{unit.Id} | пилот: {pilot} | сторона: {unit.Side} | тип: {unit.Kind} | состояние: {unit.State} | корпус: {unit.Hull}/{unit.MaxHull}");
        }
        return lines;
    }
}
