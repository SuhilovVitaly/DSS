---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0017-world-integration
ticket: EP-0035-US-0017-TK-0002-world-step
title: Интеграция motion шага и атомарных событий встречи
stage: approved
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0017-TK-0001-campaign-dispatch, EP-0035-US-0002-TK-0002-engine, EP-0035-US-0003-TK-0002-engine, EP-0035-US-0010-TK-0003-dialogue-engine, EP-0035-US-0012-TK-0002-aggregate-engine]
files_touched: 5
serves: [AC-02, AC-03, AC-04]
revision: 1
created: 2026-11-17T13:00:31Z
---

# Интеграция motion шага и атомарных событий встречи

## Why / decisions / grounding
[История и source grounding](../EP-0035-US-0017-world-integration.md). Audit обнаружил отсутствующего owner интеграции; workflow approval относится к плану, не human acceptance. D-35-19/21/33/42/43/45/47 сохраняются.

## Code context / allowlist
- src/DeepSpaceSaga.Engine/SimulationEngine.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.Time.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs
- tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us0017Tk0002Tests.cs
Все production paths существуют, кроме feature PirateGroups.cs (создаёт US0001-TK0003); test новый. Один engine layer, не более5files включая test.

## Public API / implementation steps
Подключить existing feature helpers к реальному AdvanceWorldTo, сохраняя до/после motion positions/time для swept tests; не вычислять движение из calendar milliseconds. Все callers (snapshot, speed/time commands, CaptureSaveState) проходят один pipeline. За один authoritative step собрать контакты всех групп и один report batch, выбрать capture earliest/tie-break, затем зафиксировать pause и pending encounter до публикации snapshot. Учитывать safe-zone crossing и замороженный world при battle/pause. Interrupted step должен останавливаться в согласованном motion/calendar времени без повторного продвижения economy/upkeep или player trajectory. Не вводить второй clock; если невозможно использовать существующий stop mechanism в5files, зафиксировать точную дополнительную декомпозицию Board до production. Не двигать группы при render/save/repeated snapshot. Capture commit остается engine owner; durable boundary далее US16.

## Invariants / scope
Engine authoritative, transport async;0 motion on pause;1wu=100m. Нельзя добавлять вторую реализацию partial method. Сохранять existing candidate/commit/receipts и first-discovery flags. Никаких изменений client/contracts/content из этого тикета. Новые product решения и новые save versions вне scope.

## Tests / validation plan
NOT RUN. Focused Ep0035Us0017Tk0002Tests проверяет: FastStep_NoTunnelThroughSensorsCaptureOrSafeZone; DifferentTimeScales_SameMotionOutcome; MultipleNewGroups_OneReport; EarliestCapture_StableTie; DetectionAndCaptureSameStep_PendingPaused; SaveWhilePaused_NoMovementOrRepeat; BattleActive_WorldFrozen; ExistingEconomyNavigationSecurity_NoRegression. Использовать actual SimulationEngine и existing session/world fixtures, не копию алгоритма. После implementation из D:/DeepSpaceSaga/DSS создать unique absolute run D:/DeepSpaceSaga/artifacts/validation/ep0035-integration/<unique-run>. Каждый process tree ограничить300000ms, timeout завершает дерево и не считается PASS.

    dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~Ep0035Us0017Tk0002Tests --results-directory <absolute-run>/tests --logger trx
    dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build
    dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.cs src/DeepSpaceSaga.Engine/SimulationEngine.EconomyTime.cs src/DeepSpaceSaga.Engine/SimulationEngine.Time.cs src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us0017Tk0002Tests.cs

## Definition of Done
Served AC подтверждены focused integration evidence, existing behavior сохранено; нет выхода за allowlist. Native ускорение/пауза/диалог/перехват отдельно, acceptance OPEN до проверки. Planning не разрешает production/build/commit. US0006 documentation после всех интеграций.

Execution scope expands MapEvasion.cs/PirateComposition.cs and a PirateWorldStep partial: swept player acquisition and capture choose earliest event; all group motion is sampled at that committed boundary. Existing integral-ms clock rounds event up by at most1motion ms while exact capture time remains recorded. Calendar mapping uses the existing AdvanceWorldTo mapping, applies equal-time economy events once, then resets the same clock. Snapshot/save/travel share pause boundary; pending report/capture/battle/storage freeze world. No second clock or render motion.

Build PASS us17-tk2-build.log; scoped whitespace format performed. Tests after publication: Engine Ep0035 (all epic integration layers), EconomyTime/StationSecurity relevant regression, Client sensor/capture/recovery. Native remains NOT RUN.

### Story validation corrections
Independent review found fractional motion remainder loss on repeated paused snapshots. Scope includes PirateWorldStep, regression test, source-anchored EconomyTimeContinuity fixtures and structural JSON comparisons in TK-0001 tests (immutable collection reference equality is not save equality). Initial story run: 103 passed / 79 failed; 77 content-root failures, two collection equality failures. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/us17-engine-tests.log. Fix validation pending.

Repeated run: fractional regressions and EP-0035 tests pass; 77 fixture errors remain because CallerFilePath resolves at the caller. Economy fixture now owns its correctly anchored root. Revalidation pending.

Scope also includes shared RationScheduleTests fixture used by EconomyTimeContinuity: its transitive binary-relative content path caused the remaining 74 errors. Direct scenario fixtures now pass. No production expectation changed.
