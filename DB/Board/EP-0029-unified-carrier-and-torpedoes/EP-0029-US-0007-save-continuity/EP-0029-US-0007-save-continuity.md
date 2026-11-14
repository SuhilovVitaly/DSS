---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0007-save-continuity
title: Сохранение корабля, потерь и боезапаса
stage: draft
dependencies: [US-0006]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Сохранение корабля, потерь и боезапаса

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок продолжает кампанию с теми же людьми, дронами, повреждениями и остатком торпед. Старые сохранения открываются без бесплатной выдачи нового стартового набора. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Новый Save21 сохраняет carrier state schema1, crew, reservations, wing, ammo, settlement/rearm receipts и destroyed state; round-trip сохраняет значения и ID.
- **AC-02**: Save18/19 сначала проходят существующее legacy очищение, Save20 переводится в Save21; не выдаются ангары, пилоты, деньги и 42 торпеды. Старый мир без крыла остаётся без крыла.
- **AC-03**: Невалидные ссылки, дубли мест/ID, ammo<0 или >capacity, неизвестная версия и недопустимый fingerprint отвергаются до публикации новой сессии.
- **AC-04**: Save запрещён во время активного боя; завершённый outcome сохраняется после release battle. Reload не повторяет settlement или refill.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0006 — Необратимые последствия завершённого боя](../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-battle-consequences.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Версия 21 и безопасная миграция](EP-0029-US-0007-TK-0001-save21-schema/EP-0029-US-0007-TK-0001-save21-schema.md) | engine | [US-0006-TK-0003](../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-TK-0003-post-battle-state/EP-0029-US-0006-TK-0003-post-battle-state.md) | AC-01, AC-02, AC-03 |
| [TK-0002 — Запись и восстановление постоянного состояния](EP-0029-US-0007-TK-0002-carrier-roundtrip/EP-0029-US-0007-TK-0002-carrier-roundtrip.md) | engine | [US-0007-TK-0001](EP-0029-US-0007-TK-0001-save21-schema/EP-0029-US-0007-TK-0001-save21-schema.md) | AC-01, AC-03, AC-04 |
| [TK-0003 — Совместимость нового inventory с каталогом](EP-0029-US-0007-TK-0003-catalog-compatibility/EP-0029-US-0007-TK-0003-catalog-compatibility.md) | engine | [US-0007-TK-0002](EP-0029-US-0007-TK-0002-carrier-roundtrip/EP-0029-US-0007-TK-0002-carrier-roundtrip.md) | AC-02, AC-03 |

## Completion evidence

- AC-01: тикеты TK-0001, TK-0002; named tests и наблюдаемый результат внутри тикетов.
- AC-02: тикеты TK-0001, TK-0003; named tests и наблюдаемый результат внутри тикетов.
- AC-03: тикеты TK-0001, TK-0002, TK-0003; named tests и наблюдаемый результат внутри тикетов.
- AC-04: тикеты TK-0002; named tests и наблюдаемый результат внутри тикетов.
- Future evidence: уникальный run под D:/DeepSpaceSaga/artifacts/validation/ep-0029/; после реализации сюда записать результаты и ссылки.
- Сейчас implementation/automated/native/product: **NOT RUN / OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
