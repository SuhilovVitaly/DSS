using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public class ApproachRouteProjectionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2.9999)]
    [InlineData(5)]
    public void Preview_uses_the_exact_route_and_ends_on_the_target_line(double targetSpeed)
    {
        var ship = new ObjectMotionSnapshot("ship", -1000, 500, 3, 270,
            ActiveEngineCommandType: NavigationComputerCommandTypes.Approach,
            NavigationPhase: ApproachLineCaptureMath.Phase);
        var route = ApproachLineCaptureMath.Plan(ship, 0, 0, 90, targetSpeed, 10, 4)!;
        ship = ship with { ApproachRoute = route };
        var projector = new NavigationTrajectoryProjector();
        var points = projector.Project(ship, out bool confirmed, out var endpoint);
        var expected = ApproachLineCaptureMath.Predict(ship, route.DurationMs);
        Assert.True(confirmed);
        Assert.Equal(ship.X, points[0].X);
        Assert.Equal(ship.Y, points[0].Y);
        Assert.Equal(expected.X, endpoint.X, 7);
        Assert.Equal(expected.Y, endpoint.Y, 7);
        Assert.Equal(endpoint, points[^1]);
        Assert.InRange(Math.Abs(endpoint.Y), 0, .001);
        Assert.InRange(points.Count, 2, 801);

        var mid = new LinearMotionPredictor().Predict(ship, 1234);
        var remaining = projector.Project(mid, out _, out var sameEndpoint);
        Assert.Equal(mid.X, remaining[0].X);
        Assert.Equal(mid.Y, remaining[0].Y);
        Assert.Equal(endpoint.X, sameEndpoint.X, 7);
        Assert.Equal(endpoint.Y, sameEndpoint.Y, 7);
    }
}
