using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public class TextInputBoxTests
{
    [Fact]
    public void New_box_starts_empty()
    {
        var box = new TextInputBox();

        Assert.Equal(string.Empty, box.Text);
        Assert.Equal(0, box.Length);
    }

    [Fact]
    public void TryAppendChar_appends_printable_characters()
    {
        var box = new TextInputBox();

        Assert.True(box.TryAppendChar('A'));
        Assert.True(box.TryAppendChar('1'));
        Assert.True(box.TryAppendChar(' '));

        Assert.Equal("A1 ", box.Text);
    }

    [Theory]
    [InlineData('\n')]
    [InlineData('\t')]
    [InlineData('\r')]
    [InlineData('\b')]
    [InlineData((char)27)] // Escape
    public void TryAppendChar_rejects_control_characters(char c)
    {
        var box = new TextInputBox();

        Assert.False(box.TryAppendChar(c));
        Assert.Equal(string.Empty, box.Text);
    }

    [Fact]
    public void TryAppendChar_rejects_once_max_length_reached()
    {
        var box = new TextInputBox(maxLength: 3);

        Assert.True(box.TryAppendChar('a'));
        Assert.True(box.TryAppendChar('b'));
        Assert.True(box.TryAppendChar('c'));
        Assert.False(box.TryAppendChar('d'));

        Assert.Equal("abc", box.Text);
    }

    [Fact]
    public void Backspace_removes_last_character()
    {
        var box = new TextInputBox();
        box.TryAppendChar('a');
        box.TryAppendChar('b');

        Assert.True(box.Backspace());
        Assert.Equal("a", box.Text);
    }

    [Fact]
    public void Backspace_on_empty_text_returns_false_and_stays_empty()
    {
        var box = new TextInputBox();

        Assert.False(box.Backspace());
        Assert.Equal(string.Empty, box.Text);
    }

    [Fact]
    public void Clear_empties_the_text()
    {
        var box = new TextInputBox();
        box.TryAppendChar('a');
        box.TryAppendChar('b');

        box.Clear();

        Assert.Equal(string.Empty, box.Text);
        Assert.Equal(0, box.Length);
    }

    [Fact]
    public void Backspace_after_reaching_max_length_allows_appending_again()
    {
        var box = new TextInputBox(maxLength: 2);
        box.TryAppendChar('a');
        box.TryAppendChar('b');
        Assert.False(box.TryAppendChar('c'));

        Assert.True(box.Backspace());
        Assert.True(box.TryAppendChar('c'));
        Assert.Equal("ac", box.Text);
    }

    [Fact]
    public void OnTextInput_forwards_to_TryAppendChar()
    {
        var box = new TextInputBox();

        box.OnTextInput('x');
        box.OnTextInput('\n'); // rejected control char

        Assert.Equal("x", box.Text);
    }

    [Fact]
    public void OnKeyDown_backspace_removes_last_character()
    {
        var box = new TextInputBox();
        box.OnTextInput('a');
        box.OnTextInput('b');

        box.OnKeyDown(Key.Backspace);

        Assert.Equal("a", box.Text);
    }

    [Fact]
    public void OnKeyDown_non_backspace_key_is_ignored()
    {
        var box = new TextInputBox();
        box.OnTextInput('a');

        box.OnKeyDown(Key.Enter);
        box.OnKeyDown(Key.A);

        Assert.Equal("a", box.Text);
    }

    [Fact]
    public void Constructor_rejects_non_positive_max_length()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextInputBox(maxLength: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextInputBox(maxLength: -1));
    }
}
