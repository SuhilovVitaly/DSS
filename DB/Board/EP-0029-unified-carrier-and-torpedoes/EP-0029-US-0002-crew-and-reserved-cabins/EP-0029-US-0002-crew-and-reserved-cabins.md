---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0002-crew-and-reserved-cabins
title: Шесть членов экипажа и два зарезервированных места
stage: draft
dependencies: [US-0001]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Шесть членов экипажа и два зарезервированных места

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок узнаёт тех же четырёх женщин-пилотов на корабле и в бою. Два свободных места обозначены как резерв для пленных и не заняты фиктивными персонажами. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Экипаж содержит капитана, Ирину Вольскую и Evelyn Ward / Audrey Bennett / Claire Morgan / Rachel Hayes с выбранными портретами; все четыре пилота female.
- **AC-02**: Два модуля по 4 места дают 8 мест: 6 заняты, 2 зарезервированы, 0 свободны для обычного назначения. Пленных и механики захвата нет.
- **AC-03**: Имя, пол, портрет, роль и навыки имеют один источник; pilot и crew identity не расходятся при Load.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0001 — Модульный корпус и ангарные места](../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-carrier-modules.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Общий контракт персонажа и жилья](EP-0029-US-0002-TK-0001-crew-contract/EP-0029-US-0002-TK-0001-crew-contract.md) | contracts | [US-0001-TK-0004](../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-TK-0004-module-validation/EP-0029-US-0001-TK-0004-module-validation.md) | AC-01, AC-02, AC-03 |
| [TK-0002 — Единая запись экипажа и назначения кают](EP-0029-US-0002-TK-0002-crew-runtime/EP-0029-US-0002-TK-0002-crew-runtime.md) | engine | [US-0002-TK-0001](EP-0029-US-0002-TK-0001-crew-contract/EP-0029-US-0002-TK-0001-crew-contract.md) | AC-01, AC-02, AC-03 |
| [TK-0003 — Имена и портреты стартовой четвёрки](EP-0029-US-0002-TK-0003-pilot-catalog/EP-0029-US-0002-TK-0003-pilot-catalog.md) | content-data | [US-0002-TK-0002](EP-0029-US-0002-TK-0002-crew-runtime/EP-0029-US-0002-TK-0002-crew-runtime.md) | AC-01 |

## Completion evidence

- AC-01: TK-0001/0002/0003 — `Ep0029Us0002Tk0001Tests.CrewIdentityRoundTrip`, `Ep0029Us0002Tk0003Tests.FourPilotsFemaleAndAssetsExist` + `SkillsUnchangedUnderNewNames` — PASS.
- AC-02: TK-0001/0002 — `Ep0029Us0002Tk0001Tests.ReservationIsNotOccupant`, `Ep0029Us0002Tk0002Tests.EightCabinsSixPeopleTwoReserved` — PASS (8 total / 6 occupied / 2 reserved / 0 free).
- AC-03: TK-0001/0002 — `CrewIdentityRoundTrip` (single source, CrewId stable) + `HousingDamagePreservesCrew` (all 6 IDs survive) — PASS.
- Artifacts: `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-005..007-*`.
- Implementation/automated: **PASS**. Native/product: **OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
