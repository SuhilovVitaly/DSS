---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0017-world-integration
ticket: EP-0035-US-0017-TK-0001-campaign-dispatch
title: Единый dispatcher campaign hooks
stage: approved
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0033-US-0005-TK-0002-engine]
files_touched: 4
serves: [AC-01]
revision: 1
created: 2026-11-17T13:00:31Z
---

# Единый dispatcher campaign hooks

## Why / decisions / grounding
[История и source grounding](../EP-0035-US-0017-world-integration.md). Audit обнаружил отсутствующего owner интеграции; workflow approval относится к плану, не human acceptance. D-35-19/21/33/42/43/45/47 сохраняются.

## Code context / allowlist
- src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.StationRepair.cs
- src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs
- tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us0017Tk0001Tests.cs
Все production paths существуют, кроме feature PirateGroups.cs (создаёт US0001-TK0003); test новый. Один engine layer, не более5files включая test.

## Public API / implementation steps
Перенести единственные реализации PrepareDemoCampaignCommand, ValidateDemoCampaignSections и TickDemoCampaignSections в центральный owner DemoCampaign.cs; существующие StationRepair/CrewUpkeep тела оформить именованными helpers. Сохранить все dispatch ветки EP33, candidate/commit, duplicate/stale guarantees и календарные сутки. Предоставить явно именованные extensibility hooks для encounters, sensors, reports и debug без повторных partial implementations/reflection. Порядок: prepare immutable candidate, validation, commit; неизвестная команда по-прежнему unsupported. Feature tickets добавляют свои вызовы в allowlisted DemoCampaign.cs. Этот тикет не реализует gameplay EP35.

## Invariants / scope
Engine authoritative, transport async;0 motion on pause;1wu=100m. Нельзя добавлять вторую реализацию partial method. Сохранять existing candidate/commit/receipts и first-discovery flags. Никаких изменений client/contracts/content из этого тикета. Новые product решения и новые save versions вне scope.

## Tests / validation plan
NOT RUN. Focused Ep0035Us0017Tk0001Tests проверяет: ExistingRepairSupplyRecruitmentCommands_Unchanged; CrewDebtAndUpkeep_ExactlyOnce; UnknownCommand_Unsupported; DuplicateAndRejected_NoMutation; CalendarTickAndSave_NoDoubleCharge. Использовать actual SimulationEngine и existing session/world fixtures, не копию алгоритма. После implementation из D:/DeepSpaceSaga/DSS создать unique absolute run D:/DeepSpaceSaga/artifacts/validation/ep0035-integration/<unique-run>. Каждый process tree ограничить300000ms, timeout завершает дерево и не считается PASS.

    dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~Ep0035Us0017Tk0001Tests --results-directory <absolute-run>/tests --logger trx
    dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj -c Release --artifacts-path <absolute-run>/build
    dotnet format src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --verify-no-changes --include src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs src/DeepSpaceSaga.Engine/SimulationEngine.StationRepair.cs src/DeepSpaceSaga.Engine/SimulationEngine.CrewUpkeep.cs tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us0017Tk0001Tests.cs

## Definition of Done
Served AC подтверждены focused integration evidence, existing behavior сохранено; нет выхода за allowlist. Native ускорение/пауза/диалог/перехват отдельно, acceptance OPEN до проверки. Planning не разрешает production/build/commit. US0006 documentation после всех интеграций.

## Execution 2026-11-17
Central dispatcher implemented; repair and upkeep retain named feature helpers. No gameplay EP35 is claimed. Release Engine.Tests build: PASS, zero warnings/errors. Scoped whitespace and git diff --check: PASS. Tests NOT RUN until US0017 completion; existing EP33 service regressions plus Ep0035Us0017Tk0001Tests are scheduled. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-execution/dispatch-build.log and dispatch-format.log. Story acceptance remains OPEN.
