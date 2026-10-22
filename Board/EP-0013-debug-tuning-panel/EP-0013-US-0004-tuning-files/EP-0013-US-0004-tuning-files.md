---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0004-tuning-files
stage: in-progress
---

# Файлы

Contract and acceptance: [Documentation](../Documentation.md). All supplied revision2 criteria for US4 apply. Final US7 owns project-wide documentation and Graphify, including original US5-TK5.

## Story verification2026-10-22

AC1/AC2: full25-value confirmed file, strict250ms coalescing, atomic replacement,
failure/retry and final/session-swap flush (Client61/61; final lifecycle56/56).
AC3: app-start original-catalog reference export (Client44/44). Builds/format/scoped
diff PASS. Both file tickets are implemented; AC4's reset button and confirmed
file deletion are intentionally covered by existing US5-TK3. Source specification
places that integration after this story; keep this story's final acceptance open
until US5-TK3, without inventing a duplicate reset ticket or claiming a UI test.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
