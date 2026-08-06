using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Per-object visual smoothing for label plaque position.
/// Target position (from orbit layout) is set every frame;
/// visible position catches up exponentially with a ~180 ms time constant.
/// </summary>
internal sealed class ObjectLabelSmoother
{
    /// <summary>Smoothing time constant in seconds.</summary>
    private const double SmoothTime = 0.18;

    /// <summary>Distance threshold (px) above which we treat a jump as a teleport.</summary>
    private const float TeleportThresholdPx = 300f;

    private readonly Dictionary<string, SmoothState> _states = new(StringComparer.Ordinal);

    /// <summary>
    /// Advance the visible position toward the target and return the current
    /// visible plaque rectangle.
    /// </summary>
    public SKRect Update(
        string objectId,
        SKRect targetPlaque,
        SKPoint targetCenter,
        double deltaSeconds,
        int viewportW,
        int viewportH,
        bool reset)
    {
        float targetCx = targetCenter.X;
        float targetCy = targetCenter.Y;
        float pw = targetPlaque.Width;
        float ph = targetPlaque.Height;

        if (!_states.TryGetValue(objectId, out var state) || reset)
        {
            // First appearance or explicit reset — snap immediately.
            state = new SmoothState(targetCx, targetCy);
            _states[objectId] = state;
            return targetPlaque;
        }

        // Detect teleport-like jumps.
        float jumpDx = targetCx - state.VisibleCx;
        float jumpDy = targetCy - state.VisibleCy;
        if (jumpDx * jumpDx + jumpDy * jumpDy > TeleportThresholdPx * TeleportThresholdPx)
        {
            state = new SmoothState(targetCx, targetCy);
            _states[objectId] = state;
            return targetPlaque;
        }

        // Exponential smoothing: visible += (target - visible) * (1 - exp(-dt/τ)).
        float t = (float)(1.0 - Math.Exp(-deltaSeconds / SmoothTime));
        float newCx = state.VisibleCx + (targetCx - state.VisibleCx) * t;
        float newCy = state.VisibleCy + (targetCy - state.VisibleCy) * t;

        // If we're very close, snap to avoid micro-jitter.
        if (Math.Abs(newCx - targetCx) < 0.5f && Math.Abs(newCy - targetCy) < 0.5f)
        {
            newCx = targetCx;
            newCy = targetCy;
        }

        state = new SmoothState(newCx, newCy);
        _states[objectId] = state;

        float halfW = pw / 2f;
        float halfH = ph / 2f;
        return new SKRect(newCx - halfW, newCy - halfH, newCx + halfW, newCy + halfH);
    }

    /// <summary>
    /// Remove stale state for objects that no longer exist.
    /// </summary>
    public void RemoveStaleExcept(HashSet<string> activeObjectIds)
    {
        var toRemove = new List<string>();
        foreach (string id in _states.Keys)
        {
            if (!activeObjectIds.Contains(id))
                toRemove.Add(id);
        }

        foreach (string id in toRemove)
            _states.Remove(id);
    }

    /// <summary>
    /// Reset all state (e.g. on viewport resize).
    /// </summary>
    public void ResetAll()
    {
        _states.Clear();
    }

    private readonly record struct SmoothState(float VisibleCx, float VisibleCy);
}
