---
epic: EP-0023-pilot-card-commands
story: EP-0023-US-0008-project-documentation
title: Документация экрана боя и скинов
stage: draft
dependencies: [EP-0023-US-0001, EP-0023-US-0002, EP-0023-US-0003, EP-0023-US-0004, EP-0023-US-0005, EP-0023-US-0006, EP-0023-US-0007]
created: 2026-11-11T14:30:00Z
source_request: "Обновить документацию экрана боя, инженерные документы и контракт скинов под новую карточку пилота и сжатую правую панель; завести ExecutionStatus эпика."
current_review: epic
revision: 1
---

# Документация экрана боя и скинов

## Входное техническое задание

Документы: `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md`, `DB/Documentation/02-FirstRelease/Screens/ScreenCatalog.md`, `DB/Documentation/04-Engineering/TacticalBattle.md`, `DB/Documentation/04-Engineering/TacticalBattleSkins.md`, `src/DeepSpaceSaga.Client/Skins/README.md`.

## User story

Как разработчик, я хочу, чтобы документация описывала текущий экран боя, чтобы следующие эпики опирались на правильный вид.

## Dependencies

[EP-0023-US-0001](../EP-0023-US-0001-card-column/EP-0023-US-0001-card-column.md), [EP-0023-US-0002](../EP-0023-US-0002-move-in-card/EP-0023-US-0002-move-in-card.md), [EP-0023-US-0003](../EP-0023-US-0003-fire-in-card/EP-0023-US-0003-fire-in-card.md), [EP-0023-US-0004](../EP-0023-US-0004-salvo-in-card/EP-0023-US-0004-salvo-in-card.md), [EP-0023-US-0005](../EP-0023-US-0005-compact-panel/EP-0023-US-0005-compact-panel.md), [EP-0023-US-0006](../EP-0023-US-0006-skins/EP-0023-US-0006-skins.md), [EP-0023-US-0007](../EP-0023-US-0007-view-regression/EP-0023-US-0007-view-regression.md)

## Completion evidence

- Документы описывают колонку, раскрытие, команды в карточке и сжатую панель; ExecutionStatus заведён.

## Acceptance criteria

- **US8-AC1.** Экран боя (`Screens/TacticalBattle.md`) описывает карточку пилота, раскрытие выбранной карточки по фазам, команды только у выбранного дрона, прокрутку колонки и сжатую правую панель; инженерный документ — типы `PilotColumnLayout`, блоки карточки, маршрутизацию команд; контракт скинов — роли UiSpec §9.
- **US8-AC2.** `ExecutionStatus.md` эпика содержит статус каждого тикета, пути artifacts и список пересозданных снимков; ссылки в документах проверены.

## Non-goals

Документация других экранов.

## Invariants

Документы не ссылаются на макеты и временные каталоги.

## Решения и согласование

Решения пользователя — [D-01–D-04](../Documentation.md#решения-пользователя), допущения — [A-01–A-13](../Documentation.md#технические-допущения). Ожидает утверждения эпика пользователем.

## Ticket map

| ID | Название | Слой | Зависимости | Критерии |
|---|---|---|---|---|
| [EP-0023-US-0008-TK-0001-docs](EP-0023-US-0008-TK-0001-docs/EP-0023-US-0008-TK-0001-docs.md) | Документация карточки пилота с командами | docs | `EP-0023-US-0007-TK-0001-snapshots` | US8-AC1, US8-AC2 |
