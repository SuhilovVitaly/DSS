---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0015-project-documentation"
ticket: "EP-0012-US-0015-TK-0005-refresh-graph-and-links"
title: "refresh-graph-and-links"
stage: "implemented"
layer: "tooling"
depends_on: ["EP-0012-US-0015-TK-0004-update-documents-4"]
files_touched: 58
serves: ["US15-AC2"]
created: "2026-10-20T07:48:46.640Z"
revision: 2
---

# EP-0012-US-0015-TK-0005-refresh-graph-and-links

## Why

[Документация (обязательная завершающая история)](../EP-0012-US-0015-project-documentation.md) · [Контракт D/A/G](../../Documentation.md) · [Execution DAG](../../Tickets.md). Результат: refresh-graph-and-links; доказать US15-AC2 в границах этой карточки.

## Decisions / Assumptions

D-USER-01: внешние файлы игнорируются. D-USER-02: UI feedback<=100мс, authoritative projectile на scene clock. Правила профиль/радиус/урон/разброс берутся из Documentation и story AC. A01–A06/A-PLAN — assumptions, не дополнительное согласование. Proposed файлы/API ещё не существуют.

## Code context

Точный allowlist (58 файлов). Current baseline `60418af19fdf2634f4d7bb27ecb2d075c28882b3`. Scope уточнён до редактирования по EpicExecutionPrompt§6: актуальные контракты, историческая точность и все связанные документы. Evidence/Board metadata являются обязательным результатом.

| File | Current state | Allowed change |
|---|---|---|
| `Board/EP-0012-combat-rebuild/Documentation.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-contracts-and-schema.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-combat-catalog-v3.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-point-aim-torpedoes.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0004-point-aim-countermissiles/EP-0012-US-0004-point-aim-countermissiles.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0005-snapshot-privacy/EP-0012-US-0005-snapshot-privacy.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0006-threats-and-evasion/EP-0012-US-0006-threats-and-evasion.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0007-mukha-ai-v3/EP-0012-US-0007-mukha-ai-v3.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0008-encounter-and-time/EP-0012-US-0008-encounter-and-time.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-scenarios-and-saves.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0010-tactical-map/EP-0012-US-0010-tactical-map.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-aiming-and-launch.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0012-maneuver-and-threat-ui/EP-0012-US-0012-maneuver-and-threat-ui.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-combat-state-and-feedback.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0014-balance-and-acceptance/EP-0012-US-0014-balance-and-acceptance.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0015-project-documentation/EP-0012-US-0015-project-documentation.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0016-combat-localization/EP-0012-US-0016-combat-localization.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-event-and-command-integration.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-legacy-combat-cleanup.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-scenario-and-client-regressions.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/EP-0012-combat-rebuild.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/ImplementationStatus.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/PublicationStatus.md` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/Tickets.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/evidence/documentation-inventory.json` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/evidence/final-validation.json` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Board/EP-0012-combat-rebuild/evidence/us0015-tk0005-results.json` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/01-Requirements/EngineRequirements.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/02-FirstRelease/Mechanics/CrewAndHabitation.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/02-FirstRelease/Mechanics/TetrarchClass.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/02-FirstRelease/Screens/ScenarioSelect.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/04-Engineering/PointAimCombat.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/04-Engineering/PointCombatDocumentation.md` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/06-Tooling/CountermeasureGraphify.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/06-Tooling/TradingSolarGraphify.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `Documentation/README.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/.graphify_detect.json` | NEW | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/.graphify_labels.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/.graphify_python` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/.graphify_root` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/.graphify_semantic_new.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/GRAPH_REPORT.md` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/community-cohesion.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/cost.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/counts.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/diagnostics.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/ep0012-semantic.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/extraction.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/graph.html` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/graph.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/input-evidence.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/manifest.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/pruned-unresolved-edges.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/rebuild.py` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/semantic-coverage.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/source-manifest.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/unlocated-references.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |
| `src/graphify-out/verification.json` | Exists | Current EP12 contract, supersession or bounded evidence; preserve dated results. |

## Public API after the change

No public API change; использовать контракт зависимых тикетов.

## Implementation steps

1. Проверить depends_on, baseline и конкретные consumer references. Если отсутствует зависимый API или требуется файл вне allowlist — не обходить зависимость и не закрывать тикет; конкретизировать bounded Board scope.
2. После документации обновить существующий Graphify workflow в отдельной bounded операции, проверить локальные Markdown ссылки и результаты; граф — навигация, не runtime evidence. При отсутствии инструмента явно NOT RUN, без самостоятельной установки зависимостей.
3. Добавить/обновить named checks ниже одновременно с реализацией. Удаляемые legacy tests заменять проверками нового контракта, сохраняющиеся инварианты не выкидывать.
4. Записать фактические команды/результаты при исполнении; сейчас всё NOT RUN.

## Out of scope / Invariants

Один production layer: tooling; matching tests только указанного проекта. Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Не переносить внешний симулятор, не менять CombatSim, не включать concealed enemy point/deadline/modules в UI/forecast/events. Не исполнять другие тикеты под видом consumer fixes.

## Tests

Actual validation is recorded in machine evidence; historical planning checklist below states required gates.

- **Graph_and_links_have_current_paths**: записи не ссылаются на удалённый CombatShmel и удалённые API как на текущие.

Документарный/ручной gate: проверить перечисленные критерии и локальные ссылки; `git diff --check`. Product build не требуется для evidence-only изменений. Native выполняется только при отдельном разрешении на игру; иначе NOT RUN с причиной. Graphify output проверяется при фактическом rebuild.

## Definition of Done

- Served AC US15-AC2 доказаны указанными named checks; результаты и baseline записаны честно.
- Diff ограничен allowlist, нет незавершённого stub/mock или скрытого следующего слоя.
- Owning build, targeted tests, scoped format и diff check выполнены там, где меняется код; known baseline failures отделены.
- Межслойный breaking API не объявлен интегрированным до consumer migration и общего build/test gate.
- Native/product/balance gates не закрыты зелёными unit tests; никакого commit/push/игры без соответствующего поручения.

## Status / Evidence

2026-10-21: tooling implementation, selective semantic extraction, project-wide documentation inventory, Board synchronization and publication ledger complete. Final graph export/hash/location/edge validation and exact link/case results are recorded in [machine evidence](../../evidence/us0015-tk0005-results.json) and [final audit](../../evidence/final-validation.json). Graph is navigation only; token usage null/unmeasured. Human/product/balance OPEN. Exact containing SHA is verified after separate commit/push, never prewritten.
