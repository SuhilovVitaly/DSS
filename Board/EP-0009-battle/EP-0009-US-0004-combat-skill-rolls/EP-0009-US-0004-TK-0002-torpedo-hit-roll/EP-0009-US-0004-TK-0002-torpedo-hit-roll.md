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

STATUS: NOT STARTED

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
