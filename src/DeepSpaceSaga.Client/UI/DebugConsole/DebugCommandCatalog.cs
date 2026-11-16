namespace DeepSpaceSaga.Client.UI.DebugConsole;

/// <summary>The single registration point of console commands; every EP-0030 story adds one Register line here.</summary>
internal static class DebugCommandCatalog
{
    public static DebugCommandRegistry CreateDefault()
    {
        var registry = new DebugCommandRegistry();
        return registry;
    }
}
