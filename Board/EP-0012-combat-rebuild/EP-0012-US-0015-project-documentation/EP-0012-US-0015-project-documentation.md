---
epic: "EP-0012-combat-rebuild"
story: "EP-0012-US-0015-project-documentation"
title: "Документация (обязательная завершающая история)"
stage: "draft"
dependencies: ["EP-0012-US-0001-contracts-and-schema", "EP-0012-US-0002-combat-catalog-v3", "EP-0012-US-0003-point-aim-torpedoes", "EP-0012-US-0004-point-aim-countermissiles", "EP-0012-US-0005-snapshot-privacy", "EP-0012-US-0006-threats-and-evasion", "EP-0012-US-0007-mukha-ai-v3", "EP-0012-US-0008-encounter-and-time", "EP-0012-US-0009-scenarios-and-saves", "EP-0012-US-0010-tactical-map", "EP-0012-US-0011-aiming-and-launch", "EP-0012-US-0012-maneuver-and-threat-ui", "EP-0012-US-0013-combat-state-and-feedback", "EP-0012-US-0014-balance-and-acceptance", "EP-0012-US-0016-combat-localization", "EP-0012-US-0017-event-and-command-integration", "EP-0012-US-0018-legacy-combat-cleanup", "EP-0012-US-0019-scenario-and-client-regressions"]
created: "2026-10-20T07:48:46.640Z"
source_request: "Приложенное ТЗ EP-0012 revision2; внешние ссылки исключены"
current_review: "planning-structural-checked"
revision: 1
---

# US-0015 — Документация (обязательная завершающая история)

## Входное техническое задание

[Эпик](../EP-0012-combat-rebuild.md) · [D/A/G и полный контракт](../Documentation.md) · [Межисторийный ticket DAG](../Tickets.md)


**Acceptance criteria**
- **US15-AC1:** Обновлены документы проекта:
  - `Documentation/04-Engineering/{BasicTorpedoCombat, CountermeasureCombat, ModuleCombat, ModuleCombatDocumentation, PhaseThreeCombatAcceptance, CombatReports}.md` — или вместо них новый `PointAimCombat.md` со ссылками из старых;
  - `Documentation/02-FirstRelease/TechnicalTasks/{CombatTrajectoryForecasting, TacticalCombatReports}.md`;
  - `Documentation/02-FirstRelease/Mechanics/{TacticalMapAndManeuvering, TetrarchClass}.md`;
  - `Documentation/03-Design/TacticalMapSpecification.md`;
  - `Documentation/01-Requirements/EngineRequirements.md`;
  - `Documentation/00-Process/CLAUDE.md` (новые данные);
  - `Board/EP-0009-battle/ImplementationStatus.md`: раздел «Superseded by EP-0012».
- **US15-AC2:** Граф (Graphify) перестроен, ссылки проверены.



## Non-goals / Invariants

Engine владеет состоянием, командами, AI, RNG и исходами. Client использует только публичную границу и уже отображаемые подтверждённые данные. Пауза не продвигает физику/AI/reload; часы real/calendar/motion различаются. Save/Load атомарен; CommandId идемпотентен; ties — ordinal ID. Во время планирования код, игра, commit/push не выполняются. Scope этой истории ограничен её AC и allowlists; соседние истории не выполняются неявно. D-USER-02 имеет приоритет над исходным<=100ms projectile требованием.

## Dependencies

Execution order задаётся ticket DAG, который допускает чередование слоёв между историями. Stories — тематические контейнеры, не атомарные build units; metadata dependencies перечисляет полностью завершённые истории до начала этой; обязательные межслойные depends_on отдельных тикетов дополняют этот список. Входящие prerequisite stories: EP-0012-US-0014-balance-and-acceptance. Нативные открытые gates EP11 не объявляются закрытыми EP12.

## Ticket map

| Order / Ticket | Layer | Depends on | Serves | Files |
|---|---|---|---|---:|
| 69 / [EP-0012-US-0015-TK-0001-update-documents-1](EP-0012-US-0015-TK-0001-update-documents-1/EP-0012-US-0015-TK-0001-update-documents-1.md) | documentation | EP-0012-US-0014-TK-0003-native-acceptance | US15-AC1 | 5 |
| 70 / [EP-0012-US-0015-TK-0002-update-documents-2](EP-0012-US-0015-TK-0002-update-documents-2/EP-0012-US-0015-TK-0002-update-documents-2.md) | documentation | EP-0012-US-0015-TK-0001-update-documents-1 | US15-AC1 | 4 |
| 71 / [EP-0012-US-0015-TK-0003-update-documents-3](EP-0012-US-0015-TK-0003-update-documents-3/EP-0012-US-0015-TK-0003-update-documents-3.md) | documentation | EP-0012-US-0015-TK-0002-update-documents-2 | US15-AC1 | 3 |
| 72 / [EP-0012-US-0015-TK-0004-update-documents-4](EP-0012-US-0015-TK-0004-update-documents-4/EP-0012-US-0015-TK-0004-update-documents-4.md) | documentation | EP-0012-US-0015-TK-0003-update-documents-3 | US15-AC1 | 3 |
| 73 / [EP-0012-US-0015-TK-0005-refresh-graph-and-links](EP-0012-US-0015-TK-0005-refresh-graph-and-links/EP-0012-US-0015-TK-0005-refresh-graph-and-links.md) | tooling | EP-0012-US-0015-TK-0004-update-documents-4 | US15-AC2 | 3 |

## Decisions / Assumptions

D01–D18/O1–O12 входного документа и D-USER-01/02, A01–A06 и A-PLAN-01–12 из Documentation обязательны в указанном статусе. Новые типы, partial и tests — proposed.

## Gaps and backlog

G-01–G-06 из Documentation. Все missing consumer references блокируют закрытие соответствующего тикета, без неограниченных проходов. Native/product проверяются отдельно; planning не даёт их evidence.

## Decision / review log

2026-10-20T07:48:46.640Z: создано по поручению пользователя. Разделение слоёв/<=5 файлов — техническая декомпозиция. Stage draft, implementation NOT STARTED, проверки кода не запускались.
