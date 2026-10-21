# Deep Space Saga Documentation

## Current EP-0012 contract — 2026-10-21

Start current combat work at the new point-aim contract and EngineRequirements PointAim section. WriterSave18; EP9/EP10/EP11 sections below are dated implementation/navigation history. Combat and launch forecasts now reuse admitted observed NPC motion. Current implementation/evidence/publication is in Board/EP-0012-combat-rebuild; balance and human acceptance OPEN. Graph rebuild follows final texts in US15TK5. [Point-aim combat contract](04-Engineering/PointAimCombat.md).

## Наблюдаемые будущие линии NPC — 2026-10-18

[Контракт и покрытие EP-0011](04-Engineering/NpcObservedTrajectories.md) · [Board и публикация](../Board/EP-0011-short-fixes/ImplementationStatus.md). NPC future line продолжает подтверждённые два равных секундных поворота; иначе прямая. Combat/launch forecasts сохраняют прежнюю модель. Automated PASS; native/human OPEN, игру не запускали.

## Боевые доклады EP-0010 — 2026-10-16

[ТЗ: панель боевых докладов](02-FirstRelease/TechnicalTasks/TacticalCombatReports.md) · [эпик, истории и тикеты](../Board/EP-0010-combat-reports/EP-0010-combat-reports.md). Реализованы E01–E07,200-card accordion, Save17 continuation, надёжная доставка и8 иллюстраций. [Текущая архитектура](04-Engineering/CombatReports.md) · [проверки и отдельные SHA/push](../Board/EP-0010-combat-reports/ImplementationStatus.md). Native/human/FPS acceptance OPEN. Самостоятельный запуск игры агентом запрещён пользователем.

## Исторический бой EP-0009 — 2026-10-12

[Модульный бой](04-Engineering/ModuleCombat.md) · [канонические требования](01-Requirements/EngineRequirements.md#phase-four-module-combat) · [100 сидов Engine](../Board/EP-0009-battle/evidence/balance.md) · [открытая нативная матрица](../Board/EP-0009-battle/evidence/native.md). Текущий SaveFormatVersion17; EP-0008 и Save16 ниже — историческая запись. Native/human приёмка EP9 остаётся открытой; Graphify повторно перестроен в EP10 (см. ниже).

## Актуализация EP-0008 — 2026-10-12

[Боевой контракт этапа EP-0008](01-Requirements/EngineRequirements.md#phase-three-combat) · [Defeat](02-FirstRelease/Screens/Defeat.md) · [Acceptance и открытые gates](04-Engineering/PhaseThreeCombatAcceptance.md) · [Инвентаризация документов](04-Engineering/PhaseThreeCombatDocumentation.md). Save16, две ПРО PlayerShipOnly, hostile pursuit/fire, ручной пуск и terminal recovery были реализованы на этом этапе. Текущие изменения EP-0009 описаны выше.


## Сверка реализации — 2026-10-10

Обновление после исправлений: F01–F05 закрыты кодом и регрессиями; EP-0004 включён в текущий рабочий набор. Баланс, 80 FPS и человеческая приёмка остаются открытыми. Текущий статус — `Documentation/04-Engineering/EpicReview20261010/Fixes.md`.

Обновлены связанные требования, механики, экраны и отчёты EP-0001–0004. Обзор различает текущую ветку, отдельную поставку EP-0004, подтверждённые дефекты и открытые gates.

[Текущее устройство](04-Engineering/TradingAndSolarSystem.md) · [Ревью, дефекты и остаток](04-Engineering/EpicReview20261010/README.md). Датированные записи ниже сохраняют историческое значение; они не являются новым подтверждением готовности.

This folder is the single home for DSS project documentation.

## Start Here

- [Agent guide](00-Process/AGENTS.md) - rules and project notes for coding agents.
- [Developer guide](00-Process/CLAUDE.md) - build, architecture, and implementation conventions.
- [Documentation system](00-Process/DocumentationSystem.md) - where to put new docs and how to update existing ones.
- [Engine requirements](01-Requirements/EngineRequirements.md) - main requirements checkpoint for the DSS engine.
- [First release requirements](01-Requirements/FirstReleaseRequirements.md) - release scope and links to first-release details.
- [Approach requirements](01-Requirements/EngineRequirements.md#approach-shortest-route) - shortest rendezvous, captured trailing-point fallback, and planner version 3 (2026-09-20).
- [Approach implementation](04-Engineering/ApproachRoutes.md) - solver, numerical tolerances, prediction, and save compatibility.

- [Countermeasure combat](04-Engineering/CountermeasureCombat.md) — исторический EP-0007/0008; текущие правила и Save18 описаны в [PointAimCombat](04-Engineering/PointAimCombat.md).
- [Countermeasure Graphify](06-Tooling/CountermeasureGraphify.md) — корпус, перестройка и проверка графа.

- [AI map environment](04-Engineering/AiMapEnvironment.md) — EP-0004: контракты, конфигурация, исторический Save15 (текущий18), слои и открытая FPS acceptance.
- [Solar-system concept](02-FirstRelease/Mechanics/SolarSystemMapConcept.md) — продуктовый контракт карты.
- [EP-0002 implementation](04-Engineering/EP-0002-Implementation.md) и [EP-0003 evidence](Validation/EP-0003-Implementation.md) — датированные поставки предшественников.

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

- [EP-0004 Graphify](06-Tooling/AiMapGraphify.md) — текущий код, документы, hash manifest и ограничения AST.

Graphify обновлён2026-10-16 в завершающей истории EP10: [corpus, rebuild и ограничения](06-Tooling/TradingSolarGraphify.md). AST охватывает текущий код, семантика документов выборочная; граф служит навигации и не доказывает runtime/native acceptance. Отмена2026-10-12 относится к прежнему запуску.


### Final graph integrity — 2026-10-21

Fresh offline export:792code +311Markdown =1103source SHA256 verified;16038nodes/50778edges/514communities. All21artifact hashes verified;0missing sources/invalid line locations/dangling edges/self-loops. Source canonicalization produced794self-loops, explicitly audited/pruned from derived graph;1112unresolved AST edges and2886reference-only symbols remain explicit limitations. Selective EP12 semantics:21inputs/3host-agent chunks,52nodes/58edges, token usage null/unmeasured. Actual CLI OwnProjectileSnapshot query succeeds (131BFS nodes, explicit700token truncation). Canonical/current Board/tool docs:1324local links and65anchors verified. JSON has full graph; HTML is an aggregated view. Generated audit/status/navigation documents are excluded from graph input hashes to avoid circular results; graph navigation does not establish runtime or human acceptance.
