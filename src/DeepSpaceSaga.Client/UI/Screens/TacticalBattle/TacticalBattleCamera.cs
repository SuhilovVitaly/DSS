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
    private const double MinimumFitWidth = 600;
    private readonly CameraState _camera = new(0, 0, 1);
    private int _width = 1, _height = 1;
    private double _fittedWidth = MinimumFitWidth;
    private TacticalBattleSnapshot? _observed;
    private SKPoint? _panOrigin;
    public bool UserView { get; private set; }
    public bool Panning => _panOrigin.HasValue;
    public TacticalBattleView View => new(_width, _height, _camera.FocusX, _camera.FocusY, _camera.PixelsPerWorldUnit);

    public void Update(TacticalBattleSnapshot snapshot, int width, int height, bool freeze = false)
    {
        bool resized = (_width, _height) != (width, height);
        Resize(width, height);
        if (freeze && _observed is not null) return;
        bool changed = _observed?.BattleId != snapshot.BattleId || _observed.Phase != snapshot.Phase || _observed.Turn != snapshot.Turn;
        _observed = snapshot;
        if (changed || resized) Fit(snapshot);
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
        UserView = false; EndPan();
        var points = snapshot.Units.Where(u => snapshot.Phase == TacticalBattlePhase.Move || u.Kind == TacticalUnitKind.Drone).Select(u => u.Pose).ToList();
        points.AddRange(snapshot.Paths.SelectMany(p => p.Points));
        if (snapshot.Phase == TacticalBattlePhase.Move)
            foreach (var order in snapshot.Suggestions)
            {
                var u = snapshot.Units.FirstOrDefault(u => u.Id == order.DroneId);
                if (u is null || u.Side != snapshot.PlayerSide) continue;
                var end = TacticalGeometry.Integrate(new(u.Pose.X, u.Pose.Y, u.Pose.Course), order.Speed, order.Turn, snapshot.Rules.Substeps);
                points.Add(new(end.X, end.Y, end.Course));
            }
        if (points.Count == 0) points.Add(new(0, 0, 0));
        double left = points.Min(p => p.X), right = points.Max(p => p.X), top = points.Min(p => p.Y), bottom = points.Max(p => p.Y);
        double margin = snapshot.Phase == TacticalBattlePhase.Move ? 50 : 70;
        double ppu = PyMath.Max(.00001, PyMath.Min((_width - 448 - 2 * margin) / (double)PyMath.Max(1, right - left), (_height - 12 - 2 * margin) / (double)PyMath.Max(1, bottom - top)));
        _fittedWidth = (double)_width / ppu;
        // TacticalBattleView projects around the entire canvas; offset focus to center the safe area.
        double safeX = (202 + _width - 246) * .5, safeY = _height * .5;
        _camera.SetFocus((left + right) * .5 + (_width * .5 - safeX) / (double)ppu,
            (top + bottom) * .5 + (_height * .5 - safeY) / (double)ppu);
        SetZoom(ppu);
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
