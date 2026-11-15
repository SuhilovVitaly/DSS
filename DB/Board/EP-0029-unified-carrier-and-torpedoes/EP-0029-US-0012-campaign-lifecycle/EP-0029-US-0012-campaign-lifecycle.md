---
epic: EP-0029-unified-carrier-and-torpedoes
story: EP-0029-US-0012-campaign-lifecycle
title: Реальный учебный запуск и надёжное завершение
stage: draft
dependencies: [US-0006, US-0007, US-0008, US-0010, US-0011]
created: 2026-11-14T19:42:24Z
source_request: "Создай подробный эпик со всеми сторисами и тикетами."
current_review: complete
revision: 1
---

# Реальный учебный запуск и надёжное завершение

## Входное техническое задание

Часть [EP-0029](../Documentation.md). Утверждены единый корабль, 2 двухместных ангара, 2 жилых модуля по4 места (6 людей+2 резерва), четыре женщины-пилота, реальные потери обучения, магазины6/12 и42 торпеды в трюме. Захват пленных исключён.

## User story

Игрок начинает обучение без обхода ownership, а после боя получает сохранённые последствия. Выход в меню и сбой записи не дают скрытую бесплатную перезагрузку имущества. История принимается по наблюдаемому состоянию и критериям ниже; существование DTO или документа не означает готовности пользовательского пути.

## Grounding

Текущая реализация и source pointers находятся в [Grounding эпика](../Documentation.md#grounding--текущая-реализация). Изменяемые файлы перечислены в Code context тикетов. Новые классы/API явно проектируемые.

## Acceptance criteria

- **AC-01**: Tutorial и F8 передают source ship/revision; игрок не выбирает бесплатный loadout. Начальная ошибка возвращает в меню без публикации частичной кампании.
- **AC-02**: После успешного settlement и release активного боя создаётся отдельный автоматический checkpoint кампании; ручные saves не перезаписываются. Mid-tutorial confirmed exit тоже фиксирует потери до выхода в меню.
- **AC-03**: При ShipDestroyed открыт terminal screen; при потере только дронов доступна карта с уцелевшим кораблём. Restart/повтор экранной анимации не возвращает погибший корабль.
- **AC-04**: Если запись checkpoint не удалась, UI явно показывает ошибку и сохраняет session для retry, не выдаёт успех и не закрывает её молча. Load старого ручного save разрешён: Ironman не вводится.

## Non-goals

Захват/обмен пленных, найм, верфь, рыночное пополнение, ремонт, новый тип оружия, realtime бой, Ironman и сохранение посреди боя исключены. История не расширяет scope соседних тикетов.

## Dependencies

- [US-0006 — Необратимые последствия завершённого боя](../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-battle-consequences.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0007 — Сохранение корабля, потерь и боезапаса](../EP-0029-US-0007-save-continuity/EP-0029-US-0007-save-continuity.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0008 — Перезарядка дронов из грузового трюма](../EP-0029-US-0008-rearm-between-battles/EP-0029-US-0008-rearm-between-battles.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0010 — Корабль, пилоты и боезапас на экране боя](../EP-0029-US-0010-tactical-presentation/EP-0029-US-0010-tactical-presentation.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.
- [US-0011 — Готовый учебный Tetrarch и стартовый состав](../EP-0029-US-0011-starter-loadout/EP-0029-US-0011-starter-loadout.md): завершённая prerequisite поверхность. Наличие Board файла не равно выполнению.

## Invariants

Engine владеет ресурсами и итогами; Client показывает immutable snapshots. Один ShipId/DroneId/CrewId сохраняется между поверхностями. Расходы идемпотентны, пакетные отказы атомарны. Реальное обучение не имеет бесплатного reset. Два reserved места не создают пленных. На Load имущество не выдаётся.

## Approved ticket map

Workflow approval автоматический по поручению создать весь пакет; human approval подробной новой карты не заявляется.

| ID / Title | Layer | Depends on | Served criteria |
|---|---|---|---|
| [TK-0001 — Автосохранение результата без потери сессии](EP-0029-US-0012-TK-0001-checkpoint-transport/EP-0029-US-0012-TK-0001-checkpoint-transport.md) | local-client | [US-0006-TK-0003](../EP-0029-US-0006-battle-consequences/EP-0029-US-0006-TK-0003-post-battle-state/EP-0029-US-0006-TK-0003-post-battle-state.md), [US-0007-TK-0003](../EP-0029-US-0007-save-continuity/EP-0029-US-0007-TK-0003-catalog-compatibility/EP-0029-US-0007-TK-0003-catalog-compatibility.md), [US-0008-TK-0004](../EP-0029-US-0008-rearm-between-battles/EP-0029-US-0008-TK-0004-rearm-client-handle/EP-0029-US-0008-TK-0004-rearm-client-handle.md), [US-0010-TK-0005](../EP-0029-US-0010-tactical-presentation/EP-0029-US-0010-TK-0005-battle-locale/EP-0029-US-0010-TK-0005-battle-locale.md), [US-0011-TK-0003](../EP-0029-US-0011-starter-loadout/EP-0029-US-0011-TK-0003-tutorial-instructions/EP-0029-US-0011-TK-0003-tutorial-instructions.md) | AC-02, AC-04 |
| [TK-0002 — Подключение Tutorial и F8 к собственному кораблю](EP-0029-US-0012-TK-0002-owned-client-entry/EP-0029-US-0012-TK-0002-owned-client-entry.md) | client | [US-0012-TK-0001](EP-0029-US-0012-TK-0001-checkpoint-transport/EP-0029-US-0012-TK-0001-checkpoint-transport.md) | AC-01 |
| [TK-0003 — Маршруты завершения, гибели и retry](EP-0029-US-0012-TK-0003-finish-and-defeat/EP-0029-US-0012-TK-0003-finish-and-defeat.md) | client | [US-0012-TK-0002](EP-0029-US-0012-TK-0002-owned-client-entry/EP-0029-US-0012-TK-0002-owned-client-entry.md) | AC-02, AC-03, AC-04 |
| [TK-0004 — Тексты сохранения, ошибок и гибели](EP-0029-US-0012-TK-0004-lifecycle-locale/EP-0029-US-0012-TK-0004-lifecycle-locale.md) | content-data | [US-0012-TK-0003](EP-0029-US-0012-TK-0003-finish-and-defeat/EP-0029-US-0012-TK-0003-finish-and-defeat.md) | AC-02, AC-03, AC-04 |

## Completion evidence

- AC-01: тикеты TK-0002; named tests и наблюдаемый результат внутри тикетов.
- AC-02: тикеты TK-0001, TK-0003, TK-0004; named tests и наблюдаемый результат внутри тикетов.
- AC-03: тикеты TK-0003, TK-0004; named tests и наблюдаемый результат внутри тикетов.
- AC-04: тикеты TK-0001, TK-0003, TK-0004; named tests и наблюдаемый результат внутри тикетов.
- Future evidence: уникальный run под D:/DeepSpaceSaga/artifacts/validation/ep-0029/; после реализации сюда записать результаты и ссылки.
- Сейчас implementation/automated/native/product: **NOT RUN / OPEN**.

## Decisions and review log

2026-11-14T19:42:24Z — время записи, не отправки. Пользователь: «Пока только резервируем два места, захват пилотов сделаем позже. По торпедам согласен: один выстрел — одна торпеда, перезарядка из трюма между боями. Создай подробный эпик со всеми сторисами и тикетами.»
Концепция согласована. Assumptions A-01..A-06 берутся из эпика. Story draft отличает новую детализацию от human approval; ticket approved означает workflow-only. Поручения реализовать/публиковать нет.

## Gaps and backlog

Блокирующих вопросов для planning нет. Native проверка, баланс и совместимость должны быть доказаны исполнением. Если исходники изменились, перечитать owning files и обновить конкретный тикет до правок, сохранив лимит5 файлов; не расширять allowlist молча.
