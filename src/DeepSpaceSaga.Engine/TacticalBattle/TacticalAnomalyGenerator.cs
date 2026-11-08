using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal static class TacticalAnomalyGenerator
{
    public static ImmutableArray<TacticalAnomaly> Generate(TacticalBattleCatalog catalog, long seed,
        IReadOnlyList<TacticalDrone> drones, IReadOnlyList<TacticalCarrier> carriers)
    {
        var random = new Mulberry32(unchecked((uint)RngStreamSeedDerivation.DeriveStreamSeed(unchecked((ulong)seed), "tactical-anomalies")));
        var rules = catalog.Rules.Anomalies;
        int heatCount = random.RandInt(rules.HeatCount[0], rules.HeatCount[1]);
        int dustCount = random.RandInt(rules.DustCount[0], rules.DustCount[1]);
        double xA = drones.Where(d => d.Side == "A").Average(d => d.Pose.X);
        double xB = drones.Where(d => d.Side == "B").Average(d => d.Pose.X);
        double inset = (1 - rules.BandFraction) / 2.0 * (xB - xA);
        double x0 = xA + inset, x1 = xB - inset;
        double y0 = drones.Min(d => d.Pose.Y), y1 = drones.Max(d => d.Pose.Y);
        var starts = drones.Cast<TacticalUnit>().Concat(carriers).Select(u => u.Pose).ToArray();
        var result = ImmutableArray.CreateBuilder<TacticalAnomaly>();
        GenerateKind(TacticalAnomalyKind.Heat, heatCount, "heat");
        GenerateKind(TacticalAnomalyKind.Dust, dustCount, "dust");
        return result.ToImmutable();

        void GenerateKind(TacticalAnomalyKind kind, int count, string prefix)
        {
            for (int i = 0; i < count; i++)
            {
                bool found = false; double x = 0, y = 0;
                for (int attempt = 0; attempt < rules.MaxAttempts; attempt++)
                {
                    x = random.Uniform(x0, x1); y = random.Uniform(y0, y1);
                    if (starts.Any(p => TacticalGeometry.Dist(x, y, p.X, p.Y) < rules.Clearance)) continue;
                    found = true; break;
                }
                if (!found) continue;
                double spread = Range(rules.Spread);
                int clumpCount = random.RandInt(rules.Clumps[0], rules.Clumps[1]);
                var clumps = ImmutableArray.CreateBuilder<TacticalClump>(clumpCount);
                for (int c = 0; c < clumpCount; c++)
                {
                    double radius = Math.Sqrt(-2 * Math.Log(1 - random.Random()));
                    var (sin, cos) = Math.SinCos(2 * Math.PI * random.Random());
                    double cx = x + radius * cos * spread * rules.CenterSpread[0];
                    double cy = y + radius * sin * spread * rules.CenterSpread[1];
                    double amplitude = Range(rules.ClumpAmp);
                    double sigmaX = spread * Range(rules.ClumpSigmaX), sigmaY = spread * Range(rules.ClumpSigmaY);
                    double rotation = random.Uniform(0, Math.PI);
                    clumps.Add(new(cx, cy, amplitude, sigmaX, sigmaY, rotation));
                }
                result.Add(new(FormattableString.Invariant($"{prefix}-{i + 1}"), kind, clumps.MoveToImmutable())
                { CenterX = x, CenterY = y });
            }
        }
        double Range(ImmutableArray<double> range) => random.Uniform(range[0], range[1]);
    }
}
