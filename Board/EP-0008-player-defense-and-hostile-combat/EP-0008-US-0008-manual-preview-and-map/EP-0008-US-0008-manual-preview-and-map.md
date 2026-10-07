---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0008-manual-preview-and-map
title: "Прогноз ручного перехвата и дальности без перегрузки карты"
stage: draft
dependencies: ["EP-0008-US-0007-independent-defense-panels"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: planning-only
---

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
| 26 | [EP-0008-US-0008-TK-0001-preview-geometry](EP-0008-US-0008-TK-0001-preview-geometry/EP-0008-US-0008-TK-0001-preview-geometry.md) — Геометрия прогноза ручного пуска конкретного аппарата | client | AC-0001, AC-0003 |
| 27 | [EP-0008-US-0008-TK-0002-hover-ranges-and-countdown](EP-0008-US-0008-TK-0002-hover-ranges-and-countdown/EP-0008-US-0008-TK-0002-hover-ranges-and-countdown.md) — Круги hover и время полёта на карте | client | AC-0002, AC-0003 |
| 28 | [EP-0008-US-0008-TK-0003-journal-presentation](EP-0008-US-0008-TK-0003-journal-presentation/EP-0008-US-0008-TK-0003-journal-presentation.md) — Читаемый журнал новых событий боя | client | AC-0003 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.
