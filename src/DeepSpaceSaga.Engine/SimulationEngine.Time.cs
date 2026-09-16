using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private StationDistrict _stationDistrict;
    private readonly HashSet<string> _stationTravelReceipts = new(StringComparer.Ordinal);

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
