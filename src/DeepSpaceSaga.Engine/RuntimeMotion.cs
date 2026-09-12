using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

internal static class RuntimeMotion
{
    private static readonly LinearMotionPredictor Predictor = new();

    internal static ObjectMotionSnapshot At(SpaceObjectRuntime obj, long gameTimeMs)
    {
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
