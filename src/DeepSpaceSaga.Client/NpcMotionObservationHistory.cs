namespace DeepSpaceSaga.Client;

internal readonly record struct NpcMotionObservation(string ObjectId, double MotionTimeMs,
    double X, double Y, double SpeedKmS, double Direction);

/// <summary>Bounded physical-second samples of confirmed, display-admitted NPC poses.</summary>
internal sealed class NpcMotionObservationHistory
{
    private const double TimeTolerance = 1e-6;
    private const double AngleTolerance = 0.001;
    private readonly Dictionary<string, Track> _tracks = new(StringComparer.Ordinal);
    private Guid? _session;
    private double? _time;

    internal int ObjectCount => _tracks.Count;
    internal int SampleCount => _tracks.Values.Sum(t => t.Headings.Count);

    internal void Clear()
    {
        _tracks.Clear();
        _time = null;
        _session = null;
    }

    internal void ObserveScene(Guid? sessionId, double motionTimeMs,
        IReadOnlyList<NpcMotionObservation> visibleAliveNpcs)
    {
        if (_session != sessionId || (_time is { } time && motionTimeMs < time)) Clear();
        _session = sessionId;
        _time = motionTimeMs;
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pose in visibleAliveNpcs)
        {
            if (!double.IsFinite(pose.MotionTimeMs) || !double.IsFinite(pose.Direction) ||
                !double.IsFinite(pose.X) || !double.IsFinite(pose.Y) || !double.IsFinite(pose.SpeedKmS)) continue;
            present.Add(pose.ObjectId);
            if (!_tracks.TryGetValue(pose.ObjectId, out var track))
                _tracks.Add(pose.ObjectId, new(pose));
            else track.Observe(pose);
        }
        foreach (var id in _tracks.Keys.Where(id => !present.Contains(id)).ToArray()) _tracks.Remove(id);
    }

    internal double TurnRateDegPerSec(string objectId) => _tracks.TryGetValue(objectId, out var track) ? track.Rate : 0;

    private static double Delta(double from, double to) => ((to % 360 - from % 360 + 540) % 360 + 360) % 360 - 180;

    private sealed class Track
    {
        internal readonly Queue<double> Headings = new(3);
        private NpcMotionObservation _raw;
        private double _nextSampleTime;
        internal double Rate { get; private set; }

        internal Track(NpcMotionObservation pose) => Reset(pose);

        private void Reset(NpcMotionObservation pose)
        {
            _raw = pose;
            _nextSampleTime = pose.MotionTimeMs + 1000;
            Headings.Clear();
            Headings.Enqueue(pose.Direction);
            Rate = 0;
        }

        internal void Observe(NpcMotionObservation pose)
        {
            double elapsed = pose.MotionTimeMs - _raw.MotionTimeMs;
            if (elapsed == 0)
            {
                if (pose.X != _raw.X || pose.Y != _raw.Y || pose.SpeedKmS != _raw.SpeedKmS ||
                    Delta(_raw.Direction, pose.Direction) != 0) Reset(pose);
                return;
            }
            double delta = Delta(_raw.Direction, pose.Direction);
            if (elapsed < 0 || elapsed > 1000 + TimeTolerance || Math.Abs(delta) == 180)
            {
                Reset(pose);
                return;
            }
            // Interpolate only between admitted poses. A packet gap never invents samples.
            while (_nextSampleTime <= pose.MotionTimeMs)
            {
                double fraction = (_nextSampleTime - _raw.MotionTimeMs) / elapsed;
                if (Headings.Count == 3) Headings.Dequeue();
                Headings.Enqueue(_raw.Direction + delta * fraction);
                _nextSampleTime += 1000;
                Rate = 0;
                if (Headings.Count == 3)
                {
                    var headings = Headings.ToArray();
                    double a = Delta(headings[0], headings[1]), b = Delta(headings[1], headings[2]);
                    if (Math.Abs(a) > AngleTolerance && Math.Abs(b) > AngleTolerance &&
                        Math.Abs(a) != 180 && Math.Abs(b) != 180 && Math.Sign(a) == Math.Sign(b) &&
                        Math.Abs(a - b) <= AngleTolerance + 1e-12) Rate = (a + b) / 2;
                }
            }
            _raw = pose;
        }
    }
}
