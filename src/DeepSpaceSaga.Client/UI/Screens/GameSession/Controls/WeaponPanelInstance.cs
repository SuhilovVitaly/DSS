namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>Stable command-panel identity, independent of display order and caption.</summary>
public sealed record WeaponPanelInstance(string OwnerObjectId, string ModuleId, string Caption)
{
    public string Key => OwnerObjectId.Length + ":" + OwnerObjectId + ModuleId;
}
