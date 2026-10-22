---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0003-save-and-startup
stage: done
---

# Сейв и старт

Contract and acceptance: [Documentation](../Documentation.md). All supplied revision2 criteria for US3 apply. Final US7 owns project-wide documentation and Graphify, including original US5-TK5.

## Story verification2026-10-22

AC1/AC2: real Save19 midflight roundtrip, Save18 migration with/without flights,
strict integrity and atomic failed-load rollback (Engine persistence492/492 plus
focused integrity7/7; Client42/42). AC3–AC5: shared strict schema1 startup file,
catalog defaults, logged/snapshot rejection, saved overlay then normal file command
with retiming, actual LocalClient first snapshot (final Engine128/128, Contracts264/264,
Client38/38). Solution builds0warnings/errors, scoped format/diff PASS.
Self-review completed; native panel presentation is not claimed.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
