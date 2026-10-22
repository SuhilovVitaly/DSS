---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0005-tactical-panel
stage: done
---

# Панель

Contract and acceptance: [Documentation](../Documentation.md). All supplied revision2 criteria for US5 apply. Final US7 owns project-wide documentation and Graphify, including original US5-TK5.

Automated story coverage: AC1/AC3/AC5 input command/capture tests at1/1.5;
AC2 catalog25 rows/five sections/scroll/scales; AC4 boundary/rejection/default
tests; AC6 startup reason; AC7 strict RU/EN coverage; AC8 zero managed bytes
with actual RU/EN status+render over100 frames. Latest affected regression126/126,
build0warnings/errors, scoped format PASS. Self-review complete. Native/human
acceptance remains open; final new maneuver controls are covered by US6.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
