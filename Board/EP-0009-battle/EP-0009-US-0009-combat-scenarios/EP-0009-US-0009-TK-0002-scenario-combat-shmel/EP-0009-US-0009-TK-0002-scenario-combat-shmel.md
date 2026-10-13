---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
ticket: EP-0009-US-0009-TK-0002-scenario-combat-shmel
title: "scenario-combat-shmel"
stage: implemented
layer: content-data
depends_on: ["EP-0009-US-0009-TK-0001-scenario-combat-mukha"]
files_touched: "Client/Scenarios/CombatShmel/scenario.json; CombatScenariosContentTests.cs; Board evidence"
serves: ["US9-AC2"]
created: 2026-10-12
---

# EP-0009-US-0009-TK-0002-scenario-combat-shmel

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

Clone the published CombatMukha schema and loadout, switching the three NPCs to `ship.shmel` and adding the existing production `module.countermeasure.launcher.single` in a third hull cell with a distinct skill20 operator. Keep the shared Tetrarch and position/AI parameters. The Client project already copies `Scenarios/**`, and the production catalog already defines the single-barrel launcher, so no new catalog/project file is needed. The Engine `HostileAttackAiTests` fixture is independent and requires no edit; Client content tests will cover the real scenario and active AI.

## Evidence and self-review

The published Mukha encounter is reused as the source for a second selectable, paused encounter. All three NPCs now have `ship.shmel`, a Strizh engine, torpedo launcher and active single-barrel countermeasure launcher in distinct hull cells, with three distinct skill20 crew assignments. The same Tetrarch, positions and −35/0/+35 flank geometry are retained. Production loading and physical AI activation pass. Focused combat content4/4 and related content/map12/12 PASS; solution build0 warnings/errors, scoped format and diff checks PASS. Native/game NOT RUN; Graphify rebuild SKIPPED per user.

**Файлы:** `Client/Scenarios/CombatShmel/scenario.json` (новый), `CombatScenariosContentTests.cs`, `tests/DeepSpaceSaga.Engine.Tests/HostileAttackAiTests.cs` (фикстура).
**Тесты:** `Shmel_scenario_loads`, `Shmel_has_single_barrel_pd_and_operator`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0009-combat-scenarios.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
