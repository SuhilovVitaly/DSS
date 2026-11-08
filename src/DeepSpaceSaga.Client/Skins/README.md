# Tactical battle skin packs

Build and publish place the reference pack at `Skins/contrast/` next to the executable. To create another pack, copy that directory, give it a new ID and display name, and update the relative paths under `assets/`. Restart the client, choose the pack in Settings, and open a battle through F8.

The [authoring contract](../../../DB/Documentation/04-Engineering/TacticalBattleSkins.md) describes the manifest, asset limits, visual roles, and fallback behavior. [Contrast](contrast/skin.json) shows how the roles fit together.

Pilot initiative and the shared fire queue use existing visual roles. All battle
surfaces read the same admitted FireSteps projection; packs cannot change firing
order or playback timing. See the [battle contract](../../../DB/Documentation/04-Engineering/TacticalBattle.md).

The active screen uses the compact EP-0019 overlay. Use the current ui.panel, ui.pilot-card, ui.slider and ui.button roles in the authoring contract; legacy tabs/orders controls are no longer connected. Bundled DejaVu typography and height-based layout keep Classic consistent between machines. Packs only change the properties consumed by active primitives.
