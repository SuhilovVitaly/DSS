---
epic: EP-0010-combat-reports
story: EP-0010-US-0006-reliable-report-delivery
title: "Доставка докладов при пропуске snapshots"
stage: implemented
dependencies: ["EP-0010-US-0005-report-save-continuation"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Доставка докладов при пропуске snapshots

## Входное техническое задание

[Каноническое ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R07, R16, R17, R19, R20. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как игрок, я получаю все новые доклады при нескольких событиях между кадрами. Замена состояния мира не стирает ещё не прочитанные события.

## Acceptance criteria

- **US06-AC1:** SnapshotMailbox объединяет batches без дубликатов, по sequence, сохраняя fixed bootstrap baseline; overflow4096 — явная ошибка без изменения существующих типов событий.
- **US06-AC2:** SnapshotBuffer копит входящие доклады до drain UI независимо от Latest; отбрасывает baseline/repeats, не теряет first snapshot events, потоки синхронизированы и old connection не загрязняет new session.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0005-report-save-continuation Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0006-TK-0001-mailbox-report-merge](EP-0010-US-0006-TK-0001-mailbox-report-merge/EP-0010-US-0006-TK-0001-mailbox-report-merge.md) — Слияние докладов в local adapter | local-client | EP-0010-US-0005-TK-0002-capture-and-restore | US06-AC1 |
| 2 | [EP-0010-US-0006-TK-0002-buffer-pending-reports](EP-0010-US-0006-TK-0002-buffer-pending-reports/EP-0010-US-0006-TK-0002-buffer-pending-reports.md) — Потокобезопасное накопление перед UI | client | EP-0010-US-0006-TK-0001-mailbox-report-merge | US06-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

Mailbox and session inbox preserve every report across coalescing, legacy nulls and reordered snapshots; first received batch uses explicit baseline. Sequence ordering, conflict/overflow faults, lock ownership, drain and bounded memory reviewed. 28 delivery/buffer cases PASS. Screen drain integration remains US0010.
