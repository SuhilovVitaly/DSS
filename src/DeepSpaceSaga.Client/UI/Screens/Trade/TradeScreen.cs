using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>Station trading with an unchanged shared toolbar and a persistent transaction panel.</summary>
public sealed partial class TradeScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;
    private readonly GameSessionHandle? _handle;
    private readonly TradeJournal _journal;
    internal TradeModel Model { get; } = new();
    private int _width, _height, _scroll, _historyScroll, _moduleScroll;
    private SKPoint _pointer = new(-1, -1);
    private bool _history, _moduleOpen, _dragSlider, _dragScroll, _replaceInput, _controlDown, _enterHeld;
    private float _scrollGrab;
    private enum InputFocus { None, Search, Quantity }
    private InputFocus _focus;
    private string _quantityText = "1";
    internal bool IsPending => _journal.IsPending;
    internal IReadOnlyList<TradeJournal.Entry> History => _journal.Entries;
    internal bool CanConfirm => Model.AuthoritativeQuote is { DisabledReason: null } && Model.Quote.DisabledReason is null &&
        !IsPending && _handle is not null && _handle.Failure is null;
    internal int ScrollOffset => _scroll;

    // ── Authoritative quote lifecycle (EP-0001-US-0003-TK-0004) ──
    // Everything that a quote's numbers or binding depend on. Any change starts exactly one new request.
    private readonly record struct QuoteKey(string StationId, long? MarketRevision, string ShipId, string ModuleId,
        string CommandType, string ItemId, long Quantity, long PlayerCredits, long ItemStock, long MaxSellable,
        long? FreeStockCapacity, long Cargo, long FreeCargoKg, long FuelAmountKg, long FuelCapacityKg, bool ModuleReady);
    private QuoteKey? _quoteKey;
    private long _quoteGeneration;
    private string _quoteRequestId = "";
    private CancellationTokenSource? _quoteCts;
    private Task<TradeQuoteSnapshot>? _quoteTask;
    private bool _requoteOnce, _closed;

    public TradeScreen(SnapshotBuffer? buffer = null, GameSessionHandle? handle = null)
    {
        _buffer = buffer; _handle = handle; _journal = handle?.Trades ?? new TradeJournal();
    }
    private void Refresh()
    {
        bool staleResult = _journal.Refresh(_buffer);
        Model.Refresh(_buffer?.Latest?.Snapshot);
        _requoteOnce |= staleResult;
        UpdateQuote();
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, ListCount - TradeLayout.VisibleRows));
        _historyScroll = Math.Clamp(_historyScroll, 0, Math.Max(0, _journal.Entries.Count - TradeLayout.VisibleRows));
        _moduleScroll = Math.Clamp(_moduleScroll, 0, Math.Max(0, Model.Modules.Length - 5));
    }
    private QuoteKey? CurrentQuoteKey()
    {
        var snapshot = _buffer?.Latest?.Snapshot;
        if (snapshot?.DockedStationTrade is not { } trade || string.IsNullOrEmpty(snapshot.PlayerShipObjectId) ||
            Model.Item is not { } item || Model.Module is not { } module || Model.Quantity <= 0) return null;
        return new(trade.StationObjectId, trade.MarketRevision, snapshot.PlayerShipObjectId, module.ModuleId, Model.CommandType,
            item.ItemTypeId, Model.Quantity, snapshot.PlayerCredits, item.StockQuantity, item.MaxSellableQuantity, item.FreeStockCapacity,
            Model.Cargo(item.ItemTypeId), module.AvailableCapacityKg ?? 0, module.FuelAmountKg ?? 0, module.FuelCapacityKg ?? 0,
            module.PowerState == "On" && module.OperationalState == "Ready" && module.StructurePoints > 0);
    }
    /// <summary>
    /// Runs on the UI path only (from Refresh). A changed key cancels the previous request, drops its quote and
    /// starts one new request; an unchanged key starts nothing. A completed response is applied only when it
    /// answers the current RequestId and binding; the render loop never blocks on an unfinished task.
    /// </summary>
    private void UpdateQuote()
    {
        if (_closed) return;
        var key = CurrentQuoteKey();
        if (key != _quoteKey || _requoteOnce)
        {
            _requoteOnce = false;
            CancelQuoteRequest();
            _quoteKey = key;
            if (key is null) Model.InvalidateQuote("QuoteRequired");
            else if (_handle is null) Model.InvalidateQuote("QuoteUnavailable");
            else StartQuoteRequest(key.Value);
        }
        if (_quoteTask is not { IsCompleted: true } task) return;
        _quoteTask = null;
        _quoteCts?.Dispose(); _quoteCts = null;
        if (!task.IsCompletedSuccessfully) Model.InvalidateQuote("QuoteUnavailable");
        else if (_quoteKey is { } current && Answers(task.Result, current)) Model.ApplyQuote(task.Result);
        else Model.InvalidateQuote("InvalidQuote");
    }
    private bool Answers(TradeQuoteSnapshot quote, QuoteKey key) =>
        quote.RequestId == _quoteRequestId && quote.ObjectId == key.ShipId && quote.ModuleId == key.ModuleId &&
        quote.CommandType == key.CommandType && quote.ItemTypeId == key.ItemId && quote.RequestedQuantity == key.Quantity &&
        (string.IsNullOrEmpty(quote.StationObjectId) || quote.StationObjectId == key.StationId);
    private void StartQuoteRequest(QuoteKey key)
    {
        _quoteGeneration++;
        _quoteRequestId = $"TQ-{_quoteGeneration:D6}-{Guid.NewGuid():N}";
        _quoteCts = new CancellationTokenSource();
        Model.InvalidateQuote("QuoteLoading");
        var request = new TradeQuoteRequest(_quoteRequestId, key.ShipId, key.ModuleId, key.CommandType, key.ItemId, key.Quantity);
        try { _quoteTask = _handle!.GetTradeQuoteAsync(request, _quoteCts.Token).AsTask(); }
        catch (Exception error) { _quoteTask = Task.FromException<TradeQuoteSnapshot>(error); }
    }
    /// <summary>Make the current generation inactive: its late completion is never observed.</summary>
    private void CancelQuoteRequest()
    {
        _quoteTask = null;
        if (_quoteCts is not { } cts) return;
        _quoteCts = null;
        cts.Cancel(); cts.Dispose();
    }
    private int ListCount => Model.FuelMode ? Model.Modules.Length : Model.Rows.Length;
    private int CurrentCount => _history ? _journal.Entries.Count : ListCount;
    private int CurrentOffset { get => _history ? _historyScroll : _scroll; set { if (_history) _historyScroll = value; else _scroll = value; } }
    private SKPoint Local(float x, float y) => new(x - TradeLayout.PanelLeft(_width), y - TradeLayout.PanelTop(_height));
    internal static string L(string key) => Localization.Get("TradeUX." + key);
    internal static string N(long value) => value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
    internal static string F(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, L(key), args);
    private void SetQuantity(long quantity) { Model.Quantity = Math.Max(0, quantity); _quantityText = Model.Quantity.ToString(CultureInfo.InvariantCulture); _focus = InputFocus.None; }
    private void SetMode(TradeMode mode) { Model.SetMode(mode); SetQuantity(Model.Quantity); _scroll = 0; _history = false; _moduleOpen = false; }
    private void SelectRow(int index)
    {
        if (index < 0 || index >= ListCount) return;
        if (Model.FuelMode) Model.SelectModule(Model.Modules[index].ModuleId);
        else Model.Select(Model.Rows[index].ItemTypeId);
        SetQuantity(Model.Quantity);
    }
    private SKRect ScrollThumb
    {
        get
        {
            var track = TradeLayout.Scroll;
            float height = Math.Max(30, track.Height * TradeLayout.VisibleRows / Math.Max(TradeLayout.VisibleRows, CurrentCount));
            float top = track.Top + (track.Height - height) * CurrentOffset / Math.Max(1, CurrentCount - TradeLayout.VisibleRows);
            return new(track.Left, top, track.Right, top + height);
        }
    }
    private void SetSlider(float x)
    {
        long maximum = Model.Quote.Maximum;
        double fraction = Math.Clamp((x - TradeLayout.Slider.Left) / TradeLayout.Slider.Width, 0, 1);
        SetQuantity(maximum <= 0 ? 0 : Math.Clamp((long)Math.Round((decimal)fraction * (maximum - 1)) + 1, 1, maximum));
    }
    public void OnActivated()
    {
        _focus = InputFocus.None; _moduleOpen = false; _dragSlider = _dragScroll = false;
        _controlDown = _enterHeld = false; _stationHovered = _exitHovered = false;
        _closed = false; Array.Clear(_toolbarHoverStarted); Refresh();
    }
    public void OnDeactivated()
    {
        _focus = InputFocus.None; _dragSlider = _dragScroll = false; _controlDown = _enterHeld = false;
        // Cancel only the quote request; a sent trade keeps completing through the handle and the session journal.
        _closed = true; CancelQuoteRequest(); _quoteKey = null;
    }
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);
    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return ScreenEvent.None;
        Refresh(); var p = Local(x, y);
        if (StationToolbar.ExitButtonLocalRect().Contains(p)) return ScreenEvent.CloseTrade;
        if (StationNameRect.Contains(p)) return ScreenEvent.NavigateToStation;
        if (!SKRect.Create(0, 0, TradeLayout.PanelWidth, TradeLayout.PanelHeight).Contains(p)) return ScreenEvent.CloseTrade;
        if (_moduleOpen)
        {
            for (int i = 0; i < Math.Min(5, Model.Modules.Length); i++)
                if (TradeLayout.ModuleOption(i).Contains(p)) { Model.SelectModule(Model.Modules[i + _moduleScroll].ModuleId); _moduleOpen = false; return ScreenEvent.None; }
            _moduleOpen = false;
            return ScreenEvent.None;
        }
        _focus = InputFocus.None;
        if (TradeLayout.MarketTab.Contains(p)) SetMode(TradeMode.Buy);
        else if (TradeLayout.FuelTab.Contains(p)) SetMode(TradeMode.Refuel);
        else if (TradeLayout.History.Contains(p)) { _history = !_history; _historyScroll = 0; }
        else if (TradeLayout.Scroll.Contains(p) && CurrentCount > TradeLayout.VisibleRows)
        {
            _dragScroll = true; _scrollGrab = ScrollThumb.Contains(p) ? p.Y - ScrollThumb.Top : ScrollThumb.Height / 2;
            DragScroll(p.Y);
        }
        else if (TradeLayout.Rows.Contains(p))
        {
            if (!_history) SelectRow(_scroll + (int)((p.Y - TradeLayout.Rows.Top) / TradeLayout.RowHeight));
        }
        else if (!_history && !Model.FuelMode && TradeLayout.Search.Contains(p))
        {
            if (TradeLayout.SearchClear.Contains(p)) { Model.Query = ""; _scroll = 0; }
            _focus = InputFocus.Search; _replaceInput = false;
        }
        else if (!_history && !Model.FuelMode && TradeLayout.All.Contains(p)) { Model.SetFilter(TradeFilter.All); _scroll = 0; }
        else if (!_history && !Model.FuelMode && TradeLayout.Resources.Contains(p)) { Model.SetFilter(TradeFilter.Resources); _scroll = 0; }
        else if (!_history && !Model.FuelMode && TradeLayout.Goods.Contains(p)) { Model.SetFilter(TradeFilter.Goods); _scroll = 0; }
        else if (!_history && !Model.FuelMode && TradeLayout.CargoOnly.Contains(p)) { Model.ToggleCargoOnly(); _scroll = 0; }
        else if (!_history && !Model.FuelMode && Array.FindIndex(TradeLayout.Headers, r => r.Contains(p)) is var column && column >= 0)
        { Model.SetSort((TradeSort)column); RevealSelection(); }
        else if (Model.Item is not null)
        {
            if (!Model.FuelMode && TradeLayout.Buy.Contains(p)) SetMode(TradeMode.Buy);
            else if (!Model.FuelMode && TradeLayout.Sell.Contains(p)) SetMode(TradeMode.Sell);
            else if (TradeLayout.Module.Contains(p) && Model.Modules.Length > 1) { _moduleOpen = true; _moduleScroll = 0; }
            else if (TradeLayout.Quantity.Contains(p)) { _focus = InputFocus.Quantity; _replaceInput = true; }
            else if (TradeLayout.Minus.Contains(p)) SetQuantity(Math.Max(1, Model.Quantity - 1));
            else if (TradeLayout.Plus.Contains(p)) SetQuantity(Model.Quantity < Model.Quote.Maximum ? Model.Quantity + 1 : Model.Quote.Maximum);
            else if (TradeLayout.Max.Contains(p)) SetQuantity(Model.Quote.Maximum);
            else if (TradeLayout.Slider.Contains(p)) { _dragSlider = true; SetSlider(p.X); }
            else if (TradeLayout.Confirm.Contains(p)) Submit();
            else for (int i = 0; i < TradeLayout.Presets.Length; i++)
                if (TradeLayout.Presets[i].Contains(p))
                {
                    if (Model.FuelMode) { Model.FillTank(new[] { 50, 75, 100 }[i]); SetQuantity(Model.Quantity); }
                    else SetQuantity(Math.Min(Model.Quote.Maximum, new[] { 10, 50, 100 }[i]));
                }
        }
        Refresh(); return ScreenEvent.None;
    }
    private void Submit()
    {
        Refresh();
        if (!CanConfirm || Model.Item is not { } item || Model.Module is not { } module || _buffer?.Latest?.Snapshot.PlayerShipObjectId is not { } shipId) return;
        // Freshness guard: the shown quote must answer exactly the current key; Refresh has just re-checked it.
        if (Model.AuthoritativeQuote is not { } quote || _quoteKey is not { } key || key != CurrentQuoteKey() || !Answers(quote, key)) return;
        // Requested quantity (not the executable part) travels with the exact shown binding.
        string id = _handle!.SendTradeCommand(shipId, module.ModuleId, key.CommandType, item.ItemTypeId, Model.Quantity,
            quote.QuoteId, quote.MarketRevision);
        _journal.Track(new(id, item.ItemTypeId, module.ModuleId, Model.Mode, Model.Quantity, item.UnitPriceCredits, ModuleLabel: ModuleLabel(module),
            QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision, QuotedTotalCredits: quote.TotalCredits));
    }
    private void RevealSelection()
    {
        int index = Model.FuelMode ? Array.FindIndex(Model.Modules, m => m.ModuleId == Model.SelectedModuleId)
            : Array.FindIndex(Model.Rows, i => i.ItemTypeId == Model.SelectedItemId);
        if (index >= 0) _scroll = Math.Clamp(_scroll, Math.Max(0, index - TradeLayout.VisibleRows + 1), index);
    }
    public void OnMouseUp(float x, float y) { _dragSlider = _dragScroll = false; }
    private void DragScroll(float y)
    {
        float range = TradeLayout.Scroll.Height - ScrollThumb.Height;
        CurrentOffset = (int)Math.Round(Math.Clamp((y - TradeLayout.Scroll.Top - _scrollGrab) / Math.Max(1, range), 0, 1) * Math.Max(0, CurrentCount - TradeLayout.VisibleRows));
    }
    public bool OnMouseMove(float x, float y)
    {
        Refresh(); _pointer = Local(x, y); UpdateToolbarHover(_pointer);
        if (_dragSlider) SetSlider(_pointer.X);
        if (_dragScroll) DragScroll(_pointer.Y);
        if (_pointer.Y < StationToolbar.Height) return _stationHovered || _exitHovered;
        if (_moduleOpen) return Enumerable.Range(0, Math.Min(5, Model.Modules.Length)).Any(i => TradeLayout.ModuleOption(i).Contains(_pointer));
        if (TradeLayout.Confirm.Contains(_pointer)) return CanConfirm;
        if (TradeLayout.MarketTab.Contains(_pointer) || TradeLayout.FuelTab.Contains(_pointer) || TradeLayout.History.Contains(_pointer)) return true;
        if (TradeLayout.Rows.Contains(_pointer)) return !_history && CurrentOffset + (int)((_pointer.Y - TradeLayout.Rows.Top) / TradeLayout.RowHeight) < ListCount;
        if (!_history && !Model.FuelMode && (TradeLayout.Search.Contains(_pointer) || TradeLayout.CargoOnly.Contains(_pointer) ||
            TradeLayout.All.Contains(_pointer) || TradeLayout.Resources.Contains(_pointer) || TradeLayout.Goods.Contains(_pointer) || TradeLayout.Headers.Any(r => r.Contains(_pointer)))) return true;
        return Model.Item is not null && (TradeLayout.Quantity.Contains(_pointer) || TradeLayout.Minus.Contains(_pointer) || TradeLayout.Plus.Contains(_pointer) ||
            TradeLayout.Max.Contains(_pointer) || TradeLayout.Slider.Contains(_pointer) || TradeLayout.Presets.Any(r => r.Contains(_pointer)) ||
            (Model.Modules.Length > 1 && TradeLayout.Module.Contains(_pointer)) || (!Model.FuelMode && (TradeLayout.Buy.Contains(_pointer) || TradeLayout.Sell.Contains(_pointer))));
    }
    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        Refresh(); var p = Local(x, y); int step = delta > 0 ? -1 : delta < 0 ? 1 : 0;
        if (_moduleOpen) _moduleScroll = Math.Clamp(_moduleScroll + step, 0, Math.Max(0, Model.Modules.Length - 5));
        else if (TradeLayout.Catalog.Contains(p)) CurrentOffset = Math.Clamp(CurrentOffset + step, 0, Math.Max(0, CurrentCount - TradeLayout.VisibleRows));
        return ScreenEvent.None;
    }
    public void OnTextInput(char c)
    {
        if (char.IsControl(c) || _controlDown) return;
        if (_focus == InputFocus.Search)
        {
            if (_replaceInput) { Model.Query = ""; _replaceInput = false; }
            if (Model.Query.Length < 80) Model.Query += c;
            _scroll = 0;
        }
        if (_focus == InputFocus.Quantity && c is >= '0' and <= '9')
        {
            if (_replaceInput) { _quantityText = ""; _replaceInput = false; }
            if (_quantityText.Length < 19) _quantityText += c;
            ParseQuantity();
        }
    }
    private void ParseQuantity() => Model.Quantity = long.TryParse(_quantityText, NumberStyles.None, CultureInfo.InvariantCulture, out long value) ? value : 0;
    public ScreenEvent OnKeyDown(Key key)
    {
        Refresh();
        if (key is Key.ControlLeft or Key.ControlRight) { _controlDown = true; return ScreenEvent.None; }
        if (_controlDown && key == Key.A && _focus != InputFocus.None) { _replaceInput = true; return ScreenEvent.None; }
        if (key == Key.Escape)
        {
            if (_moduleOpen) { _moduleOpen = false; return ScreenEvent.None; }
            if (_focus != InputFocus.None) { _focus = InputFocus.None; return ScreenEvent.None; }
            if (_history) { _history = false; return ScreenEvent.None; }
            return ScreenEvent.CloseTrade;
        }
        if (key == Key.Enter)
        {
            if (_enterHeld) return ScreenEvent.None;
            _enterHeld = true;
            if (_focus != InputFocus.None) { _focus = InputFocus.None; return ScreenEvent.None; }
            if (_moduleOpen) { _moduleOpen = false; return ScreenEvent.None; }
            Submit(); return ScreenEvent.None;
        }
        if (key is Key.Backspace or Key.Delete && _focus != InputFocus.None)
        {
            if (_focus == InputFocus.Search) Model.Query = _replaceInput || key == Key.Delete ? "" : Model.Query.Length > 0 ? Model.Query[..^1] : "";
            else { _quantityText = _replaceInput || key == Key.Delete ? "" : _quantityText.Length > 0 ? _quantityText[..^1] : ""; ParseQuantity(); }
            _replaceInput = false;
        }
        if (key == Key.Tab) { _focus = _focus == InputFocus.Search && Model.Item is not null ? InputFocus.Quantity : InputFocus.Search; _replaceInput = _focus == InputFocus.Quantity; }
        if (_focus == InputFocus.None && !_history && key is Key.Up or Key.Down)
        {
            int current = Model.FuelMode ? Array.FindIndex(Model.Modules, m => m.ModuleId == Model.SelectedModuleId) : Array.FindIndex(Model.Rows, i => i.ItemTypeId == Model.SelectedItemId);
            int index = Math.Clamp(current + (key == Key.Down ? 1 : -1), 0, Math.Max(0, ListCount - 1));
            SelectRow(index);
            _scroll = Math.Clamp(_scroll, Math.Max(0, index - TradeLayout.VisibleRows + 1), index);
        }
        return ScreenEvent.None;
    }
    public void OnKeyUp(Key key) { if (key is Key.ControlLeft or Key.ControlRight) _controlDown = false; if (key == Key.Enter) _enterHeld = false; }
}
