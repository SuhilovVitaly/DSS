---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0005-project-documentation
ticket: EP-0015-US-0005-TK-0001-project-documentation
title: "Документация проекта"
stage: draft
layer: docs
depends_on: [EP-0015-US-0004-TK-0003-linux-documentation]
serves: [AC-01]
created: 2026-10-26
---

# Документация проекта

## Why

Обязательный завершающий результат раздела 6 EpicExecutionPrompt.

## Code context

Документация DB/Documentation, README и документы рядом с кодом/инструментами, Board этого и связанных эпиков; tools/graphify/rebuild.py для действующего offline rebuild. Точный impact ledger заполняется по финальному diff перед изменением. Production API не меняется.

## Implementation steps

Инвентаризировать итоговый diff и зависимости документов. Обновить актуальные требования, архитектуру, пути данных/сохранений/debug, шрифты, audio, экраны и инструменты; README и asset workflow. Сохранить датированную историю. Проверить источники и локальные ссылки. Scope определяется итоговым impact ledger; расширение записывать перед правкой.

## Tests and invariants

Content/source verification, локальные Markdown-ссылки, ID/DAG/count audit, git diff --check; для графа manifest/source hashes и endpoints. Automated, native, physical-input, audio и human gates учитываются отдельно. Нельзя выдумывать PASS или независимое review.

## Definition of Done

Все указанные AC подтверждены реальным evidence; собственная проверка обозначена self-review. Отдельный коммит с полным ID, немедленный push и remote verification. Артефакты только в D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/<run>/.
