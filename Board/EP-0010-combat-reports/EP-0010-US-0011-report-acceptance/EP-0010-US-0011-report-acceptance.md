---
epic: EP-0010-combat-reports
story: EP-0010-US-0011-report-acceptance
title: "Воспроизводимая приёмка боя и интерфейса"
stage: draft
dependencies: ["EP-0010-US-0010-tactical-screen-integration"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: planning-only
revision: 1
---

# Воспроизводимая приёмка боя и интерфейса

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R08, R09, R10, R11, R17, R19, R20, R21, R22, R23, R24. [Эпик](../EP-0010-combat-reports.md). Документ создан 2026-10-16; поручение — сохранение и декомпозиция, execution NOT STARTED.

## User story

Как владелец игры, я могу проверить все доклады на воспроизводимых условиях и увидеть границы автоматической и визуальной приёмки. Один случайный бой не заменяет матрицу проверки.

## Acceptance criteria

- **US11-AC1:** Engine acceptance на production CombatMukha и целевых fixtures подтверждает все E01–E07, ordering/correlation/privacy/SaveLoad без изменения боевого баланса или RNG.
- **US11-AC2:** Client integration подтверждает end-to-end delivery, 200/4096 limits, first snapshot, UI/input/lifecycle и supported viewport/locale матрицу; broad checks выполнены с отдельными baseline failures.
- **US11-AC3:** Native/human/FPS evidence заполнено реальными измерениями либо оставлено NOT RUN/OPEN с причиной; 80 FPS gate отдельно от функциональной графики и headless.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0010-tactical-screen-integration Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0011-TK-0001-engine-scenario-proof](EP-0010-US-0011-TK-0001-engine-scenario-proof/EP-0010-US-0011-TK-0001-engine-scenario-proof.md) — Сценарные доказательства всех семи типов | engine | EP-0010-US-0010-TK-0003-session-reset-and-defeat | US11-AC1 |
| 2 | [EP-0010-US-0011-TK-0002-client-integration-proof](EP-0010-US-0011-TK-0002-client-integration-proof/EP-0010-US-0011-TK-0002-client-integration-proof.md) — Интеграционная матрица доставки и UI | client | EP-0010-US-0011-TK-0001-engine-scenario-proof | US11-AC2 |
| 3 | [EP-0010-US-0011-TK-0003-native-visual-and-performance](EP-0010-US-0011-TK-0003-native-visual-and-performance/EP-0010-US-0011-TK-0003-native-visual-and-performance.md) — Визуальная приёмка и frame-time evidence | client | EP-0010-US-0011-TK-0002-client-integration-proof | US11-AC3 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. Все новые API/файлы — proposed, наличие ещё не означает реализацию. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»
