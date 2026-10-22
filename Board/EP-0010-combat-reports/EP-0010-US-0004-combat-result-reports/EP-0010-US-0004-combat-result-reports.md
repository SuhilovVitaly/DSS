---
epic: EP-0010-combat-reports
story: EP-0010-US-0004-combat-result-reports
title: "Подтверждённые исходы и повреждения"
stage: implemented
dependencies: ["EP-0010-US-0003-launch-reports"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Подтверждённые исходы и повреждения

## Входное техническое задание

[Каноническое ТЗ](https://github.com/SuhilovVitaly/DSS/blob/aa8750dbac35f25b82b3ec44cbe5011fc02740a0/Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R10, R11, R12, R16, R21. [Эпик](../EP-0010-combat-reports.md). Документ создан2026-10-16; затем выполнено отдельное execution-поручение по EpicExecutionPrompt. Реальные результаты и открытая приёмка — в ImplementationStatus и Execution evidence.

## User story

Как игрок, я получаю понятный итог попадания или перехвата. Фатальный удар по противнику показан одним докладом, а повреждение моего корабля сообщает капитан.

## Acceptance criteria

- **US04-AC1:** Успешный перехват ПРО игрока даёт E05; Miss/Expired/SelfDestruct/TargetLost и чужие перехваты не становятся успехом.
- **US04-AC2:** Каждый контакт даёт одну E06 hit/destroyed или E07 с фактическим уменьшением модуля; разные projectiles при одном времени не объединяются, terminal outcome сохраняет приоритет.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0003-launch-reports Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0004-TK-0001-intercept-results](EP-0010-US-0004-TK-0001-intercept-results/EP-0010-US-0004-TK-0001-intercept-results.md) — E05 успешного перехвата | engine | EP-0010-US-0003-TK-0002-confirmed-launches | US04-AC1 |
| 2 | [EP-0010-US-0004-TK-0002-contact-summary](EP-0010-US-0004-TK-0002-contact-summary/EP-0010-US-0004-TK-0002-contact-summary.md) — E06–E07 после полного применения удара | engine | EP-0010-US-0004-TK-0001-intercept-results | US04-AC2 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. API/файлы реализованы и проверены по ticket evidence; исходные proposed описания ниже сохранены как контекст планирования. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

E05/E06/E07 implemented. Checked structural success filters, captured operator, one summary per impact after destroy resolution, armor-only and zero damage boundaries, distinct projectile episodes and final defeat snapshot. 60 report/combat cases PASS. Native/art/UI gates remain OPEN; no balance or RNG changes.
