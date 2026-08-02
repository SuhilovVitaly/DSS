using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ScreenStackTests
{
    private sealed class TestScreen : IScreen
    {
        public int ActivatedCount;
        public int DeactivatedCount;
        public int RenderedCount;
        public ScreenEvent? NextMouseEvent = null;
        public bool IsInteractive = false;
        public ScreenEvent? NextKeyEvent = null;

        public void Render(SKCanvas canvas, int width, int height) => RenderedCount++;

        public ScreenEvent OnMouseDown(float x, float y) => NextMouseEvent ?? ScreenEvent.None;

        public bool OnMouseMove(float x, float y) { return IsInteractive; }

        public ScreenEvent OnKeyDown(Key key) => NextKeyEvent ?? ScreenEvent.None;

        public void OnActivated() => ActivatedCount++;

        public void OnDeactivated() => DeactivatedCount++;
    }

    [Fact]
    public void SetRoot_activates_the_screen()
    {
        var stack = new ScreenStack();
        var screen = new TestScreen();

        stack.SetRoot(screen);

        Assert.Equal(1, screen.ActivatedCount);
        Assert.Equal(0, screen.DeactivatedCount);
        Assert.Same(screen, stack.Current);
    }

    [Fact]
    public void Push_deactivates_previous_and_activates_new()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        var overlay = new TestScreen();
        stack.SetRoot(root);

        stack.Push(overlay);

        Assert.Equal(1, root.ActivatedCount);
        Assert.Equal(1, root.DeactivatedCount);

        Assert.Equal(1, overlay.ActivatedCount);
        Assert.Equal(0, overlay.DeactivatedCount);

        Assert.Same(overlay, stack.Current);
    }

    [Fact]
    public void Pop_restores_previous_screen()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        var overlay = new TestScreen();
        stack.SetRoot(root);
        stack.Push(overlay);

        stack.Pop();

        Assert.Equal(1, overlay.DeactivatedCount);
        Assert.Equal(2, root.ActivatedCount); // SetRoot + Pop reactivation
        Assert.Same(root, stack.Current);
    }

    [Fact]
    public void Pop_at_root_does_nothing()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        stack.SetRoot(root);

        stack.Pop();

        Assert.Equal(1, root.ActivatedCount);
        Assert.Equal(0, root.DeactivatedCount);
        Assert.Same(root, stack.Current);
    }

    [Fact]
    public void Replace_swaps_screen_and_deactivates_old()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        var replacement = new TestScreen();
        stack.SetRoot(root);

        stack.Replace(replacement);

        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, replacement.ActivatedCount);
        Assert.Same(replacement, stack.Current);
    }

    [Fact]
    public void ReplaceAll_clears_entire_stack()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        var overlay = new TestScreen();
        var mainMenu = new TestScreen();
        stack.SetRoot(root);
        stack.Push(overlay); // deactivates root (1st)

        stack.ReplaceAll(mainMenu);

        // root deactivated once by Push; ReplaceAll only deactivates Current (overlay)
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, overlay.DeactivatedCount);
        Assert.Equal(1, mainMenu.ActivatedCount);
        Assert.Same(mainMenu, stack.Current);
    }

    [Fact]
    public void DeactivateAll_deactivates_all_without_reactivation()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        var overlay = new TestScreen();
        stack.SetRoot(root);
        stack.Push(overlay); // deactivates root (1st)

        stack.DeactivateAll();

        // root deactivated once by Push; DeactivateAll only deactivates Current (overlay)
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, overlay.DeactivatedCount);
        Assert.Equal(1, root.ActivatedCount);
        Assert.Equal(1, overlay.ActivatedCount);
    }

    [Fact]
    public void SetRoot_on_non_empty_stack_clears_first()
    {
        var stack = new ScreenStack();
        var root = new TestScreen();
        var overlay = new TestScreen();
        var newRoot = new TestScreen();
        stack.SetRoot(root);
        stack.Push(overlay); // deactivates root (1st)

        stack.SetRoot(newRoot);

        // root deactivated once by Push; SetRoot only deactivates Current (overlay)
        Assert.Equal(1, root.DeactivatedCount);
        Assert.Equal(1, overlay.DeactivatedCount);
        Assert.Equal(1, newRoot.ActivatedCount);
        Assert.Same(newRoot, stack.Current);
    }
}
