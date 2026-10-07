---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0005-manual-countermeasure-fire
title: "Ручной перехват любой вражеской торпеды"
stage: draft
dependencies: ["EP-0008-US-0004-hostile-pursuit-and-orbit"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: planning-only
---

# Ручной перехват любой вражеской торпеды

## User story

Игрок выбирает вражескую торпеду и запускает ПРО с конкретного готового аппарата на расстоянии до200км. Он может рисковать при нулевом шансе или неизвестной встрече, повторять попытки и запускать две ПРО по одной цели.

## Источник и границы

Решения Q05–Q11,Q16,Q25–Q26,Q29,Q31,Q35,Q48; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Ручной fire адресован module/target/CommandId, работает на паузе и при auto on; readiness/оператор/range проверяются Engine.
- **AC-0002:** Любая вражеская торпеда допустима независимо от её цели; non-torpedo/own/friendly/neutral/expired отвергаются без side effects.
- **AC-0003:** No solution/0% не блокируют manual; повторные и одновременные ручные пуски разрешены после готовности выбранных аппаратов.
- **AC-0004:** Preview обоих модулей authoritative и связан с selected target; первый успешный spawn любого типа помечает общую automatic attempt.

## Зависимости

- [EP-0008-US-0004-hostile-pursuit-and-orbit](../EP-0008-US-0004-hostile-pursuit-and-orbit/EP-0008-US-0004-hostile-pursuit-and-orbit.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 15 | [EP-0008-US-0005-TK-0001-manual-guidance](EP-0008-US-0005-TK-0001-manual-guidance/EP-0008-US-0005-TK-0001-manual-guidance.md) — План и преследование без гарантированной встречи | motion | AC-0003 |
| 16 | [EP-0008-US-0005-TK-0002-manual-launch-execution](EP-0008-US-0005-TK-0002-manual-launch-execution/EP-0008-US-0005-TK-0002-manual-launch-execution.md) — Команда ручного пуска с безопасным повтором | engine | AC-0001, AC-0002, AC-0003, AC-0004 |
| 17 | [EP-0008-US-0005-TK-0003-defense-fire-catalog](EP-0008-US-0005-TK-0003-defense-fire-catalog/EP-0008-US-0005-TK-0003-defense-fire-catalog.md) — Ручная команда в каталоге ПРО | content-data | AC-0001 |
| 18 | [EP-0008-US-0005-TK-0004-defense-preview-projection](EP-0008-US-0005-TK-0004-defense-preview-projection/EP-0008-US-0005-TK-0004-defense-preview-projection.md) — Авторитетный шанс и причины недоступности каждого аппарата | engine | AC-0002, AC-0004 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.
