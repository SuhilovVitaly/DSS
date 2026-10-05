using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SeededBeltAsteroidsTests
{
    private static ScenarioFile Generate(SolarSystemGenerationConfig? config = null) => SolarSystemGenerator.Generate(
        SeededWorldBootstrapTests.Scenario(), config ?? GenerationInputSchemaTests.Config(), SeededWorldBootstrapTests.Registry(), 123);

    [Fact]
    public void BeltSeedAndOrbitContainment()
    {
        var source = Generate();
        Assert.Equal(ScenarioLoader.Serialize(source), ScenarioLoader.Serialize(Generate()));
        foreach (var belt in source.GameState.SolarSystem!.Belts)
        {
            string prefix = $"SYS-AST-{belt.Id[9..]}-";
            var asteroids = source.GameState.SpaceObjects.Where(o => o.ObjectId.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            Assert.Equal(12, asteroids.Length);
            foreach (var asteroid in asteroids)
            {
                Assert.Equal("Permanent", asteroid.PersistenceType); Assert.Equal("Asteroid", asteroid.ObjectType);
                Assert.True(asteroid.IsKnown);
                Assert.InRange(asteroid.MassKg!.Value, 1000000, 1000000000);
                Assert.Contains(asteroid.CompositionType, new[] { "Ice", "Silicate", "Iron" });
                foreach (long time in new long[] { 0, 1000, 1000000, long.MaxValue })
                {
                    var pose = OrbitalMotionMath.At(new(asteroid.ObjectId, 0, 0, 0, 0), asteroid.Orbit!, time);
                    Assert.InRange(Math.Sqrt(pose.X * pose.X + pose.Y * pose.Y), belt.InnerRadius, belt.OuterRadius);
                }
            }
            // At least one 85-degree empty sector survives in the generated phase distribution.
            var phases = asteroids.Select(o => o.Orbit!.InitialPhase + o.Orbit.PhaseOffsetDegrees).Order().ToArray();
            double maxGap = phases.Zip(phases.Skip(1)).Max(p => p.Second - p.First);
            maxGap = Math.Max(maxGap, phases[0] + 360 - phases[^1]);
            Assert.True(maxGap >= 85);
        }
    }

    [Fact]
    public void DecorationDoesNotCreateEntities()
    {
        var a = Generate(GenerationInputSchemaTests.Config() with { DecorationSamplesPerBelt = 2048 });
        var b = Generate(GenerationInputSchemaTests.Config() with { DecorationSamplesPerBelt = 8192 });
        Assert.Equal(ScenarioLoader.Serialize(a), ScenarioLoader.Serialize(b));
    }

    [Fact]
    public void TemporaryScenarioAsteroidsPreserved()
    {
        var source = SeededWorldBootstrapTests.Scenario();
        var generated = Generate();
        var before = source.GameState.SpaceObjects.Where(o => o.PersistenceType == "Temporary").ToArray();
        var after = generated.GameState.SpaceObjects.Where(o => o.PersistenceType == "Temporary").ToArray();
        Assert.Equal(before.Length, after.Length);
        foreach (var asteroid in before)
        {
            var actual = after.Single(o => o.ObjectId == asteroid.ObjectId);
            Assert.Equal(asteroid.SpeedMps, actual.SpeedMps); Assert.Equal(asteroid.DirectionDegrees, actual.DirectionDegrees);
            Assert.Equal(asteroid.MovementType, actual.MovementType); Assert.Null(actual.Orbit);
        }
    }
}
