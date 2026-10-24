---
epic: EP-0009-module-combat
story: EP-0009-US-0006-torpedo-fire-modes
ticket: EP-0009-US-0006-TK-0003-mode-selector-ui
title: "mode-selector-ui"
stage: implemented
layer: client
depends_on: ["EP-0009-US-0006-TK-0002-set-mode-command"]
files_touched: "ground-before-edit"
serves: ["US6-AC3"]
created: 2026-10-12
---

# EP-0009-US-0006-TK-0003-mode-selector-ui

STATUS: IMPLEMENTED — headless UI validation complete; game/native NOT RUN by user instruction.

Execution scope before edit (2026-10-12): the launcher panel is the fixed `CommandsPanel` group and `GameSessionScreen` owns its send/enable hooks; `GameSessionHandle` currently has no `ModeId` send path. Add the mode command to the panel, route the three-mode selector through the handle, display authoritative mode/readiness/broken state, localize mode text, and test the paused send and visual status. This necessarily extends the planned files to `GameSessionHandle.cs`, `CommandsPanel.cs` and existing panel fixtures. No Graphify/game/native runs.

The launcher panel now cycles Standard → Maneuverability → Overheat → Standard, sending `ModeId` through `GameSessionHandle`. It blocks repeated sends until authoritative state or rejection, displays localized current mode with remaining MotionTimeMs reload and "broken until docked", and disables fire/mode changes for a broken launcher. The paused panel regression passes (21/21 focused tests), including transport identity, authoritative follow-up, timer and broken state. An initial full Client run found seven old exact-status/icon-list assertions; those now check the new status and text fallback. Final validation evidence is recorded in `ImplementationStatus.md`; no game/native/Graphify runs.

**Задача.**
1. На панели торпедного аппарата (EP-0006/EP-0008): переключатель трёх режимов, отложенная смена с таймером, состояние «Сломан до дока».
2. Локализация названий режимов.

**Файлы:** `GameSessionScreen.Combat.cs`, панель торпедного аппарата, `Data/Locale/English.json`, `Data/Locale/Russian.json`, `tests/DeepSpaceSaga.Client.Tests/TorpedoCommandPanelTests.cs`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0006-torpedo-fire-modes.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
