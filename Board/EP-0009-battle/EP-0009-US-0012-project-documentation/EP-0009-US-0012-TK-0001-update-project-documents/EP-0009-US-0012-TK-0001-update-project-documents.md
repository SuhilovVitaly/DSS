---
epic: EP-0009-module-combat
story: EP-0009-US-0012-project-documentation
ticket: EP-0009-US-0012-TK-0001-update-project-documents
title: "update-project-documents"
stage: draft
layer: documentation
depends_on: ["EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native"]
files_touched: "Documentation/00-Process/CLAUDE.md; Documentation/README.md; Documentation/01-Requirements/{EngineRequirements,FirstReleaseRequirements}.md; Documentation/02-FirstRelease/{Mechanics,Screens} combat pages; Documentation/04-Engineering/{ModuleCombat,ModuleCombatDocumentation,BasicTorpedoCombat,CountermeasureCombat}.md; Board/EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md; Board/EP-0009-battle/{Documentation,ImplementationStatus,PublicationStatus}.md; this ticket"
serves: ["US12-AC1"]
created: 2026-10-12
---

# EP-0009-US-0012-TK-0001-update-project-documents

STATUS: DOCUMENTATION UPDATED; NATIVE/HUMAN ACCEPTANCE OPEN

Составить список документов, где упоминается хотя бы одно из:
- HP корпуса;
- формула EP-0008 «точность − манёвренность»;
- «навыки не влияют»;
- один снаряд на аппарат;
- auto-attempt flag;
- `ship.tetrarch` как единственный повреждаемый класс;
- числа 55/5, 200/100/200 км, 12 км/с, 60 с, 10 с.

Каждый документ обновить или указать, почему он не меняется.

## Result

[Impact inventory](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/ModuleCombatDocumentation.md) lists changed and reviewed-unchanged documents, with their disposition. Current requirements, first-release mechanics/screens, process navigation, engineering combat reports and EP-0008 supersession now describe the real Save17/module contract. Dated earlier evidence remains historical. The absent old `CombatDesign` source is not represented as updated; the new sampler README documents its independent approximations. The original 12-story/38-ticket Board plan remains intact. US11-AC3 native acceptance stays OPEN/NOT RUN; Graphify rebuild SKIPPED by user instruction. This ticket changes documentation only; validation is link/content/diff audit, not a new runtime or native test.

Validation: relative file targets across 21 changed Markdown files PASS; actual Board card count 12 stories/38 tickets; targeted stale-current wording scan repaired the EP-0008 header claims that still called Save16 and Hull HP current. `git diff --check` PASS. Full anchor/nav audit remains TK-0002.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0012-project-documentation.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
