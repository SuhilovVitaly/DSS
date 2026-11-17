using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// EP-0034 US-0006: the obligations journal is reachable from the map. A "J" button on the mechanics panel and
/// Ctrl+J (no conflict with Ctrl+F/Ctrl+S) both raise the existing ScreenEvent.OpenContracts; SkiaWindow opens
/// the Contracts overlay as a modal (pause-on-open), where acceptance stays limited to a docked ship by the engine.
/// </summary>
public sealed partial class GameSessionScreen
{
    private SKRect _lastJournalButtonRect;
    private bool _isJournalButtonHovered;

    internal SKRect LastJournalButtonRect => _lastJournalButtonRect;
}
