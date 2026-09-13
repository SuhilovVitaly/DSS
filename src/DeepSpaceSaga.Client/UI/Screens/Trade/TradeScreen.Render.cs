using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

public sealed partial class TradeScreen
{
    private static SKRect R(float x, float y, float w, float h = 25) => SKRect.Create(x, y, w, h);
    private void Button(TradePainter p, SKRect rect, string label, bool active = false, bool enabled = true, bool primary = false, float size = 16) =>
        p.Button(rect, label, rect.Contains(_pointer), active, enabled, primary, size);

    public void Render(SKCanvas canvas, int width, int height)
    {
        _width = width; _height = height; Refresh();
        float pl = TradeLayout.PanelLeft(width), pt = TradeLayout.PanelTop(height);
        MenuStyle.DrawPanel(canvas, SKRect.Create(pl, pt, TradeLayout.PanelWidth, TradeLayout.PanelHeight));
        // Keep the original shared toolbar call and all its coordinates. New content is clipped below it.
        DrawToolbar(canvas, pl, pt);
        canvas.Save(); canvas.Translate(pl, pt);
        canvas.ClipRect(new SKRect(0, StationToolbar.Height, TradeLayout.PanelWidth, TradeLayout.PanelHeight));
        using (var p = new TradePainter(canvas))
        {
            p.Box(new(1, 61, 1599, 799), TradePainter.Background, radius: 0);
            Button(p, TradeLayout.MarketTab, L("Market"), !Model.FuelMode);
            Button(p, TradeLayout.FuelTab, L("Fuel"), Model.FuelMode);
            p.Text(L(_buffer?.Latest?.Snapshot.DockedStationTrade is null ? "NotDocked" : "Paused"), R(450, 84, 534, 36), 14, TradePainter.Muted, align: SKTextAlign.Right);
            p.Box(TradeLayout.Catalog, border: TradePainter.Border);
            p.Box(TradeLayout.Detail, border: TradePainter.Border);
            if (_history) DrawHistory(p);
            else if (Model.FuelMode) DrawTanks(p);
            else DrawCatalog(p);
            DrawDetails(p);
            Button(p, TradeLayout.History, F("History", _journal.Entries.Count), _history, size: 14);
            p.Text(L("Keys"), R(420, 757, 1140, 30), 13, TradePainter.Muted, align: SKTextAlign.Right);
            if (CurrentCount > TradeLayout.VisibleRows)
            {
                p.Box(TradeLayout.Scroll, TradePainter.Border, radius: 5);
                p.Box(ScrollThumb, TradePainter.Muted, radius: 5);
            }
            if (_moduleOpen) DrawModuleOptions(p);
        }
        canvas.Restore();
        DrawToolbarTooltips(canvas, pl, pt);
    }
    private void DrawCatalog(TradePainter p)
    {
        p.Box(TradeLayout.Search, border: _focus == InputFocus.Search ? TradePainter.Cyan : TradePainter.Border);
        p.Text(Model.Query.Length == 0 ? L("Search") : Model.Query + (_focus == InputFocus.Search ? " |" : ""), R(48, 160, 383, 40), 16,
            Model.Query.Length == 0 ? TradePainter.Muted : TradePainter.TextColor);
        if (Model.Query.Length > 0) p.Text("×", TradeLayout.SearchClear, 22, TradePainter.Muted, align: SKTextAlign.Center);
        Button(p, TradeLayout.All, L("All"), Model.Filter == TradeFilter.All);
        Button(p, TradeLayout.Resources, L("Resources"), Model.Filter == TradeFilter.Resources);
        Button(p, TradeLayout.Goods, L("Goods"), Model.Filter == TradeFilter.Goods);
        p.Box(R(38, 218, 18, 18), Model.CargoOnly ? TradePainter.Cyan : TradePainter.Surface, TradePainter.Border, 3);
        p.Text(L("CargoOnly"), R(67, 211, 390, 33), 15, TradePainter.Muted);
        string[] headings = [L("Item"), L("Price"), L("StationStock"), L("SelectedCargo")];
        for (int i = 0; i < headings.Length; i++)
        {
            string title = headings[i] + (Model.Sort == (TradeSort)i ? Model.Descending ? " ↓" : " ↑" : "");
            p.Text(title, TradeLayout.Headers[i], 14, TradePainter.Muted, align: i == 0 ? SKTextAlign.Left : SKTextAlign.Right);
        }
        p.Line(36, 283, 970);
        for (int row = 0; row < TradeLayout.VisibleRows && row + _scroll < Model.Rows.Length; row++)
        {
            var item = Model.Rows[row + _scroll]; var rect = TradeLayout.Row(row);
            bool selected = item.ItemTypeId == Model.SelectedItemId;
            if (selected || rect.Contains(_pointer)) p.Box(rect, selected ? TradePainter.Selected : TradePainter.Surface, radius: 0);
            if (selected) p.Box(R(rect.Left, rect.Top + 3, 3, rect.Height - 6), TradePainter.Cyan, radius: 1);
            p.Icon(item.ItemTypeId, R(47, rect.Top + 6, 38, 38));
            p.Text(TradeItemPresentation.ItemDisplayName(item.ItemTypeId), R(99, rect.Top, 365, 50), 18, bold: selected);
            p.Text(N(item.UnitPriceCredits), R(478, rect.Top, 157, 50), 18, align: SKTextAlign.Right);
            p.Text(N(item.StockQuantity), R(635, rect.Top, 167, 50), 18, align: SKTextAlign.Right);
            p.Text(N(Model.Cargo(item.ItemTypeId)), R(802, rect.Top, 168, 50), 18, align: SKTextAlign.Right);
            p.Line(36, rect.Bottom, 970);
        }
        if (Model.Rows.Length == 0) p.Paragraph(L(_buffer?.Latest?.Snapshot.DockedStationTrade is null ? "NotDocked" : "NoMatches"), R(65, 335, 800, 100), 19);
        p.Text(L("PriceNote"), R(36, 703, 510, 27), 13, TradePainter.Muted);
        p.Text(F("Rows", Model.Rows.Length), R(730, 703, 240, 27), 13, TradePainter.Muted, align: SKTextAlign.Right);
    }
    private string ModuleLabel(InstalledModuleSnapshot module) => F(Model.FuelMode ? "TankNumber" : "HoldNumber",
        (Array.FindIndex(Model.Modules, m => m.ModuleId == module.ModuleId) + 1).ToString("D2"));
    private void DrawTanks(TradePainter p)
    {
        p.Text(L("TankTitle"), R(40, 158, 925, 38), 24, bold: true);
        p.Paragraph(L("TankDescription"), R(40, 207, 905, 62), 16);
        for (int row = 0; row < TradeLayout.VisibleRows && row + _scroll < Model.Modules.Length; row++)
        {
            var tank = Model.Modules[row + _scroll]; var rect = TradeLayout.Row(row);
            p.Box(rect, tank.ModuleId == Model.SelectedModuleId ? TradePainter.Selected : TradePainter.Surface, radius: 0);
            p.Text(ModuleLabel(tank), R(48, rect.Top, 525, 50), 17);
            p.Text(F("KgCapacity", N(tank.FuelAmountKg ?? 0), N(tank.FuelCapacityKg ?? 0)), R(590, rect.Top, 365, 32), 16, align: SKTextAlign.Right);
            double fraction = (double)(tank.FuelAmountKg ?? 0) / Math.Max(1, tank.FuelCapacityKg ?? 0);
            p.Bar(R(590, rect.Top + 35, 365, 5), fraction, fraction);
            p.Line(36, rect.Bottom, 970);
        }
        if (Model.Modules.Length == 0) p.Text(L("NoTank"), R(48, 325, 900, 40), 18, TradePainter.Muted);
        p.Text(L("FuelNote"), R(40, 704, 920, 26), 14, TradePainter.Muted);
    }
    private void DrawDetails(TradePainter p)
    {
        var item = Model.Item;
        if (item is null)
        {
            p.Text(L("SelectItem"), R(1044, 114, 510, 44), 24, bold: true);
            p.Paragraph(L(Model.FuelMode ? "FuelUnavailable" : "SelectDescription"), R(1044, 176, 490, 150), 18);
            DrawStatus(p); return;
        }
        p.Icon(item.ItemTypeId, R(1040, 99, 66, 66));
        p.Text(TradeItemPresentation.ItemDisplayName(item.ItemTypeId), R(1123, 95, 430, 36), 25, bold: true);
        p.Text(Model.FuelMode ? L("FuelService") : F("UnitMass", N(item.UnitMassKg)), R(1123, 135, 430, 26), 15, TradePainter.Muted);
        p.Text(TradeItemPresentation.ItemDescription(item.ItemTypeId), R(1040, 164, 520, 20), 12, TradePainter.Muted);
        if (Model.FuelMode) p.Text(L("FillTank"), R(1040, 192, 520, 36), 19, TradePainter.Cyan);
        else { Button(p, TradeLayout.Buy, L("Buy"), Model.Mode == TradeMode.Buy); Button(p, TradeLayout.Sell, L("Sell"), Model.Mode == TradeMode.Sell); }
        p.Text(L(Model.FuelMode ? "Tank" : Model.Mode == TradeMode.Buy ? "Destination" : "Source"), R(1040, 233, 520), 14, TradePainter.Muted);
        string moduleText = Model.Module is { } m ? ModuleLabel(m) + " · " + (Model.FuelMode
            ? F("KgCapacity", N(m.FuelAmountKg ?? 0), N(m.FuelCapacityKg ?? 0)) : F("FreeKg", N(m.AvailableCapacityKg ?? 0)))
            : L(Model.FuelMode ? "NoTank" : "NoContainer");
        Button(p, TradeLayout.Module, moduleText, enabled: Model.Modules.Length > 0, size: 15);
        if (Model.Modules.Length > 1) p.Chevron(1544, TradeLayout.Module.MidY);
        p.Text(L(Model.FuelMode ? "AddFuel" : "Quantity"), R(1040, 307, 520), 14, TradePainter.Muted);
        Button(p, TradeLayout.Minus, "−"); Button(p, TradeLayout.Plus, "+"); Button(p, TradeLayout.Max, L("Max"), size: 12);
        p.Box(TradeLayout.Quantity, border: _focus == InputFocus.Quantity ? TradePainter.Cyan : TradePainter.Border);
        string quantityText = _focus == InputFocus.Quantity ? _quantityText + " |" : N(Model.Quantity);
        p.Text(quantityText, R(1110, 337, 320, 40), 22, bold: true, align: SKTextAlign.Center);
        for (int i = 0; i < 3; i++) Button(p, TradeLayout.Presets[i], Model.FuelMode ? new[] { "50%", "75%", "100%" }[i] : new[] { "10", "50", "100" }[i]);
        var quote = Model.Quote;
        double fraction = quote.Maximum <= 1 ? 0 : (double)(Math.Clamp(Model.Quantity, 1, quote.Maximum) - 1) / (quote.Maximum - 1);
        p.Bar(R(1040, 449, 520, 5), fraction, fraction);
        p.Box(R(1040 + (float)fraction * 508, 445, 12, 12), TradePainter.Cyan, radius: 6);
        p.Text(quote.DisabledReason is { } error ? L(error) : F("Maximum", N(quote.Maximum), L(quote.LimitReason)),
            R(1040, 470, 520, 25), 13, quote.DisabledReason is null ? TradePainter.Muted : TradePainter.Red);
        p.Line(1040, 504, 1560);
        p.Text(L("Preview"), R(1040, 513, 520, 22), 13, TradePainter.Muted, true);
        long credits = _buffer?.Latest?.Snapshot.PlayerCredits ?? 0;
        Summary(p, 540, L("Balance"), quote.DisabledReason is null ? $"{N(credits)} → {N(quote.BalanceAfter)}" : N(credits));
        string amountLabel = L(Model.FuelMode ? "Tank" : "CargoAmount");
        long cargoAfter = Model.Mode == TradeMode.Sell ? quote.CargoQuantity - Model.Quantity : quote.CargoQuantity + Math.Min(Model.Quantity, long.MaxValue - quote.CargoQuantity);
        string amount = Model.FuelMode ? F("KgLoadChange", N(quote.AmountBefore), N(quote.AmountAfter), N(Model.Module?.FuelCapacityKg ?? 0)) : $"{N(quote.CargoQuantity)} → {N(cargoAfter)}";
        Summary(p, 567, amountLabel, quote.DisabledReason is null ? amount : "—");
        if (!Model.FuelMode && Model.Module?.CargoCapacityKg is > 0 and var cargoCapacity)
            Summary(p, 594, L("CargoLoad"), quote.DisabledReason is null
                ? F("KgLoadChange", N(cargoCapacity - quote.AmountBefore), N(cargoCapacity - quote.AmountAfter), N(cargoCapacity))
                : F("KgCapacity", N(cargoCapacity - quote.AmountBefore), N(cargoCapacity)));
        else if (!Model.FuelMode) Summary(p, 594, L("FreeSpace"), quote.DisabledReason is null ? F("KgChange", N(quote.AmountBefore), N(quote.AmountAfter)) : F("Kg", N(quote.AmountBefore)));
        else Summary(p, 594, L("StationStock"), F("Kg", N(item.StockQuantity)));
        if (Model.Module is { } module)
        {
            long capacity = Model.FuelMode ? module.FuelCapacityKg ?? 0 : module.CargoCapacityKg ?? 0;
            if (capacity > 0)
            {
                double before = Model.FuelMode ? (double)quote.AmountBefore / capacity : 1 - (double)quote.AmountBefore / capacity;
                double after = quote.DisabledReason is not null ? before : Model.FuelMode ? (double)quote.AmountAfter / capacity : 1 - (double)quote.AmountAfter / capacity;
                p.Bar(R(1040, 623, 520, 5), before, after);
            }
        }
        p.Text(L("Total"), R(1040, 633, 160, 26), 19, bold: true);
        p.Text(quote.DisabledReason == "ValueOverflow" ? "—" : F("Tokens", N(quote.Total)), R(1205, 633, 350, 26), 21, bold: true, align: SKTextAlign.Right);
        string confirm = IsPending ? L("Pending") : F(Model.Mode switch { TradeMode.Sell => "ConfirmSell", TradeMode.Refuel => "ConfirmFuel", _ => "ConfirmBuy" }, N(Model.Quantity), N(quote.Total));
        Button(p, TradeLayout.Confirm, confirm, enabled: CanConfirm, primary: true, size: 16);
        DrawStatus(p);
    }
    private static void Summary(TradePainter p, float y, string name, string value)
    { p.Text(name, R(1040, y, 240), 15, TradePainter.Muted); p.Text(value, R(1290, y, 270), 16, align: SKTextAlign.Right); }
    private string EntryMessage(TradeJournal.Entry entry)
    {
        string item = TradeItemPresentation.ItemDisplayName(entry.ItemId);
        if (entry.Result is null) return F("SendingItem", item);
        if (entry.Result.Status != CommandResultStatus.Executed) return F("Rejected", item, Reason(entry.Result.ReasonCode));
        long quantity = entry.Result.ExecutedQuantity ?? entry.RequestedQuantity;
        string key = quantity < entry.RequestedQuantity ? "PartialResult" : "SuccessResult";
        decimal total = (decimal)quantity * entry.UnitPrice;
        return F(key, item, N(quantity), total.ToString("N0").Replace(',', ' '), N(entry.RequestedQuantity));
    }
    private void DrawStatus(TradePainter p)
    {
        string message; SKColor color;
        if (_handle?.Failure is not null) { message = L("ConnectionLost"); color = TradePainter.Red; }
        else if (IsPending) { message = L("AwaitingStation"); color = TradePainter.Muted; }
        else if (_journal.Latest is { } entry) { message = EntryMessage(entry); color = entry.Result?.Status == CommandResultStatus.Executed ? TradePainter.Green : TradePainter.Red; }
        else { message = Model.Item is not null && Model.Quote.DisabledReason is { } reason ? L(reason) : L("Ready"); color = TradePainter.Muted; }
        p.Text(message, R(1040, 716, 520, 23), 12, color);
    }
    private static string Reason(string? code) => code switch
    {
        CommandReasonCodes.CargoCapacityExceeded => L("CapacityLimit"), CommandReasonCodes.FuelCapacityExceeded => L("TankLimit"),
        CommandReasonCodes.InsufficientPlayerCredits => L("MoneyLimit"), CommandReasonCodes.InsufficientStationStock => L("StockLimit"),
        CommandReasonCodes.InsufficientCargoQuantity => L("CargoLimit"), CommandReasonCodes.ModuleUnavailable => L("ModuleUnavailable"),
        CommandReasonCodes.NotDocked => L("NotDocked"), CommandReasonCodes.InvalidQuantity => L("EnterQuantity"),
        "value_overflow" => L("ValueOverflow"), _ => L("TradeRejected")
    };
    private void DrawHistory(TradePainter p)
    {
        p.Text(F("History", _journal.Entries.Count), R(40, 157, 900, 40), 24, bold: true);
        p.Text(L("HistoryNote"), R(40, 209, 920, 35), 15, TradePainter.Muted);
        for (int row = 0; row < TradeLayout.VisibleRows && row + _historyScroll < _journal.Entries.Count; row++)
        {
            var entry = _journal.Entries[_journal.Entries.Count - 1 - row - _historyScroll]; var rect = TradeLayout.Row(row);
            p.Icon(entry.ItemId, R(48, rect.Top + 6, 38, 38));
            string mode = L(entry.Mode switch { TradeMode.Sell => "Sell", TradeMode.Refuel => "Fuel", _ => "Buy" });
            p.Text(mode + " · " + entry.ModuleLabel, R(101, rect.Top, 250, 50), 14, TradePainter.Muted);
            p.Text(EntryMessage(entry), R(363, rect.Top, 595, 50), 15,
                entry.Result is null ? TradePainter.Muted : entry.Result.Status == CommandResultStatus.Executed ? TradePainter.Green : TradePainter.Red);
            p.Line(36, rect.Bottom, 970);
        }
        if (_journal.Entries.Count == 0) p.Text(L("HistoryEmpty"), R(48, 333, 900, 40), 18, TradePainter.Muted);
    }
    private void DrawModuleOptions(TradePainter p)
    {
        for (int i = 0; i < Math.Min(5, Model.Modules.Length); i++)
        {
            var module = Model.Modules[i + _moduleScroll];
            string amount = Model.FuelMode ? F("KgCapacity", N(module.FuelAmountKg ?? 0), N(module.FuelCapacityKg ?? 0)) : F("FreeKg", N(module.AvailableCapacityKg ?? 0));
            Button(p, TradeLayout.ModuleOption(i), ModuleLabel(module) + " · " + amount, module.ModuleId == Model.SelectedModuleId, size: 14);
        }
    }
}
