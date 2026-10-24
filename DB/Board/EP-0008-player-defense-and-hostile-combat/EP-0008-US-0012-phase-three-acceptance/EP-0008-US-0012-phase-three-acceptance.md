---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0012-phase-three-acceptance
title: "Проверяемый двусторонний бой"
stage: draft
dependencies: ["EP-0008-US-0011-phase-three-save-continuation"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-reviewed-native-open
---

## Актуализация EP-0008 — 2026-10-12

Acceptance/self-review опубликованы; native matrix13/14 и human/FPS gates остаются OPEN. [Фактическое evidence](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/04-Engineering/PhaseThreeCombatAcceptance.md), [ticket → SHA → push](../PublicationStatus.md). `stage: draft` — исходная метка планирования, не утверждение о пользовательском APPROVED. Дальнейшие game/native запуски отложены пользователем.


# Проверяемый двусторонний бой

## User story

Поставка проверяется на настоящем сценарии и нескольких враждебных кораблях. Автоматические проверки, нативное окно и результаты review фиксируются отдельно.

## Источник и границы

Решения Q01–Q51; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Полный бой через actual NewGame/commands/session без телепортаций и direct HP edits доказывает преследование, NPC shots, auto/manual defenses, repeats, timeout, defeat и current save continuation.
- **AC-0002:** Существующие проверки адаптированы к согласованным изменениям без ослабления независимых assertions; полный solution regression проходит либо честно фиксирует несвязанный baseline.
- **AC-0003:** Native1280x720/1920x1080 UI100/120/150% доказывает все панели, hover/map/timers, defeat/load; runtime PASS не подменяет native.
- **AC-0004:** Review каждого слоя и всей цепочки закрывает confirmed defects; показатели производительности основаны на измерениях, не исторических отчётах.

## Зависимости

- [EP-0008-US-0011-phase-three-save-continuation](../EP-0008-US-0011-phase-three-save-continuation/EP-0008-US-0011-phase-three-save-continuation.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 40 | [EP-0008-US-0012-TK-0001-compatibility-fixtures](EP-0008-US-0012-TK-0001-compatibility-fixtures/EP-0008-US-0012-TK-0001-compatibility-fixtures.md) — Адаптация существующих проверок к новой фазе | validation | AC-0002 |
| 41 | [EP-0008-US-0012-TK-0002-end-to-end-combat](EP-0008-US-0012-TK-0002-end-to-end-combat/EP-0008-US-0012-TK-0002-end-to-end-combat.md) — Сквозное доказательство боя с настоящим AI | engine | AC-0001, AC-0002, AC-0004 |
| 42 | [EP-0008-US-0012-TK-0003-native-ui-acceptance](EP-0008-US-0012-TK-0003-native-ui-acceptance/EP-0008-US-0012-TK-0003-native-ui-acceptance.md) — Нативная приёмка панелей, боя и поражения | client | AC-0001, AC-0003, AC-0004 |
| 43 | [EP-0008-US-0012-TK-0004-epic-review](EP-0008-US-0012-TK-0004-epic-review/EP-0008-US-0012-TK-0004-epic-review.md) — Итоговое review контрактов и границ | validation | AC-0001, AC-0002, AC-0003, AC-0004 |

## Completion evidence

Покрытие AC и фактические ограничения приведены в текущем execution status и acceptance report; наличие запланированных проверок само по себе не является evidence. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Исходная запись описывала план; текущий запрос разрешает автономное исполнение.
