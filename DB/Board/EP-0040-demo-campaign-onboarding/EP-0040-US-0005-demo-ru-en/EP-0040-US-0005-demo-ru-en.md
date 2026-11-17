---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0040-demo-campaign-onboarding
story: EP-0040-US-0005-demo-ru-en
title: Демо полностью доступно на русском и английском
stage: approved
plan_status: approved
priority: P0
dependencies: [EP-0040-US-0001-first-voyage-guidance, EP-0040-US-0002-threat-guidance, EP-0039-US-0003-captain-finale]
created: 2026-11-16T20:32:46.441Z
implementation_status: not-started
---

# Демо полностью доступно на русском и английском

## Входное техническое задание

Добавлено по текущему поручению заполнить EP-0032…EP-0041 тикетами и закрыть пробелы первого демо. [Контракт эпика](../Documentation.md).

## User story

Как игрок, я прохожу всю демо на выбранном русском или английском языке. Условия контрактов, ошибки, сюжет и результаты используют единые названия; смена языка и загрузка не изменяют игровые решения.

## Dependencies

- [EP-0040-US-0001-first-voyage-guidance](../EP-0040-US-0001-first-voyage-guidance/EP-0040-US-0001-first-voyage-guidance.md)
- [EP-0040-US-0002-threat-guidance](../EP-0040-US-0002-threat-guidance/EP-0040-US-0002-threat-guidance.md)
- [EP-0039-US-0003-captain-finale](../../EP-0039-demo-cluster-story/EP-0039-US-0003-captain-finale/EP-0039-US-0003-captain-finale.md)

## Acceptance criteria

- **AC-01.** RU/EN покрывают обязательства, услуги, угрозы, сюжет, обучение, финал, звук и причины отказа; технические ключи не видны игроку.
- **AC-02.** Длинные строки и числовые значения не закрывают действие в меню, журнале и итогах; скролл и клавиатура позволяют прочитать полный текст.
- **AC-03.** Выбор языка сохраняется; после перезапуска и загрузки используются те же ключи и форматирование единиц.
- **AC-04.** Автоматический parity ключей и ручное прохождение обеих локализаций фиксируются раздельно.

## Non-goals

Полная игра, универсальный квестовый движок и публикация не входят.

## Gaps and backlog

Значения баланса — рабочие assumptions до плейтеста. Implementation NOT STARTED; native/product acceptance OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0040-US-0005-TK-0001-locale-parity](EP-0040-US-0005-TK-0001-locale-parity/EP-0040-US-0005-TK-0001-locale-parity.md) | Полные RU/EN ключи демо | content-data | EP-0040-US-0001-TK-0002-guidance-text, EP-0040-US-0002-TK-0002-guidance-text, EP-0039-US-0003-TK-0004-client | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0040-US-0005-TK-0002-locale-layout](EP-0040-US-0005-TK-0002-locale-layout/EP-0040-US-0005-TK-0002-locale-layout.md) | Читаемость и сохранение языка | client | EP-0040-US-0005-TK-0001-locale-parity | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.

