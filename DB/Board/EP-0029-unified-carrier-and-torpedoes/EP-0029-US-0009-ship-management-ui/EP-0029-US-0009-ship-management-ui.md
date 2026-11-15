---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0009-ship-management-ui
title: Экран корабля, ангаров и экипажа
stage: draft
dependencies: [US-0008]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Экран корабля, ангаров и экипажа

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок открывает корабль и видит реальный корпус, два ангара, четыре пилота, два жилых модуля и торпеды в трюме. Перезарядка выполняется на этом экране с понятным результатом. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Ship screen показывает название/class/hull, installed modules, bay slots, drone builds/state/ammo, crew portraits и жильё 6/8 с reserved2; нет заглушек вместо доступных данных.
- **AC-02**: Кнопка перезарядки работает для выбранного дрона, показывает максимум и количество, блокируется при pending и объясняет отказ; keyboard/mouse совпадают.
- **AC-03**: RU/EN, узкое/полное окно и scaled UI не обрезают имена и запасы; glyph/portrait fallback не меняет identity.
- **AC-04**: 2 места зарезервированы, список пленных пуст; нет кнопки захвата/выкупа/найма пленных.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0008 — Перезарядка дронов из грузового трюма](../EP-0029-US-0008-rearm-between-battles/EP-0029-US-0008-rearm-between-battles.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Модель экрана корабля](EP-0029-US-0009-TK-0001-ship-read-model/EP-0029-US-0009-TK-0001-ship-read-model.md) | client | [US-0008-TK-0004](../EP-0029-US-0008-rearm-between-battles/EP-0029-US-0008-TK-0004-rearm-client-handle/EP-0029-US-0008-TK-0004-rearm-client-handle.md) | AC-01, AC-04 |
| [TK-0002 — Панели модулей, крыла и людей](EP-0029-US-0009-TK-0002-ship-layout/EP-0029-US-0009-TK-0002-ship-layout.md) | client | [US-0009-TK-0001](EP-0029-US-0009-TK-0001-ship-read-model/EP-0029-US-0009-TK-0001-ship-read-model.md) | AC-01, AC-03, AC-04 |
| [TK-0003 — Выбор количества и подтверждение перезарядки](EP-0029-US-0009-TK-0003-rearm-controls/EP-0029-US-0009-TK-0003-rearm-controls.md) | client | [US-0009-TK-0002](EP-0029-US-0009-TK-0002-ship-layout/EP-0029-US-0009-TK-0002-ship-layout.md) | AC-02 |
| [TK-0004 — Тексты корабля и резервных мест](EP-0029-US-0009-TK-0004-ship-localization/EP-0029-US-0009-TK-0004-ship-localization.md) | content-data | [US-0009-TK-0003](EP-0029-US-0009-TK-0003-rearm-controls/EP-0029-US-0009-TK-0003-rearm-controls.md) | AC-02, AC-03, AC-04 |

## Completion evidence

- AC-01: TK-0001/0002 — `ShipViewUsesSnapshotNotDefaults`, `FourPortraitsAndOfficerAreDistinct` — PASS.
- AC-02: TK-0003/0004 — `DoubleClickSendsOneRequest`, `RearmFailureKeysExist` — PASS.
- AC-03: TK-0002/0004 — `LayoutAt1280And1920KeepsControlsInside`, `ShipLocaleParity` — PASS.
- AC-04: TK-0001/0002/0004 — `LegacyShipShowsNoWing`, `DestroyedDroneNotShownReady`, `ReservedTextNeverClaimsCaptives` — PASS.
- Artifacts: `D:/DeepSpaceSaga/artifacts/validation/ep-0029/run-029..032-*`.
- Implementation/automated: **PASS**. Native/product: **OPEN** (hull diagram, module list, реальные PNG-портреты).

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
