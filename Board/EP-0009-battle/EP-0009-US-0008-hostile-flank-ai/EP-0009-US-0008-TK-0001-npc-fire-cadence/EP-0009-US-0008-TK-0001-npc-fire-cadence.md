---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
ticket: EP-0009-US-0008-TK-0001-npc-fire-cadence
title: "npc-fire-cadence"
stage: implemented
layer: engine
depends_on: ["EP-0009-US-0007-TK-0002-defense-panels-burst-ui"]
files_touched: "ground-before-edit"
serves: ["US8-AC1"]
created: 2026-10-12
---

# EP-0009-US-0008-TK-0001-npc-fire-cadence

STATUS: IMPLEMENTED — headless checks passed; game/native NOT RUN by user instruction.

Execution scope before edit (2026-10-12): NPC launcher cooldown and repeated firing already flow through shared `TryLaunchTorpedo`; the EP-0008 hostile planner still launches solely on range and no torpedo TTL exists for either owner. Add the D33 reachable-intercept gate to the hostile planner and its internal executor, capture per-projectile lifetime at launch, schedule deterministic expiry before later contacts, and persist/validate the lifetime in combat state. This extends Contracts and Save/Load plus focused tests beyond the planned file list. Legacy nullable lifetime remains readable until Save17 migration. No Graphify/game/native runs.

Result: hostile fire reuses the shared launch cooldown and requires the planned intercept at most 0.8 of configured torpedo lifetime. NPC torpedoes capture the production 120-second lifetime; all torpedoes expire at their captured physical deadline with journal/impact facts, and Save/Load resumes the same expiry. The previously infinite-life NPC exception is removed. Older tests that assumed an impossible NPC launch were adapted to reachable trajectories without dropping their range/PRO assertions. New NPC/TTL3/3, combat-focused376/376, Contracts192/192, Client1856/1856 excluding the separately verified slow market case, solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN; Graphify rebuild SKIPPED per user.

**Файлы:** файл планировщика огня NPC из EP-0008 (на HEAD), `SimulationEngine.Combat.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileFireTests.cs` (EP-0008; адаптировать), `tests/DeepSpaceSaga.Engine.Tests/NpcTorpedoLifetimeTests.cs` (новый).
**Тесты:** `Npc_fires_every_15s_when_reachable`, `No_fire_when_intercept_unreachable`, `Npc_torpedo_expires_120s`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0008-hostile-flank-ai.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
