---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0009-project-documentation
ticket: EP-0004-US-0009-TK-0002-final-evidence-and-graph
title: Итоговые evidence, Board и граф
stage: approved
layer: documentation-tooling
depends_on: [EP-0004-US-0009-TK-0001-sync-project-docs]
serves: [AC-0003]
---

# Итоговые evidence, Board и граф

## Code context и шаги

Scope: карточки и индексы EP-0004, ImplementationStatus.md, итоговый технический отчёт Documentation, документация Graphify и его производные артефакты. Перед изменениями записать точный перечень путей.

Сверить статусы/зависимости/покрытие/количества и таблицу `тикет → SHA → push`. Проверить ссылки, ID и отсутствие stale references. Перестроить граф после окончательных изменений кода и текстов, проверить непустой результат, источники, dangling references и ограничения AST. Медиа игры исключить. Сохранить команды воспроизведения и provenance.

## Проверка и Definition of Done

Automated и native результаты перечислены раздельно и подтверждены фактическим запуском; невыполненные gates имеют NOT RUN и открытую приёмку. `git diff --check` проходит, все ссылки разрешаются. Проведён self-review. Отдельный коммит с полным ID опубликован, собственный SHA проверен после создания средствами Git. Итоговый Git status не содержит неопубликованных изменений эпика; посторонняя работа сохранена. Если код пришлось исправить, сначала завершить исправление и проверки, затем повторить синхронизацию и перестроение графа.
