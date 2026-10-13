---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0008-manual-preview-and-map
title: "Прогноз ручного перехвата и дальности без перегрузки карты"
stage: draft
dependencies: ["EP-0008-US-0007-independent-defense-panels"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-pass-native-open
---

## Актуализация EP-0008 — 2026-10-12

Функциональные тикеты реализованы, проверены и опубликованы; self-review завершено. [Фактическое evidence](../../../Documentation/04-Engineering/PhaseThreeCombatAcceptance.md), [ticket → SHA → push](../PublicationStatus.md). `stage: draft` — исходная метка планирования, не утверждение о пользовательском APPROVED. Дальнейшие game/native запуски отложены пользователем.


# Прогноз ручного перехвата и дальности без перегрузки карты

## User story

Hover над пуском показывает прогноз конкретного аппарата. Радиусы появляются только у соответствующей панели, а активная ПРО показывает шанс и остаток времени.

## Источник и границы

Решения Q30,Q38,Q45–Q46,Q50; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Hover доступного fire даёт shared-Motion trajectory/encounter и authoritative chance; no solution явно показано без ложной точки встречи.
- **AC-0002:** Hover панели torpedo показывает её range; hover конкретной defense panel показывает auto/manual radii. Без hover круги не рисуются, в том числе старый selected-pirate круг.
- **AC-0003:** Live PR показывает remaining lifetime/chance; prediction отличим от confirmed flight. Pause/zoom/UI scale и per-module hover не меняют gameplay.

## Зависимости

- [EP-0008-US-0007-independent-defense-panels](../EP-0008-US-0007-independent-defense-panels/EP-0008-US-0007-independent-defense-panels.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 27 | [EP-0008-US-0008-TK-0001-preview-geometry](EP-0008-US-0008-TK-0001-preview-geometry/EP-0008-US-0008-TK-0001-preview-geometry.md) — Геометрия прогноза ручного пуска конкретного аппарата | client | AC-0001, AC-0003 |
| 28 | [EP-0008-US-0008-TK-0002-hover-ranges-and-countdown](EP-0008-US-0008-TK-0002-hover-ranges-and-countdown/EP-0008-US-0008-TK-0002-hover-ranges-and-countdown.md) — Круги hover и время полёта на карте | client | AC-0002, AC-0003 |
| 29 | [EP-0008-US-0008-TK-0003-journal-presentation](EP-0008-US-0008-TK-0003-journal-presentation/EP-0008-US-0008-TK-0003-journal-presentation.md) — Читаемый журнал новых событий боя | client | AC-0003 |

## Completion evidence

Покрытие AC и фактические ограничения приведены в текущем execution status и acceptance report; наличие запланированных проверок само по себе не является evidence. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Исходная запись описывала план; текущий запрос разрешает автономное исполнение.

## Execution review — 2026-10-11

All three tickets implemented and individually published by this execution chain. AC-0001: real hover preview equals real Engine manual launch at100/120/150%, captured deadline suppresses false encounters,0% remains legal and full save state is unchanged by hover. AC-0002: per-module auto/manual/torpedo rings only on hover, camera-scaled; no selected-enemy circle. AC-0003: physical pause/5x countdown, distinct wrapped labels, gray prediction, frozen facts and real-file loaded journal without effect replay. Final full Client1832/1832PASS; Engine1837/1837 at TK-0001. Own review complete, not independent approval. Native remains OPEN for US-0012. Next: authoritative defeat contract/transition/clock.
