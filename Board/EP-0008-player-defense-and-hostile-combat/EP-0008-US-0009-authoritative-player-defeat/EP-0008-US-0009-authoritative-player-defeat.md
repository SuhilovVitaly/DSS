---
epic: EP-0008-player-defense-and-hostile-combat
story: EP-0008-US-0009-authoritative-player-defeat
title: "Поражение останавливает весь игровой мир"
stage: draft
dependencies: ["EP-0008-US-0008-manual-preview-and-map"]
created: 2026-10-07
revision: 1
current_review: complete
validation_status: automated-pass
---

# Поражение останавливает весь игровой мир

## User story

При нуле HP корабля игрока поражение фиксируется в Engine. Мир останавливается на границе гибели и не продолжает бой или экономику в остатке большого шага.

## Источник и границы

Решения Q27–Q28; полный контекст в [журнале решений](../Decisions.md) и [контракте эпика](../Documentation.md). Технические допущения A01–A12 отделены от пользовательских ответов. Scope ограничен результатом этой истории; смежные механики из Non-goals эпика не добавляются.

## Acceptance criteria

- **AC-0001:** Поражение фиксируется один раз на lethal hull impact, сохраняются death time/ship/wreck identity и authoritative terminal snapshot.
- **AC-0002:** После terminal event не продвигаются физика, календарные системы, AI, пуски/RNG; speed/resume/gameplay команды не могут возобновить мир.
- **AC-0003:** Capture/Save после defeat отклоняется с понятной причиной до записи; snapshots/UI и загрузка новой сессии доступны.

## Зависимости

- [EP-0008-US-0008-manual-preview-and-map](../EP-0008-US-0008-manual-preview-and-map/EP-0008-US-0008-manual-preview-and-map.md): production-результат зависимости должен быть реализован и проверен до этой истории.

## Карта тикетов и покрытия

| Шаг | Тикет | Layer | Serves |
|---|---|---|---|
| 30 | [EP-0008-US-0009-TK-0001-defeat-contract](EP-0008-US-0009-TK-0001-defeat-contract/EP-0008-US-0009-TK-0001-defeat-contract.md) — Контракт терминального состояния сессии | contracts | AC-0001, AC-0003 |
| 31 | [EP-0008-US-0009-TK-0002-defeat-transition](EP-0008-US-0009-TK-0002-defeat-transition/EP-0008-US-0009-TK-0002-defeat-transition.md) — Атомарная гибель игрока и запрет действий | engine | AC-0001, AC-0002, AC-0003 |
| 32 | [EP-0008-US-0009-TK-0003-defeat-clock-boundary](EP-0008-US-0009-TK-0003-defeat-clock-boundary/EP-0008-US-0009-TK-0003-defeat-clock-boundary.md) — Остановка времени на событии поражения | engine | AC-0001, AC-0002 |

## Completion evidence

Все AC доказаны связанными тикетами, проверками реального результата и review. Конкретные команды и запланированные регрессии находятся в карточках. Native/UI требования подтверждаются реальным окном отдельно от automated tests; при недоступности это NOT RUN, не PASS.

## Gaps и исполнение

Блокирующих продуктовых вопросов для планирования нет. Proposed API/названия файлов уточняются по актуальному baseline; изменения в scope фиксируются в карточке. Общие числовые/временные инварианты находятся в эпике, включая current-format save и terminal clock. История draft и не является утверждением о пройденных тестах.

При будущем запуске по [EpicExecutionPrompt](../../EpicExecutionPrompt.md) каждый тикет выполняется, проверяется, коммитится и публикуется отдельно. Текущая задача только создаёт план.

## Execution review — 2026-10-11

AC-0001: three real hits produce exactly one terminal outcome with death event/time/player/wreck identity. AC-0002: large and split intervals stop at the same boundary, the remaining world/AI/launch/RNG/economy interval is discarded, and latched clocks resist stale resume requests. AC-0003: both central capture entrypoints reject terminal saves including a capture that itself reaches death; snapshots remain valid with no player, invalid Load preserves state, valid new world clears it. Contracts185/185, TK-0002 Engine1842/1842 and Client130/130, TK-0003 final affected113/113; build/format/diffPASS. Own review complete. File transport and user-facing modal/recovery are the next story; native acceptance OPEN.
