---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
ticket: EP-0009-US-0008-TK-0002-flank-approach-math
title: "flank-approach-math"
stage: implemented
layer: motion
depends_on: ["EP-0009-US-0008-TK-0001-npc-fire-cadence"]
files_touched: "ground-before-edit"
serves: ["US8-AC2"]
created: 2026-10-12
---

# EP-0009-US-0008-TK-0002-flank-approach-math

STATUS: IMPLEMENTED — pure math/headless tests passed; game/native NOT RUN by user instruction.

Execution scope before edit (2026-10-12): `HostileOrbitMath.Plan` is the pure EP-0008 orbital controller and its existing callers must retain identical default behavior. The historical Python simulator path is absent (ExecutionDecisions O5), so implement the explicit D33 formula from the epic as a separate configured `PlanAttack` path: far-range target lead plus `flankOffsetDeg × min(1,(d-standoff)/100)`, close-range retreat, and the existing orbit controller inside the band. New phase values require additive Contracts and Client labels, with focused Motion/roundtrip tests. Engine scenario parameters and live wiring belong to TK-0003. No Graphify/game/native runs.

Result: Motion `PlanAttack` preserves exact EP-0008 `Plan` output when no band is configured. Configured far targets use target lead and distance-scaled signed flank angle; within the band the previous orbit planner steers while retaining pursuit fallback for an unreachable moving centre; inside the inner boundary it steers away. Bounded acceleration/turn and 100 ms analytic segments remain. `FlankApproach` and `Retreat` phases roundtrip in Contracts and have Client labels. Focused Motion17/17; full Motion173/173, Contracts194/194, related Engine27/27 and Client10/10, solution build0 warnings/errors, scoped format/diff PASS. Historical Python `AttackAI` source is unavailable; equivalence to it is not claimed. Live scenario binding follows TK-0003. Native/game NOT RUN; Graphify rebuild SKIPPED per user.

**Задача.** В Motion-математику относительной орбиты EP-0008 (US-0004-TK-0002 EP-0008) добавить параметр радиуса и фазу захода с фланговым смещением. Формулы должны совпадать с `src/DeepSpaceSaga.CombatSim/combatsim/ai.py` (`AttackAI`). Курс: 0° — вверх, отсчёт по часовой стрелке.
**Файлы:** файл относительной орбиты из EP-0008 (на HEAD), `tests/DeepSpaceSaga.Motion.Tests/<RelativeOrbit>Tests.cs` (EP-0008; дополнить), `tests/DeepSpaceSaga.Motion.Tests/FlankApproachTests.cs` (новый).
**Тесты:** `Default_params_equal_ep8_behavior`, `Far_target_lead_with_flank`, `Inside_band_orbits`, `Too_close_retreats`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0008-hostile-flank-ai.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
