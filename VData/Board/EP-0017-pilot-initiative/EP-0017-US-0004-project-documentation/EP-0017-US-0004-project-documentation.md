---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0004-project-documentation
title: Актуальная документация инициативной стрельбы
stage: done
approval_kind: execution-mandate
dependencies: [EP-0017-US-0001-pilot-initiative, EP-0017-US-0002-initiative-fire, EP-0017-US-0003-fire-order-presentation]
---

# Актуальная документация инициативной стрельбы

Как игрок и разработчик, я хочу получить согласованные инструкции и текущие контракты нового боя, чтобы пользоваться инициативой без противоречий со старой одновременной моделью.

Источник: раздел 6 [промта исполнения](../../EpicExecutionPrompt.md) и текущее поручение пользователя. История выполняется последней, после всех функциональных историй; tooling export US-0003/TK-0005 не заменяет проверку документации всего проекта.

## Acceptance criteria

- AC-01: по итоговому diff выполнена инвентаризация документов во всём репозитории; таблица document/change/update/check сохранена в Board.
- AC-02: актуальны требования, механики, экраны, архитектура, JSON/RNG/timeline, Save20 boundary, README и руководства рядом с кодом, тестами и tools; исторические V4 evidence сохранены с указателями замены.
- AC-03: Board содержит фактические статусы, coverage, ticket-to-SHA/push, automated/native/product gates; проверены ссылки, IDs, количества и зависимости.
- AC-04: после финальных исходников и текстов перестроен и проверен Graphify; hashes, source locations и ограничения отражены в отчёте; disposable outputs убраны.

## Tickets

- [TK-0001](EP-0017-US-0004-TK-0001-current-contracts-and-evidence/EP-0017-US-0004-TK-0001-current-contracts-and-evidence.md): AC-01…04.

## Execution — 2026-11-07

Техническая реализация и проверки завершены; опубликованные тикеты и evidence: [ExecutionStatus](../ExecutionStatus.md). Self-review не является независимым review. Human/product/balance/physical-input acceptance отдельно остаётся открытой.
