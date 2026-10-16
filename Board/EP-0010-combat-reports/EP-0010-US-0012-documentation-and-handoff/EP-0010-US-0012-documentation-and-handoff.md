---
epic: EP-0010-combat-reports
story: EP-0010-US-0012-documentation-and-handoff
title: "Документация и честный статус поставки"
stage: implemented
dependencies: ["EP-0010-US-0011-report-acceptance"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: documented-acceptance-open
revision: 1
---

# Документация и честный статус поставки

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R01, R02, R03, R04, R05, R06, R07, R08, R09, R10, R11, R12, R13, R14, R15, R16, R17, R18, R19, R20, R21, R22, R23, R24. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как владелец проекта, я нахожу спецификацию, тикеты и реальные результаты приёмки по устойчивым ссылкам. План, реализация и визуальное принятие различены.

## Acceptance criteria

- **US12-AC1:** После реализации canonical spec/EngineRequirements/GameSession отражают фактический контракт и additive save semantics; Board связывает каждый ticket с evidence, а native/human/performance gaps сохраняет открытыми до реальной приёмки.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0011-report-acceptance Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0012-TK-0001-project-documentation](EP-0010-US-0012-TK-0001-project-documentation/EP-0010-US-0012-TK-0001-project-documentation.md) — Синхронизация спецификации, экрана и статуса эпика | documentation | EP-0010-US-0011-TK-0003-native-visual-and-performance | US12-AC1 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Execution scope amendment — 2026-10-16

Текущий запрос запускает EpicExecutionPrompt.md и разрешает реализацию, расширение scope, отдельные commit/push и Graphify rebuild. Предыдущие ограничения planning-only остаются историей.

US12-AC1 расширен до всего §6 промта: инвентаризация влияния по итоговому diff во всём репозитории; актуализация canonical и связанных документов вне Documentation; сохранение датированных результатов; сверка ссылок, ID, количества карточек, покрытия и ticket-to-published-SHA; перестройка и проверка Graphify после кода и текстов. Эта история выполняется последней; незакрытые native/human/FPS gates сохраняют честный статус.

## Execution result —2026-10-16

Expanded documentation inventory applied across canonical requirements, screen, engineering, tooling/README, related EP9 and all EP10 cards. Status distinguishes headless PASS from native/human/FPS OPEN; no autonomous game launches allowed. Ticket publication table uses real verified SHAs and leaves own final SHA to Git post-commit check. Link/ID/DAG audit and fresh Graphify extraction/export validated before separate final commit/push. See DocumentationInventory.md, DocumentationAudit.json, FinalReview.md and ImplementationStatus.md in epic root.
