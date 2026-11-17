using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record PiratePoint(double X, double Y);
public sealed record PirateCarrierData(string CarrierId, string SignatureTypeId, ShipModuleData Sensor,
    bool IsDestroyed = false);
public sealed record PirateGroupData(string GroupId, double X, double Y, double Direction, double SpeedKmS,
    ImmutableArray<PirateCarrierData> Members, ImmutableArray<PiratePoint> PatrolPoints, int NextPatrolPoint = 1,
    string Phase = "patrol", PiratePoint? LastObservedPlayer = null, bool Retired = false,
    int? ApproximateDroneCount = null, bool ReportConsumed = false);
public sealed record PirateWorldData(int Version, double CapturedPlayerMaxSpeedKmS, double AccelerationKmS2,
    double TurnRateDegreesPerSecond, long LastMotionTimeMs, ImmutableArray<PirateGroupData> Groups);
