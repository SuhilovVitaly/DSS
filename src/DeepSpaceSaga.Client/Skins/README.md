# Tactical battle skin packs

Build and publish place the reference pack at `Skins/contrast/` next to the executable. To create another pack, copy that directory, give it a new ID and display name, and update the relative paths under `assets/`. Restart the client, choose the pack in Settings, and open a battle through F8.

The [authoring contract](../../../DB/Documentation/04-Engineering/TacticalBattleSkins.md) describes the manifest, asset limits, visual roles, and fallback behavior. [Contrast](contrast/skin.json) shows how the roles fit together.

## Xenon Star

From the DSS repository root, publish to a fresh local output directory with the licensed Kodiak Graphics pack available:

```powershell
$run = Get-Date -Format 'yyyyMMdd-HHmmss'
$output = "D:/DeepSpaceSaga/artifacts/validation/ep-0022/$run"
dotnet publish src/DeepSpaceSaga.Client -c Release --artifacts-path "$output/build" -p:XenonStarSource="D:/DeepSpaceSaga/DSS-Images/Xenon Star" -o "$output/client"
```

Choose **Xenon Star** in Settings, then open an active battle. The executable reads `Skins/xenon-star` from its own installation and the pack is optional. When checking a build without the pack, use a fresh output directory. The publish command above produces a local copy with the original PNG slices. Confirm the scope of your acquisition terms before distributing that output.

The [client project](../DeepSpaceSaga.Client.csproj) maps these exact paths under `Sliced` to the [package manifest](xenon-star/skin.json).

| Source under `Sliced` | Use |
|---|---|
| `Cockpit HUD/In Space/Weapon Systems Screen/weapons_bg.png` | Phase panel, report and exit dialog texture |
| `Cockpit HUD/In Space/System Map/smb_normal.png` | Secondary button art |
| `Cockpit HUD/In Space/System Map/smb_hover.png` | Primary action art |
| `Modular Elements/HUD Frames/hudf11.png` | Side frame behind tactical layers |
| `Modular Elements/Horizontal Lines/hl1.png` | Top and bottom rules behind tactical layers |
| `Modular Elements/Target Reticles/tre14.png` | Assigned target accent |
| `Modular Elements/Target Reticles/tre7.png` | Alternative target accent |
| `Cockpit HUD/In Space/Ship Status - You and Target/your_screen_bg.png` | Pilot and build card texture |
| `Cockpit HUD/In Space/Ship Status - You and Target/hull_fill.png` | Segmented meter overlay |

Artwork by Kodiak Graphics. [Publisher's Xenon Star listing](https://kodiakgraphics.itch.io/xenon-star)

Pilot initiative and the shared fire queue use existing visual roles. All battle
surfaces read the same admitted FireSteps projection; packs cannot change firing
order or playback timing. See the [battle contract](../../../DB/Documentation/04-Engineering/TacticalBattle.md).

Classic, Contrast and Xenon Star use the compact overlay and expand the selected pilot card for movement, fire or salvo details. Use the current ui.panel, ui.pilot-card, ui.slider and ui.button roles in the authoring contract; legacy tabs/orders controls are no longer connected. Bundled DejaVu typography and height-based layout keep Classic consistent between machines. Packs only change the properties consumed by active primitives.

Pilot gauges use `ui.pilot-card.bar.armor/hull/mind` for fill and `ui.pilot-card.gauge.track` for the track. `ui.pilot-card.icon.armor/hull/mind` supplies stroke colors and falls back to the gauge colors. `ui.pilot-card.divider` provides stroke and dash, and `ui.pilot-card.scroll` provides track fill and thumb stroke. The pilot card art scales to its full height; segmented gauge art is clipped to the filled portion.

Build cards use the panel, box, bar and text roles with bundled typography. Slot identities, damage, evasion, decoys and anomaly geometry come from the captured battle data.

## Tabletop

A profile with no saved battle skin selects the bundled Tabletop pack when available. Explicit choices still apply to the active battle. The rest of the desktop client uses the shared [Tabletop shell](../../../DB/Documentation/03-Design/TabletopClient.md).

Build and publish also include `Skins/tabletop`, with its ready-to-use manifest and 24 PNGs. Choose **Tabletop** in Settings, then open a battle. It supplies 32-direction pixel ships, torpedoes and impact strips, painted anomaly clouds, movement/fire templates, steel windows and 150×40 pilot cards with a separate command panel. The right windows retain their layout. Battle rules, commands, FireSteps timing and saves are shared with Classic.

The independent opt-in metrics `field.style.tabletop`, `ui.style.steel` and `ui.pilot-column.slim` each use value 1. They work with other package IDs. The slim flight hint supports Space to skip the current playback; Enter remains available. The [authoring contract](../../../DB/Documentation/04-Engineering/TacticalBattleSkins.md) covers sprite geometry, roles and resource limits.

The supplied manifest is preserved in canonical Board; runtime only normalizes a pasted Markdown artifact in the `ui.build.name` key. The24 PNGs are unchanged. Original bake sources are unavailable, so there is currently no supported reproducible rebake command. Runtime installation does not depend on them. See [materials and bake status](../../../DB/Board/EP-0024-battle-field-redesign/Materials/README.md).
