---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0006-metrics-acceptance-docs
title: Метрики, приёмка и документация
stage: approved
dependencies: [EP-0028-US-0001-hint-framework, EP-0028-US-0002-comms-panel, EP-0028-US-0003-officer-lessons, EP-0028-US-0004-hints-toggle, EP-0028-US-0005-reliable-first-battle]
created: 2026-11-14T15:04:43Z
source_request: "US-0006 Метрики и приёмка: per-step metrics; playtest (small monitor) + docs."
current_review: complete
revision: 1
---

# Метрики, приёмка и документация

[Эпик](../Documentation.md) · [Тикеты](../Tickets.md)

## Входное техническое задание

Из дизайна: локальные события по шагам (показан/выполнен/закрыт/подсказки выключены) через существующую инфраструктуру `TacticalBattleMetrics`; плейтест на меньшем мониторе; документация. Последняя история эпика по [EpicExecutionPrompt.md](../../EpicExecutionPrompt.md) §6.

## User story

Как разработчик, я вижу по локальным файлам метрик, на каком шаге игроки закрывают подсказки или выключают их, чтобы улучшать обучение. Как владелец продукта, я получаю нативную проверку обучения в окне на меньшем мониторе на RU и EN, с включёнными и выключенными подсказками. Документация проекта описывает режим обучения, офицера, формат уроков и настройку так, как они реализованы.

## Acceptance criteria

- **AC-01.** Для боя обучения пишется локальный JSON событий шагов (`shown`, `completed`, `dismissed`, `expired`, `hints-disabled`, `hints-enabled`) рядом с отчётами боя; без персональных данных; ошибки записи не ломают бой.
- **AC-02.** Нативная приёмка на меньшем мониторе (`DSS_VALIDATION_SMALL_MONITOR=1`): RU и EN, подсказки вкл/выкл, журнал H, Tabletop, победа по Enter; результаты и снимки — в артефактах; G-01 (клавиша E) проверена и классифицирована.
- **AC-03.** Документация (`Screens/TacticalBattle.md`, `Screens/Settings.md`, `04-Engineering/TacticalBattle.md`) и Board EP-0028 отражают фактический результат; ссылки и ID проверены.

## Non-goals

Отправка метрик по сети; аналитические дашборды; «Повторить обучение» (D-05).

## Invariants

- Полные `dotnet build DeepSpaceSaga.sln` и `dotnet test DeepSpaceSaga.sln` — один раз после TK-0001 (последний функциональный тикет), падения классифицируются относительно базы (G-03).
- Документация — только `.md` в `DB/Documentation/` по [DocumentationSystem.md](../../../Documentation/00-Process/DocumentationSystem.md).

## Dependencies

Все функциональные истории US-0001…US-0005.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](EP-0028-US-0006-TK-0001-step-metrics/EP-0028-US-0006-TK-0001-step-metrics.md) | Метрики шагов обучения | client | US-0004-TK-0003, US-0003-TK-0005 | AC-01 |
| [TK-0002](EP-0028-US-0006-TK-0002-native-acceptance/EP-0028-US-0006-TK-0002-native-acceptance.md) | Нативная приёмка | verification | TK-0001, US-0005-TK-0002 | AC-02 |
| [TK-0003](EP-0028-US-0006-TK-0003-project-docs/EP-0028-US-0006-TK-0003-project-docs.md) | Документация проекта | documentation | TK-0002 | AC-03 |

Карта принята автоматическим workflow. Слои `verification` и `documentation` не содержат production-кода.

## Gaps and backlog

- G-01 (E): по итогам TK-0002 — тикет в EP-maintenance либо закрытие гэпа.
- Дефекты, найденные в TK-0002, исправляются отдельными тикетами-исправлениями этой истории с собственным коммитом и push.

## Completion evidence

Тест метрик зелёный; полный прогон решения с классификацией; отчёт нативной приёмки со снимками; обновлённые документы и Board.

## Журнал

2026-11-14T15:04:43Z — история создана по утверждённому плану.
