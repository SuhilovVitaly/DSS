---
epic: EP-0009-module-combat
story: EP-0009-US-0007-manual-defense-bursts
ticket: EP-0009-US-0007-TK-0002-defense-panels-burst-ui
title: "defense-panels-burst-ui"
stage: implemented
layer: client
depends_on: ["EP-0009-US-0007-TK-0001-manual-fire-burst"]
files_touched: "ground-before-edit"
serves: ["US7-AC2"]
created: 2026-10-12
---

# EP-0009-US-0007-TK-0002-defense-panels-burst-ui

STATUS: IMPLEMENTED — headless checks passed; game/native NOT RUN by user instruction.

Execution scope before edit (2026-10-12): two defense instances and D06 chance breakdown already render through `GameSessionScreen.ManualDefense.cs` and `CommandsPanel` detail rows; `DefenseSnapshot` already carries `Barrels`, `PendingBurstShots`, and `ReadyAtMotionTimeMs`. Add localized per-instance barrel count, pending next shot, and launch-based reload timer to those existing details, preserving the chance/flight lines and viewport wrapping. `ObjectInfoPanel.cs` describes selected flight rather than launcher status and does not need editing. Extend `DefenseCommandPanelTests` and related layout assertions. No Graphify/game/native runs.

Result: each defense panel shows its own localized barrel count, pending second-shot countdown, and reload countdown based on authoritative MotionTimeMs. Existing D06 preview chance breakdown and active-flight lines remain. Headless tests cover separate modules and layout, target panels9/9, Client excluding the separately verified slow market scenario1856/1856; solution build0 warnings/errors, scoped format/diff PASS. Game/native NOT RUN and Graphify rebuild SKIPPED per user.

**Файлы:** панель ПРО из EP-0008 (на HEAD), `GameSessionScreen.Countermeasures.cs`, `ObjectInfoPanel.cs`, `Data/Locale/Russian.json`, `tests/DeepSpaceSaga.Client.Tests/DefenseCommandPanelTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0007-manual-defense-bursts.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
