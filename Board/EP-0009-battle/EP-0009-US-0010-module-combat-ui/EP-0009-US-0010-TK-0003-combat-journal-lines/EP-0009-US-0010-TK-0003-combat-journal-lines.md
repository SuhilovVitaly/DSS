---
epic: EP-0009-module-combat
story: EP-0009-US-0010-module-combat-ui
ticket: EP-0009-US-0010-TK-0003-combat-journal-lines
title: "combat-journal-lines"
stage: implemented
layer: client
depends_on: ["EP-0009-US-0010-TK-0002-module-health-ui"]
files_touched: "Client/CombatJournalPanel.cs; Client/Data/Locale/Russian.json; Client/Data/Locale/English.json; Client/CombatJournalPanelTests.cs; Client/PhaseThreeJournalUiTests.cs; Board evidence"
serves: ["US10-AC3"]
created: 2026-10-12
---

# EP-0009-US-0010-TK-0003-combat-journal-lines

STATUS: IMPLEMENTED — headless verified, publication pending

## Actual scope before editing

Engine already emits the six new event types and their payloads. The panel currently prints the event title but omits attack/defense, per-module before/after durability, destroyed module, ship death cause, skip reason and launcher-breakdown mode. It also labels interception with the old accuracy-minus-maneuverability presentation. Format the existing journal DTO without changing Engine events, add only the necessary English/Russian keys, and cover representative event lines with headless panel tests. Native/game and Graphify runs remain deferred by user.

## Evidence and self-review

The panel now presents torpedo attack/defense/chance/roll, interception's base+defense−attack formula and captured ratings, hit module with armor/structure before→after, destroyed module ID, localized critical/threshold ship-death cause, skipped second-shot reason and Overheat breakdown mode/chance/roll. English and Russian strings are present. New/related journal and locale tests22/22 PASS; broad Client1876/1876 excluding the historically slow market route suite PASS, solution build0 warnings/errors, scoped format and diff checks PASS. The first broad run found the old interception-label expectation and one transient trajectory-allocation assertion; after adapting the former, both passed in an isolated 8/8 run and the full broad repeat passed. US10 self-review: AC1 defeat boundary, AC2 module panel/bar, AC3 journal lines PASS headlessly. Native defeat/UI acceptance NOT RUN and Graphify rebuild SKIPPED per user.

**Файлы:** `CombatJournalPanel.cs`, `Data/Locale/Russian.json`, `Data/Locale/English.json`, `tests/DeepSpaceSaga.Client.Tests/CombatJournalPanelTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0010-module-combat-ui.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
