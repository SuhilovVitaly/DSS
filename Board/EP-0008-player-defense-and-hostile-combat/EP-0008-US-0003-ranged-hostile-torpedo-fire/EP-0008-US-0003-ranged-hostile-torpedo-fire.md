---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0003-ranged-hostile-torpedo-fire
title: "Враждебные корабли стреляют по игроку"
stage: implemented
dependencies: ["EP-0008-US-0002-dual-defense-loadout"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-pass
---

## Актуализация EP-0008 — 2026-10-12

Функциональные тикеты реализованы, проверены и опубликованы; self-review завершено. [Фактическое evidence](../../../Documentation/04-Engineering/PhaseThreeCombatAcceptance.md), [ticket → SHA → push](../PublicationStatus.md). `stage: draft` — исходная метка планирования, не утверждение о пользовательском APPROVED. Дальнейшие game/native запуски отложены пользователем.


# Враждебные корабли стреляют по игроку

## User story

Каждый враждебный вооружённый корабль запускает торпеду по игроку в радиусе своего аппарата. Игрок подчиняется той же дальности, но управление чужими модулями через player command остаётся запрещено.

## Источник и границы

Решения Q02–Q04,Q17,Q41,Q44; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Дальность<=moduleRangeKm применяется к обеим сторонам; выход цели за радиус не отменяет текущий полёт.
- **AC-0002:** Только Enemy NpcShip с рабочим аппаратом/оператором автоматически стреляет по живому игроку; пауза запрещает AI-пуски.
- **AC-0003:** На аппарат одна активная торпеда; после завершения следующий пуск без отдельного cooldown при валидных условиях, без двойного пуска или зависания интервала.

## Зависимости

- [EP-0008-US-0002-dual-defense-loadout](../EP-0008-US-0002-dual-defense-loadout/EP-0008-US-0002-dual-defense-loadout.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 8 | [EP-0008-US-0003-TK-0001-shared-launch-validation](EP-0008-US-0003-TK-0001-shared-launch-validation/EP-0008-US-0003-TK-0001-shared-launch-validation.md) — Общий validated launch с разделением полномочий | engine | AC-0001, AC-0002 |
| 9 | [EP-0008-US-0003-TK-0002-hostile-fire-scheduler](EP-0008-US-0003-TK-0002-hostile-fire-scheduler/EP-0008-US-0003-TK-0002-hostile-fire-scheduler.md) — Автопуск и повторная стрельба внутри физического интервала | engine | AC-0002, AC-0003 |
| 10 | [EP-0008-US-0003-TK-0003-player-range-feedback](EP-0008-US-0003-TK-0003-player-range-feedback/EP-0008-US-0003-TK-0003-player-range-feedback.md) — Понятная недоступность торпедного пуска вне дальности | client | AC-0001 |

## Completion evidence

Покрытие AC и фактические ограничения приведены в текущем execution status и acceptance report; наличие запланированных проверок само по себе не является evidence. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Исходная запись описывала план; текущий запрос разрешает автономное исполнение.

## Execution self-review — 2026-10-11

AC-0001: shared inclusive range in Engine/Client, physical launch poses, alternate
module ranges, surviving active flights after exit, UI disabled/rejected/pending
identity checks. AC-0002: real Enemy-NPC launch path, all owners/modules, live-player
guard, pause/calendar-only no-fire, no forged player receipts. AC-0003: per-launcher
busy state, immediate eligible integer-ms relaunch under A07, overlap termination
and exhausted-ID progress guards, large/split intervals and real-file continuation.
All three tickets validated and separately committed/pushed. Last ticket: Client1770,
Motion9 and Engine32 PASS, build/format/diff PASS. Self-review is not independent
review or user approval. Native/final corpus remain epic gates. This evidence
supersedes the historical planning-only prose above.
