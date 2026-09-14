namespace DeepSpaceSaga.Client.UI;

/// <summary>Monotonic UI-time zoom. Input accumulates against the target, hit tests use the rendered camera.</summary>
internal sealed class CameraZoomTransition
{
    private double _startPpu, _targetPpu, _elapsed;
    private float _anchorX, _anchorY;
    internal bool Active { get; private set; }
    internal double TargetPpu(CameraState camera) => Active ? _targetPpu : camera.PixelsPerWorldUnit;
    internal void Cancel() => Active = false;

    internal void Start(CameraState camera, double target, float x, float y, TacticalMapSettings settings, int w, int h)
    {
        _startPpu = camera.PixelsPerWorldUnit;
        _targetPpu = Math.Clamp(target, settings.MinimumPpu, settings.MaximumPpu);
        _anchorX = x; _anchorY = y; _elapsed = 0;
        Active = _startPpu != _targetPpu;
        if (settings.ZoomAnimationMs == 0)
        {
            camera.ZoomAt(_targetPpu / _startPpu, x, y, w, h, settings.MinimumPpu, settings.MaximumPpu);
            Active = false;
        }
    }

    internal void Advance(CameraState camera, double seconds, bool follow, TacticalMapSettings settings, int w, int h)
    {
        if (!Active) return;
        _elapsed += Math.Max(0, seconds) * 1000;
        double t = Math.Min(1, _elapsed / settings.ZoomAnimationMs);
        double eased = t * t * (3 - 2 * t);
        double ppu = t == 1 ? _targetPpu : Math.Exp(Math.Log(_startPpu) + Math.Log(_targetPpu / _startPpu) * eased);
        camera.ZoomAt(ppu / camera.PixelsPerWorldUnit, follow ? w / 2f : _anchorX, follow ? h / 2f : _anchorY,
            w, h, settings.MinimumPpu, settings.MaximumPpu);
        if (t == 1) Active = false;
    }
}
