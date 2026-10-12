---
epic: EP-0009-module-combat
story: EP-0009-US-0005-weapon-fire-cadence
ticket: EP-0009-US-0005-TK-0001-launcher-cooldown
title: "launcher-cooldown"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0004-TK-0003-intercept-roll-skills"]
files_touched: "Engine combat/hostile fire, Contracts launcher snapshot, combat save validation, Client combat projection, and affected headless tests"
serves: ["US5-AC1"]
created: 2026-10-12
---

# EP-0009-US-0005-TK-0001-launcher-cooldown

STATUS: IMPLEMENTED; publication pending.

Execution scope before edit: `SimulationEngine.Combat.cs` and `SimulationEngine.HostileFire.cs` enforce one active torpedo until impact. `LauncherCombatSnapshot` has both legacy `ActiveTorpedoObjectId` and additive `ActiveTorpedoIds`, with Save/Load validation in `Scenario/CombatSaveData.cs` and projection in Client `GameSessionScreen.Combat.cs`. Self-destruct, status text and command tests consume the old singular field, so the migration must include those callers and focused Save/Load regressions. `ModuleTypeDefinition.LauncherReloadMs` and content value 15000 already exist. Mode multiplier remains 1 until US-0006. No Graphify/game/native runs.

Implementation evidence (2026-10-12): launcher readiness is now set at launch to MotionTimeMs + 15000 and gates player and NPC fire independently of active projectiles. The launcher tracks multiple flight IDs; termination removes only its own ID, and self-destruct can address a selected active flight. Save validation checks every launcher/projectile link and accepts the legacy singular JSON field on load. The Client displays Reloading until the due time. New headless regressions cover early rejection, second player launch while first flies, command-id idempotence, two-flight Save/Load, and NPC firing on cooldown. Historical single-flight and immediate-ready test assumptions were migrated. Engine non-corpus 1970/1970; Contracts 192/192; Client 1859/1859; solution build 0 warnings/errors; scoped format and `git diff --check` PASS. Game/native NOT RUN and Graphify rebuild SKIPPED by user instruction. Separate commit and remote SHA verification follow.

**Файлы:** `SimulationEngine.Combat.cs` (guard Busy; на `99e15a2` `:40`, `:65`, `:101`, `:281`), `tests/DeepSpaceSaga.Engine.Tests/TorpedoLaunchTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileFireTests.cs` (EP-0008), `tests/DeepSpaceSaga.Engine.Tests/LauncherCooldownTests.cs` (новый).
**Тесты:** `Second_launch_after_15s_while_first_flying`, `Launch_before_ready_rejected_with_reason`, `Npc_fires_on_cooldown_not_after_flight`, `Idempotent_command_id`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0005-weapon-fire-cadence.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
