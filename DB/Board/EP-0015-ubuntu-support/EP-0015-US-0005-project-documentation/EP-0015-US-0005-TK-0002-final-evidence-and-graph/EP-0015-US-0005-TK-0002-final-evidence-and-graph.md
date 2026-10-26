---
epic: EP-0015-ubuntu-support
story: EP-0015-US-0005-project-documentation
ticket: EP-0015-US-0005-TK-0002-final-evidence-and-graph
title: "Финальный evidence и граф"
stage: draft
acceptance_status: open
execution_status: implemented
layer: docs
depends_on: [EP-0015-US-0005-TK-0001-project-documentation]
serves: [AC-02, AC-03]
created: 2026-10-26
---

# Финальный evidence и граф

> Текущий сводный результат, checkpoints и открытые gates: [FinalValidation](../../FinalValidation.md); публикация: [PublicationStatus](../../PublicationStatus.md). Записи отдельных прогонов ниже — история evidence, а не обещание текущего PASS.

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

## Implementation and evidence

2026-10-26: Board synchronized to5 stories/16 tickets, explicit execution/acceptance statuses, ticket-to-SHA ledger and dated documentation impact. FinalValidation.md and ExecutionStatus.md distinguish current Windows results, earlier CI source states, pending CI and manual gates. Documentation corpus remains Markdown-only;84 Board+Documentation files have0 broken local links and ticket/story DAG is acyclic (20261026-final-graph/audit.py and final-audit.json).

Graph workflow includes EP-0015. Three required host semantic agents re-read63 current source files, producing109 nodes/120 edges/3 hyperedges with source hashes and selective semantic coverage. Full selected structural corpus:869 code,372 Markdown. Output and manifests under20261026-final-graph/graph; graph navigation is not runtime/acceptance evidence. Parser recovery, pruned references and aggregation limitations are recorded in FinalValidation.md.

Current Client full regression2349/2349 and Engine fixed-source full regression2486/2486 PASS on Windows. The additional full corpus with actual SDK8.0.427 PASS5429/5429, exact discovery/TRX coverage and0 failures/skips. Both Linux Client corpora PASS2349/2349; native audio Play/Stop6/6 and no-device1/1 on both Ubuntu versions. Final5ac2de3d package runs in clean Ubuntu24 without system .NET through X11 and real Weston/XWayland,13 GPU frames and production save/load each; release-audit.json and release archive retained under20261026-final-linux-native. This ticket publishes a truthful audit and current documentation; it does not mark the epic or human/product acceptance complete. Cleanup evidence is recorded under20261026-cleanup; final remote SHA is verified after commit/push and entered into the excluded publication ledger.
