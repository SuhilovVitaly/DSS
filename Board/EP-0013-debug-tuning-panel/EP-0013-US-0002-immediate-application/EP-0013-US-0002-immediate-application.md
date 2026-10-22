---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0002-immediate-application
stage: done
---

# Немедленное применение

Contract and acceptance: [Documentation](../Documentation.md). All supplied revision2 criteria for US2 apply. Final US7 owns project-wide documentation and Graphify, including original US5-TK5.

## Story verification 2026-10-22

AC1/AC2: current-pose speed clamp, turn and NPC limits, evasion phase/return
replan (ship regression227/227). AC3/AC4: live flight endpoint/fuse/radius and
foreign privacy, simultaneous CM-first detonation (projectile regression107/107).
AC5/AC6: both launcher reload classes, exactly-once cues, seeded partition
equivalence (reload regression118/118). Engine builds0warnings/errors; scoped
format/diff checks PASS. Self-review complete. US6 will extend ship retiming to
its new speed-hold channel; Save/Load of tuned continuations is explicitly US3.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
