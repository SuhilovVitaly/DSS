---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
ticket: EP-0009-US-0008-TK-0003-ai-params-from-scenario
title: "ai-params-from-scenario"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0008-TK-0002-flank-approach-math"]
files_touched: "ScenarioData.cs; ScenarioLoader.cs; SimulationEngine.cs; SimulationEngine.HostileCombat.cs; HostileCombatSaveData.cs; HostileAttackAiTests.cs; Board evidence"
serves: ["US8-AC2", "US8-AC3"]
created: 2026-10-12
---

# EP-0009-US-0008-TK-0003-ai-params-from-scenario

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

The scenario `ai` block must cross the `SpaceObjectData` to `SpaceObjectRuntime` load/save boundary before `RefreshHostileCombat` can call configured `PlanAttack`. Hostile save validation currently hard-codes half launcher range, so it must accept the configured radius. These runtime and save-validation files are necessary beyond the draft file list. `PirateScenarioTests.cs` contains Client scenario coverage; focused Engine integration tests will cover this ticket's live controller, defaults, persistence and partition behavior without changing unrelated Client fixtures.

## Evidence and self-review

Configured NPC `ai` values now pass through scenario validation, runtime and Save/Load into `PlanAttack`. A missing block calls the exact EP-0008 planner. Invalid standoff, band and flank are rejected before world replacement; non-NPC blocks are rejected. Save validation compares the stored radius with the captured configured value. The focused Engine tests cover 50 km band entry, opposite flank steering, default fallback, partition equality, save continuation and invalid input (9/9); related hostile regressions passed 37/37 before the final orbit assertion was added. Solution build passed with 0 warnings/errors, scoped `dotnet format whitespace --verify-no-changes` and `git diff --check` passed. Real Mukha content is planned in US-0009; native/game NOT RUN and Graphify rebuild SKIPPED per user instruction.

**Файлы:** `Scenario/ScenarioData.cs` (блок `ai` у NPC), `Scenario/ScenarioLoader.cs` (валидация), файл контроллера ИИ EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/PirateScenarioTests.cs`.
**Тесты:** `Mukha_closes_to_50km_and_orbits`, `Flank_offsets_spread_attackers`, `Missing_ai_block_uses_ep8_defaults`, `Partition_invariance`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0008-hostile-flank-ai.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
