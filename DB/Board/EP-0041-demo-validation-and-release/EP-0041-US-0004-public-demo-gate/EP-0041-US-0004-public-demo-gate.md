---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0041-demo-validation-and-release
story: EP-0041-US-0004-public-demo-gate
title: Приёмка демо на 60–120 минут
stage: approved
plan_status: approved
priority: P1
workstream: cross-cutting
dependencies: [EP-0041-US-0003-release-candidate, EP-0040-US-0003-independent-demo, EP-0039-US-0004-optional-events, EP-0032-US-0006-documentation, EP-0033-US-0006-documentation, EP-0034-US-0007-documentation, EP-0035-US-0006-documentation, EP-0036-US-0005-documentation, EP-0037-US-0005-documentation, EP-0038-US-0004-documentation, EP-0039-US-0006-documentation, EP-0040-US-0004-documentation]
created: 2026-11-16T19:49:59Z
implementation_status: not-started
---

# Приёмка демо на 60–120 минут

## Связь с техническим заданием

Одобренный план, Д11: Сквозная проверка и выпуск демо. [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). Сквозные gates EP-0029, EP-0024/0026 и нового цикла. Это приёмка готовых функций, а не поздняя реализация журналов или persistence.

## User story

Как владелец, я получаю проверенную сборку с завершённой короткой кампанией и явным списком оставшихся ограничений. Выпуск не подменяется прохождением автоматических тестов.

## Зависимости

- [EP-0041-US-0003-release-candidate](../EP-0041-US-0003-release-candidate/EP-0041-US-0003-release-candidate.md)
- [EP-0040-US-0003-independent-demo](../../EP-0040-demo-campaign-onboarding/EP-0040-US-0003-independent-demo/EP-0040-US-0003-independent-demo.md)
- [EP-0039-US-0004-optional-events](../../EP-0039-demo-cluster-story/EP-0039-US-0004-optional-events/EP-0039-US-0004-optional-events.md)
- [EP-0032-US-0006-documentation](../../EP-0032-demo-campaign-continuity/EP-0032-US-0006-documentation/EP-0032-US-0006-documentation.md)
- [EP-0033-US-0006-documentation](../../EP-0033-demo-company-services/EP-0033-US-0006-documentation/EP-0033-US-0006-documentation.md)
- [EP-0034-US-0007-documentation](../../EP-0034-demo-transport-contracts/EP-0034-US-0007-documentation/EP-0034-US-0007-documentation.md)
- [EP-0035-US-0006-documentation](../../EP-0035-demo-pirate-encounters/EP-0035-US-0006-documentation/EP-0035-US-0006-documentation.md)
- [EP-0036-US-0005-documentation](../../EP-0036-demo-bounties-and-loot/EP-0036-US-0005-documentation/EP-0036-US-0005-documentation.md)
- [EP-0037-US-0005-documentation](../../EP-0037-demo-voyage-economy/EP-0037-US-0005-documentation/EP-0037-US-0005-documentation.md)
- [EP-0038-US-0004-documentation](../../EP-0038-demo-pilot-progression/EP-0038-US-0004-documentation/EP-0038-US-0004-documentation.md)
- [EP-0039-US-0006-documentation](../../EP-0039-demo-cluster-story/EP-0039-US-0006-documentation/EP-0039-US-0006-documentation.md)
- [EP-0040-US-0004-documentation](../../EP-0040-demo-campaign-onboarding/EP-0040-US-0004-documentation/EP-0040-US-0004-documentation.md)

## Completion evidence

- **AC-01.** Новый игрок достигает финала за целевые 60–120 минут без обязательного однообразного гринда и покупает полезное улучшение.
- **AC-02.** Native производительность измерена на согласованном оборудовании относительно требования 80 FPS; отклонения явно разрешены владельцем либо исправлены.
- **AC-03.** Подтверждены критические сохранения, локализация, звук, отсутствие тупиков и отдельная продуктовая приёмка.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

Запуск только на проверенном меньшем мониторе с DSS_VALIDATION_SMALL_MONITOR=1; без такой возможности headless не закрывает native gate. Публикация требует отдельного поручения.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Пользователь одобрил общий план и поручил материализацию. Детализация этой истории подготовлена агентом в рамках одобренного объёма; отдельное human approval формулировок и допущений не заявляется. Исторически story detail был draft; текущий workflow status указан в frontmatter и новой карте ниже. Это не препятствует завершению запрошенного создания документов и не разрешает реализацию.

- 2026-11-16T19:49:59Z — время фиксации запроса в Board, не доказанное время отправки: «План одобряю. Оформи его в Board: создай необходимые эпики и пользовательские истории с приоритетами и зависимостями, учитывая уже сделанное в EP-0021 и EP-0029. Пока без тикетов и реализации.».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась. При будущей подготовке соблюдать предел пяти тикетов на историю; если детализация не помещается, сначала разделить story.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN. Создание Markdown не закрывает эти gates.


## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Новый игрок достигает финала за целевые 60–120 минут без обязательного однообразного гринда и покупает полезное улучшение.
- **AC-02.** Native производительность измерена на согласованном оборудовании относительно требования 80 FPS; отклонения явно разрешены владельцем либо исправлены.
- **AC-03.** Подтверждены критические сохранения, локализация, звук, отсутствие тупиков и отдельная продуктовая приёмка.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0041-US-0003-release-candidate](../EP-0041-US-0003-release-candidate/EP-0041-US-0003-release-candidate.md)
- [EP-0040-US-0003-independent-demo](../../EP-0040-demo-campaign-onboarding/EP-0040-US-0003-independent-demo/EP-0040-US-0003-independent-demo.md)
- [EP-0039-US-0004-optional-events](../../EP-0039-demo-cluster-story/EP-0039-US-0004-optional-events/EP-0039-US-0004-optional-events.md)
- [EP-0032-US-0006-documentation](../../EP-0032-demo-campaign-continuity/EP-0032-US-0006-documentation/EP-0032-US-0006-documentation.md)
- [EP-0033-US-0006-documentation](../../EP-0033-demo-company-services/EP-0033-US-0006-documentation/EP-0033-US-0006-documentation.md)
- [EP-0034-US-0007-documentation](../../EP-0034-demo-transport-contracts/EP-0034-US-0007-documentation/EP-0034-US-0007-documentation.md)
- [EP-0035-US-0006-documentation](../../EP-0035-demo-pirate-encounters/EP-0035-US-0006-documentation/EP-0035-US-0006-documentation.md)
- [EP-0036-US-0005-documentation](../../EP-0036-demo-bounties-and-loot/EP-0036-US-0005-documentation/EP-0036-US-0005-documentation.md)
- [EP-0037-US-0005-documentation](../../EP-0037-demo-voyage-economy/EP-0037-US-0005-documentation/EP-0037-US-0005-documentation.md)
- [EP-0038-US-0004-documentation](../../EP-0038-demo-pilot-progression/EP-0038-US-0004-documentation/EP-0038-US-0004-documentation.md)
- [EP-0039-US-0006-documentation](../../EP-0039-demo-cluster-story/EP-0039-US-0006-documentation/EP-0039-US-0006-documentation.md)
- [EP-0040-US-0004-documentation](../../EP-0040-demo-campaign-onboarding/EP-0040-US-0004-documentation/EP-0040-US-0004-documentation.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0041-US-0004-TK-0001-release-matrix](EP-0041-US-0004-TK-0001-release-matrix/EP-0041-US-0004-TK-0001-release-matrix.md) | Полнота технической приёмки | tooling | EP-0041-US-0003-TK-0002-package-smoke, EP-0040-US-0003-TK-0001-independent-protocol, EP-0039-US-0004-TK-0004-client, EP-0032-US-0006-TK-0002-board-status, EP-0033-US-0006-TK-0002-board-status, EP-0034-US-0007-TK-0002-board-status, EP-0035-US-0006-TK-0002-board-status, EP-0036-US-0005-TK-0002-board-status, EP-0037-US-0005-TK-0002-board-status, EP-0038-US-0004-TK-0002-board-status, EP-0039-US-0006-TK-0002-board-status, EP-0040-US-0004-TK-0002-board-status | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0041-US-0004-TK-0002-native-performance](EP-0041-US-0004-TK-0002-native-performance/EP-0041-US-0004-TK-0002-native-performance.md) | Нативная производительность и окружение | tooling | EP-0041-US-0004-TK-0001-release-matrix | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0041-US-0004-TK-0003-owner-demo-acceptance](EP-0041-US-0004-TK-0003-owner-demo-acceptance/EP-0041-US-0004-TK-0003-owner-demo-acceptance.md) | Приёмка законченной демо 60–120 минут | tooling | EP-0041-US-0004-TK-0002-native-performance | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.

