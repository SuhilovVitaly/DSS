---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0008-fog-of-war-debug
ticket: EP-0035-US-0008-TK-0005-session-overrides
title: Передача overrides перед запуском сессии
stage: draft
implementation_status: implemented-tests-pending
layer: local-client
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0012-TK-0004-debug-engine, EP-0035-US-0008-TK-0001-contracts, EP-0035-US-0008-TK-0002-engine]
files_touched: 3
serves: [AC-01, AC-03, AC-04]
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Передача overrides перед запуском сессии

## Why / decisions
[История](../EP-0035-US-0008-fog-of-war-debug.md), D-35-40: overrides действуют до закрытия приложения, не сохраняются между запусками. Normal fogtrue/chance10. Четыре ordinary группы создаются только NewGame.

## Code context / allowlist
- src/DeepSpaceSaga.Engine.LocalClient/LocalGameSessionConnection.cs
- src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs
- tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us0008Tk0005Tests.cs
Grounding: DebugCommand.cs IDebugConsoleHost.Session nullable; DebugCommandContext фиксирует session и IsCurrent. UI/SkiaWindow.cs:120 создаёт console controller. LocalGameSessionConnection.cs constructor запускает engine loop, поэтому options нужны до первого tick, не после UI render. AppDebugOverrides и тест — новые файлы.

## Implementation / API
Передать transient options создания/загрузки по contracts до первого engine tick/publication. NewGame получает effective chance до seeded creation4groups; Load сохраняет existing groups и применяет только fog override/defaulttrue до первого отображаемого snapshot. Не запускать simulation между session construction и применением fog. Не сериализовать overrides, не менять fingerprint/save содержимое. Existing singleton callers без options получают нормальные defaults. Matching integration tests в Engine.Tests, поскольку отдельного LocalClient.Tests сейчас нет.
Проект transient options DTO/host API описать в prerequisite contracts; production реализации здесь нет. При drift source paths согласовать Board до выхода за allowlist.

## Tests
NOT RUN: MainMenuReadSet_NoSession; OverridesSurviveNewGameLoadWithinProcess; NewProcess_NormalDefaults; SaveContainsNoDebugOverrides; ChanceChange_DoesNotMutateExistingGroups; InitialGenerationUsesOverrideBeforeFirstTick; StaleOrRejectedFog_NoFalseAck. Focused matching test project filter Ep0035Us0008Tk0005Tests. Artifact root D:/DeepSpaceSaga/artifacts/validation/ep0035-overrides/<unique-run>, process cap300000ms; build owning project и format scoped C#. Native main-menu UX отдельно.

## Invariants / non-goals / DoD
Один слой,<=5 files; no hidden save/profile persistence, no production change при grooming. No-fog только projection, никаких discovery/AI/RNG эффектов. Tests/code review плюс native evidence перед implementation acceptance; unrelated gameplay сохраняется. Story/operator availability не решается этим тикетом.


## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.

## Execution 2026-11-17
Process-scoped Engine state is reused through LocalClient before any connection loop starts; no duplicate persisted options model. Added real first-snapshot integration coverage. Build/static checks below; tests deferred to US-0008 boundary. Scope follows listed connection and Engine.Tests files.
