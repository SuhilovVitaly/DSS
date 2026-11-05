---
epic: EP-0014-module-panels
story: EP-0014-US-0006-localization-and-guide
ticket: EP-0014-US-0006-TK-0005-transport-startup-reports
stage: implemented
layer: engine-local-client
depends_on: [EP-0014-US-0006-TK-0004-native-panel-validation]
files_touched: 2
serves: [AC-02]
---

# Preserve report bootstrap before transport subscription

## Reproduction and scope before implementation
Full Client integration repeatedly loses reload reports after baseline0/latest4 with an empty visible store; isolated execution passes. LocalGameSessionConnection starts its engine producer before the asynchronous reader selects combat transport. Early combat updates and bootstrap are discarded while _usesCombatUpdates=false, but Engine clears their pending reports. A later full snapshot has missing sequence payloads and the lossless inbox correctly waits for the gap forever. Do not weaken the report assertion or call this a proven baseline failure.

## Code context
- src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs: wait for the first selected snapshot/combat stream before capturing updates; preserve cancellation/disposal and both consumer modes. A connection has one transport owner. Reject conflicting transport selection explicitly.
- tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs: delayed subscription with actual production report bootstrap must retain original report sequences; preserve existing legacy snapshot tests.

## Acceptance and validation
Get a deterministic delayed-reader failure before the fix. After the fix, first combat bootstrap retains initial reports and the real reload notification test enqueues and alerts once. Legacy snapshot delivery remains live; disposal without a reader completes. No Engine outcomes, command identity, RNG, schema or inbox delivery rules change. Run local-session/report/presentation regressions and full Client suite, scoped formatting, owning build and diff check; cap every test process300000ms. Commit and immediately verify push, then perform final documentation last.

## Verified outcome
The corrected deterministic regression does not consume reports through CaptureSnapshot before subscribing. Restoring original startup behavior makes it fail with an empty report payload (report-startup-red-actual.trx); gated startup passes with report sequence1 delivered by the bootstrap update's frame/full payloads. Focused133/133 (client-report-startup-green.trx), complete Client2318/2318 (client-full-verified.trx). Source restored in finally after the controlled red experiment. Earlier fixture attempts consumed the report themselves and are not used as proof. One transport mode is selected on first reader; cancellation releases the producer even if no reader ever subscribes.

## Implementation evidence

Gate local update capture on reader selection to preserve report bootstrap

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution/client-full-verified.trx and client-full-verified-format.log. Owning build, scoped format and diff checks passed. Diff self-reviewed against ticket and invariants. Human acceptance and independent review are not claimed. Native evidence, if run, is recorded separately.
