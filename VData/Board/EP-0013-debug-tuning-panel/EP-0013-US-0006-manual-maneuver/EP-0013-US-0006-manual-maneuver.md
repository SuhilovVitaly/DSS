---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0006-manual-maneuver
stage: done
---

# Маневрирование

Contract and acceptance: [Documentation](../Documentation.md). All supplied revision2 criteria for US6 apply. Final US7 owns project-wide documentation and Graphify, including original US5-TK5.

Automated/self-review complete: AC1/AC2 shared251 Motion tests and Engine
analytical cases/caps/partitions; AC3 both orders, cycle/Approach/evasion cancellation;
AC4/AC5 exact command counts/aliases/opposite stacks/10s; AC6 Shift/manual replacement;
AC7 real PlayerShipOnly and combat; AC8 S/Ctrl+S/Ctrl+C/E/Z/C and actual hints;
AC9 strict schema2/v1 warning migration; AC10 damage, recovery, focus resync and
save/load input release. Engine304/304, Client202/202 before hints; hints133/133,
build0warnings/errors, scoped format PASS. AC11 is covered by the five-second
Engine/forecast boundary and partition comparison in SpeedHoldEngineTests.
Final real OpenGL managed-input evidence: RU1× and EN1.5× PASS; physical input,
human/audio acceptance remains OPEN. See [final evidence](../FinalValidation.md).
Current stage denotes implementation.

## Публикация

Каждый завершённый тикет оформляется отдельным коммитом. После каждого коммита сразу выполняется push. Перед push проверяются целевая ветка и remote, после push — совпадение SHA удалённой ветки с локальным HEAD. Переход к следующему тикету — после подтверждённой публикации; сбой push фиксируется как незавершённая публикация.
