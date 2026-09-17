using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static long? ResolveNextPortFee(SpaceObjectData ship, long time)
    {
        if (!ship.IsDocked) return null;
        if (ship.NextPortFeeDueGameTimeMs is { } next) return next;
        long first = ship.FirstPortFeeGameTimeMs ?? time;
        return checked(first + ((time - first) / GameCalendar.DayMs + 1) * GameCalendar.DayMs);
    }

    private long NextPortFeeTime() => _objects
        .Where(o => o.InitialMotion.ObjectId == PlayerShipObjectId
            && o.IsDocked && !o.IsDestroyed && o.NextPortFeeDueGameTimeMs is not null
            && _objects.Any(station => station.InitialMotion.ObjectId == o.DockedStationObjectId))
        .Select(o => o.NextPortFeeDueGameTimeMs!.Value).DefaultIfEmpty(long.MaxValue).Min();

    private PortFeeSnapshot? BuildPortFeeSnapshot()
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        return ship is { IsDocked: true, FirstPortFeeGameTimeMs: { } first, NextPortFeeDueGameTimeMs: { } next }
            ? new(first, next, ship.PortFeeDebt) : null;
    }

    private void RenewPortFees(long time)
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            var ship = _objects[i];
            if (ship.InitialMotion.ObjectId != PlayerShipObjectId || !ship.IsDocked || ship.IsDestroyed ||
                ship.NextPortFeeDueGameTimeMs is not { } due || due > time) continue;
            int stationIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == ship.DockedStationObjectId);
            if (stationIndex < 0) continue;
            var station = _objects[stationIndex];
            long fee = station.PortFeeCreditsPerDay ?? 0;
            long paid = Math.Min(PlayerCredits, fee);
            long debt = checked(ship.PortFeeDebt + fee - paid);
            long stationCredits = checked(station.Credits + paid);
            long next = checked(due + GameCalendar.DayMs);
            PlayerCredits -= paid;
            _objects[stationIndex] = station with { Credits = stationCredits };
            _objects[i] = ship with { PortFeeDebt = debt, NextPortFeeDueGameTimeMs = next };
            RecordShipEvent(ship.InitialMotion.ObjectId, "", "port_fee_renewed",
                paid < fee ? "port_fee_debt" : null, due);
        }
    }
}
