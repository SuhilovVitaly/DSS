# AGENTS.md

The sole diagnostic artifact root is `D:/DeepSpaceSaga/artifacts/validation/`.
Do not create `D:/DeepSpaceSaga/DSS/artifacts/`; avoid relative `artifacts/...`
output paths when running repository commands. Tracked evidence explicitly
required inside Board remains part of the canonical task documentation.

## DSS Board is the only task source

For Deep Space Saga, `D:/DeepSpaceSaga/DSS/Board/` is the only authoritative
location for epics, user stories and tickets. Read, create and update these
artifacts only there, following the canonical Epic -> User Story -> Ticket
layout. Resolve IDs and dependencies against Board before planning,
implementation or review. Copies in chat, archives, other directories or old
reports are reference material and must not replace the current Board files.
If a supplied artifact is outside Board, locate its canonical Board version;
if none exists, record the gap and create a Board artifact only within the
user-authorized planning scope. Do not implement from an external copy.

## Validation artifacts

- Run builds and tests from the DSS repository root.
- Store all new build, test, formatting, review and diagnostic outputs in `D:\DeepSpaceSaga\artifacts\validation\<task>\<run>\`, using a unique timestamp or identifier for each run.
- Create that directory first. Use absolute paths for stdout/stderr redirection, logs, TRX, coverage, profiler output and temporary validation scripts; pass `dotnet test --results-directory` explicitly.
- Do not write validation artifacts directly into the workspace or repository root, or create new task-specific validation folders beside `artifacts`.
- Never create temporary task folders (including `ep*`, probe, review, native-capture or cache folders) in either root. Use only the task's designated `artifacts\validation\<task>\<run>\` directory.
- The same prohibition covers baseline/comparison folders, extracted `git archive` snapshots and partial source copies such as `CombatPresentationBaseline`, whatever their name. Keep diagnostic outputs in the designated artifacts directory and use Codex-managed worktrees for source isolation. Do not create junctions or symlinks to production source inside disposable diagnostic folders.
- Before completing a task, stop task-owned processes using its temporary outputs and remove its disposable scripts, caches, probe projects, intermediate renders and test results. Retain only evidence explicitly required by the task or referenced by its final report, inside the designated artifacts directory; remove empty run folders. Preserve other tasks' files and active runs.
- Verify cleanup and ensure the task left no temporary files or folders in the workspace or repository root; correct accidental root output before reporting completion.
- New reports must reference the actual artifact paths. Historical report paths are not templates for new commands.

## Git worktrees

- Never create sibling repository copies or worktrees such as `D:\DeepSpaceSaga\DSS-EP-*`, or place additional checkouts inside the workspace or repository root.
- For necessary isolation, inspect existing Codex-managed worktrees and reuse a suitable one, or create one with Codex's `create_worktree` tool. Do not manually run `git worktree add` or `git clone` for task isolation.
- Before completing work, preserve needed commits, uncommitted changes and non-ignored untracked files in a recoverable snapshot or branch; preserve needed ignored files separately. Archive task-created managed worktrees using Codex's `archive_worktree` tool after preserving needed work. Preserve the primary checkout and other tasks' active checkouts.

## Requirements Engineering

For an explicit request to execute or continue an entire DSS epic, use `$epic-executor` at `D:/DeepSpaceSaga/.agents/skills/epic-executor/SKILL.md`, based on `Board/EpicExecutionPrompt.md`. Do not apply its execution/publication authorization to planning, discussion, skill maintenance or an unrelated single-ticket request.

For creating, refining or decomposing DSS Board epics, user stories and tickets, use `$epic-writer` at `D:/DeepSpaceSaga/.agents/skills/epic-writer/SKILL.md`. Its two references contain the epic/story and story/ticket workflows. Do not recreate the former workspace `Agents` directory.

For a standalone technical assignment, implementation specification, acceptance criteria or requirements analysis without requested Board artifacts, use `$requirements-engineer`. If that work also requests Board materialization, use `$epic-writer` for the Board portion.

For DSS feature specs:

- Read `Documentation/00-Process/CLAUDE.md` and `Documentation/01-Requirements/EngineRequirements.md` first.
- Treat `Documentation/01-Requirements/EngineRequirements.md` as the source of truth.
- Return the feature task as Markdown in the response unless the user explicitly asks to save it.
- Ask one focused requirements question at a time when a design choice is genuinely open.
- Do not edit requirements documents unless the user explicitly asks to persist accepted decisions.

## Documentation curation

`Documentation/` is a curated Markdown-only description of the game, current
contracts and contributor workflows. Follow `DocumentationSystem.md` in this
directory. Do not add test projects, run/review reports, logs, JSON/TRX, dumps,
archives, screenshots or copies of Board tasks here, including in nested folders.
Update the current contract instead of appending conflicting historical summaries.
Store execution history in Board and disposable outputs under workspace artifacts.
