---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
ticket: EP-0009-US-0006-TK-0001-mode-runtime
title: "mode-runtime"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0005-TK-0003-pd-reload-and-attempts"]
files_touched: "Engine combat, hostile fire, dialogue docking, RNG/persistence/save validation; Contracts torpedo flight; focused tests"
serves: ["US6-AC1", "US6-AC2"]
created: 2026-10-12
---

# EP-0009-US-0006-TK-0001-mode-runtime

STATUS: IMPLEMENTED; publication pending.

Execution scope before edit (2026-10-12): `TryLaunchTorpedo` currently uses the raw module accuracy/maneuverability and fixed launcher reload, while `LauncherCombatSnapshot.ModeId`/`IsBrokenUntilDock` and the validated `GameDataRegistry.FireModes` are inert. Combat save already persists launcher snapshots and separate hit/location RNG states but has no launcher-breakdown RNG. Hostile fire scheduling must skip broken launchers. This ticket therefore also touches `Scenario/CombatSaveData.cs`, combat persistence/bootstrap, hostile fire, and Save/Load tests beyond the planned files. `torpedo.setMode` and selector UI remain TK-0002/TK-0003. No Graphify/game/native runs.

Implementation evidence (2026-10-12): mode accuracy/maneuverability bonuses and multiplier are consumed at launch and frozen in the flight; production Maneuverability yields 48% interception and 18750 ms reload. Overheat draws from independent `combat.launcher.breakdown.v1` state after the torpedo launches, persists its stream state/counter, and marks the launcher broken until a subsequent docking dialogue commits. Broken launchers reject player fire and are skipped by hostile scheduling. Focused mode tests3/3 and docking/dialogue/mode group26/26; Engine non-corpus1975/1975; Client1859/1859, Contracts192/192; solution build0 warnings/errors, scoped format/diff PASS. `torpedo.setMode` and selector UI remain next tickets. Game/native NOT RUN, Graphify rebuild SKIPPED by user instruction. Separate publication follows.

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `Combat/FireModeMath.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/FireModeRuntimeTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/TorpedoHitRollTests.cs`.
**Тесты:** `Maneuverability_mode_lowers_intercept_to_48`, `Overheat_breakdown_deterministic`, `Nonstandard_reload_18_75s`, `Broken_launcher_rejects_until_dock`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0006-torpedo-fire-modes.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
