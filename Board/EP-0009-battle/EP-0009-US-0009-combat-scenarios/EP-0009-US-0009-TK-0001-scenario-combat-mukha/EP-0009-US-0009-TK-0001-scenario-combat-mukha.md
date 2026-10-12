---
epic: EP-0009-module-combat
story: EP-0009-US-0009-combat-scenarios
ticket: EP-0009-US-0009-TK-0001-scenario-combat-mukha
title: "scenario-combat-mukha"
stage: implemented
layer: content-data
depends_on: ["EP-0009-US-0008-TK-0003-ai-params-from-scenario"]
files_touched: "Client/Scenarios/CombatMukha/scenario.json; Client/Data/Modules/LivingQuarters/modules-livingquarters.json; tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs; AllScenarioSystemContentTests.cs; SystemMapNavigationTests.cs; Board evidence"
serves: ["US9-AC1"]
created: 2026-10-12
---

# EP-0009-US-0009-TK-0001-scenario-combat-mukha

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

The existing `PlayerShipOnly` Tetrarch has nine modules but only three crew/cabins. The approved EP-0009 execution decision A09 requires three operators plus a pilot and four living places, so the production living-quarters catalog needs one explicit four-cabin variant. `ScenarioRepository` already enumerates every valid `Scenarios/**/scenario.json` and the Client project copies that tree; no project-file or UI change is needed. New content tests will load the real scenario through the repository and Engine content loader.

The broad Client run exposed `AllScenarioSystemContentTests` assuming every listed scenario is also in the solar generation allowlist. CombatMukha is intentionally not in that allowlist because its starting positions are a local encounter. Update that test to compare the configured solar subset and execute its solar assertions only for that subset; the dedicated combat content test proves the new scenario starts.

An isolated repeat showed `SystemMapNavigationTests.FitAndResizePreserveWorldDistances` has the same solar-only assumption (`snapshot.SolarSystemMap!.SystemRadius`) while iterating all listed scenarios. Restrict that solar-system geometry test to the configured solar subset as well; the combat scenario remains covered by its content/start test.

## Evidence and self-review

The real `CombatMukha` scenario is listed by `ScenarioRepository`, loads with production content, starts paused and activates three hostile AI states on physical advance. The player has nine light-armored modules, four crew with skill20, distinct weapon operators and pilot, and four cabins via an explicit catalog variant. Three Strizh/torpedo Mukhas have the specified positions, speed, headings, armor, skills and flank offsets. The broad Client run first found two solar-only tests assuming every listed scenario has a solar system; both were scoped to the solar configuration, then passed with all Client tests in the run (1863/1863, excluding the previously isolated market suite). Focused final content2/2, solution build0 warnings/errors, scoped format and diff checks PASS. Game/native NOT RUN, Graphify rebuild SKIPPED per user.

**Файлы:** `Client/Scenarios/CombatMukha/scenario.json` (новый), `tests/DeepSpaceSaga.Client.Tests/CombatScenariosContentTests.cs` (новый), `Client/DeepSpaceSaga.Client.csproj` (только если копирование `Scenarios\**` не подхватит новую папку).
**Тесты:** `Mukha_scenario_listed_and_loads`, `Positions_ai_armor_match_spec`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0009-combat-scenarios.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
