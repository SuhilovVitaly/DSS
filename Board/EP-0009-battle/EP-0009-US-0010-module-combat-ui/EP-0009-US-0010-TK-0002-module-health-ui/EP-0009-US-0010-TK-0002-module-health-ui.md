---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
ticket: EP-0009-US-0010-TK-0002-module-health-ui
title: "module-health-ui"
stage: implemented
layer: client
depends_on: ["EP-0009-US-0010-TK-0001-defeat-from-module-rule"]
files_touched: "Contracts/ObjectMotionSnapshot.cs; Engine/SimulationEngine.cs; Client/ObjectInfoPanel.cs; Client/ObjectLabelRenderer.cs; Client/ObjectLabelLayout.cs; Client/GameSessionScreen.cs; Client/GameSessionScreen.MapView.cs; Client/GameSessionScreen.Combat.cs; Client/CombatMapVisualTests.cs; Client/ObjectInfoPanelTests.cs; Client/PirateScenarioTests.cs; Client/StationResourceFieldContentTests.cs; Client/FullMapRenderEvidenceTests.cs; Engine/ModuleHealthProjectionTests.cs; Board evidence"
serves: ["US10-AC2"]
created: 2026-10-12
---

# EP-0009-US-0010-TK-0002-module-health-ui

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

`AuthoritativeSnapshot.InstalledModules` only projects the player ship. US10-AC2 requires the selected ship, including a known NPC, to show each module's authoritative armor, structure, disable timer and destroyed state. Add a compact per-known-ship module health projection on `ObjectMotionSnapshot`, gate it by the existing `known` mask, and carry it to `ObjectInfoPanelData`. Replace the existing hull-bar predicate/fill with a sum of current armor+structure over sum of maxima, independent of ship class. This needs Contracts and Engine beyond the draft Client file list. Existing `ObjectLabelLayout` bar space can be reused, but its API and tests should name the new module bar. Focused headless tests will verify masking, numbers, timer and pixels; native render remains deferred by user.

## Evidence and self-review

Known player and NPC ships project every module's authoritative armor, structure, disable deadline and destruction flag; unidentified NPC rows expose an empty module list. The selected-object panel displays these values, rounds remaining disable time up to seconds, wraps long values, and scrolls both rows within a capped panel height so map targets remain clickable. The map bar sums current armor+structure across modules and divides by their summed maxima, independent of hull HP or ship class; its geometry, fill and click target use the renamed module-bar contract. Initial broad Client run exposed two direct regressions: snapshot record equality after adding an `ImmutableArray`, and the expanded panel intercepting map clicks. Those were repaired. It also exposed a stale Default_500 file hash from the preceding scenario-schema migration, updated to the current file. A second broad run exposed that a full first row could leave the second row zero height; height is now split across both rows. Focused Engine projection1/1, Client panel/map52/52 and broad Client1873/1873 (excluding the historically slow market route suite) headless tests PASS; solution build0 warnings/errors, scoped format and diff checks PASS. Native window/game interaction NOT RUN by user instruction; Graphify rebuild SKIPPED.

**Файлы:** `ObjectInfoPanel.cs`, `ObjectLabelRenderer.cs`, `GameSessionScreen.cs`, `MapView.cs`, `tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0010-module-combat-ui.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
