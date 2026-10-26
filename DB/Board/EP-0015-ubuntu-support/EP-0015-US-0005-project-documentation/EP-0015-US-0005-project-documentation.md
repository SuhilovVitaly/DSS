---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0005-project-documentation
title: "Актуальная документация поддержки Ubuntu"
stage: draft
acceptance_status: open
execution_status: implemented_validation_open
dependencies: [EP-0015-US-0001, EP-0015-US-0002, EP-0015-US-0003, EP-0015-US-0004]
created: 2026-10-26
---

# Актуальная документация поддержки Ubuntu

## User story

Как разработчик и игрок, я читаю согласованное описание фактически реализованных путей, шрифтов, звука, сборки и ограничений Ubuntu. Могу отличить проверенные результаты от оставшихся native/human gates и найти публикацию каждого тикета.

## Acceptance criteria

- **AC-01.** Итоговый diff сопоставлен со всеми релевантными документами: требования, архитектура, экраны, сохранения, конфигурация, README, инструменты и asset workflow. Обновлены текущие контракты; история не выдана за новые проверки.
- **AC-02.** Board содержит карту покрытия, зависимости, реальные результаты, остаточные gates и ticket-to-SHA с remote verification. Ссылки, ID, количество и DAG проверены.
- **AC-03.** После финального кода/текстов перестроен и проверен Graphify с актуальными hashes. Временные task-owned outputs убраны; нужное evidence находится только в workspace artifacts.

## Dependencies

Все четыре функциональные истории; выполняется последней по разделу 6 [EpicExecutionPrompt](../../EpicExecutionPrompt.md).

## Ticket map

1. [Документация проекта](EP-0015-US-0005-TK-0001-project-documentation/EP-0015-US-0005-TK-0001-project-documentation.md): AC-01.
2. [Финальный evidence и граф](EP-0015-US-0005-TK-0002-final-evidence-and-graph/EP-0015-US-0005-TK-0002-final-evidence-and-graph.md): AC-02, AC-03.

## Decisions and gaps

Создание и выполнение разрешены прямым запросом пользователя 2026-10-26. Это разрешение исполнения draft, а не заявление о человеческой приёмке. Native Ubuntu/Windows, физический ввод и слышимость звука требуют отдельных доказательств.
