---
epic: EP-0010-combat-reports
story: EP-0010-US-0002-range-reports
title: "Первый доклад о входе противника в дальность"
stage: implemented
dependencies: ["EP-0010-US-0001-report-contracts"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Первый доклад о входе противника в дальность

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R16, R18, R20, R21. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как капитан, я узнаю от сканера, что конкретная Муха вошла в радиус торпед. Повторные входы не засоряют доклады.

## Acceptance criteria

- **US02-AC1:** Engine создаёт монотонные доклады и outbox; bootstrap baseline отделён от новых событий, doc report generation не меняет RNG/боевые исходы.
- **US02-AC2:** E01 выдаётся один раз на известного живого enemy; доступные Scanner/launcher, configured range, включительная граница, disabled/destroyed и три независимых Мухи проверены на физических шагах.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0001-report-contracts Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0002-TK-0001-report-outbox](EP-0010-US-0002-TK-0001-report-outbox/EP-0010-US-0002-TK-0001-report-outbox.md) — Сессия докладов и авторитетный outbox | engine | EP-0010-US-0001-TK-0002-snapshot-batch | US02-AC1 |
| 2 | [EP-0010-US-0002-TK-0002-first-range-entry](EP-0010-US-0002-TK-0002-first-range-entry/EP-0010-US-0002-TK-0002-first-range-entry.md) — E01 по физическому состоянию мира | engine | EP-0010-US-0002-TK-0001-report-outbox | US02-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

US02-AC1/2 implemented. Checked bounded publish/reset, physical fast path and combat substeps, available scanner/launcher and inclusive catalogue distance, stable target order, disabled recovery and once-per-ID. 11 focused cases and 14 combat regressions PASS. No gameplay/RNG/time changes. Save/load baseline continuity belongs to US0005. Native acceptance remains OPEN.
