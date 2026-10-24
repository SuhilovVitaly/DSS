---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
ticket: EP-0009-US-0006-TK-0002-set-mode-command
title: "set-mode-command"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0006-TK-0001-mode-runtime"]
files_touched: "ground-before-edit"
serves: ["US6-AC3"]
created: 2026-10-12
---

# EP-0009-US-0006-TK-0002-set-mode-command

STATUS: IMPLEMENTED — headless checks passed; game/native NOT RUN by user instruction.

Execution scope before edit (2026-10-12): Contracts already declare `TorpedoLauncherCommandTypes.SetMode` and `PlayerCommand.ModeId`, while the Engine dispatcher has no branch and the torpedo command catalog has no definition. The runtime launcher stores mode and reload time from TK-0001. Durable command-ID idempotence requires extending combat Save/Load state and validation in addition to the planned dispatcher/runtime/content/test files. The command must distinguish precombat immediate selection from an in-combat cycle using live torpedoes and enemy ships inside launch range. The category command allowlist (`Client/Data/module-types.json`) and its real-content assertion also require updating so the new command can execute. No Graphify/game/native runs.

Implemented `torpedo.setMode` through the production dispatcher and command catalog. A valid launcher mode applies immediately outside combat; with a live torpedo or enemy ship within launcher range it adds one base reload cycle to `max(ReadyAt,t)`. Invalid category/mode is rejected. Processed command IDs persist and validate through Save/Load so a duplicate cannot add a second cycle. Focused four cases pass, including pause, flight, PD rejection and post-load duplicate. Engine broad non-corpus run was interrupted after its pre-existing catalog assertion failed; that assertion was updated and passed in the focused repeat. Client/Contracts, build and formatting results are recorded in `ImplementationStatus.md`. Native/game NOT RUN; Graphify rebuild SKIPPED per user.

**Файлы:** `SimulationEngine.Combat.cs`, `Client/Data/Commands/TorpedoLauncher/commands.json`, `SimulationEngine.cs` (диспетчер), `tests/DeepSpaceSaga.Engine.Tests/SetFireModeTests.cs` (новый).
**Тесты:** `Out_of_combat_immediate`, `In_combat_costs_one_cycle`, `Pd_rejects_non_standard`, `Paused_switch_consistent`, `Duplicate_ignored`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0006-torpedo-fire-modes.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
