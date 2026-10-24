---
epic: EP-0014-module-panels
story: EP-0014-US-0006-localization-and-guide
ticket: EP-0014-US-0006-TK-0003-snapshot-render-consistency
stage: implemented
layer: client
depends_on: [EP-0014-US-0006-TK-0001-localization]
files_touched: 3
serves: [AC-02]
---

# Keep speed controls consistent during snapshot transitions

## Evidence and decision
The broader module regression produced IndexOutOfRangeException in DrawSpeedPanel at SpeedValues[i], recorded in client-panel-localization-verified.trx under D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/. The extended peace options can shrink between reading label count and indexing values because snapshots arrive asynchronously. This required corrective ticket is authorized by whole-epic execution; language application remains restart-based.

## Code context and scope
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs: capture one option set for each draw, pair its labels, preserve the rendered button mapping for mouse input.
- tests/DeepSpaceSaga.Client.Tests/SpeedPanelTests.cs: exercise concurrent authoritative speed transitions and existing input semantics.

## Acceptance
Rendering concurrent peace speed changes does not index a different-length array. Every drawn button uses the matching value and label. Mouse input respects the rendered mapping. Existing combat restrictions, pause history and keyboard shortcuts remain authoritative. No Engine/RNG/save changes.

## Validation and review
Run SpeedPanel, module Engine/time and localization tests, scoped formatter and diff check; retain the reproduced failure separately. Review input bounds and old snapshots. Native/human/independent acceptance is separate. Commit this ticket and immediately push before proceeding.

## Review scope correction before edit
GameSessionScreen.Encounter.cs speed tooltip uses the same hit-test index; migrate that read to the rendered value mapping as well to avoid an adjacent out-of-range access.

## Implementation evidence

Keep rendered speed labels values mouse input and tooltip mappings consistent

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-panel-snapshot-consistency-reviewed.trx and client-panel-snapshot-consistency-reviewed-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Native, human acceptance and independent review are not claimed.
