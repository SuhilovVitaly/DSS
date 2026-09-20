# Deep Space Saga Documentation

This folder is the single home for DSS project documentation.

## Start Here

- [Agent guide](00-Process/AGENTS.md) - rules and project notes for coding agents.
- [Developer guide](00-Process/CLAUDE.md) - build, architecture, and implementation conventions.
- [Documentation system](00-Process/DocumentationSystem.md) - where to put new docs and how to update existing ones.
- [Engine requirements](01-Requirements/EngineRequirements.md) - main requirements checkpoint for the DSS engine.
- [First release requirements](01-Requirements/FirstReleaseRequirements.md) - release scope and links to first-release details.
- [Approach requirements](01-Requirements/EngineRequirements.md#approach-shortest-route) - shortest rendezvous, captured trailing-point fallback, and planner version 3 (2026-09-20).
- [Approach implementation](04-Engineering/ApproachRoutes.md) - solver, numerical tolerances, prediction, and save compatibility.

## Sections

| Folder | Purpose |
|---|---|
| `00-Process/` | Agent instructions, developer workflow, documentation maintenance rules. |
| `01-Requirements/` | Product and engine requirements that define source-of-truth behavior. |
| `02-FirstRelease/` | First-release screens, mechanics, and implementation task specs. |
| `03-Design/` | Visual, UX, map, portrait, and UI asset specifications. |
| `04-Engineering/` | Engineering notes, code-review records, performance investigations, schemas, and generated evidence. |
| `05-Backlog/` | Older task lists and discrepancy backlogs that are useful but not active source-of-truth specs. |
| `06-Tooling/` | Documentation for repository tools and local asset workflows. |

Operational skill files that must be discovered in-place, such as the character-generator `SKILL.md` and its `references/`, stay beside the asset workflow they power. Link to them from `Documentation/` instead of moving them.

## Update Rules

1. Update the closest existing document instead of creating a duplicate.
2. Keep source-of-truth decisions in `01-Requirements/` or the relevant first-release document.
3. Put implementation investigations, measurements, and one-off reviews in `04-Engineering/`.
4. When a document moves or is renamed, update links in Markdown, code comments, and project files in the same change.
5. Keep old paths out of new documentation; link from `Documentation/` paths.
