---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0004-tuning-files
stage: done
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

US5-TK3 now closes automated AC4: two-click reset sends all25 defaults as one
command, matching executed receipt+confirmed defaults delete current JSON,
rejection preserves it, later edit wins, reference JSON is byte-identical.
Client regression116/116 and final16/16; solution build0warnings/errors, format
PASS. Self-review/automated story criteria complete; native/human acceptance open.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
