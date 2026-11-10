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
        AddPeriphery();
        return result.ToImmutable();

        void GenerateKind(TacticalAnomalyKind kind, int count, string prefix)
        {
            for (int i = 0; i < count; i++)
                TryPlace(kind, FormattableString.Invariant($"{prefix}-{i + 1}"), x0, x1, y0, y1, 0);
        }

        void AddPeriphery()
        {
            if (heatCount + dustCount == 0) return;
            double margin = rules.Spread[1];
            double left = starts.Min(p => p.X) - margin;
            double cellWidth = (starts.Max(p => p.X) + margin - left) / 3;
            double spacing = rules.Spread[0] * 2;
            for (int row = 0; row < 2; row++)
            {
                double top = row == 0 ? y0 - 3 * margin : y1 + 2 * margin;
                for (int column = 0; column < 3; column++)
                {
                    bool heat = heatCount > 0 && (dustCount == 0 || (row + column) % 2 == 0);
                    var kind = heat ? TacticalAnomalyKind.Heat : TacticalAnomalyKind.Dust;
                    double cellLeft = left + column * cellWidth;
                    TryPlace(kind, FormattableString.Invariant($"periphery-{row + 1}-{column + 1}"),
                        cellLeft, cellLeft + cellWidth, top, top + margin, spacing);
                }
            }
        }

        void TryPlace(TacticalAnomalyKind kind, string id, double left, double right, double top, double bottom, double spacing)
        {
            for (int attempt = 0; attempt < rules.MaxAttempts; attempt++)
            {
                double x = random.Uniform(left, right), y = random.Uniform(top, bottom);
                if (starts.Any(p => TacticalGeometry.Dist(x, y, p.X, p.Y) < rules.Clearance)) continue;
                if (spacing > 0 && result.Any(a => TacticalGeometry.Dist(x, y, a.CenterX, a.CenterY) < spacing)) continue;
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
                result.Add(new(id, kind, clumps.MoveToImmutable()) { CenterX = x, CenterY = y });
                return;
            }
        }
        double Range(ImmutableArray<double> range) => random.Uniform(range[0], range[1]);
    }
}
