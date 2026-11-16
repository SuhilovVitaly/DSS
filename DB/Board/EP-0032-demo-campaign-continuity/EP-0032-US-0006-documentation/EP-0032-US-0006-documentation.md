---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0032-demo-campaign-continuity
story: EP-0032-US-0006-documentation
title: Документация проекта по итогам эпика
stage: approved
plan_status: approved
priority: P0
workstream: feature
dependencies: [EP-0032-US-0001-campaign-roundtrip, EP-0032-US-0002-permanent-pilot-loss, EP-0032-US-0003-persistent-outcomes, EP-0032-US-0004-demo-campaign-start, EP-0032-US-0005-autosave-and-continue, EP-0032-US-0007-consistent-campaign-actions]
created: 2026-11-16T20:02:53Z
implementation_status: not-started
---

# Документация проекта по итогам эпика

## Связь с техническим заданием

Одобренный план, Д2: обязательная завершающая история (DB/Board/EpicExecutionPrompt.md, раздел 6). [Контракт и grounding эпика](../Documentation.md), [общая дорожная карта](../../README.md#дорожная-карта-первой-демо-версии). История добавлена при сверке Board с сокращённым планом 2026-11-16: у этого результата плана не было владельца.

## User story

Как исполнитель следующих эпиков и владелец демо, я читаю актуальные требования, механики и экраны, которые описывают фактический результат этого эпика. Board эпика показывает реальные статусы историй, проверки и ограничения. Прежние описания, заменённые этим эпиком, помечены ссылкой на новый контракт, а не переписаны задним числом.

## Зависимости

- [EP-0032-US-0001-campaign-roundtrip](../EP-0032-US-0001-campaign-roundtrip/EP-0032-US-0001-campaign-roundtrip.md)
- [EP-0032-US-0002-permanent-pilot-loss](../EP-0032-US-0002-permanent-pilot-loss/EP-0032-US-0002-permanent-pilot-loss.md)
- [EP-0032-US-0003-persistent-outcomes](../EP-0032-US-0003-persistent-outcomes/EP-0032-US-0003-persistent-outcomes.md)
- [EP-0032-US-0004-demo-campaign-start](../EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md)
- [EP-0032-US-0005-autosave-and-continue](../EP-0032-US-0005-autosave-and-continue/EP-0032-US-0005-autosave-and-continue.md)
- [EP-0032-US-0007-consistent-campaign-actions](../EP-0032-US-0007-consistent-campaign-actions/EP-0032-US-0007-consistent-campaign-actions.md)

## Completion evidence

- **AC-01.** Инвентаризация влияния: по итоговому diff, контрактам, командам, конфигурации и версиям схем перечислены все затронутые документы с результатом проверки.
- **AC-02.** Обновлены канонические документы DB/Documentation (требования, механики, экраны, сохранения) и затронутые документы вне Documentation; исторические отчёты сохранены с пометкой о замене.
- **AC-03.** Board эпика, реестр DB/Board/README.md и DemoPlan синхронизированы; ссылки, ID и количества проверены; Graphify перестроен либо помечен NOT RUN с причиной.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Ограничения и gaps

История выполняется последней в эпике и зависит от всех его функциональных историй. Не заменяет документационные AC внутри функциональных историй.

Численные настройки и продуктовые допущения не являются согласованными значениями без отдельной фиксации. Engine остаётся владельцем мира, времени, денег и исходов; Client показывает подтверждённое состояние. Прежняя граница «без тикетов» заменена текущим поручением; декомпозиция и allowlist приведены ниже.

## Статус и review evidence

Добавлена агентом по поручению владельца найти и добавить недостающие истории. Отдельное human approval формулировок не заявляется: story stage — draft, plan_status — approved (сокращённый план одобрен ранее).

- 2026-11-16T20:02:53Z — время фиксации поручения в Board, не доказанное время отправки: «на основе этого пройди по созданными эпикам и сторисам в  D:\DeepSpaceSaga\DSS\DB\Board\ и реши каких сторисов или эпиков нам не хватает. там же добавь недостающие».
- Проверка автора: результат демонстрируем, зависимости указаны, completion evidence наблюдаемо; тикетная декомпозиция не выполнялась.
- Implementation: NOT STARTED. Automated/runtime/native/product acceptance: NOT RUN / OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Входное техническое задание

пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо Связь с сокращённым демо-контрактом и исходные критерии сохранены выше.

## Acceptance criteria

- **AC-01.** Инвентаризация влияния: по итоговому diff, контрактам, командам, конфигурации и версиям схем перечислены все затронутые документы с результатом проверки.
- **AC-02.** Обновлены канонические документы DB/Documentation (требования, механики, экраны, сохранения) и затронутые документы вне Documentation; исторические отчёты сохранены с пометкой о замене.
- **AC-03.** Board эпика, реестр DB/Board/README.md и DemoPlan синхронизированы; ссылки, ID и количества проверены; Graphify перестроен либо помечен NOT RUN с причиной.
- **AC-04.** Представление результата, причины отказа, учёт и сохранение проверены в применимых пользовательских путях; присутствие DTO или теста не считается доказательством приёмки.

## Non-goals

Полный продукт, новая верфь, captive/VIP механики, фракции, universal quests и публикация. Нельзя закрыть product acceptance только наличием кода.

## Dependencies

- [EP-0032-US-0001-campaign-roundtrip](../EP-0032-US-0001-campaign-roundtrip/EP-0032-US-0001-campaign-roundtrip.md)
- [EP-0032-US-0002-permanent-pilot-loss](../EP-0032-US-0002-permanent-pilot-loss/EP-0032-US-0002-permanent-pilot-loss.md)
- [EP-0032-US-0003-persistent-outcomes](../EP-0032-US-0003-persistent-outcomes/EP-0032-US-0003-persistent-outcomes.md)
- [EP-0032-US-0004-demo-campaign-start](../EP-0032-US-0004-demo-campaign-start/EP-0032-US-0004-demo-campaign-start.md)
- [EP-0032-US-0005-autosave-and-continue](../EP-0032-US-0005-autosave-and-continue/EP-0032-US-0005-autosave-and-continue.md)
- [EP-0032-US-0007-consistent-campaign-actions](../EP-0032-US-0007-consistent-campaign-actions/EP-0032-US-0007-consistent-campaign-actions.md)

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0032-US-0006-TK-0001-current-contracts](EP-0032-US-0006-TK-0001-current-contracts/EP-0032-US-0006-TK-0001-current-contracts.md) | Актуальные механики и технические контракты | tooling | EP-0032-US-0001-TK-0003-client, EP-0032-US-0002-TK-0003-client, EP-0032-US-0003-TK-0004-terminal-save-policy, EP-0032-US-0004-TK-0004-client, EP-0032-US-0005-TK-0002-continue-menu, EP-0032-US-0007-TK-0005-campaign-client | AC-01, AC-02, AC-04 |
| 2. [EP-0032-US-0006-TK-0002-board-status](EP-0032-US-0006-TK-0002-board-status/EP-0032-US-0006-TK-0002-board-status.md) | Board, реестр и состояние демо | tooling | EP-0032-US-0006-TK-0001-current-contracts | AC-01, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.

