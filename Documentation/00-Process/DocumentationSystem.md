# Documentation System

## Goals

- One documentation root: `Documentation/`.
- Stable top-level categories that are easy to scan.
- Requirements separate from investigations.
- First-release gameplay docs grouped by release area.
- Old task lists preserved, but clearly marked as backlog.

## Folder Map

### `00-Process/`

Operational guidance for contributors and agents. Use this for repository rules, coding workflow, and documentation process.

### `01-Requirements/`

Authoritative product and engine decisions. Use this for durable requirements that implementation should follow.

Current primary documents:

- `EngineRequirements.md`
- `FirstReleaseRequirements.md`

### `02-FirstRelease/`

Release-specific details. Keep feature scope here when it is specific to the first playable release.

- `Screens/` - UI screens and transitions.
- `Mechanics/` - gameplay mechanics.
- `TechnicalTasks/` - implementation tasks derived from release requirements.

### `03-Design/`

Visual and interaction design docs: tactical map, portraits, UI asset standards, style prompts, and reusable design references.

### `04-Engineering/`

Engineering research and evidence: schemas, performance investigations, code reviews, generated test result files, probes, and implementation notes.

### `05-Backlog/`

Historical or pending work lists. Move a backlog item into `01-Requirements/`, `02-FirstRelease/`, or `04-Engineering/` only when it becomes active and maintained.

### `06-Tooling/`

Docs for repository tools, asset generation utilities, and local workflows.

## Naming Rules

- Use descriptive PascalCase filenames for durable specs: `EngineRequirements.md`, `ItemCatalogSchema.md`.
- Keep dated investigation folders when the date is part of the evidence trail: `TradeUI20260912/`.
- Prefer one topic per file.
- Put generated evidence next to the engineering note that explains it.

## Updating Requirements

When a decision changes:

1. Update the authoritative document first.
2. Update release-level summaries that point to it.
3. Update code comments and tests that cite the old location or behavior.
4. Keep a short dated note inside the document when the change reverses an earlier decision.

## Root Files

The repository root keeps small compatibility shims for `AGENTS.md` and `CLAUDE.md`. The real content lives in `Documentation/00-Process/`.

## Operational Skill Files

Some `.md` files are part of an executable local workflow rather than general project documentation. For example, `src/DeepSpaceSaga.Client/Images/Persons/W4/Portraits/character-generator/SKILL.md` and its `references/` folder must remain beside the generator scripts and assets. Keep those files in-place and link to them from `Documentation/`.
