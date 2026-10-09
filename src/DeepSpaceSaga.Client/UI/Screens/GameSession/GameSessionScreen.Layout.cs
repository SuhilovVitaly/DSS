using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private (IReadOnlyList<(string Label, string Value)> Lines, float LabelWidth) _infoPanelLayout;
    private SKRect _gameTimeRect;
    private string _gameTimeText = string.Empty;

    private void LayoutOverlayPanels(BufferedSnapshot? buffered)
    {
        _uiViewportW = _viewportW / _uiScale;
        _uiViewportH = _viewportH / _uiScale;
        _lastSpeedPanelRect = ComputeSpeedPanelRect();
        _lastScalePanelRect = new(ComputeScaleSpeedRowLeft(), ComputeScaleSpeedRowY(),
            ComputeScaleSpeedRowLeft() + ComputeScalePanelWidth(),
            ComputeScaleSpeedRowY() + ScalePanelPadY * 2 + ScaleBtnH + ScaleIndicatorSize + 2);
        for (int i = 0; i < SpeedLabels.Length; i++)
        {
            float x = _lastSpeedPanelRect.Left + SpeedPanelPadX + i * (SpeedBtnW + SpeedBtnGap);
            float y = _lastSpeedPanelRect.Top + SpeedPanelPadY;
            _speedButtonRects[i] = new(x, y, x + SpeedBtnW, y + SpeedBtnH);
        }
        for (int i = 0; i < ScaleLabels.Length; i++)
        {
            float x = _lastScalePanelRect.Left + ScalePanelPadX + i * (ScaleBtnW + ScaleBtnGap);
            float y = _lastScalePanelRect.Top + ScalePanelPadY;
            _scaleButtonRects[i] = new(x, y, x + ScaleBtnW, y + ScaleBtnH);
        }
        const float portraitButtonWidth = 188;
        float panelWidth = MechanicsButtonWidth * 2 + MechanicsButtonGap * 2 + MechanicsPanelPadding * 2 + portraitButtonWidth;
        float panelHeight = MechanicsButtonHeight + MechanicsPanelPadding * 2;
        float left = (_uiViewportW - panelWidth) / 2, top = _uiViewportH - panelHeight - PanelMargin;
        _lastMechanicsPanelRect = new(left, top, left + panelWidth, top + panelHeight);
        float buttonX = left + MechanicsPanelPadding, buttonY = top + MechanicsPanelPadding;
        _lastFinanceButtonRect = new(buttonX, buttonY, buttonX + MechanicsButtonWidth, buttonY + MechanicsButtonHeight);
        buttonX += MechanicsButtonWidth + MechanicsButtonGap;
        _lastShipButtonRect = new(buttonX, buttonY, buttonX + MechanicsButtonWidth, buttonY + MechanicsButtonHeight);
        buttonX += MechanicsButtonWidth + MechanicsButtonGap;
        _lastTempCharacterImageButtonRect = new(buttonX, buttonY, buttonX + portraitButtonWidth, buttonY + MechanicsButtonHeight);
        LayoutMapToolbar();
        if (_panelVisible) _infoPanelLayout = LayoutInfoPanel(buffered);
        float commandsBottom = _panelVisible && _lastPanelRect.Left < CommandsPanel.PanelWidth + PanelMargin
            ? _lastPanelRect.Top - PanelMargin : _uiViewportH;
        _commandsPanel.Layout(buffered?.Snapshot.InstalledModules ?? ImmutableArray<InstalledModuleSnapshot>.Empty, commandsBottom);
        _combatJournalPanel.Layout(_uiViewportW, _uiViewportH, buffered?.Snapshot.CombatJournal ?? default);
        LayoutObjectInfoPanel();
        _gameTimeText = GameTimeDisplay.Minutes(buffered?.Snapshot.GameTimeMs) + " · " + GameTimeDisplay.Status(PresentationSpeed);
        if (buffered?.Snapshot.MissingRations > 0) _gameTimeText += " · Не хватает рационов";
        float timeWidth = _gameTimeTextPaint.MeasureText(_gameTimeText) + 24;
        _gameTimeRect = new(_uiViewportW / 2 - timeWidth / 2, ComputeScaleSpeedRowY() - 128,
            _uiViewportW / 2 + timeWidth / 2, ComputeScaleSpeedRowY() - 98);
    }

    private void LayoutObjectInfoPanel()
    {
        var player = FindPlayerShip(_renderStates);
        var selected = FindRenderStateById(_activeObjectId ?? _selectedObjectId);
        _objectInfoPanel.Layout(_uiViewportW, PanelMargin,
            ToObjectInfoPanelData(player), ToObjectInfoPanelData(selected, player), _uiViewportH);
    }
}
