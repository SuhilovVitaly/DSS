---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0002-immediate-application
stage: draft
---

# Немедленное применение

Contract and acceptance: [Documentation](../Documentation.md). All supplied revision2 criteria for US2 apply. Final US7 owns project-wide documentation and Graphify, including original US5-TK5.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
