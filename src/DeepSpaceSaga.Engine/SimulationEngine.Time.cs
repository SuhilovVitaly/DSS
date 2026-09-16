using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private StationDistrict _stationDistrict;
    private EconomyTimeData _economyTime = new();

    private EconomyTimeData CaptureEconomyTime() => _economyTime with {
        StationDistrict = _stationDistrict,
        TravelReceipts = _stationTravelReceipts.Order(StringComparer.Ordinal).ToArray()
    };
    private readonly HashSet<string> _stationTravelReceipts = new(StringComparer.Ordinal);

    private long NextContractDeadline() => (_economyTime.ActiveContracts ?? [])
        .Where(c => !c.DeadlineMissed && c.DeadlineGameTimeMs > _processedWorldTimeMs)
        .Select(c => c.DeadlineGameTimeMs).DefaultIfEmpty(long.MaxValue).Min();

    private void ApplyContractDeadlines(long time)
    {
        _economyTime = _economyTime with { ActiveContracts = (_economyTime.ActiveContracts ?? []).Select(c =>
        {
            if (c.DeadlineMissed || c.DeadlineGameTimeMs > time) return c;
            RecordShipEvent(PlayerShipObjectId!, "", "contract_deadline_missed", c.ContractId, c.DeadlineGameTimeMs);
            return c with { DeadlineMissed = true };
        }).ToArray() };
    }

    public StationTravelResult TravelStation(StationTravelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (_worldStateLock)
        {
            if (string.IsNullOrWhiteSpace(command.CommandId) || !Enum.IsDefined(command.Destination))
                return new(false, "invalid_transition", CaptureSnapshot());
            if (_stationTravelReceipts.Contains(command.CommandId))
                return new(true, null, CaptureSnapshot());
            var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
            if (ship is not { IsDocked: true, IsDestroyed: false } || _dialogue.Active is not null ||
                _clock.Speed != SimulationSpeed.Speed0)
                return new(false, "station_travel_requires_docked_pause", CaptureSnapshot());
            if (command.Destination == _stationDistrict)
                return new(false, "already_in_district", CaptureSnapshot());

            long targetTime = checked(_clock.GameTimeMs + GameCalendar.HourMs);
            AdvanceWorldTo(targetTime);
            _clock.Reset(targetTime, SimulationSpeed.Speed0);
            _stationDistrict = command.Destination;
            _stationTravelReceipts.Add(command.CommandId);
            return new(true, null, CaptureSnapshot());
        }
    }
}
