---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
ticket: EP-0009-US-0011-TK-0001-save-format-17
title: "save-format-17"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0010-TK-0003-combat-journal-lines"]
files_touched: "Contracts/CombatSnapshot.cs; Engine/Scenario/{ScenarioData,ScenarioLoader,CombatSaveData,CountermeasureSaveData,HostileCombatSaveData}.cs; Engine/SimulationEngine.CombatPersistence.cs; tests/Engine/{CatalogCompatibility,CombatSaveLoad,CombatSaveSchema,CountermeasureSaveSchema,DisabledModule,PhaseThreeSaveContinuation,PhaseThreeSaveSchema,PhaseThreeSaveVersion,TradingEconomySaveSchema}Tests.cs; tests/Client/{LocalSessionIntegration,PhaseThreeSessionRoundtrip}Tests.cs; tests/Contracts/{HostileCombatSnapshot,ResourceSurveySnapshot,SystemMapContract}Tests.cs; Board evidence"
serves: ["US11-AC1"]
created: 2026-10-12
---

# EP-0009-US-0011-TK-0001-save-format-17

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

The current save entrypoint already rejects all versions other than 16 and serializes module durability, active torpedo lists, defense queues, mode/broken state, three RNG state/counter pairs and NPC AI parameters. This ticket raises the envelope to 17 and subsystem versions to defense3/combat2/hostile2, makes these continuation facts mandatory for v17, replaces the obsolete 100% hit-chance preflight with a valid range, and proves a v16 file is rejected before mutation. `HostileCombatSaveData.cs` and hardcoded-version tests extend the draft file list. Retain format0 scenario loading and historical test fixtures where they do not represent an on-disk v17 save.

V17 rejects v16 without migration, so the old singular active-torpedo JSON migration hook has no valid save use. Remove that property from `Contracts/CombatSnapshot.cs` and its normalization branch; v17 uses the required active-flight list exclusively. The older auto-attempt collection is removed from the defense payload and its test fixtures. Unarmored modules represent zero armor with `armorClass:none` and nullable `armor`, while armored modules require a saved armor value.

The required Contracts regression run exposed eight record-equality assertions left behind by TK-0002's `ModuleHealth` snapshot field: default `ImmutableArray` becomes empty after JSON. Update the three affected legacy roundtrip test files to compare the new field structurally while preserving equality checks on all pre-existing snapshot fields. This is a confirmed TK-0002 test gap repaired in this ticket before publication.

## Evidence and self-review

Save envelope17 now captures defense3, combat2 and hostile2. Existing runtime fields preserve module durability/disable/destruction, launcher readiness/mode/broken state/active flights, defense burst queues, all three named combat RNG state/counter pairs and NPC AI parameters. V17 preflight requires current payload versions, RNG state, mode-command continuation, armored module values and a readiness deadline for active/queued defense. It accepts a captured hit percentage anywhere in 0..100. V16 and lower fail the save entrypoint before mutation; obsolete attempted-torpedo and singular active-flight fields are rejected from v17 JSON. Existing simulation tests cover mid-burst, breakdown and RNG continuation; new tests cover format/payload roundtrip, disabled durability, AI, bounded chance and malformed state atomicity. Engine save/compatibility167/167 and related combat326/326, Client save8/8 and broad1876/1876 excluding the historically slow market route suite, Contracts194/194, solution build0 warnings/errors, scoped format and diff checks PASS. Full Engine corpus and native/game interaction NOT RUN; Graphify rebuild SKIPPED by user. Headless US11-AC1 PASS, human acceptance remains OPEN.

**Файлы:** `Scenario/ScenarioData.cs`, `Scenario/ScenarioLoader.cs`, `Scenario/CombatSaveData.cs`, `Scenario/CountermeasureSaveData.cs`, `SimulationEngine.CombatPersistence.cs`. Тесты по A12, шестым файлом: `tests/DeepSpaceSaga.Engine.Tests/CombatSaveLoadTests.cs`.
**Тесты:** `Roundtrip_mid_burst`, `Roundtrip_disabled_module_timer`, `Roundtrip_broken_overheat`, `Roundtrip_ai_params`, `V16_rejected_cleanly`, `Rng_counters_continue`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0011-save-and-combat-acceptance.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
