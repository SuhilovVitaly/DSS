# AGENTS.md

## Image Assets

- Save all generated or edited project images under `DSS-Images\temp`.
- Do not use other `DSS-Images` subfolders for new image outputs unless the user explicitly asks for a different destination.
- Exception requested by the user: the modular female portrait pack is stored and maintained in `src/DeepSpaceSaga.Client/Images/Persons/W/PortraitGenerator`. Its `Sources`, `Generated`, and `Golden` folders are authoring/test data and must not ship with the game.
- The new complete-face concept is stored in `src/DeepSpaceSaga.Client/Images/Persons/W1` with `Ovals`, `Faces`, and `Hair` assets. Maintain it there, preserve the old W pack, and exclude W1 `Sources` and `Generated` from shipping.
- W2 uses `src/DeepSpaceSaga.Client/Images/Persons/W2`: whole `Heads`, one `Neck`, `Clothes`, and new `Hair`. Preserve W and W1. Exclude W2 `Sources` and `Generated` from shipping.
- W4 uses `src/DeepSpaceSaga.Client/Images/Persons/W4`: three complete `Portraits` (head, neck and hair in one image) and shared `Clothes`. Preserve earlier packs; exclude `Sources` and `Generated` from shipping.
- M4 is the analogous male pack in `src/DeepSpaceSaga.Client/Images/Persons/M4`, with complete `Portraits` and compatible shared `Clothes`. Save accepted male PNGs directly in M4/Portraits; do not create M4 Sources or Generated. The shared Character Generator skill supports both W4 and M4.
- The Character Generator skill is maintained in `W4/Portraits/character-generator`. Its runs save only accepted final PNGs in W4/Portraits for women or M4/Portraits for men; do not create Sources or Generated in either pack. Use temporary previews outside the project and remove them after validation.

## Requirements Engineering

When the user asks to turn a feature idea into a technical assignment, implementation spec, acceptance criteria, AI-ready feature task, PRD-to-implementation plan, or DSS requirements planning artifact, use `$requirements-engineer`.

For DSS feature specs:

- Read `CLAUDE.md` and `deep_space_saga_engine_requirements.md` first.
- Treat `deep_space_saga_engine_requirements.md` as the source of truth.
- Return the feature task as Markdown in the response unless the user explicitly asks to save it.
- Ask one focused requirements question at a time when a design choice is genuinely open.
- Do not edit requirements documents unless the user explicitly asks to persist accepted decisions.
