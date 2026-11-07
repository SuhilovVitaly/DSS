using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Immutable projection captured with a single battle snapshot.</summary>
public readonly record struct TacticalBattleView(int PixelWidth, int PixelHeight, double CenterX, double CenterY, double PixelsPerUnit)
{
    public double WorldWidth => (double)PixelWidth / PixelsPerUnit;
    public SKPoint Project(TacticalPoseSnapshot pose) => Project(pose.X, pose.Y);
    public SKPoint Project(double x, double y) => new(
        (float)(PixelWidth * 0.5 + (x - CenterX) * PixelsPerUnit),
        (float)(PixelHeight * 0.5 + (y - CenterY) * PixelsPerUnit));
    public (double X, double Y) Unproject(float x, float y) =>
        (CenterX + (x - PixelWidth * 0.5) / (double)PixelsPerUnit, CenterY + (y - PixelHeight * 0.5) / (double)PixelsPerUnit);
}

/// <summary>Owns tactical presentation only; it never writes to the world camera or the battle.</summary>
internal sealed class TacticalBattleCamera
{
    private const double FitMargin = 70;
    private const double MinimumFitWidth = 600;
    private readonly CameraState _camera = new(0, 0, 1);
    private int _width = 1, _height = 1;
    private double _fittedWidth = MinimumFitWidth;
    private TacticalBattleSnapshot? _observed;
    private SKPoint? _panOrigin;
    public bool UserView { get; private set; }
    public bool Panning => _panOrigin.HasValue;
    public TacticalBattleView View => new(_width, _height, _camera.FocusX, _camera.FocusY, _camera.PixelsPerWorldUnit);

    public void Update(TacticalBattleSnapshot snapshot, int width, int height)
    {
        Resize(width, height);
        if (ReferenceEquals(snapshot, _observed)) return;
        bool newBattle = _observed?.BattleId != snapshot.BattleId;
        _observed = snapshot;
        if (newBattle || !UserView && snapshot.Units.Any(unit => unit.State == TacticalUnitState.Air && !Visible(unit.Pose)))
            Fit(snapshot);
    }

    private void Resize(int width, int height)
    {
        if (width < 1 || height < 1 || (width, height) == (_width, _height)) return;
        double ppu = View.PixelsPerUnit * (width / (double)_width);
        _width = width;
        _height = height;
        SetZoom(ppu);
        EndPan();
    }

    public void Fit(TacticalBattleSnapshot snapshot)
    {
        UserView = false;
        EndPan();
        double left = double.PositiveInfinity, right = double.NegativeInfinity;
        double top = double.PositiveInfinity, bottom = double.NegativeInfinity;
        foreach (var unit in snapshot.Units)
        {
            if (unit.Kind != TacticalUnitKind.Carrier && unit.State != TacticalUnitState.Air) continue;
            Add(unit.Pose.X, unit.Pose.Y);
            if (unit.State != TacticalUnitState.Air || unit.Side != snapshot.PlayerSide || unit.Limits is not { } limits) continue;

            var motion = new TacticalLimits(limits.Mass, limits.MassFactor, limits.MinSpeed, limits.MaxSpeed, limits.Change, limits.Turn);
            double speed = motion.ClampSpeed(motion.MaxSpeed, unit.Speed);
            double turn = motion.TurnLimit(speed, snapshot.Rules.TurnAtMaxSpeedFraction);
            var start = new TacticalPose(unit.Pose.X, unit.Pose.Y, unit.Pose.Course);
            AddEnd(start, speed, -turn);
            AddEnd(start, speed, 0);
            AddEnd(start, speed, turn);
            foreach (var order in snapshot.Suggestions)
                if (order.DroneId == unit.Id) { AddEnd(start, order.Speed, order.Turn); break; }
        }
        if (!double.IsFinite(left)) left = right = top = bottom = 0;
        double aspect = _width / (double)_height;
        _fittedWidth = PyMath.Max(MinimumFitWidth, PyMath.Max(right - left + 2 * FitMargin, (bottom - top + 2 * FitMargin) * aspect));
        _camera.SetFocus((left + right) * 0.5, (top + bottom) * 0.5);
        SetZoom((double)_width / _fittedWidth);

        void AddEnd(TacticalPose start, double speed, double turn)
        {
            var end = TacticalGeometry.Integrate(start, speed, turn, snapshot.Rules.Substeps);
            Add(end.X, end.Y);
        }
        void Add(double x, double y)
        {
            left = PyMath.Min(left, x); right = PyMath.Max(right, x);
            top = PyMath.Min(top, y); bottom = PyMath.Max(bottom, y);
        }
    }

    private void SetZoom(double ppu) => _camera.ZoomAt((double)ppu / _camera.PixelsPerWorldUnit,
        _width * 0.5f, _height * 0.5f, _width, _height, ppu, ppu);

    public void Zoom(float x, float y, float browserDelta)
    {
        if (browserDelta == 0 || !float.IsFinite(browserDelta) || !Contains(x, y)) return;
        UserView = true;
        _camera.ZoomAt(MouseWheelInput.ZoomFactor(browserDelta), x, y, _width, _height,
            (double)_width / (_fittedWidth * 4.0), (double)_width / (_fittedWidth * 0.125));
    }

    public bool BeginPan(float x, float y)
    {
        if (!Contains(x, y)) return false;
        _panOrigin = new(x, y);
        return true;
    }

    public bool PanTo(float x, float y)
    {
        if (_panOrigin is not { } previous || !float.IsFinite(x) || !float.IsFinite(y)) return false;
        if (x == previous.X && y == previous.Y) return true;
        var world = _camera.ScreenToWorld(_width * 0.5f + previous.X - x,
            _height * 0.5f + previous.Y - y, _width, _height);
        _camera.SetFocus(world.X, world.Y);
        _panOrigin = new(x, y);
        UserView = true;
        return true;
    }
    public void EndPan() => _panOrigin = null;

    private bool Visible(TacticalPoseSnapshot pose)
    {
        var p = _camera.WorldToScreen(pose.X, pose.Y, _width, _height);
        return Contains(p.X, p.Y);
    }

    public bool Contains(float x, float y) => x >= 0 && x < _width && y >= 0 && y < _height;
}
