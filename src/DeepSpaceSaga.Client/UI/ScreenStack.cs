using DeepSpaceSaga.Client.UI.Screens;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Stack-based screen navigation. Supports overlays (Push/Pop) and transitions (Replace).
/// Activation lifecycle: each screen gets exactly one OnActivated/OnDeactivated per transition.
/// Screens below the top are already inactive (deactivated by Push), so clearing the stack
/// only deactivates Current — not every screen.
/// </summary>
public sealed class ScreenStack
{
    private readonly Stack<IScreen> _stack = new();

    public IScreen Current => _stack.Peek();
    public int Count => _stack.Count;

    /// <summary>Returns the screen directly below the overlay, or null if none.</summary>
    public IScreen? UnderCurrent => _stack.Count > 1
        ? _stack.Skip(1).First()
        : null;

    /// <summary>Set the initial screen.</summary>
    public void SetRoot(IScreen screen)
    {
        // Only the top screen is active; deactivate it, then clear silently
        if (_stack.Count > 0)
            Current.OnDeactivated();

        _stack.Clear();
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
        // Only the top screen is active — deactivate it, then clear silently
        if (_stack.Count > 0)
            Current.OnDeactivated();

        _stack.Clear();
        _stack.Push(screen);
        screen.OnActivated();
    }

    /// <summary>Deactivate all screens (for shutdown).</summary>
    public void DeactivateAll()
    {
        if (_stack.Count > 0)
            Current.OnDeactivated();

        _stack.Clear();
    }
}
