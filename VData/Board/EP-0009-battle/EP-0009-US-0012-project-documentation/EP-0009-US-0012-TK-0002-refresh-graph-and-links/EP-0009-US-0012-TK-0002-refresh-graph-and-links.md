---
epic: EP-0009-module-combat
story: EP-0009-US-0012-project-documentation
ticket: EP-0009-US-0012-TK-0002-refresh-graph-and-links
title: "refresh-graph-and-links"
stage: draft
layer: tooling
depends_on: ["EP-0009-US-0012-TK-0001-update-project-documents"]
files_touched: "DB/Documentation/01-Requirements/EngineRequirements.md; DB/Board/EP-0009-battle/evidence/navigation.md; DB/Board/EP-0009-battle/EP-0009-US-0012-project-documentation/EP-0009-US-0012-project-documentation.md; DB/Board/EP-0009-battle/ImplementationStatus.md; DB/Board/EP-0009-battle/PublicationStatus.md; this ticket"
serves: ["US12-AC2"]
created: 2026-10-12
---

# EP-0009-US-0012-TK-0002-refresh-graph-and-links

STATUS: LINKS/IDS/DAG PASS; GRAPHIFY SKIPPED BY USER; NATIVE ACCEPTANCE OPEN

Перестроить граф и проверить ссылки, как в EP-0007 US-0010 TK-0002.

## Result under current user instruction

The user explicitly stopped Graphify updates to save tokens and required continuing without graph rebuilds. No graph command ran and `src/graphify-out` was not changed. A local Markdown audit found 28 pre-existing references to three missing `EngineRequirements.md` anchors (`phase-three-combat`, `countermeasure-combat`, `approach-shortest-route`); stable HTML anchors now preserve those links without rewriting historical headings. Final recheck: 593 relative Markdown links across EP-0009 Board, all `Documentation` Markdown and EP-0008 implementation status; **0 missing targets/anchors**. Board has 12 story cards and 38 unique ticket cards, all local ticket dependencies resolve and the dependency DAG is acyclic. See [navigation evidence](../../evidence/navigation.md). This is link/metadata evidence only, not runtime, native or Graphify evidence.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0012-project-documentation.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
