---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0001-carrier-modules
title: Модульный корпус и ангарные места
stage: draft
dependencies: []
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Модульный корпус и ангарные места

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок получает носитель с реальной схемой модулей и видит, чем ограничена вместимость крыла. Установка жилья не увеличивает прочность корпуса. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Ангар S предоставляет 1 место, M — 2; hull footprint проверяется на границы и пересечения. Два M дают ровно 4 места.
- **AC-02**: У носителя есть отдельные HullCurrent/HullMax; жилые модули не увеличивают HullMax. Повреждённые и выключенные ангары не допускают новый вылет.
- **AC-03**: Каталог явно задаёт массу, footprint и вместимость; невалидный content отклоняется до публикации мира.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

none.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Проекция корпуса и вместимости](EP-0029-US-0001-TK-0001-carrier-contract/EP-0029-US-0001-TK-0001-carrier-contract.md) | contracts | none | AC-01, AC-02 |
| [TK-0002 — Схема ангаров и общего корпуса](EP-0029-US-0001-TK-0002-module-schema/EP-0029-US-0001-TK-0002-module-schema.md) | engine | [US-0001-TK-0001](EP-0029-US-0001-TK-0001-carrier-contract/EP-0029-US-0001-TK-0001-carrier-contract.md) | AC-01, AC-02, AC-03 |
| [TK-0003 — Каталог S/M и Tetrarch](EP-0029-US-0001-TK-0003-hangar-content/EP-0029-US-0001-TK-0003-hangar-content.md) | content-data | [US-0001-TK-0002](EP-0029-US-0001-TK-0002-module-schema/EP-0029-US-0001-TK-0002-module-schema.md) | AC-01, AC-02, AC-03 |
| [TK-0004 — Проверка размещения и доступности](EP-0029-US-0001-TK-0004-module-validation/EP-0029-US-0001-TK-0004-module-validation.md) | engine | [US-0001-TK-0003](EP-0029-US-0001-TK-0003-hangar-content/EP-0029-US-0001-TK-0003-hangar-content.md) | AC-01, AC-02, AC-03 |

## Completion evidence

- AC-01: TK-0001/0002/0003/0004 — `Ep0029Us0001Tk0001Tests.CapacityAndIdentityRoundTrip`, `Ep0029Us0001Tk0003Tests.TwoMediumBaysHaveFourPlaces`, `Ep0029Us0001Tk0004Tests.OverlapAndOutsideHullRejected` — PASS.
- AC-02: TK-0001/0002/0003/0004 — `Ep0029Us0001Tk0002Tests.HousingDoesNotChangeHullMax`, `Ep0029Us0001Tk0003Tests.ClassProfileHasIndependentHull`, `Ep0029Us0001Tk0004Tests.IndependentHullProjection` + `OfflineBayKeepsInventoryButBlocksLaunch` — PASS.
- AC-03: TK-0002/0003/0004 — `Ep0029Us0001Tk0002Tests.RejectsInvalidCapacityAndFootprint` + content parse — PASS.
- Artifacts: `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-001..004-*`.
- Implementation/automated: **PASS** (contracts+engine+content, focused tests green; full Engine suite's 613 failures are pre-existing solar-system corpus/config, not EP-0029). Native/product: **OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
