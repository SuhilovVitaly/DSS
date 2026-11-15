---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0005-world-owned-sortie
title: Бой из реального корабля и его крыла
stage: draft
dependencies: [US-0001, US-0002, US-0003, US-0004]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Бой из реального корабля и его крыла

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Учебный бой и обычный вход с карты используют того же носителя и принадлежащих ему дронов. Сценарий столкновения задаёт противника и расстановку, но не выдаёт игроку новое имущество. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: SourceShipId в бою совпадает с PlayerShipObjectId; названия, HullCurrent/Max, pilot/DroneId и оснащение происходят из мира.
- **AC-02**: Запуск включает только пригодные дроны с валидным пилотом и работающим ангаром. Нет крыла/все повреждены — понятный отказ без замены сценарными дронами.
- **AC-03**: В игровом campaign entry нельзя подменить owned loadout или переключиться в бесплатный sandbox. Диагностический сценарный путь остаётся отдельным внутренним/test harness.
- **AC-04**: Бой приостанавливает мир, блокирует изменение крыла и грузовых запасов; повтор start и stale world revision не создают вторую сессию.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0001 — Модульный корпус и ангарные места](../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-carrier-modules.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0002 — Шесть членов экипажа и два зарезервированных места](../EP-0029-US-0002-crew-and-reserved-cabins/EP-0029-US-0002-crew-and-reserved-cabins.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0003 — Постоянное крыло корабля](../EP-0029-US-0003-persistent-wing/EP-0029-US-0003-persistent-wing.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0004 — Конечный боезапас в пошаговом бою](../EP-0029-US-0004-finite-torpedoes/EP-0029-US-0004-finite-torpedoes.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Запрос вылета и identity мира](EP-0029-US-0005-TK-0001-sortie-request/EP-0029-US-0005-TK-0001-sortie-request.md) | contracts | [US-0001-TK-0004](../EP-0029-US-0001-carrier-modules/EP-0029-US-0001-TK-0004-module-validation/EP-0029-US-0001-TK-0004-module-validation.md), [US-0002-TK-0003](../EP-0029-US-0002-crew-and-reserved-cabins/EP-0029-US-0002-TK-0003-pilot-catalog/EP-0029-US-0002-TK-0003-pilot-catalog.md), [US-0003-TK-0003](../EP-0029-US-0003-persistent-wing/EP-0029-US-0003-TK-0003-wing-mass/EP-0029-US-0003-TK-0003-wing-mass.md), [US-0004-TK-0005](../EP-0029-US-0004-finite-torpedoes/EP-0029-US-0004-TK-0005-ammo-exhaustion/EP-0029-US-0004-TK-0005-ammo-exhaustion.md) | AC-01, AC-04 |
| [TK-0002 — Адаптер постоянного крыла в бой](EP-0029-US-0005-TK-0002-owned-assembly/EP-0029-US-0005-TK-0002-owned-assembly.md) | engine | [US-0005-TK-0001](EP-0029-US-0005-TK-0001-sortie-request/EP-0029-US-0005-TK-0001-sortie-request.md) | AC-01, AC-02 |
| [TK-0003 — Авторитетная проверка входа](EP-0029-US-0005-TK-0003-owned-entry/EP-0029-US-0005-TK-0003-owned-entry.md) | engine | [US-0005-TK-0002](EP-0029-US-0005-TK-0002-owned-assembly/EP-0029-US-0005-TK-0002-owned-assembly.md) | AC-02, AC-03, AC-04 |

## Completion evidence

- AC-01: TK-0001/0002 — `OwnedStartSerializesIdentity`, `TwoSurvivorsStartAsTwo` (same carrier/wing on the map and in battle) — PASS.
- AC-02: TK-0002/0003 — `DamagedShipAndAmmoPersistIntoBattle`, `OwnedLoadoutOverrideRejects` (scenario provides enemy only, no free assets) — PASS.
- AC-03: TK-0003 — `WrongShipAndStaleRevisionReject`, `NoReadyDronesNoFallback` — PASS.
- AC-04: TK-0001/0003 — `OldRequestsStillDeserialize`, `DuplicateStartReturnsReceipt` — PASS.
- Artifacts: `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-016..018-*`.
- Implementation/automated: **PASS**. Native/product: **OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
