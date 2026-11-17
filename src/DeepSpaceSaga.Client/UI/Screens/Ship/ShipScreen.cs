using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>
/// Ship overlay (ТЗ DB/Documentation/02-FirstRelease/Screens/Ship.md). Renders the authoritative
/// carrier surface, the persistent wing and the between-battle rearm controls. The screen selects
/// one drone and one quantity, previews the magazine/hold transition and confirms through the
/// injected rearm transport; the local display is updated only from the authoritative response,
/// never optimistically. Closes via the × button, Escape, or a click outside the panel. Pause-on-
/// open/resume-on-close is handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this
/// screen has no speed/pause logic of its own.
/// </summary>
public sealed partial class ShipScreen : IScreen
{
    private readonly ShipViewModel _model;
    private readonly Func<DemoCampaignSnapshot?>? _campaign;
    private readonly Func<DemoCampaignCommand, CancellationToken, ValueTask<DemoCampaignResponse>>? _service;
    private StationRepairSnapshot? _repairState;
    private long? _serviceRevision;
    internal Task? PendingService;
    internal string ServiceStatus { get; private set; } = "";
    internal string CampaignSummary => CampaignRoundtripPresentation.Summary(_campaign?.Invoke()?.Company?.Roundtrip);
    private readonly Func<RearmDroneRequest, CancellationToken, ValueTask<CarrierCommandResponse>>? _rearm;
    private readonly Func<string> _commandId;
    private readonly ShipRearmController _controller = new();
    private ShipViewModel.DroneRow[] _drones;
    private int _torpedoStock;
    private int _revision;
    private int _selectedRow = -1;
    private int _quantity;
    private string _status = "";
    private int _screenWidth;
    private int _screenHeight;
    private bool _isCloseHovered;

    /// <summary>Legacy open path: no carrier surface yet — renders the explicit unsupported state.</summary>
    public ShipScreen() : this(ShipViewModel.From(null)) { }

    public ShipScreen(ShipViewModel model) : this(model, 0, null) { }

    public ShipScreen(ShipViewModel model, int torpedoStock,
        Func<RearmDroneRequest, CancellationToken, ValueTask<CarrierCommandResponse>>? rearm, Func<string>? commandId = null,
        Func<DemoCampaignSnapshot?>? campaign = null,
        Func<DemoCampaignCommand, CancellationToken, ValueTask<DemoCampaignResponse>>? service = null)
    {
        _model = model;
        _campaign = campaign;
        _service = service;
        _torpedoStock = Math.Max(0, torpedoStock);
        _rearm = rearm;
        _commandId = commandId ?? (() => Guid.NewGuid().ToString("N"));
        _drones = model.Drones.ToArray();
        _revision = model.Revision;
    }

    /// <summary>The request currently in flight; awaited by tests and never re-sent while non-null and pending.</summary>
    internal Task? PendingRearm;
    internal IReadOnlyList<ShipViewModel.DroneRow> Drones => _drones;
    internal int TorpedoStock => _torpedoStock;
    internal int Revision => _revision;
    internal int SelectedRow => _selectedRow;
    internal int SelectedQuantity => _quantity;
    internal string Status => _status;
    internal bool IsPending => _controller.IsPending;

    public void OnActivated() => _isCloseHovered = false;

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseShip : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = ShipLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == ShipButton.Close)
            return ScreenEvent.CloseShip;

        // Click on the dimmed background outside the panel also closes it.
        if (!ShipLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseShip;

        if (_model.Unsupported)
            return ScreenEvent.None;

        float sx = x - ShipLayout.PanelLeft(_screenWidth), sy = y - ShipLayout.PanelTop(_screenHeight);
        if (_service is not null && sx >= 730 && sx <= 1340)
        {
            if (sy >= 300 && sy <= 332) { RequestRepair(_model.ShipId); return ScreenEvent.None; }
            if (sy >= 340 && sy <= 372 && _selectedRow >= 0) { RequestRepair(_drones[_selectedRow].Id); return ScreenEvent.None; }
            if (sy >= 530 && sy <= 562) { ConfirmRepair(); return ScreenEvent.None; }
        }

        var (control, row) = ShipLayout.RearmHitTest(x, y, _screenWidth, _screenHeight, _drones.Length);
        switch (control)
        {
            case ShipButton.DroneRow:
                Select(row);
                break;
            case ShipButton.QuantityMinus:
                ChangeQuantity(-1);
                break;
            case ShipButton.QuantityPlus:
                ChangeQuantity(+1);
                break;
            case ShipButton.ConfirmRearm:
                StartRearm();
                break;
        }
        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = ShipLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isCloseHovered = hit == ShipButton.Close;
        return _isCloseHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (float.IsFinite(delta) && delta != 0 && y >= ShipLayout.PanelTop(_screenHeight) + 500)
            _memorialScroll = Math.Clamp(_memorialScroll - Math.Sign(delta), 0, Math.Max(0, Memorial.Length - VisibleMemorialRows));
        return ScreenEvent.None;
    }

    private void Select(int index)
    {
        if (index < 0 || index >= _drones.Length)
            return;
        if (_drones[index].Destroyed)
        {
            // A destroyed drone can never receive torpedoes; the row stays visible but unselectable.
            _status = CampaignRoundtripPresentation.RearmReason("destroyed_drone");
            return;
        }
        _selectedRow = index;
        _quantity = MaxQuantity(index);
        _status = Explanation(index);
    }

    private int MaxQuantity(int index)
    {
        var drone = _drones[index];
        return ShipWingPanel.MaxSelectableQuantity(drone.AmmoCapacity - drone.AmmoCurrent, _torpedoStock);
    }

    private string Explanation(int index)
    {
        var drone = _drones[index];
        int max = MaxQuantity(index);
        if (drone.AmmoCurrent >= drone.AmmoCapacity) return "magazine full";
        if (_torpedoStock <= 0) return "no torpedoes in the hold";
        if (max <= 0) return "nothing to transfer";
        return ShipWingPanel.PreviewText(drone.AmmoCurrent, drone.AmmoCapacity, _torpedoStock, max);
    }

    private void ChangeQuantity(int delta)
    {
        if (_selectedRow < 0)
            return;
        int max = MaxQuantity(_selectedRow);
        _quantity = ShipWingPanel.ClampQuantity(_quantity + delta, max);
        var drone = _drones[_selectedRow];
        _status = max <= 0
            ? Explanation(_selectedRow)
            : ShipWingPanel.PreviewText(drone.AmmoCurrent, drone.AmmoCapacity, _torpedoStock, _quantity);
    }

    private void StartRearm()
    {
        if (_rearm is null)
        {
            _status = "rearm is not available for this ship";
            return;
        }
        if (_selectedRow < 0)
        {
            _status = "select a drone to rearm";
            return;
        }
        var drone = _drones[_selectedRow];
        if (drone.Destroyed)
            return;
        int max = MaxQuantity(_selectedRow);
        int quantity = ShipWingPanel.ClampQuantity(_quantity, max);
        if (quantity <= 0)
        {
            _status = Explanation(_selectedRow);
            return;
        }
        // The pending barrier guarantees a double-click sends exactly one request.
        if (!_controller.TryBegin(quantity))
            return;
        _status = Localization.Get("Demo.Rearm.Pending");
        PendingRearm = SubmitAsync(drone.Id, quantity, _revision, _controller);
    }

    private async Task SubmitAsync(string droneId, int quantity, int revision, ShipRearmController controller)
    {
        try
        {
            var request = new RearmDroneRequest(_commandId(), _model.ShipId, droneId, quantity, revision);
            var response = await _rearm!(request, CancellationToken.None).ConfigureAwait(true);
            if (response.Snapshot is { } snapshot) Rebuild(snapshot);
            // Every authoritative response — accepted or rejected — carries the current revision, so a
            // stale-revision retry uses it even when the engine sends no Snapshot with the rejection.
            _revision = response.Revision;
            if (response.Accepted)
            {
                if (response.Snapshot is null) ApplyTransfer(droneId, response.Transferred);
                else _torpedoStock = Math.Max(0, _torpedoStock - response.Transferred);
                _status = string.Format(Localization.Get("Demo.Rearm.Confirmed"), response.Transferred);
                if (_selectedRow >= 0) _quantity = MaxQuantity(_selectedRow);
            }
            else
            {
                // A rejection transfers nothing: the magazine stays exactly as it was.
                _status = CampaignRoundtripPresentation.RearmReason(response.Reason);
                if (_selectedRow >= 0) _quantity = ShipWingPanel.ClampQuantity(_quantity, MaxQuantity(_selectedRow));
            }
        }
        catch (Exception)
        {
            // Transport failure: keep the last authoritative magazine and replace the in-flight status
            // with an explicit error so the player can retry; the pending barrier is released below.
            _status = CampaignRoundtripPresentation.RearmReason("unavailable");
        }
        finally
        {
            controller.End();
        }
    }

    private void ApplyTransfer(string droneId, int transferred)
    {
        if (transferred <= 0)
            return;
        int index = Array.FindIndex(_drones, drone => drone.Id == droneId);
        if (index < 0)
            return;
        var drone = _drones[index];
        _drones[index] = drone with { AmmoCurrent = Math.Min(drone.AmmoCapacity, drone.AmmoCurrent + transferred) };
        _torpedoStock = Math.Max(0, _torpedoStock - transferred);
    }

    private void Rebuild(CarrierOperationsSnapshot snapshot)
    {
        string? selectedId = _selectedRow >= 0 && _selectedRow < _drones.Length ? _drones[_selectedRow].Id : null;
        _revision = snapshot.Revision;
        _drones = ShipViewModel.From(snapshot).Drones.ToArray();
        int index = selectedId is null ? -1 : Array.FindIndex(_drones, drone => drone.Id == selectedId);
        _selectedRow = index;
        if (index >= 0) _quantity = ShipWingPanel.ClampQuantity(_quantity, MaxQuantity(index));
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = ShipLayout.PanelLeft(width);
        float pt = ShipLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + ShipLayout.PanelWidth, pt + ShipLayout.PanelHeight);
        TabletopStyle.Panel(canvas, panelRect);

        float cx = pl + ShipLayout.PanelWidth / 2f;
        canvas.DrawText("SHIP", cx, pt + ShipLayout.TitleY, MenuStyle.TextTitle);

        float textY = pt + ShipLayout.BodyStartY;
        if (_model.Unsupported)
        {
            canvas.DrawText("Ship overview: not available for this ship", cx, textY, MenuStyle.TextStatus);
            textY += ShipLayout.BodyLineHeight;
        }
        else
        {
            canvas.DrawText($"{_model.Name} ({_model.ShipClassId}) — hull {_model.HullCurrent}/{_model.HullMax}",
                cx, pt + ShipLayout.BodyStartY, MenuStyle.TextStatus);

            // Wing panel (left half): one row per owned drone, destroyed/disabled never shown ready.
            var (wl, wt, _, _) = ShipLayout.WingPanelLocalRect();
            canvas.DrawText($"wing · torpedoes in hold {_torpedoStock}", pl + wl + ShipLayout.WingRowInset,
                pt + wt + 20, MenuStyle.TextStatus);
            for (int i = 0; i < _drones.Length; i++)
            {
                var drone = _drones[i];
                var row = ShipLayout.WingRowLocalRect(i);
                var rect = new SKRect(pl + row.Left, pt + row.Top, pl + row.Right, pt + row.Bottom);
                if (i == _selectedRow)
                {
                    using var highlight = new SKPaint { Color = MenuStyle.ColorButtonHover, Style = SKPaintStyle.Fill };
                    canvas.DrawRect(rect, highlight);
                }
                string marker = i == _selectedRow ? "› " : "  ";
                string state = ShipWingPanel.DroneStateLabel(drone.Destroyed, drone.Disabled);
                canvas.DrawText($"{marker}{drone.Id} — ammo {ShipWingPanel.DroneAmmoText(drone.AmmoCurrent, drone.AmmoCapacity)} ({state})",
                    rect.Left + 6, rect.Top + 19, MenuStyle.TextStatus);
            }
            if (_selectedRow >= 0)
                DrawRearmControls(canvas, pl, pt);

            // Crew panel (right half): one line per crew member.
            var (cl, ct, _, _) = ShipLayout.CrewPanelLocalRect();
            float crewY = pt + ct;
            foreach (var member in _model.Crew)
            {
                canvas.DrawText($"{member.DisplayName} ({member.Role})", pl + cl, crewY, MenuStyle.TextStatus);
                crewY += ShipLayout.BodyLineHeight;
            }
        }

        if (_campaign is not null)
        {
            var company = _campaign()?.Company;
            var summary = company?.Roundtrip;
            using var text = new SKPaint { Color = MenuStyle.TextStatus.Color, TextSize = 18, IsAntialias = true };
            canvas.DrawText(CampaignRoundtripPresentation.Summary(summary), pl + 32, pt + 440, text);
            canvas.DrawText(company?.Outcome?.RecoveryReason is not null
                ? CampaignOutcomePresentation.Guidance(company.Outcome)
                : CampaignRoundtripPresentation.Guidance(summary), pl + 32, pt + 468, text);
            DrawMemorial(canvas, pl, pt);
        }
        DrawCloseButton(canvas, pl, pt);
        DrawServices(canvas, pl, pt);
    }

    internal void RequestRepair(string target)
    {
        if (_service is null || PendingService is { IsCompleted: false }) return;
        PendingService = SubmitService(new RepairQuoteRequest(_commandId(),
            Math.Max(_serviceRevision ?? 0, _campaign?.Invoke()?.Revision ?? 0), target));
    }

    internal void ConfirmRepair()
    {
        if (_service is null || PendingService is { IsCompleted: false } || _repairState?.Quote is not { } quote) return;
        PendingService = SubmitService(new RepairCommand(_commandId(), quote.Revision, quote.Id, quote.TargetId));
    }

    private async Task SubmitService(DemoCampaignCommand command)
    {
        ServiceStatus = Localization.Get("Demo.Repair.Pending");
        try
        {
            var response = await _service!(command, CancellationToken.None);
            _serviceRevision = response.Revision;
            if (response.Applied)
            {
                _repairState = response.StationRepair;
                ServiceStatus = StationRepairPresentation.Summary(_repairState);
            }
            else { _repairState = null; ServiceStatus = StationRepairPresentation.Reason(response.Reason); }
        }
        catch (Exception) { _repairState = null; ServiceStatus = StationRepairPresentation.Reason("unavailable"); }
    }

    private void DrawServices(SKCanvas canvas, float pl, float pt)
    {
        if (_service is null) return;
        bool ready = PendingService is not { IsCompleted: false };
        DrawLocalButton(canvas, pl, pt, (730, 300, 1340, 332), Localization.Get("Demo.Repair.QuoteCarrier"), ready);
        DrawLocalButton(canvas, pl, pt, (730, 340, 1340, 372), Localization.Get("Demo.Repair.QuoteDrone"), ready && _selectedRow >= 0);
        using var paint = new SKPaint { Color = MenuStyle.TextStatus.Color, TextSize = 16, IsAntialias = true };
        canvas.Save();
        canvas.ClipRect(new SKRect(pl + 730, pt + 380, pl + 1370, pt + 525));
        canvas.DrawText(ServiceStatus.Length == 0 ? StationRepairPresentation.Summary(null) : ServiceStatus, pl + 730, pt + 400, paint);
        float y = pt + 425;
        if (_repairState?.Quote is { } quote)
            foreach (var line in quote.Items)
            {
                canvas.DrawText(string.Format(Localization.Get("Demo.Repair.Line"), Localization.Get("Demo.Repair." + line.Kind),
                    line.Quantity, line.UnitPrice, line.Total), pl + 730, y, paint);
                y += 24;
            }
        if (_repairState?.LastResult is { } result && _repairState.Quote is null)
            canvas.DrawText($"{Localization.Get("Demo.Repair.hull")}: {result.Before.Hull} → {result.After.Hull}; " +
                $"{Localization.Get("Demo.Repair.armor")}: {result.Before.Armor} → {result.After.Armor}", pl + 730, y, paint);
        canvas.Restore();
        DrawLocalButton(canvas, pl, pt, (730, 530, 1340, 562), Localization.Get("Demo.Repair.Confirm"), ready && _repairState?.Quote is not null);
    }

    private void DrawRearmControls(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var drone = _drones[_selectedRow];
        int max = MaxQuantity(_selectedRow);
        int quantity = ShipWingPanel.ClampQuantity(_quantity, max);
        bool confirmEnabled = _rearm is not null && quantity > 0 && !_controller.IsPending;

        DrawLocalButton(canvas, panelLeft, panelTop, ShipLayout.RearmMinusLocalRect(_drones.Length), "−",
            quantity > 0 && !_controller.IsPending);
        DrawLocalButton(canvas, panelLeft, panelTop, ShipLayout.RearmPlusLocalRect(_drones.Length), "+",
            quantity < max && !_controller.IsPending);
        DrawLocalButton(canvas, panelLeft, panelTop, ShipLayout.RearmConfirmLocalRect(_drones.Length), Localization.Get("Demo.Rearm.Action"),
            confirmEnabled);

        var controls = ShipLayout.RearmControlsLocalRect(_drones.Length);
        float x = panelLeft + controls.Left;
        var preview = max > 0
            ? ShipWingPanel.PreviewText(drone.AmmoCurrent, drone.AmmoCapacity, _torpedoStock, quantity)
            : Explanation(_selectedRow);
        canvas.DrawText($"quantity {quantity} · {preview}", x, panelTop + controls.Top + 52, MenuStyle.TextStatus);
        if (_status.Length > 0)
            canvas.DrawText(_status, x, panelTop + controls.Top + 72, MenuStyle.TextStatus);
    }

    private static void DrawLocalButton(SKCanvas canvas, float panelLeft, float panelTop,
        (float Left, float Top, float Right, float Bottom) local, string text, bool enabled)
    {
        var rect = new SKRect(panelLeft + local.Left, panelTop + local.Top, panelLeft + local.Right, panelTop + local.Bottom);
        MenuStyle.DrawButton(canvas, rect, text, enabled ? ButtonState.Normal : ButtonState.Disabled);
    }

    private void DrawCloseButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = ShipLayout.CloseButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "×", _isCloseHovered ? ButtonState.Hovered : ButtonState.Normal);
    }
}
