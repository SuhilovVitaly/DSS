---
epic: EP-0010-combat-reports
story: EP-0010-US-0008-report-illustrations
title: "Графические доклады в стиле DSS"
stage: implemented
dependencies: ["EP-0010-US-0007-report-history-and-text"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed-art-open
revision: 1
---

# Графические доклады в стиле DSS

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R12, R14, R15, R22. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как игрок, я различаю смысл события по иллюстрации и докладчику. Отсутствующий ресурс не делает карточку пустой или неработоспособной.

## Acceptance criteria

- **US08-AC1:** Восемь локальных PNG 1024×576 готовы по R14; без встроенного текста, единый стиль, смысл читается в уменьшении; визуальное подтверждение фиксируется отдельно.
- **US08-AC2:** Client catalog/cache содержит восемь variants, ресурсы копируются в output; отсутствие image/portrait даёт тематический fallback; decoding/I/O не выполняются в Draw, кеш ограничен.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0007-report-history-and-text Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0008-TK-0001-launch-art](EP-0010-US-0008-TK-0001-launch-art/EP-0010-US-0008-TK-0001-launch-art.md) — Иллюстрации дальности и торпедных пусков | client | EP-0010-US-0007-TK-0002-localized-report-text | US08-AC1 |
| 2 | [EP-0010-US-0008-TK-0002-defense-art](EP-0010-US-0008-TK-0002-defense-art/EP-0010-US-0008-TK-0002-defense-art.md) — Иллюстрации ПРО, перехвата и попадания | client | EP-0010-US-0008-TK-0001-launch-art | US08-AC1 |
| 3 | [EP-0010-US-0008-TK-0003-damage-art](EP-0010-US-0008-TK-0003-damage-art/EP-0010-US-0008-TK-0003-damage-art.md) — Уничтожение противника и повреждение игрока | client | EP-0010-US-0008-TK-0002-defense-art | US08-AC1 |
| 4 | [EP-0010-US-0008-TK-0004-catalog-and-cache](EP-0010-US-0008-TK-0004-catalog-and-cache/EP-0010-US-0008-TK-0004-catalog-and-cache.md) — Каталог, доставка ресурсов и ограниченный кеш | client | EP-0010-US-0008-TK-0003-damage-art | US08-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

All8 original opaque1024x576 assets generated with built-in imagegen, normalized/visually checked at320px; scene differences reviewed. Catalogue/cache implemented, 28 client report tests PASS. Art files and catalogue copied to output; resolve does not decode. Bounded portrait cache/disposal tested. Human art acceptance OPEN; native UI acceptance belongs to US0011.
