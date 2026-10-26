---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0005-project-documentation
ticket: EP-0015-US-0005-TK-0002-final-evidence-and-graph
title: "Финальный evidence и граф"
stage: draft
layer: docs
depends_on: [EP-0015-US-0005-TK-0001-project-documentation]
serves: [AC-02, AC-03]
created: 2026-10-26
---

# Финальный evidence и граф

## Why

Обязательный завершающий результат раздела 6 EpicExecutionPrompt.

## Code context

Документация DB/Documentation, README и документы рядом с кодом/инструментами, Board этого и связанных эпиков; tools/graphify/rebuild.py для действующего offline rebuild. Точный impact ledger заполняется по финальному diff перед изменением. Production API не меняется.

## Implementation steps

Сверить все AC, тесты, native gates, статусы и опубликованные SHA. Перестроить tools/graphify/rebuild.py после финального кода и текстов; проверить sources, hashes, endpoints и ссылки. Удалить task-owned disposable outputs, сохранить referenced evidence. Проверить чистоту собственных изменений и remote SHA после commit/push.

## Tests and invariants

Content/source verification, локальные Markdown-ссылки, ID/DAG/count audit, git diff --check; для графа manifest/source hashes и endpoints. Automated, native, physical-input, audio и human gates учитываются отдельно. Нельзя выдумывать PASS или независимое review.

## Definition of Done

Все указанные AC подтверждены реальным evidence; собственная проверка обозначена self-review. Отдельный коммит с полным ID, немедленный push и remote verification. Артефакты только в D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/<run>/.
