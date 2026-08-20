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

    private readonly int _maxLength;
    private readonly StringBuilder _text = new();
    private readonly Stopwatch _caretClock = Stopwatch.StartNew();

    public TextInputBox(int maxLength = DefaultMaxLength)
    {
        if (maxLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be positive.");

        _maxLength = maxLength;
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
        if (char.IsControl(c))
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
        canvas.DrawRect(rect, MenuStyle.ButtonFillNormal);
        canvas.DrawRect(rect, MenuStyle.ButtonBorder);

        string text = Text;
        float textX = rect.Left + PaddingX;
        float textY = rect.MidY + TextPaint.TextSize / 3f;

        canvas.DrawText(text, textX, textY, TextPaint);

        if (CaretVisible)
        {
            float caretX = textX + TextPaint.MeasureText(text);
            canvas.DrawLine(caretX, rect.Top + 4f, caretX, rect.Bottom - 4f, MenuStyle.ButtonBorder);
        }
    }
}
