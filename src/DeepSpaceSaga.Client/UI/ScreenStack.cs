using DeepSpaceSaga.Client.UI.Screens;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Stack-based screen navigation. Supports overlays (Push/Pop) and transitions (Replace).
/// Activation lifecycle: each screen gets exactly one OnActivated/OnDeactivated per transition.
/// </summary>
public sealed class ScreenStack
{
    private readonly Stack<IScreen> _stack = new();

    public IScreen Current => _stack.Peek();
    public int Count => _stack.Count;

    /// <summary>Returns all screens from bottom to top (for overlay rendering).</summary>
    public IReadOnlyList<IScreen> GetAllScreens() => _stack.Reverse().ToList();

    /// <summary>Set the initial screen (stack must be empty).</summary>
    public void SetRoot(IScreen screen)
    {
        while (_stack.Count > 0)
            _stack.Pop().OnDeactivated();

        _stack.Push(screen);
        screen.OnActivated();
    }

    /// <summary>Push an overlay on top of the current screen.</summary>
    public void Push(IScreen screen)
    {
        if (_stack.Count > 0)
            Current.OnDeactivated();

        _stack.Push(screen);
        screen.OnActivated();
    }

    /// <summary>Pop the current overlay, returning to the previous screen.</summary>
    public void Pop()
    {
        if (_stack.Count <= 1)
            return;

        var old = _stack.Pop();
        old.OnDeactivated();

        Current.OnActivated();
    }

    /// <summary>Replace the current screen with a new one (e.g. MainMenu → GameSession).</summary>
    public void Replace(IScreen screen)
    {
        if (_stack.Count > 0)
        {
            var old = _stack.Pop();
            old.OnDeactivated();
        }

        _stack.Push(screen);
        screen.OnActivated();
    }

    /// <summary>Replace the entire stack with a single screen (e.g. MAIN MENU from overlay).</summary>
    public void ReplaceAll(IScreen screen)
    {
        while (_stack.Count > 0)
            _stack.Pop().OnDeactivated();

        _stack.Push(screen);
        screen.OnActivated();
    }

    /// <summary>Deactivate all screens (for shutdown).</summary>
    public void DeactivateAll()
    {
        while (_stack.Count > 0)
            _stack.Pop().OnDeactivated();
    }
}
