---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
ticket: EP-0010-US-0011-TK-0006-hover-cleanup
status: implemented
layer: client
files_touched: 6
depends_on: [EP-0010-US-0011-TK-0003-native-visual-and-performance]
validation_status: automated-passed
---

# Clear unrelated HUD hover/press states when reports consume pointer input

Full Client regression found stale CommandsPanel hover after moving from a command/toggle to report bounds. Early pointer refusal must still clear the other controls' visual states. MouseUp should release all existing pressed visuals without issuing actions.

Scope: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs; tests/DeepSpaceSaga.Client.Tests/CombatReportHoverTests.cs. Preserve map/input isolation, no game launch. Run existing CommandsPanelSkeletonTests and report tests; format/diff; separate commit/push before documentation.

Scope amendment before edits: full-suite failures also expose free-map coordinates now inside reports, clipped ObjectInfo image bodies, and empty strings with negative wrap widths at tiny resize. Include CommandsPanelSkeletonTests.cs, ApproachCameraIntegrationTests.cs, ObjectInfoPanelTests.cs and production Controls/ObjectInfoPanel.cs. Preserve selection/panning and clipping assertions; guard empty text and clamp BreakText to available characters. Existing tiny-resize regression proves the production fix.

## Evidence

226 report/commands/approach/ObjectInfo/tiny-resize headless cases PASS (evidence/regression/hover-resize-repair.trx); scoped dotnet format and git diff --check PASS. Hover cleanup preserves report refusal and releases all pressed HUD controls. Existing long-name resize now terminates for empty text/negative widths. Image body assertion retains full first image and verifies lower clipped body scrolling.
