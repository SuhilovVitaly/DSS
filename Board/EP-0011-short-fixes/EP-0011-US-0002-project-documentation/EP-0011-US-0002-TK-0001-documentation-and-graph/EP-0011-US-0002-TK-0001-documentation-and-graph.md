---
epic: EP-0011-short-fixes
story: EP-0011-US-0002-project-documentation
ticket: EP-0011-US-0002-TK-0001-documentation-and-graph
stage: implemented
depends_on: [EP-0011-US-0001-observed-npc-trajectories-TK-0003-regression-coverage]
---

# Документация и Graphify

Выполнить последним: инвентаризировать влияние итогового diff, обновить релевантные Documentation, README и Board, проверить ссылки, перестроить src/graphify-out существующим rebuild.py, проверить целостность и ограничения графа. Записать актуальные automated результаты и отдельный открытый native gate. Не переписывать датированные отчёты. Scope: релевантная документация всего проекта, Board EP-0011, src/graphify-out. Проверки: ссылки/ID/пути, graph verification, git diff --check, публикация SHA.

## Scope revision before documentation work — 2026-10-18

Current request authorizes required project-wide documentation synchronization, including EngineRequirements clarification of the newly implemented NPC future-line exception (no mechanics/Engine changes). Actual scope: Documentation/04-Engineering/NpcObservedTrajectories.md (new), Documentation README, developer guide, EngineRequirements §22, tactical design/mechanics/screen, related CombatTrajectoryForecasting specification, Graphify tooling page, Board EP-0011, XML summaries in FutureTrajectoryProjector.cs, src/graphify-out/rebuild.py and generated graph artifacts. Rebuild corpus adds this epic and excludes its changing execution report/evidence to avoid circular hashes. Root README.md absent; root AGENTS.md/CLAUDE.md are navigation aliases and need no edit. Historical prior-epic evidence retained.

## Execution — 2026-10-18

Документация синхронизирована с итоговой реализацией; перечень влияния и результаты ссылок/Graphify находятся в [execution report](../../ImplementationStatus.md); self-review не независимая приёмка. Native/human OPEN.
