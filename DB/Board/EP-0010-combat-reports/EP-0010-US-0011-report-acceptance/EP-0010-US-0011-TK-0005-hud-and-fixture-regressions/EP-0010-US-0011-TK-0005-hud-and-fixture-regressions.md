---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
ticket: EP-0010-US-0011-TK-0005-hud-and-fixture-regressions
status: implemented
layer: client
files_touched: 5
depends_on: [EP-0010-US-0011-TK-0004-map-viewport-regression]
validation_status: automated-passed
---

# Complete HUD reservation and synchronize affected test fixtures

Full regression found a real report/footer overlap without AiMap and old tests clicking the newly occupied report area. Countermeasure roundtrip test deliberately feeds original-engine continuation into a loaded presentation buffer; it must carry the equivalent loaded report baseline instead of mixing sessions.

## Exact scope

- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs: reserve existing map-toolbar rows even without AiMap.
- tests/DeepSpaceSaga.Client.Tests/CombatReportMapViewportTests.cs: solar-only toolbar/report no-overlap at every supported scale.
- tests/DeepSpaceSaga.Client.Tests/GameSessionNavigationTests.cs: move explicit navigation/drag coordinates to a free point, update exact expected world coordinates and preserve drag deltas.
- tests/DeepSpaceSaga.Client.Tests/GameSessionObjectInteractionTests.cs: corresponding free-map click/drag fixture coordinates.
- tests/DeepSpaceSaga.Client.Tests/CountermeasureSessionRoundtripTests.cs: adapt original-engine report delta to loaded bootstrap baseline while retaining all combat-effect assertions.

Run these classes, SystemMapNavigationTests, FullMapRenderEvidenceTests and report suites; scoped format/diff, separate commit/push. Run before native TK0003. Station refuel timeouts remain separate until reproduced/classified.

## Evidence

Reserved toolbar rows for solar-only and no-AiMap screens. Navigation fixtures now use free(700,300) and exact expected world(10060,9940), preserving drag deltas. Roundtrip visual comparison carries equivalent restored report baseline.166 report/navigation/interaction/roundtrip/system/full-map cases PASS (regression/corrected.trx); scoped format/diff PASS. Station-specific class rerun separately.
