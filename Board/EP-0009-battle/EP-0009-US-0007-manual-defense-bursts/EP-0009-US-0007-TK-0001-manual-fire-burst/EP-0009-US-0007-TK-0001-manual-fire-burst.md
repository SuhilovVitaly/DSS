---
epic: EP-0009-module-combat
story: EP-0009-US-0007-manual-defense-bursts
ticket: EP-0009-US-0007-TK-0001-manual-fire-burst
title: "manual-fire-burst"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0006-TK-0003-mode-selector-ui"]
files_touched: "ground-before-edit"
serves: ["US7-AC1"]
created: 2026-10-12
---

# EP-0009-US-0007-TK-0001-manual-fire-burst

STATUS: IMPLEMENTED — headless validation passed; game/native NOT RUN by user instruction.

Execution scope before edit (2026-10-12): `defense.fire` already calls the shared `LaunchCountermeasure`, so a production two-barrel module already queues its second manual shot and persists it. The remaining gap is manual readiness: it returns generic `busy` during reload and rejects a new shot from the same module while any earlier countermeasure is still guiding, even after reload. Add a distinct `not_ready` contract/localized reason, keep active-cycle `busy`, and allow a new manual burst after reload while older flights remain. Extend existing ManualCountermeasureFireTests/PdBurstTests. No Graphify/game/native runs.

Result: manual `defense.fire` uses the shared persisted two-barrel queue. During reload it now rejects with `not_ready`, while active module cycles remain `busy`; once `ReadyAtMotionTimeMs` is reached, an older guiding countermeasure no longer blocks a new manual burst. `module_disabled` and `module_destroyed` remain distinct from the existing module-health gate. Tests cover two barrels, repeat command ID, simultaneous modules targeting one torpedo, zero-chance/no-solution launch, pause, Save/Load, and same-module relaunch with older flights guiding. Focused Engine27/27, affected Client24/24, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN; Graphify rebuild SKIPPED per user.

**Файлы:** `SimulationEngine.Countermeasures.cs`, обработчик `defense.fire` из EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/ManualDefenseFireTests.cs` (EP-0008; адаптировать), `tests/DeepSpaceSaga.Engine.Tests/PdBurstTests.cs`.
**Тесты:** `Manual_fire_starts_burst`, `Manual_allowed_while_other_cm_in_flight`, `Rejects_disabled_pd`, `Idempotent`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0007-manual-defense-bursts.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
