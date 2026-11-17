---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0032-demo-campaign-continuity
story: EP-0032-US-0007-consistent-campaign-actions
title: Согласованные действия и сохранённое состояние кампании
stage: approved
plan_status: approved
priority: P0
dependencies: [EP-0031-US-0005-reduced-demo-contract]
created: 2026-11-16T20:32:46.441Z
implementation_status: implemented
---

# Согласованные действия и сохранённое состояние кампании

## Входное техническое задание

Добавлено по текущему поручению заполнить EP-0032…EP-0041 тикетами и закрыть пробелы первого демо. [Контракт эпика](../Documentation.md).

## User story

Как игрок, я получаю один подтверждённый результат действия компании даже при повторе нажатия или восстановлении соединения. После загрузки журнал обязательств и ресурсы описывают один мир; несовместимое сохранение отклоняется до замены текущей игры.

## Dependencies

- EP-0031-US-0005-reduced-demo-contract — канонический внешний prerequisite.

## Acceptance criteria

- **AC-01.** Новая команда кампании имеет identity, revision, типизированный результат и причину отказа; повтор с тем же payload возвращает исходный receipt, изменённый payload с той же identity отклоняется.
- **AC-02.** Состояние демо, квитанции и версии сохраняются атомарно; Save21 мигрирует без выдачи нового стартового имущества, неподдерживаемая версия не заменяет сессию.
- **AC-03.** Доставка через LocalClient и GameSessionHandle асинхронна; UI не списывает ресурсы по pending и не обращается к Engine из рендера.
- **AC-04.** Новые обязанности обрабатываются теми же календарными границами при обычном кадре и CaptureSaveState; нет параллельной модели мира.

## Non-goals

Полная игра, универсальный квестовый движок и публикация не входят.

## Gaps and backlog

Значения баланса — рабочие assumptions до плейтеста. Implementation NOT STARTED; native/product acceptance OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0032-US-0007-TK-0001-campaign-contract](EP-0032-US-0007-TK-0001-campaign-contract/EP-0032-US-0007-TK-0001-campaign-contract.md) | Команды, receipts и проекция кампании | contracts | EP-0031-US-0005-reduced-demo-contract | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0032-US-0007-TK-0002-campaign-state](EP-0032-US-0007-TK-0002-campaign-state/EP-0032-US-0007-TK-0002-campaign-state.md) | Версионированное состояние и единый commit | engine | EP-0032-US-0007-TK-0001-campaign-contract | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0032-US-0007-TK-0003-campaign-loader](EP-0032-US-0007-TK-0003-campaign-loader/EP-0032-US-0007-TK-0003-campaign-loader.md) | Принятие новой версии сохранения | engine | EP-0032-US-0007-TK-0002-campaign-state | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0032-US-0007-TK-0004-campaign-load](EP-0032-US-0007-TK-0004-campaign-load/EP-0032-US-0007-TK-0004-campaign-load.md) | Безопасная загрузка Save22 и транспорт | local-client | EP-0032-US-0007-TK-0003-campaign-loader | AC-01, AC-02, AC-03, AC-04 |
| 5. [EP-0032-US-0007-TK-0005-campaign-client](EP-0032-US-0007-TK-0005-campaign-client/EP-0032-US-0007-TK-0005-campaign-client.md) | Асинхронные действия без ложного успеха | client | EP-0032-US-0007-TK-0004-campaign-load | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.


## Execution — 2026-11-17

Five foundation tickets implemented. Story-end focused tests: Contracts 3, Engine/LocalClient 10, Client 4 passed. Save22, migrated Save21, receipt replay, failed candidate loading and session lifetime verified. Native/product acceptance OPEN; actual gameplay consumers continue in US-0001 onward. Evidence directory: D:/DeepSpaceSaga/artifacts/validation/ep-0032/20261117-us0007-retry/.
