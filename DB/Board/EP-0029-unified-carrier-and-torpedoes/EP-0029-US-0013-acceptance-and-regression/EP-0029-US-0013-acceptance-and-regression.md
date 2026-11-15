---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0013-acceptance-and-regression
title: Сквозная приёмка постоянного носителя
stage: draft
dependencies: [US-0012]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Сквозная приёмка постоянного носителя

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок может пройти цепочку обучение → карта → корабль → перезарядка → второй бой → сохранение/загрузка без потери identity и без выдачи ресурсов. Доказательства отдельно подтверждают расчёты, UI и продуктовый баланс. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Сквозная цепочка сохраняет один shipId, четыре crew pilot IDs, реальные потери и conservation торпед с учётом уничтоженных магазинов.
- **AC-02**: Покрыты победа, потеря части/всего крыла, гибель носителя, abort, no-ammo stalemate, повторные команды, повреждённые saves и disk failure.
- **AC-03**: Tutorial seed1 проверен с Enter-only и выполнением подсказок; нет подмены исходов/бессмертия для достижения победы. Изменение балансного исхода видно в отчёте.
- **AC-04**: Native RU/EN проверки на меньшем мониторе подтверждают имена, portraits, ammo, rearm input, переходы и размеры; headless не считается native или owner acceptance.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0012 — Реальный учебный запуск и надёжное завершение](../EP-0029-US-0012-campaign-lifecycle/EP-0029-US-0012-campaign-lifecycle.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Сквозные Engine сценарии и миграции](EP-0029-US-0013-TK-0001-engine-acceptance/EP-0029-US-0013-TK-0001-engine-acceptance.md) | engine | [US-0012-TK-0004](../EP-0029-US-0012-campaign-lifecycle/EP-0029-US-0012-TK-0004-lifecycle-locale/EP-0029-US-0012-TK-0004-lifecycle-locale.md) | AC-01, AC-02 |
| [TK-0002 — Реальный session flow и визуальная проверка](EP-0029-US-0013-TK-0002-client-acceptance/EP-0029-US-0013-TK-0002-client-acceptance.md) | client | [US-0013-TK-0001](EP-0029-US-0013-TK-0001-engine-acceptance/EP-0029-US-0013-TK-0001-engine-acceptance.md) | AC-01, AC-02, AC-03, AC-04 |
| [TK-0003 — Проверка starter assets и балансного бюджета](EP-0029-US-0013-TK-0003-content-acceptance/EP-0029-US-0013-TK-0003-content-acceptance.md) | content-data | [US-0013-TK-0002](EP-0029-US-0013-TK-0002-client-acceptance/EP-0029-US-0013-TK-0002-client-acceptance.md) | AC-01, AC-03 |

## Completion evidence

- AC-01: тикеты TK-0001, TK-0002, TK-0003; named tests и наблюдаемый результат внутри тикетов.
- AC-02: тикеты TK-0001, TK-0002; named tests и наблюдаемый результат внутри тикетов.
- AC-03: тикеты TK-0002, TK-0003; named tests и наблюдаемый результат внутри тикетов.
- AC-04: тикеты TK-0002; named tests и наблюдаемый результат внутри тикетов.
- Future evidence: уникальный run под D:/DeepSpaceSaga/artifacts/validation/ep-0029/; после реализации сюда записать результаты и ссылки.
- Сейчас implementation/automated/native/product: **NOT RUN / OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
