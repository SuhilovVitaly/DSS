namespace DeepSpaceSaga.Contracts;

/// <summary>Confirmed sensor coverage in world units (100 metres each).</summary>
public sealed record CarrierSensorSnapshot(string ModuleId, double RadiusWorldUnits, bool IsOperational);

/// <summary>Directional detection at an observed physical timestamp, independent of debug visibility.</summary>
public sealed record SensorContactSnapshot(string ObserverId, string TargetId, bool IsDetected, long ObservedMotionTimeMs);
