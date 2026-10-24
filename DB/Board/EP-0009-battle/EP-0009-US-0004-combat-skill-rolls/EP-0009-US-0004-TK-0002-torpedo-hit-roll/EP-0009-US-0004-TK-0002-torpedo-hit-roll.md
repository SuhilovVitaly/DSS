---
epic: EP-0009-module-combat
story: EP-0009-US-0004-combat-skill-rolls
ticket: EP-0009-US-0004-TK-0002-torpedo-hit-roll
title: "torpedo-hit-roll"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0004-TK-0001-chance-math"]
files_touched: "ground-before-edit"
serves: ["US4-AC2"]
created: 2026-10-12
---

# EP-0009-US-0004-TK-0002-torpedo-hit-roll

STATUS: IMPLEMENTED; headless validation PASS; native/game acceptance NOT RUN.

Execution scope before edit: launch capture and contact resolution are in `src/DeepSpaceSaga.Engine/SimulationEngine.Combat.cs`; operator resolution is in `SimulationEngine.WeaponOperators.cs`, named streams in `Rng/RngStreamNames.cs`, and state capture/restore plus validation in `SimulationEngine.CombatPersistence.cs` and `Scenario/CombatSaveData.cs`. `TorpedoSnapshot` and `CombatJournalEntry` already expose additive attack/chance/roll fields. Existing contact tests are in `TorpedoImpactTests.cs`; a focused hit-roll test file will cover the new stream, chance and miss path. No Graphify rebuild, game launch or native run.

Execution result: launcher module accuracy plus assigned operator accuracy skill is captured at launch; the mode bonus remains zero until US-0006. Contact with a ship reads active engine maneuverability plus its pilot's maneuverability skill, with the engine contribution zero while unavailable. A separate `combat.torpedo.hit.v1` stream consumes one roll at ship contact, persists its state/counter, and records `TorpedoHitRoll` before damage. A miss terminates the projectile with `TerminationKind.Miss` and no damage. Contact with a non-ship obstacle retains the prior collision path without a hit roll. Flight snapshots contain a launch-time chance estimate; journal entries contain the authoritative contact-time attack, defense, chance and roll. Save validation checks hit-roll outcomes and counter continuity. Historical guaranteed-hit fixtures and Client UI hit-flow fixtures were pinned explicitly; separate tests cover real 50% and miss behavior.

Evidence: focused torpedo-hit tests 4/4, Engine non-corpus 1969/1969, Client 1859/1859, Contracts 192/192, solution build 0 warnings/errors, scoped format and diff PASS. Broad Engine and Client suites were repeated and passed after journal-counter validation hardening. Native/game/Graphify NOT RUN by user instruction. Publication SHA and remote verification are recorded in `PublicationStatus.md` after push.

**Задача.**
1. При пуске захватывается атака: точность аппарата + навык оператора. Бонус режима до US-0006 равен 0.
2. В момент контакта берётся защита цели по D05.
3. Бросок в потоке `combat.torpedo.hit.v1`.
4. Попадание → `ApplyModuleHit`. Промах → удаление торпеды с `TerminationKind.Miss`.
5. Запись журнала `TorpedoHitRoll`; в snapshot — `HitChanceTenths`.

**Файлы:** `SimulationEngine.Combat.cs`, `Rng/RngStreamNames.cs`, `tests/DeepSpaceSaga.Engine.Tests/TorpedoHitRollTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs`.
**Тесты:** `Fifty_percent_example`, `Disabled_engine_target_easier`, `Miss_no_damage`, `Partition_invariance`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0004-combat-skill-rolls.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
