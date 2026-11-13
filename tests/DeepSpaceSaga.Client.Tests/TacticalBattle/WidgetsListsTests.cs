using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class WidgetsListsTests
{
    private static readonly SKRect Viewport = new(0, 0, 1280, 720);

    [Fact]
    public void Skin_scope_is_restored_after_nested_render_and_exception()
    {
        using (TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic))
        using (TacticalBattleSkinRenderScope.EnterRole("orders.fire"))
        {
            var nested = WidgetSkin();
            Assert.Throws<InvalidOperationException>((Action)(() =>
            {
                using var scope = TacticalBattleSkinRenderScope.Enter(nested);
                Assert.Same(nested, TacticalBattleSkinRenderScope.Current);
                Assert.Null(TacticalBattleSkinRenderScope.CurrentRole);
                using var role = TacticalBattleSkinRenderScope.EnterRole("tabs.sides");
                Assert.Equal("tabs.sides", TacticalBattleSkinRenderScope.CurrentRole);
                throw new InvalidOperationException("draw failed");
            }));
            Assert.Same(TacticalBattleSkin.Classic, TacticalBattleSkinRenderScope.Current);
            Assert.Equal("orders.fire", TacticalBattleSkinRenderScope.CurrentRole);
        }
        Assert.Null(TacticalBattleSkinRenderScope.Current);
        Assert.Null(TacticalBattleSkinRenderScope.CurrentRole);
    }

    [Fact]
    public void Styled_tabs_popup_and_tooltip_use_overrides_without_changing_unscoped_controls()
    {
        var tabs = new TabStrip([new("a", "First"), new("b", "Second")]) { Bounds = new(20, 20, 250, 50) };
        var list = new DropDown<int>([1, 2, 3], i => "Длинная подпись " + i) { HasFocus = true };
        list.Layout(new(20, 70, 250, 102), Viewport);
        list.OnKeyDown(Key.Enter);
        var popup = list.PopupBounds;
        list.OnMouseMove(list.Bounds.MidX, list.Bounds.MidY, 0);

        using var before = Draw();
        using var classic = Draw(TacticalBattleSkin.Classic);
        using var themed = Draw(WidgetSkin());
        using var after = Draw();
        Assert.NotEqual(before.GetPixel(28, 30), classic.GetPixel(28, 30));
        Assert.Equal(TabletopStyle.Inset, before.GetPixel(28, 30));
        Assert.Equal(before.Pixels, after.Pixels);
        Assert.Equal(new SKColor(123, 45, 99), themed.GetPixel(28, 30));
        Assert.Equal(new SKColor(52, 64, 102), themed.GetPixel(28, 112));
        Assert.Equal(new SKColor(68, 85, 119), themed.GetPixel(530, 127));
        Assert.Equal(popup, list.PopupBounds);
        Assert.Equal(1, list.SelectedItem);

        SKBitmap Draw(TacticalBattleSkin? skin = null)
        {
            using IDisposable? scope = skin is null ? null : TacticalBattleSkinRenderScope.Enter(skin);
            var bitmap = new SKBitmap(640, 240);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);
            tabs.Draw(canvas);
            list.Draw(canvas);
            list.DrawPopup(canvas);
            list.DrawTooltip(canvas, SKRect.Create(640, 240), 1000);
            return bitmap;
        }
    }

    [Fact]
    public void Specific_frame_uses_shared_style_for_unset_properties()
    {
        var skin = TacticalBattleSkin.Parse("""
            {"schemaVersion":1,"id":"fallback","displayName":"Fallback","roles":{
              "control.frame":{"fill":"#7788DD"},
              "orders.move.card.selected":{"stroke":"#009977"}
            }}
            """);
        using var scope = TacticalBattleSkinRenderScope.Enter(skin);
        using var bitmap = new SKBitmap(200, 100);
        using var canvas = new SKCanvas(bitmap);
        ControlVisuals.Frame(canvas, new(10, 10, 100, 40), true, true, 1, "orders.move.card.selected");
        Assert.Equal(new SKColor(119, 136, 221), bitmap.GetPixel(30, 30));
    }

    private static TacticalBattleSkin WidgetSkin() => TacticalBattleSkin.Parse("""
        {
          "schemaVersion":1,"id":"widgets","displayName":"Widgets",
          "roles":{
            "control.tab.selected":{"fill":"#7B2D63"},
            "control.dropdown.option.active":{"fill":"#344066","fontSize":16},
            "control.tooltip":{"fill":"#445577"}
          }
        }
        """);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DropDownChevronKeepsItsWidthAcrossFieldWidths(bool focused)
    {
        using var normal = Draw(355);
        using var wide = Draw(650);
        Assert.Contains(Enumerable.Range(16, 13), x =>
            Enumerable.Range(12, 11).Any(y => normal.GetPixel(x, y).Red > 120));
        for (int y = 5; y < 29; y++)
            for (int x = 3; x < 39; x++)
                Assert.Equal(normal.GetPixel(x, y), wide.GetPixel(x, y));

        SKBitmap Draw(int width)
        {
            var list = new DropDown<int>([1], _ => "Option") { HasFocus = focused };
            list.Layout(SKRect.Create(width, 34), Viewport);
            var bitmap = new SKBitmap(width, 34);
            using var canvas = new SKCanvas(bitmap);
            list.Draw(canvas);
            return bitmap;
        }
    }

    [Fact]
    public void DropDownKeyboardMouseOutsideClickAndReloadPreserveRealSelection()
    {
        var list = new DropDown<int>(Enumerable.Range(0, 30), i => "Вариант " + i) { HasFocus = true };
        list.Layout(new(20, 680, 320, 712), Viewport, maximumRows: 5);
        var changed = new List<int>(); list.Changed += changed.Add;
        list.OnKeyDown(Key.Enter);
        Assert.True(list.IsOpen);
        Assert.True(list.PopupBounds.Bottom <= list.Bounds.Top);
        Assert.True(Viewport.Contains(list.PopupBounds));
        list.OnKeyDown(Key.End);
        Assert.Equal(0, list.SelectedItem); // highlight is not a committed command
        Assert.Equal(25, list.FirstVisibleIndex);
        list.OnKeyDown(Key.Enter);
        Assert.Equal(29, list.SelectedItem); Assert.Equal(new[] { 29 }, changed);
        list.SetItems([29, 100]);
        Assert.Equal(29, list.SelectedItem);
        list.Layout(list.Bounds, Viewport);
        list.OnMouseDown(30, 690, MouseButton.Left);
        list.OnMouseDown(30, list.PopupBounds.Top + 45, MouseButton.Left);
        Assert.Equal(100, list.SelectedItem);
        Assert.False(list.IsOpen);
        list.OnKeyDown(Key.Enter);
        Assert.True(list.OnMouseDown(1200, 10, MouseButton.Left));
        Assert.False(list.IsOpen);
        Assert.False(list.OnMouseDown(1200, 10, MouseButton.Left));
        Assert.Equal(new[] { 29, 100 }, changed);
    }

    [Fact]
    public void DropDownWheelAccumulatesFractionalStepsClampsAndDoesNotChangeValue()
    {
        var list = new DropDown<int>(Enumerable.Range(0, 30), i => i.ToString()) { HasFocus = true };
        list.Layout(new(10, 10, 310, 42), Viewport, maximumRows: 5);
        list.OnKeyDown(Key.Enter);
        float y = list.PopupBounds.Top + 10;
        list.OnMouseWheel(20, y, 25); list.OnMouseWheel(20, y, 25);
        Assert.Equal(0, list.FirstVisibleIndex);
        list.OnMouseWheel(20, y, 50); Assert.Equal(1, list.FirstVisibleIndex);
        list.Layout(new(10, 10, 310, 42), Viewport, maximumRows: 5); // render repeats the same layout
        Assert.Equal(1, list.FirstVisibleIndex);
        list.OnMouseWheel(20, y, 10000); Assert.Equal(25, list.FirstVisibleIndex);
        list.OnMouseWheel(20, y, -10000); Assert.Equal(0, list.FirstVisibleIndex);
        Assert.Equal(0, list.SelectedItem);
        list.OnKeyDown(Key.Escape); Assert.False(list.IsOpen);
        list.Enabled = false; Assert.False(list.OnKeyDown(Key.Enter));
    }

    [Fact]
    public void LongCaptionsUseUnicodeEllipsisWhileRetainingFullSourceForTooltip()
    {
        string source = string.Concat(Enumerable.Repeat("🚀А́БВГД длинная подпись ", 8));
        var list = new DropDown<string>([source], s => s);
        list.Layout(new(0, 0, 180, 32), Viewport);
        string clipped = list.DisplayCaption;
        Assert.EndsWith("…", clipped);
        Assert.Equal(source, list.SelectedLabel);
        using var paint = ControlText.Paint(1);
        Assert.True(paint.MeasureText(clipped) <= 124);
        Assert.StartsWith(clipped[..^1], source);
        for (int i = 0; i < clipped.Length; i++)
            if (char.IsHighSurrogate(clipped[i])) { Assert.True(i + 1 < clipped.Length && char.IsLowSurrogate(clipped[i + 1])); i++; }
        Assert.Equal("", ControlText.Ellipsize(source, 0, paint));
    }

    [Fact]
    public void EmptyListAndVerySmallPopupHaveNoPhantomSelection()
    {
        var list = new DropDown<int>([], i => i.ToString()) { HasFocus = true };
        list.Layout(new(0, 0, 100, 30), new(0, 0, 100, 30));
        Assert.False(list.HasSelection);
        Assert.False(list.OnKeyDown(Key.Enter));
        list.OnMouseDown(10, 10, MouseButton.Left);
        Assert.False(list.IsOpen);
        list.SetItems([4]);
        Assert.Equal(4, list.SelectedItem);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.SelectIndex(1));
    }

    [Fact]
    public void WordWrapPreservesWordsNewlinesAndLongWordCharactersWithinClipWidth()
    {
        var panel = new ScrollTextPanel { AutoScroll = false };
        panel.Layout(new(0, 0, 200, 120));
        panel.SetText("Первая строка с несколькими словами\n\n" + new string('Ж', 80));
        Assert.Contains("", panel.Lines);
        Assert.Equal(80, string.Concat(panel.Lines).Count(c => c == 'Ж'));
        using var paint = ControlText.Paint(1);
        Assert.All(panel.Lines, line => Assert.True(paint.MeasureText(line) <= 172));
        Assert.Contains("Первая", panel.Lines[0]);
    }

    [Fact]
    public void LogFollowsAppendAtBottomButKeepsReadingPositionWhenUserScrollsUp()
    {
        var panel = new ScrollTextPanel();
        panel.Layout(new(0, 0, 300, 120));
        string text = string.Join('\n', Enumerable.Range(0, 30).Select(i => "Строка " + i));
        panel.SetText(text);
        Assert.Equal(panel.MaximumScroll, panel.ScrollOffset);
        panel.OnMouseWheel(20, 20, -25);
        float reading = panel.ScrollOffset;
        Assert.True(reading < panel.MaximumScroll);
        panel.SetText(text + "\nНовая запись"); Assert.Equal(reading, panel.ScrollOffset);
        panel.HasFocus = true; panel.OnKeyDown(Key.End);
        panel.SetText(text + "\nНовая запись\nЕщё одна");
        Assert.Equal(panel.MaximumScroll, panel.ScrollOffset);
        panel.AutoScroll = false; reading = panel.ScrollOffset;
        panel.SetText(panel.Text + "\nНе прокручивать"); Assert.Equal(reading, panel.ScrollOffset);
    }

    [Fact]
    public void ScrollbarDragPageAndFocusResetHaveBoundedOffsets()
    {
        var panel = new ScrollTextPanel();
        panel.Layout(new(0, 0, 300, 180));
        panel.SetText(string.Join('\n', Enumerable.Range(0, 100)));
        var thumb = panel.ScrollThumb;
        Assert.True(panel.OnMouseDown(thumb.MidX, thumb.MidY, MouseButton.Left));
        Assert.True(panel.IsDragging);
        panel.OnMouseMove(thumb.MidX, panel.ScrollTrack.Top + thumb.Height / 2);
        Assert.Equal(0, panel.ScrollOffset);
        panel.OnMouseUp(MouseButton.Right); Assert.True(panel.IsDragging);
        panel.ResetInput(); Assert.False(panel.IsDragging);
        panel.OnMouseMove(100, 10000); Assert.Equal(0, panel.ScrollOffset);
        panel.OnMouseDown(panel.ScrollTrack.MidX, panel.ScrollTrack.Bottom - 1, MouseButton.Left);
        Assert.True(panel.ScrollOffset > 0);
        panel.OnKeyDown(Key.End); Assert.Equal(panel.MaximumScroll, panel.ScrollOffset);
        panel.OnMouseWheel(20, 20, 10000); Assert.Equal(panel.MaximumScroll, panel.ScrollOffset);
        panel.OnKeyDown(Key.Home); Assert.Equal(0, panel.ScrollOffset);
    }

    [Fact]
    public void OptionalTextFilterKeepsOldInputBehaviorAndAcceptsSeedMinusCharacters()
    {
        var ordinary = new TextInputBox(); Assert.True(ordinary.TryAppendChar('Ж'));
        var seed = new TextInputBox(20, c => c is >= '0' and <= '9' or '-' or '−');
        Assert.True(seed.TryAppendChar('−')); Assert.True(seed.TryAppendChar('1'));
        Assert.False(seed.TryAppendChar('Ж')); Assert.False(seed.TryAppendChar('\n'));
        Assert.Equal("−1", seed.Text);
        seed.Backspace(); Assert.Equal("−", seed.Text);
    }

    [Fact]
    public void ShortcutButtonHintsTooltipDelayAndDisabledReasonDoNotRepeatActions()
    {
        var button = new ShortcutButton("Начать бой", "Enter", "Полная подсказка") { Bounds = new(0, 0, 200, 40), HasFocus = true };
        int count = 0; button.Clicked += () => count++;
        button.OnKeyDown(Key.Enter); button.OnKeyDown(Key.Enter, repeat: true); Assert.Equal(1, count);
        button.Enabled = false;
        Assert.False(button.OnMouseDown(10, 10, MouseButton.Left));
        button.OnMouseMove(10, 10, 0);
        Assert.False(button.TooltipReady(499)); Assert.True(button.TooltipReady(500));
        button.ResetInput(); Assert.False(button.TooltipReady(1000));
    }

    [Theory]
    [InlineData(0.75f)]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RasterPopupFits1280By720AndLabelsAndWrappedJournalStayClipped(float scale)
    {
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        string longLabel = "Мощная тяжёлая скорострельная пушка · дополнительное описание модуля для проверки полного текста";
        var list = new DropDown<string>(Enumerable.Range(0, 30).Select(i => i + " · " + longLabel), s => s) { HasFocus = true };
        list.Layout(SKRect.Create(30, 650, 300 * scale, 32 * scale), Viewport, scale, maximumRows: 6);
        list.SelectIndex(20); list.OnKeyDown(Key.Enter);
        Assert.True(Viewport.Contains(list.PopupBounds));
        var panel = new ScrollTextPanel(); panel.Layout(new(680, 30, 1240, 580), scale);
        panel.SetText(string.Join('\n', Enumerable.Range(0, 50).Select(i => $"Ход {i} · Дрон → цель. Попадание, корпус −5. Подробная запись для проверки переноса слов.")));
        var button = new ShortcutButton("Подтвердить", "Enter", "Приказы отправляются в сессию боя.") { Bounds = SKRect.Create(30, 30, 260 * scale, 42 * scale) };
        button.Draw(canvas, scale); panel.Draw(canvas); list.Draw(canvas); list.DrawPopup(canvas);
        Assert.NotEqual(SKColors.Black, bitmap.GetPixel(685, 35));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(1260, 600));
        list.OnMouseMove(list.Bounds.MidX, list.Bounds.MidY, 0);
        using var beforeImage = SKImage.FromBitmap(bitmap);
        using var beforeData = beforeImage.Encode(SKEncodedImageFormat.Png, 100);
        byte[] before = beforeData.ToArray();
        list.DrawTooltip(canvas, Viewport, 1000);
        using var afterImage = SKImage.FromBitmap(bitmap);
        using var afterData = afterImage.Encode(SKEncodedImageFormat.Png, 100);
        Assert.NotEqual(before, afterData.ToArray());
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { } root)
        {
            string directory = Path.Combine(root, "renders"); Directory.CreateDirectory(directory);
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "widgets-lists-" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png"));
            data.SaveTo(file);
        }
    }
}
