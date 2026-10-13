---
epic: EP-0009-module-combat
story: EP-0009-US-0004-combat-skill-rolls
ticket: EP-0009-US-0004-TK-0003-intercept-roll-skills
title: "intercept-roll-skills"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0004-TK-0002-torpedo-hit-roll"]
files_touched: "ground-before-edit"
serves: ["US4-AC3"]
created: 2026-10-12
---

# EP-0009-US-0004-TK-0003-intercept-roll-skills

STATUS: IMPLEMENTED; headless validation PASS; native/game acceptance NOT RUN.

Execution scope before edit: torpedo launch captures the legacy maneuverability rating in `SimulationEngine.Combat.cs`; `ResolveWeaponOperator` in `SimulationEngine.WeaponOperators.cs` still reports legacy torpedo skill as metadata. Automatic/manual interception already use `CombatChanceMath` in `SimulationEngine.Countermeasures.cs` and `SimulationEngine.ManualDefense.cs`, but require captured torpedo maneuverability with the direct operator skill. `SimulationEngine.DefensePreview.cs` calculates chance and has additive Attack/Defense/BaseChance fields to populate. Save validation in `Scenario/CountermeasureSaveData.cs` accepts base-plus-skill ratings. Relevant Engine tests are `CountermeasureResolutionTests.cs`, `DefensePreviewProjectionTests.cs`, `ManualCountermeasureFireTests.cs` and `WeaponOperatorRuntimeTests.cs`. No Graphify/game/native runs.

Execution result: torpedo maneuverability now captures its operator's `ManeuverabilitySkill` directly; countermeasure attack already captured its operator's `AccuracySkill` in TK-0001. Automatic and manual launches use those frozen ratings with the configured D04 base. Defense preview exposes attack, defense and base, and its chance matches the frozen manual launch. The production example 58+20 versus 50+20 yields 580 tenths; focused tests also show 0% and 100% contacts each consume one draw in the existing interception RNG stream. Old `TorpedoSkill` remains in legacy schema but no longer changes the EP-0009 interception rating. Historical Client catalog/presentation assertions were migrated to the new meaning.

Evidence: focused Engine 62/62 after operator migration; focused example/preview/boundaries 13/13; Engine non-corpus 1972/1972, Client 1859/1859, solution build 0 warnings/errors, scoped formatter and diff PASS. US-0004 story review: AC1 shared configured chance and preview PASS headless; AC2 contact hit roll, Strizh/pilot formula, miss and RNG Save/Load PASS headless; AC3 58% example, preview, frozen ratings, existing stream and 0%/100% draws PASS headless. Arbitrary custom chance-base Save v16 roundtrip remains OPEN pending versioned rule identity. Native/game acceptance NOT RUN and Graphify rebuild SKIPPED per user. Publication SHA and remote verification are recorded in `PublicationStatus.md` after push.

**Задача.**
1. Перехват считается по D06. Атака захватывается при пуске противоракеты, защита — при пуске торпеды.
2. `DefenseLaunchPreview` EP-0008 показывает новый шанс и его разложение.

**Файлы:** `SimulationEngine.Countermeasures.cs`, `SimulationEngine.WeaponOperators.cs`, файл построения превью EP-0008 (на HEAD), `tests/DeepSpaceSaga.Engine.Tests/CountermeasureResolutionTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/DefensePreviewTests.cs` (EP-0008; адаптировать).
**Тесты:** `Intercept_58_example`, `Preview_matches_resolution`, `Zero_and_hundred_still_draw`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0004-combat-skill-rolls.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
