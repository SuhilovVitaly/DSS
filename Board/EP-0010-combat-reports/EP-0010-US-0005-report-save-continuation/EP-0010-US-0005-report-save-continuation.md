---
epic: EP-0010-combat-reports
story: EP-0010-US-0005-report-save-continuation
title: "Сохранение отметок и чистая загрузка"
stage: implemented
dependencies: ["EP-0010-US-0004-combat-result-reports"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Сохранение отметок и чистая загрузка

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R17, R20, R21. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как игрок, я могу продолжить сохранённый бой без повторного показа старых докладов. Ранее доложенная Муха не становится новым контактом после загрузки.

## Acceptance criteria

- **US05-AC1:** Optional CombatReportsState Version1 строго валидируется до замены мира; Save17 без блока допустим с явным baseline, Save16 и ниже остаются отвергнутыми.
- **US05-AC2:** Сохраняются sequence и все ReportedRangeTargetIds, включая deleted/out-of-range; Load объединяет их с current in-range без события, новые реальные события первого snapshot не теряются; ошибка Load не меняет действующую сессию.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0004-combat-result-reports Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0005-TK-0001-save-schema-validation](EP-0010-US-0005-TK-0001-save-schema-validation/EP-0010-US-0005-TK-0001-save-schema-validation.md) — Схема и атомарная валидация состояния докладов | engine | EP-0010-US-0004-TK-0002-contact-summary | US05-AC1 |
| 2 | [EP-0010-US-0005-TK-0002-capture-and-restore](EP-0010-US-0005-TK-0002-capture-and-restore/EP-0010-US-0005-TK-0002-capture-and-restore.md) — Capture/restore и bootstrap baseline | engine | EP-0010-US-0005-TK-0001-save-schema-validation | US05-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

Save17 extension validated, required fields enforced, pre-feature baseline supported, allocator/range IDs captured and restored atomically. 83 report/save/burst cases PASS. Reviewed new-game reset, silent loaded contacts, out-of-range and removed IDs, post-load first event and defeat rejection. Native acceptance remains OPEN.
