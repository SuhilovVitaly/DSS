---
source_request: "Декомпозировать EP-0032…EP-0041 в тикеты до первого демо; добавить недостающие истории."
current_review: complete
approval_kind: workflow
revision: 2
epic: EP-0041-demo-validation-and-release
story: EP-0041-US-0007-licensed-audio-content
title: Звуковой комплект первого демо
stage: approved
plan_status: approved
priority: P0
dependencies: []
created: 2026-11-16T20:32:46.441Z
implementation_status: not-started
---

# Звуковой комплект первого демо

## Входное техническое задание

Добавлено по текущему поручению заполнить EP-0032…EP-0041 тикетами и закрыть пробелы первого демо. [Контракт эпика](../Documentation.md).

## User story

Как игрок, я слышу различимые сигналы интерфейса, угроз, боя и наград, а карта и бой имеют собственную музыкальную тему. Комплект поставляется вместе с демо и не зависит от наличия авторских рабочих файлов.

## Dependencies

none

## Acceptance criteria

- **AC-01.** Есть эффекты ui.confirm, dock.complete, encounter.warning, battle.fire, battle.hit, drone.destroyed, reward.paid и темы music.map, music.battle.
- **AC-02.** Для каждого файла известны автор, источник и разрешение на распространение; неопределённые лицензии блокируют включение в RC.
- **AC-03.** Проверены пути, форматы, длительность, отсутствие клиппинга и наличие всех файлов в publish; ручное прослушивание остаётся отдельным gate.
- **AC-04.** Отсутствующий файл даёт диагностируемую тишину, не падение; повтор одного eventId не проигрывает эффект повторно.

## Non-goals

Полная игра, универсальный квестовый движок и публикация не входят.

## Gaps and backlog

Значения баланса — рабочие assumptions до плейтеста. Implementation NOT STARTED; native/product acceptance OPEN.

## Текущая декомпозиция и review log

2026-11-16T20:32:46.441Z — время записи поручения: «пройди все эпики с 32 по 41 и наполни их сторисы тикетами. Если по мере прохождения эпика ты понимаешь что не хватает какой-то стори - ты можешь смело ее добавить уже вместе с тикетами. Реализация с 32 по 41 эпик должно привести нас к состоянию первого демо». Тикеты подготовлены workflow автоматически; отдельное человеческое утверждение допущений не заявляется. Implementation NOT STARTED; automated/native/audio/product gates NOT RUN / OPEN.

## Approved ticket map

| Порядок / ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| 1. [EP-0041-US-0007-TK-0001-audio-source](EP-0041-US-0007-TK-0001-audio-source/EP-0041-US-0007-TK-0001-audio-source.md) | Воспроизводимый исходный звуковой комплект | tooling | none | AC-01, AC-02, AC-03, AC-04 |
| 2. [EP-0041-US-0007-TK-0002-interface-effects](EP-0041-US-0007-TK-0002-interface-effects/EP-0041-US-0007-TK-0002-interface-effects.md) | Звуки интерфейса, стыковки и предупреждения | content-data | EP-0041-US-0007-TK-0001-audio-source | AC-01, AC-02, AC-03, AC-04 |
| 3. [EP-0041-US-0007-TK-0003-combat-effects](EP-0041-US-0007-TK-0003-combat-effects/EP-0041-US-0007-TK-0003-combat-effects.md) | Звуки боя и награды | content-data | EP-0041-US-0007-TK-0002-interface-effects | AC-01, AC-02, AC-03, AC-04 |
| 4. [EP-0041-US-0007-TK-0004-music-and-manifest](EP-0041-US-0007-TK-0004-music-and-manifest/EP-0041-US-0007-TK-0004-music-and-manifest.md) | Музыка, манифест и происхождение файлов | content-data | EP-0041-US-0007-TK-0003-combat-effects | AC-01, AC-02, AC-03, AC-04 |

## Invariants

Engine authority; stable IDs; один receipt/side effect; calendar deadlines; validated Load до замены сессии; отсутствие неявного starter grant. Конкретные ограничения и исключения зафиксированы в тикетах.

## Gaps and backlog

Баланс, художественное качество, реальная производительность, независимый игрок и выпускное решение не установлены планированием. Прежние вопросы о checkpoint, wage, deadline, loot и XP уточнены A-assumptions в соответствующих тикетах, не human decisions. Если allowlist не выдерживает фактический diff, сначала уточнить Board.

