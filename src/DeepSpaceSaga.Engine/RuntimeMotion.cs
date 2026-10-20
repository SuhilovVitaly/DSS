using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

internal static class RuntimeMotion
{
    private static readonly LinearMotionPredictor Predictor = new();

    internal static ObjectMotionSnapshot At(SpaceObjectRuntime obj, long gameTimeMs)
    {
        if (obj.InitialMotion.Evasion is { } evasion)
            return CurrentManeuverForecast.Sample(obj.InitialMotion, Math.Max(0, gameTimeMs - evasion.SampleMotionTimeMs));
        if (obj.InitialMotion.TurnHoldRateDegPerSec is not null || obj.InitialMotion.OwnProjectile is not null || obj.InitialMotion.ObservedProjectile is not null)
            return CurrentManeuverForecast.Sample(obj.InitialMotion, Math.Max(0, gameTimeMs - obj.StartGameTimeMs));
        if (obj.InitialMotion.Orbit is { } orbit)
            return OrbitalMotionMath.At(obj.InitialMotion, orbit, gameTimeMs);
        foreach (var module in obj.Modules)
        {
            var cycle = module.ActiveCycle;
            if (cycle?.CommandType == NavigationComputerCommandTypes.Approach && cycle.ApproachRoute is { } route)
            {
                var predicted = ApproachLineCaptureMath.Predict(obj.InitialMotion with { ApproachRoute = route },
                    Math.Max(0, gameTimeMs - cycle.StartedGameTimeMs));
                return predicted with { ApproachRoute = null, ActiveEngineCommandType = null, NavigationPhase = null };
            }
        }
        return Predictor.Predict(obj.InitialMotion, gameTimeMs - obj.StartGameTimeMs);
    }
}
