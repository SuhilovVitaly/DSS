# Tactical battle skin packs

Build and publish place the reference pack at `Skins/contrast/` next to the executable. To create another pack, copy that directory, give it a new ID and display name, and update the relative paths under `assets/`. Restart the client, choose the pack in Settings, and open a battle through F8.

The [authoring contract](../../../DB/Documentation/04-Engineering/TacticalBattleSkins.md) describes the manifest, asset limits, visual roles, and fallback behavior. [Contrast](contrast/skin.json) shows how the roles fit together.
