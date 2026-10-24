---
epic: EP-0014-module-panels
story: EP-0014-US-0006-localization-and-guide
ticket: EP-0014-US-0006-TK-0004-native-panel-validation
stage: implemented
layer: client-tools
depends_on: [EP-0014-US-0006-TK-0003-snapshot-render-consistency]
files_touched: 3
serves: [AC-02]
---

# Native module panel evidence

## Scope before implementation
Add tools/DeepSpaceSaga.Performance/ModulePanelNativeEvidence.cs and dispatch in Program.cs. Use production Engine snapshots, native SkiaWindow/OpenGL and managed screen input callbacks. Exercise RU/EN, scales 0.8/1/1.2/1.5, small viewport, torpedo priority, profile rows/statistics, actual launch/reload, blocked aim, engine panel. Save reports and actual GPU captures only under the EP-0014 evidence run. Restore runtime Settings.json. Update PointCombatNativeEvidence.cs's retired ObjectInfoPanel.OwnModuleDiagram assertion to the current data ownership so the owning tool builds.

## Acceptance and evidence
Verify actual GPU context and requested dimensions/locale/scale, numeric fixture rows, input command admission, reload status retention and blocked launch guard. Managed callbacks do not prove OS physical input or human visual acceptance. Record unavailable native gates explicitly if the host cannot open a window. Use process cap 300000ms, internal watchdog, scoped format, tool build and affected regression. Separate commit and immediate verified push.

## Conditional corrective scope
If native capture demonstrates clipped captions/body at small logical heights, first record the reproduction and expand scope to CommandsPanel.cs plus owning layout test, then fix actual body reservation without dropping panel rows or weakening geometry assertions. Final project documentation remains the last ticket.

## Native reproduction and corrective scope before edit
Native RU1.5 report captionsFit=false and GPU capture show last caption clipped at1280x720. EN1 GPU capture shows default detail font missing bar/selection glyphs. Retained initial evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/native-before-fix/. Expand to CommandsPanel.cs and ModulePanelHealthTests.cs: reserve actual preferred body height before caption sizing, cap ring to caption height, choose a glyph-capable cached font and assert glyph coverage/viewport geometry. Wide0.8 may legitimately expand Navigation together with all weapons; priority assertion applies to cramped logical heights.

## Overflow regression fixture correction before edit
Adaptive sizing now correctly fits the existing eight-repeat detail fixture at480px, so its unconditional expectation of scrolling is stale. Expand PhaseThreeCombatInfoTests.cs's long detail to24 repeats: this forces actual overflow at both200 and480 and preserves all original wrap, scroll and visible-status assertions.

## Glyph coverage correction before edit
Consolas supports Cyrillic, filled/empty bar cells and selection/critical markers, but lacks the two fractional edge glyphs. Change only Combat.Panel.BarLeft/BarRight in both Data/Locale dictionaries to bracket edges, and check the actual localized glyph string. This retains all bar semantics and removes the two remaining missing glyphs.

## Final native result
Both locales at .8/1/1.2/1.5 passed actual OpenGL checks, managed command admission, reload and blocked aim, engine times and small-viewport caption bounds. Reports and four GPU captures per case: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/native-matrix.json and native-{ru,en}-{scale}.{json,panels.png,aim.png,reload.png,engine.png}. RU1.5 GPU captures inspected for text, symbols and caption reachability. Consolas uses supported bracket edges and localized diamond-suit critical marker. Physical devices, human acceptance and independent review remain open. Tool build: native-tool-build.log, scoped format: native-tool-format.log. Complete Client run retained two failures separately in client-full-final.trx; overflow fixture correction passes here, reload-report startup race is a separate follow-up ticket.

## Implementation evidence

Validate native module panels and fix viewport and multilingual glyph defects

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-native-panel-corrected.trx and client-native-panel-corrected-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Human acceptance and independent review are not claimed. Native evidence, if run, is recorded separately.
