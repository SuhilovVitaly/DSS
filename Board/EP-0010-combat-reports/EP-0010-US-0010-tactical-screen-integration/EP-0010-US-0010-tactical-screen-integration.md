---
epic: EP-0010-combat-reports
story: EP-0010-US-0010-tactical-screen-integration
title: "Правая колонка и жизненный цикл экрана"
stage: draft
dependencies: ["EP-0010-US-0009-report-accordion"]
created: 2026-10-16
source_request: "Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами."
current_review: complete
validation_status: automated-passed
revision: 1
---

# Правая колонка и жизненный цикл экрана

## Входное техническое задание

[Каноническое ТЗ](../../../Documentation/02-FirstRelease/TechnicalTasks/TacticalCombatReports.md): R01, R02, R03, R06, R13, R20, R21, R22. [Эпик](../EP-0010-combat-reports.md). Документ создан 2026-10-16; поручение — сохранение и декомпозиция, execution NOT STARTED.

## User story

Как игрок, я вижу доклады под информацией о кораблях на тактическом экране. Панель остаётся управляемой при сворачивании блоков, смене масштаба и загрузке.

## Acceptance criteria

- **US10-AC1:** Общий layout размещает Object Info и reports в одной правой колонке с одинаковой шириной, фактическим bottom и ограничением высоты; нижние controls и journal не перекрыты.
- **US10-AC2:** GameSessionScreen потребляет inbox вне Draw, подготавливает assets и рендерит готовое состояние; input priority исключает команды/камеру/zoom из panel input; store не зависит от выбора карты.
- **US10-AC3:** NewGame/успешный Load заменяют store/buffer и освобождают ресурсы; ошибка Load сохраняет историю; old session callbacks не попадают в new session; Defeat не ждёт карточку.

## Non-goals

Изменение боевого баланса/AI, новая модель обнаружения, озвучка, автопауза и выполнение соседних историй. Не считать planning review доказательством работы продукта.

## Dependencies

EP-0010-US-0009-report-accordion Ticket DAG ниже имеет приоритет над порядком перечисления файлов.

## Grounding и invariants

Общий текущий [контекст кода](../Documentation.md#grounding). Engine определяет боевые исходы; UI не меняет RNG/часы. Доклады используют физическое время. Скрытые сведения не попадают в DTO. Модульные повреждения не заменяются Hull HP. Технические A01–A10 ТЗ — явные assumptions; это не придуманные ответы пользователя.

## Planned ticket map

| Порядок | Ticket | Layer | Depends on | Serves |
|---|---|---|---|---|
| 1 | [EP-0010-US-0010-TK-0001-right-column-layout](EP-0010-US-0010-TK-0001-right-column-layout/EP-0010-US-0010-TK-0001-right-column-layout.md) — Общая геометрия Object Info и докладов | client | EP-0010-US-0009-TK-0002-accordion-input | US10-AC1 |
| 2 | [EP-0010-US-0010-TK-0002-screen-render-and-input](EP-0010-US-0010-TK-0002-screen-render-and-input/EP-0010-US-0010-TK-0002-screen-render-and-input.md) — Подключение докладов к тактическому экрану | client | EP-0010-US-0010-TK-0001-right-column-layout | US10-AC2 |
| 3 | [EP-0010-US-0010-TK-0003-session-reset-and-defeat](EP-0010-US-0010-TK-0003-session-reset-and-defeat/EP-0010-US-0010-TK-0003-session-reset-and-defeat.md) — Чистая смена сессии и приоритет Defeat | client | EP-0010-US-0010-TK-0002-screen-render-and-input | US10-AC3 |

## Gaps and backlog

Блокирующих вопросов для планирования нет. Все новые API/файлы — proposed, наличие ещё не означает реализацию. Для art/native/performance человеческая приёмка OPEN. Перед execution сверить HEAD и существующее рабочее дерево; при изменении контекста обновить карту и allowlist до редактирования кода. Общее ограничение 5 файлов на тикет,1 production layer+matching tests; сгенерированные evidence/assets перечислены в соответствующем scope.

## Review log

- 2026-10-16: критерии и dependency map созданы; все тикеты draft/planned. User instruction: «Сохрани это ТЗ в документацию и создай по нему эпик в Board с пользовательскими историями и тикетами.»

## Implementation self-review - 2026-10-16

Right-column geometry, pre-render resource/drain hook, first input refusal and session lifecycle integrated.67 report/reliability cases PASS. Reviewed six scale/viewports, actual ObjectInfo bottom, journal/footer separation, command/speed/camera isolation, modal history, late delivery and defeat priority. Native matrix/FPS remain US0011 OPEN.
