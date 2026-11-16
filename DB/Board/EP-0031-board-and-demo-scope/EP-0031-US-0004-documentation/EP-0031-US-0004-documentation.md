---
epic: EP-0031-board-and-demo-scope
story: EP-0031-US-0004-documentation
title: Документация проекта по итогам эпика
stage: approved
dependencies: [EP-0031-US-0001-epic-registry, EP-0031-US-0002-stale-epic-headers, EP-0031-US-0003-demo-boundary]
created: 2026-11-16T19:36:44Z
source_request: "Обязательная последняя история документации (DB/Board/EpicExecutionPrompt.md, раздел 6)"
current_review: complete
revision: 1
---

# Документация проекта по итогам эпика

## Входное техническое задание

Обязательная завершающая история каждого эпика: инвентаризация влияния,
навигация, Board и evidence.

## User story

Как читатель документации, я попадаю из `DB/Documentation/README.md` в границу
демо и в реестр Board. Все ссылки, добавленные эпиком, работают. Board эпика
отражает фактический статус тикетов, а инвентаризация показывает, какие документы
проверены и почему остальные не менялись.

## Acceptance criteria

- **AC-01.** `DocumentationImpact.md` эпика перечисляет документы, изменение и результат проверки.
- **AC-02.** `DB/Documentation/README.md` ссылается на границу демо и на реестр Board.
- **AC-03.** Все относительные ссылки в изменённых файлах существуют; `Documentation.md` и `Tickets.md` эпика синхронизированы; Graphify — NOT RUN с причиной.

## Non-goals

Перестройка Graphify; push.

## Dependencies

US-0001, US-0002, US-0003.

## Approved ticket map

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](EP-0031-US-0004-TK-0001-docs-impact-and-links/EP-0031-US-0004-TK-0001-docs-impact-and-links.md) | Инвентаризация и навигация | documentation | все предыдущие | AC-01, AC-02, AC-03 |

## Gaps and backlog

Graphify rebuild — при следующем эпике, меняющем код.
