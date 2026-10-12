---
epic: EP-0009-module-combat
story: EP-0009-US-0008-hostile-flank-ai
ticket: EP-0009-US-0008-TK-0002-flank-approach-math
title: "flank-approach-math"
stage: draft
layer: motion
depends_on: ["EP-0009-US-0008-TK-0001-npc-fire-cadence"]
files_touched: "ground-before-edit"
serves: ["US8-AC2"]
created: 2026-10-12
---

# EP-0009-US-0008-TK-0002-flank-approach-math

STATUS: NOT STARTED

**Задача.** В Motion-математику относительной орбиты EP-0008 (US-0004-TK-0002 EP-0008) добавить параметр радиуса и фазу захода с фланговым смещением. Формулы должны совпадать с `src/DeepSpaceSaga.CombatSim/combatsim/ai.py` (`AttackAI`). Курс: 0° — вверх, отсчёт по часовой стрелке.
**Файлы:** файл относительной орбиты из EP-0008 (на HEAD), `tests/DeepSpaceSaga.Motion.Tests/<RelativeOrbit>Tests.cs` (EP-0008; дополнить), `tests/DeepSpaceSaga.Motion.Tests/FlankApproachTests.cs` (новый).
**Тесты:** `Default_params_equal_ep8_behavior`, `Far_target_lead_with_flank`, `Inside_band_orbits`, `Too_close_retreats`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0008-hostile-flank-ai.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
