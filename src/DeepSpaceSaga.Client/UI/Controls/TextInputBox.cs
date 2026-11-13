using System.Diagnostics;
using System.Text;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Single-line text input control (e.g. for naming a new save slot).
/// Text mutation (<see cref="TryAppendChar"/>, <see cref="Backspace"/>) is a pure,
/// unit-testable core with no <see cref="SKCanvas"/> dependency; <see cref="Render"/>
/// is the only member that touches Skia. Caret always trails end-of-text — no
/// arrow-key movement or mid-string editing in v1.
/// </summary>
public sealed class TextInputBox
{
    public const int DefaultMaxLength = 40;
    private const long CaretBlinkIntervalMs = 500;
    private const float PaddingX = 8f;

    private static readonly SKPaint TextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = MenuStyle.TypefaceRegular
    };
    private static readonly SKPaint CaretPaint = new() { Color = TabletopStyle.Gold };

    private readonly int _maxLength;
    private readonly Func<char, bool>? _characterFilter;
    private readonly StringBuilder _text = new();
    private readonly Stopwatch _caretClock = Stopwatch.StartNew();

    public TextInputBox(int maxLength = DefaultMaxLength, Func<char, bool>? characterFilter = null)
    {
        if (maxLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be positive.");

        _maxLength = maxLength;
        _characterFilter = characterFilter;
    }

    /// <summary>Current text content.</summary>
    public string Text => _text.ToString();

    /// <summary>Current text length.</summary>
    public int Length => _text.Length;

    /// <summary>
    /// Appends a printable character. Rejects control characters (Enter, Tab, Esc,
    /// Backspace's char code, etc. — those arrive via <see cref="OnKeyDown"/> through
    /// Silk.NET key edges, not as a KeyChar) and any character once at max length.
    /// Returns whether the character was accepted.
    /// </summary>
    public bool TryAppendChar(char c)
    {
        if (char.IsControl(c) || _characterFilter is not null && !_characterFilter(c))
            return false;

        if (_text.Length >= _maxLength)
            return false;

        _text.Append(c);
        return true;
    }

    /// <summary>Removes the last character, if any. Returns whether a character was removed.</summary>
    public bool Backspace()
    {
        if (_text.Length == 0)
            return false;

        _text.Length--;
        return true;
    }

    /// <summary>Clears all text.</summary>
    public void Clear() => _text.Clear();

    /// <summary>Forward from <see cref="Screens.IScreen.OnTextInput"/>.</summary>
    public void OnTextInput(char c) => TryAppendChar(c);

    /// <summary>Forward from <see cref="Screens.IScreen.OnKeyDown"/> — only Backspace is handled here.</summary>
    public void OnKeyDown(Key key)
    {
        if (key == Key.Backspace)
            Backspace();
    }

    private bool CaretVisible => (_caretClock.ElapsedMilliseconds / CaretBlinkIntervalMs) % 2 == 0;

    /// <summary>Draws the box, current text, and a blinking caret trailing the text.</summary>
    public void Render(SKCanvas canvas, SKRect rect)
    {
        if (rect.IsEmpty)
            return;

        TabletopStyle.Field(canvas, rect, focused: true);
        string text = Text;
        var content = new SKRect(rect.Left + PaddingX, rect.Top, rect.Right - PaddingX, rect.Bottom);
        if (content.IsEmpty)
            return;
        float textWidth = TextPaint.MeasureText(text);
        float textX = Math.Min(content.Left, content.Right - textWidth - 1);
        float textY = MenuStyle.VerticalCenterBaseline(rect, TextPaint);
        canvas.Save();
        canvas.ClipRect(content);
        canvas.DrawText(text, textX, textY, TextPaint);

        if (CaretVisible)
        {
            float caretX = textX + textWidth;
            canvas.DrawLine(caretX, rect.Top + 4f, caretX, rect.Bottom - 4f, CaretPaint);
        }
        canvas.Restore();
    }
}
