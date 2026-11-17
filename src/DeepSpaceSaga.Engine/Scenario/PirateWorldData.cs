using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record PiratePoint(double X, double Y);
public sealed record PirateMotionPlan(ObjectMotionSnapshot Origin, long StartMs, long EndMs);
public sealed record PirateCarrierData(string CarrierId, string SignatureTypeId, ShipModuleData Sensor, ShipModuleData Engine,
    bool IsDestroyed = false);
public sealed record PirateGroupData(string GroupId, double X, double Y, double Direction, double SpeedKmS,
    ImmutableArray<PirateCarrierData> Members, ImmutableArray<PiratePoint> PatrolPoints, int NextPatrolPoint = 1,
    string Phase = "patrol", PiratePoint? LastObservedPlayer = null, bool Retired = false,
    int? ApproximateDroneCount = null, bool ReportConsumed = false, bool CompositionMaterialized = false,
    long ContactEpoch = 0, string? LastPursuitEndReason = null, double? CapturedMotionTimeMs = null,
    PirateMotionPlan? MotionPlan = null);
public sealed record PirateWorldData(int Version, double CapturedPlayerMaxSpeedKmS, double AccelerationKmS2,
    double TurnRateDegreesPerSecond, long LastMotionTimeMs, ImmutableArray<PirateGroupData> Groups,
    PiratePoint? LastPlayerPosition = null);
