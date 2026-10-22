using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>Logical UI coordinates; resource/layout work occurs only when content changes.</summary>
internal sealed class DebugTuningPanel : IDisposable
{
    internal const float Width = 360;
    private const float FooterHeight = 38, SectionHeight = 28, RowHeight = 40;
    private float _headerHeight = 62;
    internal static readonly string[] SectionKeys = ["Debug.Tetrarch", "Debug.Mukha", "Debug.TorpedoesTetrarch", "Debug.TorpedoesMukha", "Debug.Countermissiles"];
    internal static readonly string[] ModuleIds = ["module.engine.tetrarch", "module.engine.strizh", "module.torpedo.launcher.basic", "module.torpedo.launcher.light", "module.countermeasure.launcher.basic"];
    private readonly Func<string, string> _text;
    private readonly SKFont _font = new(MenuStyle.TypefaceRegular, 12);
    private readonly SKTypeface _italicTypeface = SKTypeface.FromFamilyName("Verdana", SKFontStyle.Italic);
    private readonly SKFont _italicFont;
    private readonly SKPaint _paint = new() { IsAntialias = true, Typeface = MenuStyle.TypefaceRegular, TextSize = 12 };
    private readonly SKTextBlob[] _sections = new SKTextBlob[5];
    private readonly bool[] _collapsed = new bool[5];
    private readonly SKRect[] _sectionBounds = new SKRect[5];
    private readonly List<Row> _rows = [];
    private readonly SKTextBlob _title, _minus, _plus, _resetAll, _folder, _help, _confirmReset;
    private readonly SKTextBlob[] _helpLines;
    private SKTextBlob[] _statusLines = [];
    private string? _feedbackKey, _startupError;
    private DebugTuningFileStatus? _fileStatus;
    private bool _feedbackPending, _resetConfirm, _statusError, _helpVisible;
    private long _feedbackRevision = -1;
    private DebugTuningSnapshot? _snapshot;
    private float _contentHeight, _scroll;
    private bool _disposed;
    internal bool Visible { get; set; }
    internal SKRect Bounds { get; private set; }
    internal SKRect Viewport => new(Bounds.Left, Bounds.Top + _headerHeight, Bounds.Right, Bounds.Bottom - FooterHeight);
    internal SKRect ResetAllBounds => new(Bounds.Left + 8, Bounds.Bottom - 32, Bounds.MidX - 4, Bounds.Bottom - 6);
    internal SKRect FolderBounds => new(Bounds.MidX + 4, Bounds.Bottom - 32, Bounds.Right - 8, Bounds.Bottom - 6);
    internal bool HelpVisible => _helpVisible;
    internal string StatusText { get; private set; } = "";
    internal int RowCount => _rows.Count;
    internal int SectionCount => _sections.Length;
    internal float ScrollOffset => _scroll;
    internal bool IsDisposed => _disposed;

    internal sealed class Row(DebugTuningValue value, double catalogDefault, int section)
    {
        internal DebugTuningValue Value = value;
        internal readonly double Default = catalogDefault;
        internal readonly int Section = section;
        internal SKTextBlob Label = null!, Number = null!, DefaultNumber = null!;
        internal SKRect Bounds;
        internal double? PendingValue;
        internal double DisplayValue => PendingValue ?? Value.Value;
        internal bool Modified => Value.Value != Default;
    }

    internal DebugTuningPanel(Func<string, string>? text = null)
    {
        _italicFont = new(_italicTypeface, 12);
        _text = text ?? Localization.Get;
        _title = Blob(_text("Debug.Title"));
        _minus = Blob("−"); _plus = Blob("+"); _help = Blob("?");
        _resetAll = Blob(_text("Debug.ResetAll")); _folder = Blob(_text("Debug.OpenFolder"));
        _confirmReset = Blob(_text("Debug.ConfirmReset"));
        _helpLines = Wrap(_text("Debug.HelpOrbit") + "\n" + _text("Debug.HelpTorpedoes"));
        for (int i = 0; i < _sections.Length; i++) _sections[i] = Blob(_text(SectionKeys[i]));
    }

    internal void Prepare(float viewportWidth, float viewportHeight, DebugTuningSnapshot? snapshot)
    {
        if (_disposed) return;
        Bounds = new(Math.Max(0, viewportWidth - Width - 8), 8, Math.Max(0, viewportWidth - 8), Math.Max(108, viewportHeight - 8));
        if (!ReferenceEquals(snapshot, _snapshot))
        {
            ClearRows();
            _snapshot = snapshot;
            if (snapshot is not null)
                for (int section = 0; section < ModuleIds.Length; section++)
                    foreach (var value in snapshot.Values)
                    {
                        if (value.ModuleTypeId != ModuleIds[section]) continue;
                        double catalogDefault = value.Value;
                        foreach (var entry in snapshot.Defaults)
                            if (SameKey(value, entry)) { catalogDefault = entry.Value; break; }
                        var row = new Row(value, catalogDefault, section)
                        {
                            Label = Blob(_text("Debug." + value.Field) + (value.ProfileId is null ? "" : " · " + _text("Debug." + value.ProfileId))),
                            Number = Blob(Format(value.Value)),
                            DefaultNumber = Blob(Format(catalogDefault))
                        };
                        _rows.Add(row);
                    }
        }
        Layout();
    }

    private void Layout()
    {
        float y = 0;
        for (int section = 0; section < _sections.Length; section++)
        {
            _sectionBounds[section] = new(Bounds.Left + 6, y, Bounds.Right - 6, y + SectionHeight);
            y += SectionHeight;
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Section != section) continue;
                row.Bounds = _collapsed[section] ? SKRect.Empty : new(Bounds.Left + 8, y, Bounds.Right - 8, y + RowHeight);
                if (!_collapsed[section]) y += RowHeight;
            }
        }
        _contentHeight = y + (_helpVisible ? 12 + _helpLines.Length * 16 : 0);
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, _contentHeight - Viewport.Height));
    }

    internal bool Contains(float x, float y) => Visible && !_disposed && Bounds.Contains(x, y);
    internal bool Scroll(float x, float y, float delta)
    {
        if (!Contains(x, y)) return false;
        _scroll = Math.Clamp(_scroll - delta * RowHeight, 0, Math.Max(0, _contentHeight - Viewport.Height));
        return true;
    }

    internal bool ToggleSection(float x, float y)
    {
        if (!Contains(x, y) || !Viewport.Contains(x, y)) return false;
        y += _scroll - Viewport.Top;
        for (int i = 0; i < _sections.Length; i++)
            if (_sectionBounds[i].Contains(x, y)) { _collapsed[i] = !_collapsed[i]; Layout(); return true; }
        return false;
    }

    internal Row GetRow(int index) => _rows[index];
    internal void SetFeedback(string? errorKey, string? startupError, bool pending, bool resetConfirm, DebugTuningFileStatus? fileStatus, long revision)
    {
        if (_feedbackKey == errorKey && _startupError == startupError && _feedbackPending == pending &&
            _resetConfirm == resetConfirm && ReferenceEquals(_fileStatus, fileStatus) && _feedbackRevision == revision) return;
        _feedbackKey = errorKey; _startupError = startupError; _feedbackPending = pending;
        _resetConfirm = resetConfirm; _fileStatus = fileStatus; _feedbackRevision = revision;
        _statusError = errorKey is not null || startupError is not null || fileStatus?.Error is not null;
        StatusText = errorKey is not null ? _text(errorKey) : startupError is not null ? _text("Debug.StartupRejected") + ": " + startupError :
            fileStatus?.Error is { } diskError ? _text("Debug.WriteFailed") + ": " + diskError :
            pending ? _text("Debug.Sending") : fileStatus is { Deleted: true } && fileStatus.Revision >= revision ? _text("Debug.ResetComplete") :
            fileStatus?.WrittenAtUtc is { } written && fileStatus.Revision >= revision ? _text("Debug.AppliedFile") + " " + written.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture) :
            revision > 0 ? _text("Debug.AppliedSaving") : _text("Debug.Ready");
        foreach (var line in _statusLines) line.Dispose();
        _statusLines = Wrap(StatusText);
        _headerHeight = Math.Max(62, 34 + _statusLines.Length * 15);
        Layout();
    }

    internal bool ToggleHelp(float x, float y)
    {
        if (!Contains(x, y) || x < Bounds.Right - 32 || y > Bounds.Top + 28) return false;
        _helpVisible = !_helpVisible; Layout();
        if (_helpVisible) _scroll = Math.Max(0, _contentHeight - Viewport.Height);
        return true;
    }
    internal void SetPending(int index, double? value)
    {
        var row = _rows[index];
        if (row.PendingValue == value) return;
        row.PendingValue = value;
        row.Number.Dispose();
        row.Number = SKTextBlob.Create(Format(row.DisplayValue), value is null ? _font : _italicFont);
    }

    internal bool TryEdit(float x, float y, int wheelDirection, bool fine, out int index, out double value)
    {
        index = -1; value = 0;
        if (!Contains(x, y) || !Viewport.Contains(x, y)) return false;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i]; var rect = RowBounds(i);
            if (rect.IsEmpty || !rect.Contains(x, y)) continue;
            int direction;
            if (wheelDirection != 0)
            {
                if (x < rect.Right - 160 || x >= rect.Right - 90) return false;
                direction = wheelDirection;
            }
            else if (x >= rect.Right - 28 && x <= rect.Right - 4)
            { value = row.Default; index = i; return value != row.DisplayValue; }
            else if (x >= rect.Right - 88 && x <= rect.Right - 64) direction = -1;
            else if (x >= rect.Right - 58 && x <= rect.Right - 34) direction = 1;
            else return false;
            var (min, max, step, small) = Limits(row.Value);
            value = Math.Clamp(Math.Round(row.DisplayValue + direction * (fine ? small : step), 6), min, max);
            if (value == row.DisplayValue) return false;
            index = i; return true;
        }
        return false;
    }

    internal static (double Min, double Max, double Step, double Fine) Limits(DebugTuningValue value) => value.Field switch
    {
        "maxSpeedKmS" => (.1, 50, .5, .1),
        "accelerationMps2" => (10, 20000, 50, 10),
        "turnRateDegPerSec" => (1, 360, 2, 1),
        "reloadMs" => (100, 600000, 500, 100),
        "speedKmS" => (.1, 100, .5, .1),
        "blastRadiusKm" when value.ModuleTypeId == ModuleIds[4] => (.05, 50, .25, .05),
        "blastRadiusKm" => (.05, 50, .5, .1),
        _ => throw new InvalidOperationException("Unknown tuning field.")
    };
    internal SKRect RowBounds(int index)
    {
        var rect = _rows[index].Bounds;
        if (!rect.IsEmpty) rect.Offset(0, Viewport.Top - _scroll);
        return rect;
    }

    internal void Render(SKCanvas canvas)
    {
        if (!Visible || _disposed) return;
        Fill(canvas, Bounds, new SKColor(9, 17, 23, 250));
        Draw(canvas, _title, Bounds.Left + 10, Bounds.Top + 20, XenonStyle.CyanBright);
        Draw(canvas, _help, Bounds.Right - 20, Bounds.Top + 20, MenuStyle.ColorText);
        for (int i = 0; i < _statusLines.Length; i++)
            Draw(canvas, _statusLines[i], Bounds.Left + 10, Bounds.Top + 38 + i * 15, _statusError ? SKColors.IndianRed : MenuStyle.ColorTextDim);
        canvas.Save();
        canvas.ClipRect(Viewport);
        canvas.Translate(0, Viewport.Top - _scroll);
        for (int i = 0; i < _sections.Length; i++)
        {
            var rect = _sectionBounds[i];
            Fill(canvas, rect, new SKColor(24, 39, 48));
            Draw(canvas, _sections[i], rect.Left + 6, rect.Top + 18, XenonStyle.Cyan);
            canvas.DrawLine(rect.Right - 14, rect.MidY, rect.Right - 6, rect.MidY, Color(MenuStyle.ColorText));
            if (_collapsed[i]) canvas.DrawLine(rect.Right - 10, rect.MidY - 4, rect.Right - 10, rect.MidY + 4, _paint);
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i]; var rect = row.Bounds;
            if (rect.IsEmpty) continue;
            canvas.Save();
            canvas.ClipRect(new(rect.Left, rect.Top, rect.Right - 160, rect.Bottom));
            Draw(canvas, row.Label, rect.Left, rect.Top + 20, MenuStyle.ColorText);
            canvas.Restore();
            Draw(canvas, row.Number, rect.Right - 153, rect.Top + 17, row.Modified ? XenonStyle.OrangeAccent : MenuStyle.ColorText);
            if (row.Modified) Draw(canvas, row.DefaultNumber, rect.Right - 153, rect.Top + 33, MenuStyle.ColorTextDim);
            var (min, max, _, _) = Limits(row.Value);
            Button(canvas, _minus, new(rect.Right - 88, rect.Top + 5, rect.Right - 64, rect.Bottom - 5), row.DisplayValue > min);
            Button(canvas, _plus, new(rect.Right - 58, rect.Top + 5, rect.Right - 34, rect.Bottom - 5), row.DisplayValue < max);
            ResetButton(canvas, new(rect.Right - 28, rect.Top + 5, rect.Right - 4, rect.Bottom - 5), row.DisplayValue != row.Default);
        }
        if (_helpVisible)
            for (int i = 0; i < _helpLines.Length; i++)
                Draw(canvas, _helpLines[i], Bounds.Left + 10, _contentHeight - (_helpLines.Length - i - 1) * 16 - 3, MenuStyle.ColorText);
        canvas.Restore();
        Button(canvas, _resetConfirm ? _confirmReset : _resetAll, ResetAllBounds);
        Button(canvas, _folder, FolderBounds);
    }

    private SKPaint Color(SKColor color) { _paint.Color = color; return _paint; }
    private void Fill(SKCanvas canvas, SKRect rect, SKColor color) => canvas.DrawRect(rect, Color(color));
    private void Draw(SKCanvas canvas, SKTextBlob blob, float x, float y, SKColor color) => canvas.DrawText(blob, x, y, Color(color));
    private void Button(SKCanvas canvas, SKTextBlob blob, SKRect rect, bool enabled = true)
    {
        Fill(canvas, rect, enabled ? new SKColor(35, 49, 59) : new SKColor(19, 26, 31));
        Draw(canvas, blob, rect.MidX - blob.Bounds.Width / 2, rect.MidY + 4, enabled ? MenuStyle.ColorText : MenuStyle.ColorTextDim);
    }
    private void ResetButton(SKCanvas canvas, SKRect rect, bool enabled)
    {
        Fill(canvas, rect, enabled ? new SKColor(35, 49, 59) : new SKColor(19, 26, 31));
        _paint.Color = enabled ? MenuStyle.ColorText : MenuStyle.ColorTextDim;
        _paint.Style = SKPaintStyle.Stroke; _paint.StrokeWidth = 1.2f;
        float x = rect.MidX, y = rect.MidY;
        canvas.DrawArc(new(x - 5, y - 5, x + 5, y + 5), -60, 295, false, _paint);
        canvas.DrawLine(x - 3, y - 4, x - 6, y - 4, _paint);
        canvas.DrawLine(x - 6, y - 4, x - 6, y - 1, _paint);
        _paint.Style = SKPaintStyle.Fill;
    }
    private SKTextBlob[] Wrap(string text)
    {
        var lines = new List<SKTextBlob>();
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' '))
            {
                string next = line.Length == 0 ? word : line + " " + word;
                if (_paint.MeasureText(next) > Width - 24 && line.Length != 0) { lines.Add(Blob(line)); line = word; }
                else line = next;
                // File paths/reasons can contain a single very long token.
                while (_paint.MeasureText(line) > Width - 24 && line.Length > 1)
                {
                    int length = line.Length - 1;
                    while (length > 1 && _paint.MeasureText(line[..length]) > Width - 24) length--;
                    lines.Add(Blob(line[..length])); line = line[length..];
                }
            }
            if (line.Length != 0) lines.Add(Blob(line));
        }
        return lines.ToArray();
    }
    private SKTextBlob Blob(string text) => SKTextBlob.Create(text, _font);
    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    internal static bool SameKey(DebugTuningValue a, DebugTuningValue b) => a.ModuleTypeId == b.ModuleTypeId && a.Field == b.Field && a.ProfileId == b.ProfileId;
    private void ClearRows()
    {
        foreach (var row in _rows) { row.Label.Dispose(); row.Number.Dispose(); row.DefaultNumber.Dispose(); }
        _rows.Clear();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; ClearRows();
        foreach (var section in _sections) section.Dispose();
        _title.Dispose(); _minus.Dispose(); _plus.Dispose(); _resetAll.Dispose(); _folder.Dispose(); _help.Dispose();
        _confirmReset.Dispose();
        foreach (var line in _helpLines) line.Dispose();
        foreach (var line in _statusLines) line.Dispose();
        _font.Dispose(); _italicFont.Dispose(); _italicTypeface.Dispose(); _paint.Dispose();
    }
}
