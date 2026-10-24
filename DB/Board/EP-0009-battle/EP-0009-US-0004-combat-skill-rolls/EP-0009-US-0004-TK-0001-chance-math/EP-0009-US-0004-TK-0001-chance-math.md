---
epic: EP-0009-module-combat
story: EP-0009-US-0004-combat-skill-rolls
ticket: EP-0009-US-0004-TK-0001-chance-math
title: "chance-math"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0003-TK-0004-disabled-module-effects"]
files_touched: "ground-before-edit"
serves: ["US4-AC1"]
created: 2026-10-12
---

# EP-0009-US-0004-TK-0001-chance-math

STATUS: IMPLEMENTED; headless validation PASS; native/game acceptance NOT RUN.

Execution scope before edit: `Combat/InterceptionMath.cs` implements the EP-0008 subtraction formula; production callers are `SimulationEngine.ManualDefense.cs`, `SimulationEngine.Countermeasures.cs`, and `SimulationEngine.DefensePreview.cs`. `SimulationEngine.WeaponOperators.cs` currently keeps skill only as metadata. `Scenario/CountermeasureSaveData.cs` validates frozen flight/journal chances and legacy operator ratings, so Save/Load compatibility must be maintained while switching live calculation. Engine and Client regression expectations will need updating. No Graphify/game/native runs.

Execution result: `CombatChanceMath.ChanceTenths(attack, defense, rules)` now supplies the configured base/clamps to automatic and manual defense and preview. The production rules use base 50. Countermeasure operator accuracy skill contributes directly to the effective rating; torpedo attack/maneuverability separation is assigned to TK-0002/TK-0003. Legacy `InterceptionMath` remains solely for validating frozen EP-0008 save values. Save v16 has no chance-rules identity: historical base-zero and shipped base-fifty values validate, while Save/Load of arbitrary custom chance-base rules remains OPEN for a versioned format. Historical EP-0008 fixtures pin their old rule base, and one Client presentation assertion now tolerates either valid ship-survival outcome.

Evidence: Engine non-corpus 1961/1961, Client 1859/1859, solution build 0 warnings/errors, scoped `dotnet format whitespace --verify-no-changes` PASS, `git diff --check` PASS. Graphify rebuild, game launch and native runs SKIPPED per user instruction. Publication SHA and remote verification are recorded in `PublicationStatus.md` after push.

**Задача.**
1. Функция шанса EP-0008 (на HEAD — наследник `InterceptionMath.ChanceTenths`) заменяется на `CombatChanceMath.ChanceTenths(attack, defense, rules)`.
2. Убирается обнуление влияния навыков (EP-0008 TK-0005): навыки прибавляются напрямую. Формулу `base × skill/50` из EP-0007 не возвращать.

**Файлы:** `Combat/CombatChanceMath.cs` (переименование), `SimulationEngine.WeaponOperators.cs`, `SimulationEngine.Countermeasures.cs` (вызовы), `tests/DeepSpaceSaga.Engine.Tests/InterceptionChanceTests.cs` (переписать).
**Тесты:** `Equal_gives_500`, `Clamp_bounds`, `Rules_base_changes_result`, `Skills_change_result`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0004-combat-skill-rolls.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
