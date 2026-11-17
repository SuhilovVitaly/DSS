using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Portraits;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>Scrollable object details on the existing tactical map, without a second window.</summary>
public sealed class BattleLootWindow(BattleLootPresentation presentation) : IDisposable
{
    private readonly PrisonerPortraits _portraits = new(AppContext.BaseDirectory);
    public void Dispose() => _portraits.Dispose();
    private sealed record ActionHit(SKRect Bounds, Func<Task<bool>> Action);
    private readonly List<ActionHit> _actions = [];
    private readonly BattleLootConsumablesPanel _consumables = new(presentation);
    private readonly ModuleSalvagePresentation _modules = new(presentation);
    private float _scroll, _contentHeight;
    public SKRect Bounds { get; private set; }
    public SKRect LeaveBounds { get; private set; }
    public SKRect ConfirmBounds { get; private set; }
    public SKRect CancelBounds { get; private set; }
    public SKRect CloseBounds { get; private set; }
    internal SKRect ArrestBounds { get; private set; }
    public bool ConfirmDiscard { get; private set; }
    public Task<bool>? LastActionTask { get; private set; }
    public BattleLootPresentation Presentation => presentation;
    private static string L(string key) => Localization.Get("loot." + key);

    public bool RequestExit()
    {
        if (presentation.Pending) return false;
        ConfirmDiscard = presentation.State?.HasUnclaimed == true;
        return !ConfirmDiscard;
    }
    public void CancelExit() => ConfirmDiscard = false;
    public void Select(string id) { if (presentation.Select(id)) _scroll = 0; }

    public void Draw(SKCanvas canvas, float width, float height, bool flowPending, string? flowError = null)
    {
        _actions.Clear();
        ArrestBounds = default;
        LeaveBounds = new(16, height - 54, 250, height - 14);
        Button(canvas, LeaveBounds, L("leave"), !presentation.Pending && !flowPending);
        float panelWidth = Math.Min(500, width - 40);
        Bounds = new(width - panelWidth - 18, 34, width - 18, height - 70);
        if (presentation.Selected is null) Bounds = new(16, 34, Math.Min(width - 18, 516), 185);
        using var background = new SKPaint { Color = new(12, 23, 29, 245) };
        canvas.DrawRoundRect(Bounds, 8, 8, background);
        Text(canvas, L("title"), Bounds.Left + 16, Bounds.Top + 27, 19);
        CloseBounds = new(Bounds.Right - 42, Bounds.Top + 8, Bounds.Right - 10, Bounds.Top + 38);
        if (presentation.Selected is not null) Button(canvas, CloseBounds, "×", !presentation.Pending && !flowPending);
        if (presentation.Selected is not { } source)
            Wrapped(canvas, L("select_source"), Bounds.Left + 16, Bounds.Top + 62, panelWidth - 32, 15);
        else
        {
            Text(canvas, L("source." + source.Kind), Bounds.Left + 16, Bounds.Top + 54, 15);
            var clip = new SKRect(Bounds.Left + 12, Bounds.Top + 70, Bounds.Right - 12, Bounds.Bottom - 50);
            canvas.Save(); canvas.ClipRect(clip);
            float top = clip.Top - _scroll, y = top;
            if (source.Pilot is { } pilot)
            {
                Text(canvas, pilot.DisplayName, clip.Left + 4, y + 18, 16);
                ArrestBounds = new(clip.Left + 4, y + 30, clip.Left + 210, y + 64);
                AddButton(canvas, ArrestBounds, Localization.Get(pilot.Captured ? "Prisoners.Captured" : presentation.Pending ? "Prisoners.Pending" : "Prisoners.Arrest"),
                    pilot.Alive && !pilot.Captured && pilot.CaptureReason is null && !presentation.Pending && !flowPending,
                    () => presentation.SubmitAsync((id, revision, battle, drone) => new ArrestPilotCommand(id, revision, battle, drone)), clip);
                if (!pilot.Captured && pilot.CaptureReason is { } reason) Wrapped(canvas, L(reason), clip.Left + 4, y + 86, clip.Width - 8, 12);
                y += 112;
                if (_portraits.Get(pilot) is { } portrait)
                {
                    // Uniform fit; preserve the full kneeling pose, including knees and hands.
                    float scale = Math.Min((clip.Width - 8) / portrait.Width, 240f / portrait.Height);
                    float w = portrait.Width * scale, h = portrait.Height * scale;
                    canvas.DrawBitmap(portrait, new SKRect(clip.MidX - w / 2, y, clip.MidX + w / 2, y + h));
                    y += h + 12;
                }
            }
            foreach (var row in _consumables.Rows())
            {
                Text(canvas, row.Label + " · " + string.Format(L("quantity"), row.Quantity), clip.Left + 4, y + 18, 14);
                if (row.ChanceBasisPoints is { } chance)
                    Text(canvas, string.Format(L("chance"), ModuleSalvagePresentation.ChanceText(chance, Culture())), clip.Left + 4, y + 39, 13);
                float buttonY = y + 44;
                AddButton(canvas, new(clip.Left + 4, buttonY, clip.Left + 180, buttonY + 30),
                    L(row.Id == "scrap" ? "scrap" : "collect") + (row.Id == "scrap" ? "" : " 1"), row.Enabled && !flowPending,
                    () => _consumables.ActivateAsync(row.Id, 1), clip);
                if (row.Id != "scrap" && row.Quantity > 1)
                    AddButton(canvas, new(clip.Left + 188, buttonY, clip.Right - 4, buttonY + 30), L("collect") + " " + row.Quantity,
                        row.Enabled && !flowPending, () => _consumables.ActivateAsync(row.Id, row.Quantity), clip);
                if (row.Reason is { } reason) Wrapped(canvas, L(reason), clip.Left + 4, y + 94, clip.Width - 8, 12);
                else if (row.Status is { } status) Text(canvas, L("state." + status), clip.Left + 4, y + 94, 12);
                y += 122;
            }
            foreach (var row in _modules.Rows(Culture()))
            {
                Text(canvas, row.Name, clip.Left + 4, y + 18, 14);
                Text(canvas, row.Condition + " · " + row.Chance, clip.Left + 4, y + 39, 12);
                Text(canvas, row.EstimatedValue, clip.Left + 4, y + 59, 12);
                AddButton(canvas, new(clip.Left + 4, y + 69, clip.Left + 180, y + 99), L("salvage"), row.Enabled && !flowPending,
                    () => _modules.AttemptAsync(row.InstanceId), clip);
                Text(canvas, row.Status, clip.Left + 190, y + 90, 12);
                if (row.Reason is { } reason) Wrapped(canvas, reason, clip.Left + 4, y + 119, clip.Width - 8, 12);
                y += 145;
            }
            if (_consumables.Rows().IsEmpty && source.Modules.IsEmpty && source.Pilot is null)
            { Text(canvas, L("empty"), clip.Left + 4, y + 20, 14); y += 45; }
            _contentHeight = y - top;
            canvas.Restore();
            _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _contentHeight - clip.Height));
            Text(canvas, L("estimate_notice"), Bounds.Left + 16, Bounds.Bottom - 19, 12);
        }
        if (presentation.LastReason is { } last) Wrapped(canvas, L(last), 270, height - 37, Math.Max(100, width - 550), 13);
        else if (!string.IsNullOrWhiteSpace(flowError)) Wrapped(canvas, flowError, 270, height - 37, Math.Max(100, width - 550), 13);
        if (ConfirmDiscard)
        {
            var box = new SKRect((width - 520) / 2, (height - 230) / 2, (width + 520) / 2, (height + 230) / 2);
            using var shade = new SKPaint { Color = new(0, 0, 0, 180) }; canvas.DrawRect(0, 0, width, height, shade);
            canvas.DrawRoundRect(box, 8, 8, background);
            int count = presentation.State?.Objects.Count(s => s.Consumables.Any(c => c.Remaining > 0) ||
                s.Scrap?.State == LootAttemptStates.NotAttempted || s.Modules.Any(m => m.State == LootAttemptStates.NotAttempted) ||
                s.Pilot is { Alive: true, Captured: false }) ?? 0;
            Wrapped(canvas, string.Format(L("exit_warning"), count), box.Left + 20, box.Top + 30, box.Width - 40, 16);
            ConfirmBounds = new(box.Left + 20, box.Bottom - 62, box.MidX - 8, box.Bottom - 20);
            CancelBounds = new(box.MidX + 8, box.Bottom - 62, box.Right - 20, box.Bottom - 20);
            Button(canvas, ConfirmBounds, L("confirm_leave"), !flowPending && !presentation.Pending);
            Button(canvas, CancelBounds, L("cancel"), !flowPending && !presentation.Pending);
        }
    }

    // Locale selection belongs to the active dictionary, not the OS locale.
    private static CultureInfo Culture() => CultureInfo.GetCultureInfo(Localization.Get("loot.number_culture"));
    public bool Click(SKPoint point, bool pending)
    {
        if (pending || presentation.Pending) return false;
        if (ConfirmDiscard)
        {
            if (CancelBounds.Contains(point)) CancelExit();
            return ConfirmBounds.Contains(point);
        }
        if (LeaveBounds.Contains(point)) return RequestExit();
        if (presentation.Selected is not null && CloseBounds.Contains(point)) { presentation.ClearSelection(); return false; }
        foreach (var hit in _actions)
            if (hit.Bounds.Contains(point)) { LastActionTask = hit.Action(); break; }
        return false;
    }
    public bool Scroll(SKPoint point, float delta)
    {
        if (!Bounds.Contains(point) || ConfirmDiscard) return false;
        _scroll = Math.Clamp(_scroll + delta * .4f, 0, Math.Max(0, _contentHeight - (Bounds.Height - 120)));
        return true;
    }
    private void AddButton(SKCanvas c, SKRect bounds, string label, bool enabled, Func<Task<bool>> action, SKRect clip)
    {
        Button(c, bounds, label, enabled);
        if (enabled && bounds.Top >= clip.Top && bounds.Bottom <= clip.Bottom) _actions.Add(new(bounds, action));
    }
    private static void Button(SKCanvas c, SKRect bounds, string label, bool enabled)
    {
        using var paint = new SKPaint { Color = enabled ? new(38, 83, 97) : new(35, 45, 50), IsAntialias = true };
        c.DrawRoundRect(bounds, 4, 4, paint);
        Text(c, label, bounds.Left + 10, bounds.MidY + 5, 14);
    }
    private static void Text(SKCanvas c, string text, float x, float y, float size)
    {
        using var paint = new SKPaint { Color = new(230, 237, 240), TextSize = size, IsAntialias = true };
        c.DrawText(text, x, y, paint);
    }
    private static void Wrapped(SKCanvas c, string text, float x, float y, float width, float size)
    {
        using var paint = new SKPaint { TextSize = size };
        string line = "";
        foreach (var word in text.Split(' '))
        {
            if (line.Length > 0 && paint.MeasureText(line + " " + word) > width)
            { Text(c, line, x, y, size); y += size + 6; line = ""; }
            line = line.Length == 0 ? word : line + " " + word;
        }
        Text(c, line, x, y, size);
    }
}
