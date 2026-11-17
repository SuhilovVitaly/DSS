---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0011-bounty-map-area
title: Район цели bounty на звёздной карте
stage: approved
plan_status: executed
approval_kind: workflow
current_review: implementation-review
revision: 1
created: 2026-11-17T19:41:16Z
priority: P1
workstream: feature
dependencies: [EP-0036-US-0001-target-bounty]
implementation_status: implemented
planning_status: decomposed
source_request: "прочитай нарезанные тикеты эпика и реши что мы забыли и что нужно добавить"
---

# Район цели bounty на звёздной карте

## Current execution status

Implementation is complete under the explicit owner execution request. Planning-only authorizations and NOT STARTED statements below are dated planning history, superseded by this execution. Automated evidence, independent reviews and remaining native/product gates: [ExecutionStatus](../ExecutionStatus.md). Native/game execution NOT RUN — owner only.


## Связь с техническим заданием

[Эпик и решение D-36-36](../Documentation.md). В US1 Tracking только включает заказ в журнал. Район поиска виден лишь как текст на доске, и игроку негде увидеть, куда лететь. Пользователь выбрал: «Да, район поиска».

## User story

Как игрок, я отслеживаю bounty и вижу на звёздной карте район, где патрулирует цель, с подписью цели и награды. Район не раскрывает текущего положения необнаруженной группы.

## Grounding

- `src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs:54–60` — группа размещается в `definition.Region` (X, Y, Radius) с квадратным патрулём `PatrolRadius`.
- `src/DeepSpaceSaga.Contracts/PirateGroupsContracts.cs:7` — PirateGroupSnapshot с текущей позицией и IsDetected (fog of war).
- `src/DeepSpaceSaga.Client/UI/Demo/PirateGroupsPresentation.cs`, `GameSessionScreen.cs:1322` — слой маркеров групп на карте.
- US1 TK1 проектирует `BountyOrder(..., string? SearchRegion, ...)` без геометрии.

## Acceptance criteria

- **AC-01:** Для каждого bounty с Tracking=true и открытой целью на звёздной карте показан район поиска (центр и радиус по content-региону группы, покрывающему её патруль) с подписью цели и награды.
- **AC-02:** Район статичен и одинаков для обнаруженной и необнаруженной группы; позицию группы показывает только существующий маркер EP-0035.
- **AC-03:** Tracking=false, Claimable, Paid и Unavailable не показывают район; переключение Tracking отражается после подтверждённого snapshot.
- **AC-04:** Save/Load и повторное открытие дают тот же район; подписи RU/EN.

## Approved ticket map

Planning map, не human approval implementation:

| ID | Layer | Dependencies | Criteria |
|---|---|---|---|
| [EP-0036-US-0011-TK-0001-search-area-projection](EP-0036-US-0011-TK-0001-search-area-projection/EP-0036-US-0011-TK-0001-search-area-projection.md) | engine | EP-0036-US-0001-TK-0002-engine | AC-01…04 |
| [EP-0036-US-0011-TK-0002-starmap-area](EP-0036-US-0011-TK-0002-starmap-area/EP-0036-US-0011-TK-0002-starmap-area.md) | client | EP-0036-US-0011-TK-0001-search-area-projection, EP-0036-US-0001-TK-0003-client | AC-01…04 |

## Non-goals

Автопилот к району, точная позиция цели, новые сенсорные механики, маркеры для неотслеживаемых заказов.

## Gaps and backlog

Формула радиуса (Region.Radius + PatrolRadius·√2) — технический baseline; визуальную читаемость проверяет плейтест. Native/product acceptance OPEN.

Planning DECOMPOSED / DRAFT; implementation NOT STARTED; automated/native/product NOT RUN.
