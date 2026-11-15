---
description: Show the current EP-0029 position (ticket, commit, in-flight, next)
allowed-tools: Read, Bash(git log:*), Bash(git status:*)
---

Report where the EP-0029 work currently stands.

1. Read `D:/DeepSpaceSaga/artifacts/validation/ep-0029/STATUS.md` if it exists.
2. Run `git -C D:/DeepSpaceSaga/DSS log -1 --format='%h %s'` and `git -C D:/DeepSpaceSaga/DSS status -sb`.
3. Output a compact block:
   - Epic / story / current ticket(s)
   - Last pushed commit (prefer the live git value; say if the status file is stale)
   - Work currently in flight
   - Next tickets
   - Open gates / known reds
4. Do not modify any file. If the status file is missing, derive what you can from git and say the file is absent.
