namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Metadata for one playable scenario, as listed in the client's New Game -&gt; scenario
/// picker. Plain JSON-serializable DTO — no graphics dependency, same shape for
/// in-process and (future) network implementations. Mirrors <see cref="SaveSlotInfo"/>.
/// </summary>
/// <param name="ScenarioPath">
/// Full path to the scenario's "scenario.json" file — passed back unchanged to
/// <c>IGameSessionFactory.CreateSessionFromScenario</c> when the player picks this row.
/// </param>
public sealed record ScenarioInfo(
    string ScenarioPath,
    string ScenarioId,
    string Name,
    string Description);
