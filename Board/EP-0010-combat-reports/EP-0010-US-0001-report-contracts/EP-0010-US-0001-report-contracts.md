---
epic: EP-0010-combat-reports
story: EP-0010-US-0001-report-contracts
title: "Авторитетный контракт боевого доклада"
stage: draft
dependencies: []
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Авторитетный контракт боевого доклада

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R11, R12, R16, R17. [Эпик](../EP-0010-combat-reports.md). Документ создан 2026-10-16; поручение — сохранение и декомпозиция, execution NOT STARTED.

## User story

Как игрок, я получаю доклады о подтверждённых событиях с устойчивыми участниками и временем. Представление не меняется после удаления объектов из мира.

## Acceptance criteria

- **US01-AC1:** Immutable DTO передаёт E01–E07, Hit/Destroyed variant, sequence, physical time, EpisodeId, source IDs, captured display names, reporter identity/role/optional portrait и разрешённые damage details без графических типов.
- **US01-AC2:** Optional batch в AuthoritativeSnapshot сохраняет JSON round-trip и совместимость default/empty; baseline и latest различены, сериализация не требует Client/Engine ссылок.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

Нет других историй. Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0001-TK-0001-immutable-report](EP-0010-US-0001-TK-0001-immutable-report/EP-0010-US-0001-TK-0001-immutable-report.md) — Неизменяемые данные доклада | contracts | none | US01-AC1 |
| 2 | [EP-0010-US-0001-TK-0002-snapshot-batch](EP-0010-US-0001-TK-0002-snapshot-batch/EP-0010-US-0001-TK-0002-snapshot-batch.md) — Пакет докладов в snapshot | contracts | EP-0010-US-0001-TK-0001-immutable-report | US01-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. Все новые API/файлы — proposed, наличие ещё не означает реализацию. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Story self-review — 2026-10-16

US01-AC1/AC2 покрыты16 новыми executable cases; полная Contracts suite210/210. Проверены immutable records/arrays, отсутствие graphics/runtime dependencies, captured nullable identity/privacy, module facts, E01–E07 и Hit/Destroyed, default/empty и старый snapshot JSON, независимость sequence и физического времени. Подтверждённых дефектов в scoped contracts после review не найдено. Это self-review автора; runtime emit/delivery/SaveLoad/UI принадлежат следующим историям и не заявляются реализованными.
