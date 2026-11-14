---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0010-tactical-presentation
title: Корабль, пилоты и боезапас на экране боя
stage: draft
dependencies: [US-0005, US-0006, US-0009]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Корабль, пилоты и боезапас на экране боя

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок узнаёт свой корабль и экипаж в тактическом бою. Карточки показывают текущий боезапас, журнал объясняет пропуски, итог показывает действительные потери. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Носитель использует world appearance/class/name; pilot cards отображают общие crew names/gender/portraits без старых Юна/Олега.
- **AC-02**: Карточка каждого дрона показывает current/capacity; нулевой боезапас запрещает выбор цели, но оставляет E/D при их обычных условиях.
- **AC-03**: Во время replay ammo/HP читаются из текущего FireStep, не из финального состояния; перемотка не вызывает расход.
- **AC-04**: Итог различает lost drones, disabled survivors, ship destruction, aborted и stalemate; UI не обещает бесплатный ремонт.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0005 — Бой из реального корабля и его крыла](../EP-0029-US-0005-world-owned-sortie/EP-0029-US-0005-world-owned-sortie.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0006 — Необратимые последствия завершённого боя](../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-battle-consequences.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0009 — Экран корабля, ангаров и экипажа](../EP-0029-US-0009-ship-management-ui/EP-0029-US-0009-ship-management-ui.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Портреты и имена из общей записи экипажа](EP-0029-US-0010-TK-0001-pilot-identity/EP-0029-US-0010-TK-0001-pilot-identity.md) | client | [US-0005-TK-0003](../EP-0029-US-0005-world-owned-sortie/EP-0029-US-0005-TK-0003-owned-entry/EP-0029-US-0005-TK-0003-owned-entry.md), [US-0006-TK-0003](../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-TK-0003-post-battle-state/EP-0029-US-0006-TK-0003-post-battle-state.md), [US-0009-TK-0004](../EP-0029-US-0009-ship-management-ui/EP-0029-US-0009-TK-0004-ship-localization/EP-0029-US-0009-TK-0004-ship-localization.md) | AC-01 |
| [TK-0002 — Одинаковый облик носителя в мире и бою](EP-0029-US-0010-TK-0002-carrier-appearance/EP-0029-US-0010-TK-0002-carrier-appearance.md) | client | [US-0010-TK-0001](EP-0029-US-0010-TK-0001-pilot-identity/EP-0029-US-0010-TK-0001-pilot-identity.md) | AC-01 |
| [TK-0003 — Боезапас в карточках и приказах](EP-0029-US-0010-TK-0003-ammo-controls/EP-0029-US-0010-TK-0003-ammo-controls.md) | client | [US-0010-TK-0002](EP-0029-US-0010-TK-0002-carrier-appearance/EP-0029-US-0010-TK-0002-carrier-appearance.md) | AC-02, AC-03 |
| [TK-0004 — Отчёт о потерях и причины пропуска](EP-0029-US-0010-TK-0004-outcome-and-log/EP-0029-US-0010-TK-0004-outcome-and-log.md) | client | [US-0010-TK-0003](EP-0029-US-0010-TK-0003-ammo-controls/EP-0029-US-0010-TK-0003-ammo-controls.md) | AC-04 |
| [TK-0005 — Локализация торпед и последствий](EP-0029-US-0010-TK-0005-battle-locale/EP-0029-US-0010-TK-0005-battle-locale.md) | content-data | [US-0010-TK-0004](EP-0029-US-0010-TK-0004-outcome-and-log/EP-0029-US-0010-TK-0004-outcome-and-log.md) | AC-01, AC-02, AC-04 |

## Completion evidence

- AC-01: тикеты TK-0001, TK-0002, TK-0005; named tests и наблюдаемый результат внутри тикетов.
- AC-02: тикеты TK-0003, TK-0005; named tests и наблюдаемый результат внутри тикетов.
- AC-03: тикеты TK-0003; named tests и наблюдаемый результат внутри тикетов.
- AC-04: тикеты TK-0004, TK-0005; named tests и наблюдаемый результат внутри тикетов.
- Future evidence: уникальный run под D:/DeepSpaceSaga/artifacts/validation/ep-0029/; после реализации сюда записать результаты и ссылки.
- Сейчас implementation/automated/native/product: **NOT RUN / OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
