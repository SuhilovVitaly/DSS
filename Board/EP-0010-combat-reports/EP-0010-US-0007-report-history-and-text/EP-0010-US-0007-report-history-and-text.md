---
epic: EP-0010-combat-reports
story: EP-0010-US-0007-report-history-and-text
title: "История карточек и локализованные доклады"
stage: implemented
dependencies: ["EP-0010-US-0006-reliable-report-delivery"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# История карточек и локализованные доклады

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R03, R04, R05, R07, R11, R12, R13, R19, R20. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как игрок, я могу прочитать последние события боя и вернуться к старому докладу. Имена и реплики остаются понятными после исчезновения участников.

## Acceptance criteria

- **US07-AC1:** Store хранит последние200, newest-first, ровно один ExpandedId при непустой истории; manual select и new event следуют R05; selection карты не влияет.
- **US07-AC2:** Russian/English formatter покрывает E01–E07 и variants, физическое время, captured имена/службы, разрешённые damage facts, отсутствующие данные и длинные строки.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0006-reliable-report-delivery Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0007-TK-0001-history-state](EP-0010-US-0007-TK-0001-history-state/EP-0010-US-0007-TK-0001-history-state.md) — Ограниченная история и раскрытая карточка | client | EP-0010-US-0006-TK-0002-buffer-pending-reports | US07-AC1 |
| 2 | [EP-0010-US-0007-TK-0002-localized-report-text](EP-0010-US-0007-TK-0002-localized-report-text/EP-0010-US-0007-TK-0002-localized-report-text.md) — Реплики и заголовки Russian/English | client | EP-0010-US-0007-TK-0001-history-state | US07-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

Bounded store and localized captured-fact presentation implemented. Reviewed single expanded card, keyboard selection, newest batch, duplicates/reset/eviction, both E06 variants, role fallback, unavailable participants and equal placeholders. 24 client report cases PASS. Layout and native checks remain later stories.
