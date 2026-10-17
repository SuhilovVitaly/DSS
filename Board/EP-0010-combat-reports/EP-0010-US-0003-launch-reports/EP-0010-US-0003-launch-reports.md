---
epic: EP-0010-combat-reports
story: EP-0010-US-0003-launch-reports
title: "Доклады экипажа о фактических пусках"
stage: implemented
dependencies: ["EP-0010-US-0002-range-reports"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Доклады экипажа о фактических пусках

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R09, R11, R12, R16, R18. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как капитан, я различаю вражеский пуск, наш торпедный пуск и работу ПРО. Доклад показывает службу или реально назначенного оператора в момент действия.

## Acceptance criteria

- **US03-AC1:** Reporter resolver фиксирует существующего crew/operator/captain и доступный portrait; отсутствие scanner identity/portrait приводит к role fallback, без новых NPC или назначения экипажа.
- **US03-AC2:** E02/E03/E04 формируются только для фактических пусков; каждый выстрел очереди отдельно, отказы/пропуски исключены, E02 не воспроизводит пуски при выключенном Scanner.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0002-range-reports Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0003-TK-0001-captured-reporters](EP-0010-US-0003-TK-0001-captured-reporters/EP-0010-US-0003-TK-0001-captured-reporters.md) — Докладчики и безопасные имена участников | engine | EP-0010-US-0002-TK-0002-first-range-entry | US03-AC1 |
| 2 | [EP-0010-US-0003-TK-0002-confirmed-launches](EP-0010-US-0003-TK-0002-confirmed-launches/EP-0010-US-0003-TK-0002-confirmed-launches.md) — E02–E04 из фактов пуска | engine | EP-0010-US-0003-TK-0001-captured-reporters | US03-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

Captured-role and confirmed-launch AC implemented. 26 report/journal/burst cases PASS. Checked event authority order, actual projectile identity, masking and no scanner replay. Runtime outcomes and existing journal IDs preserved. Native acceptance deferred to US0011.
