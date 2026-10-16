---
epic: EP-0010-combat-reports
story: EP-0010-US-0009-report-accordion
title: "Карточки аккордеона и чтение докладов"
stage: draft
dependencies: ["EP-0010-US-0008-report-illustrations"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Карточки аккордеона и чтение докладов

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R04, R05, R06, R07, R13, R15, R22. [Эпик](../EP-0010-combat-reports.md). Документ создан 2026-10-16; поручение — сохранение и декомпозиция, execution NOT STARTED.

## User story

Как игрок, я вижу одну раскрытую графическую карточку и заголовки остальных. Я могу открыть прошлый доклад мышью или клавиатурой.

## Acceptance criteria

- **US09-AC1:** Рендер раскрытой карточки включает art, отдельный portrait/role, время/заголовок/реплику/facts; остальные только titlebar, перенос текста и clipping корректны.
- **US09-AC2:** Аккордеон имеет согласованные hit rectangles, внутреннюю прокрутку и keyboard navigation; новый факт не крадёт focus, click/wheel внутри не пропускаются к карте.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0008-report-illustrations Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0009-TK-0001-card-rendering](EP-0010-US-0009-TK-0001-card-rendering/EP-0010-US-0009-TK-0001-card-rendering.md) — Рендер раскрытой и свёрнутой карточки | client | EP-0010-US-0008-TK-0004-catalog-and-cache | US09-AC1 |
| 2 | [EP-0010-US-0009-TK-0002-accordion-input](EP-0010-US-0009-TK-0002-accordion-input/EP-0010-US-0009-TK-0002-accordion-input.md) — Ввод и навигация по карточкам | client | EP-0010-US-0009-TK-0001-card-rendering | US09-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. Все новые API/файлы — proposed, наличие ещё не означает реализацию. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

Prepared card render and accordion interaction implemented. Reviewed cached wrap, clipping, separate portrait, fallback, logical expansion, focus/pointer ownership, consumed wheel/keys and resize geometry.38 client report cases PASS. Native visual/input acceptance remains OPEN until integrated screen run.
