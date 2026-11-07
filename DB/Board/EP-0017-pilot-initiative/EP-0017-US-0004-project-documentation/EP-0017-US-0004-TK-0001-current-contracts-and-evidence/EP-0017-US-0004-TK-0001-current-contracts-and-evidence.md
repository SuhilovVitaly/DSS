---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0004-project-documentation
ticket: EP-0017-US-0004-TK-0001-current-contracts-and-evidence
title: Синхронизация контрактов, навигации и evidence
stage: approved
layer: tooling
approval_kind: execution-mandate
depends_on: [EP-0017-US-0003-TK-0005-export-and-documentation]
---

# Синхронизация контрактов, навигации и evidence

## Why / Decisions

Закрывает AC-01…04 родительской истории и обязательный раздел 6 промта исполнения. Выполняется после всех функциональных тикетов.

## Code context / Scope

Разрешены релевантные Markdown во всём репозитории, карточки и отчёты этого Board; tools/graphify/rebuild.py только если нужен охват EP-0017. Перечень файлов уточняется инвентаризацией до записи. Production simulation не меняется. Исторический лимит пяти файлов расширен по разделу 6 промта для полного охвата документации.

## Implementation steps

1. По итоговому diff найти затронутые API, поля, правила и тексты. Записать inventory document/change/update/check.
2. Синхронизировать текущие контракты и зависимые README/экранные инструкции; historical V4 сохранить и пометить заменённое поведение.
3. Обновить coverage, статусы, точные evidence paths и опубликованные SHA, не выдумывая собственный SHA заранее.
4. Перестроить offline Graphify в абсолютный уникальный workspace validation run; проверить hashes/locations/endpoints и ограничения. Проверить Markdown links/IDs/counts/dependencies, git diff --check и cleanup.

## Tests / Definition of Done

Все AC истории имеют проверяемое evidence; diff/link/content checks проходят, каждый required gate имеет реальный статус. Отдельный commit и немедленный push с проверкой remote. Временные outputs только D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/<run>/; сохранить только evidence отчёта.

## Assumptions / Out of scope

Это execution-added story по полномочию пользователя, не отдельное product approval. Нет перебалансировки, новой симуляции или переписывания historical результатов.
