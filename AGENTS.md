# AGENTS.md

## Requirements Engineering

When the user asks to turn a feature idea into a technical assignment, implementation spec, acceptance criteria, AI-ready feature task, PRD-to-implementation plan, or DSS requirements planning artifact, use `$requirements-engineer`.

For DSS feature specs:

- Read `CLAUDE.md` and `deep_space_saga_engine_requirements.md` first.
- Treat `deep_space_saga_engine_requirements.md` as the source of truth.
- Return the feature task as Markdown in the response unless the user explicitly asks to save it.
- Ask one focused requirements question at a time when a design choice is genuinely open.
- Do not edit requirements documents unless the user explicitly asks to persist accepted decisions.
