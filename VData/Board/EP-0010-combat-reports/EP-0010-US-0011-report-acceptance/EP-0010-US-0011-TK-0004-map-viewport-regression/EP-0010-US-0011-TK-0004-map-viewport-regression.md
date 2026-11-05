---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
ticket: EP-0010-US-0011-TK-0004-map-viewport-regression
status: implemented
layer: client
files_touched: 2
depends_on: [EP-0010-US-0011-TK-0002-client-integration-proof]
validation_status: automated-passed
---

# Correct the available map viewport after adding reports

Full Client regression found MapLayerControlsTests.OverlapSelectionAndControlsAtAllScales failing at1280x720/UI150: AvailableMapRect includes the new report panel although pointer routing correctly consumes it. This is a confirmed EP0010 integration defect, not unrelated map work.

## Scope and steps

- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs: include actual report bounds in existing free-viewport obstacle list, preserving camera math.
- tests/DeepSpaceSaga.Client.Tests/CombatReportMapViewportTests.cs: six-scale proof that free map rectangle does not intersect reports.
- Run new test plus existing MapLayerControlsTests and report suites; scoped format/diff; separate commit/immediate push before native ticket.

Native TK0003 depends on this corrective ticket. Full-suite station timeouts remain under investigation and are not classified as pre-existing without evidence.

## Evidence

AvailableMapRect now includes actual report bounds. Six new geometry cases and all three existing MapLayerControls cases PASS (9). Scoped format/diff PASS. Full Client run completed1948PASS/23FAIL before repair: remaining fixed-coordinate UI fixtures, mixed-session report baseline fixture and station refuel timeouts require separate follow-up; not marked green.
