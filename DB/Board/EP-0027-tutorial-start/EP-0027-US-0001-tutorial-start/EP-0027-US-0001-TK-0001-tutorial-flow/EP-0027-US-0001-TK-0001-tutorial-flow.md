---
epic: EP-0027-tutorial-start
story: EP-0027-US-0001-tutorial-start
ticket: EP-0027-US-0001-TK-0001-tutorial-flow
title: Скрытый сценарий Tutorial и маршрут обучающего боя
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
serves: [AC1, AC2, AC3, AC4, AC5, AC6]
files_touched:
  - src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs
  - src/DeepSpaceSaga.Engine.LocalClient/ScenarioRepository.cs
  - src/DeepSpaceSaga.Client/Scenarios/Tutorial/scenario.json
  - src/DeepSpaceSaga.Client/Data/Maps/solar-system.json
  - src/DeepSpaceSaga.Client/IGameSessionFactory.cs
  - src/DeepSpaceSaga.Client/Program.cs
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.Overlay.cs
  - src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/PhasePanel.Outcome.cs
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - tests/DeepSpaceSaga.Engine.Tests/ScenarioMetadataHiddenTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TutorialScenarioTests.cs
  - tests/DeepSpaceSaga.Client.Tests/AllScenarioSystemContentTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleNavigationTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ScenarioModuleCleanupTests.cs
  - tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs
  - DB/Documentation/02-FirstRelease/Screens/MainMenu.md
  - DB/Documentation/02-FirstRelease/Screens/ScenarioSelect.md
  - DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md
  - DB/Documentation/02-FirstRelease/Screens/TacticalBattleSetup.md
  - DB/Documentation/04-Engineering/TacticalBattle.md
  - DB/Board/EP-0027-tutorial-start/Documentation.md
  - DB/Board/EP-0027-tutorial-start/Tickets.md
  - DB/Board/EP-0027-tutorial-start/EP-0027-US-0001-tutorial-start/EP-0027-US-0001-tutorial-start.md
  - DB/Board/EP-0027-tutorial-start/EP-0027-US-0001-tutorial-start/EP-0027-US-0001-TK-0001-tutorial-flow/EP-0027-US-0001-TK-0001-tutorial-flow.md
---

# Скрытый сценарий Tutorial и маршрут обучающего боя

[История](../EP-0027-US-0001-tutorial-start.md) · [Эпик](../../Documentation.md)

## Code context

- `ScenarioMetadata` получает необязательный `bool? Hidden` (`hidden`, не пишется при null). `ScenarioRepository.ListScenarios` пропускает скрытые сценарии, `ScenarioRepository.FindTutorialScenarioPath` находит файл с `scenarioId: "tutorial"`.
- `Scenarios/Tutorial/scenario.json` — копия Default; `Tutorial` добавлен в `enabledScenarios` карты солнечной системы.
- `IGameSessionFactory.GetTutorialScenarioPath()` — член интерфейса по умолчанию (null); `Program.LocalGameSessionFactory` возвращает путь из `Scenarios`.
- `SkiaWindow` ветвит «Новую игру» по числу сохранений и запускает обучение через `StartTutorialAsync`; `TacticalBattleFlow` и экран боя получают клиентский режим обучения.
- Для сохранения в бою используется существующий отказ Engine; механика боя не меняется.
- `ScenarioModuleCleanupTests.EveryNewGameEntryLoadsWithoutRealtimeCombat` сверяет список выбора только с нескрытыми `scenario.json`; Tutorial проверяется отдельно на отсутствие realtime-боя.
- `ScenarioGroupTranslationTests.AllShippedScenariosCovered` ожидает папку `Tutorial` среди поставляемых сценариев; `V2StartCases` и `InlineData` этого класса Tutorial не включают.

## Критерии приёмки

Критерии 1–6 [истории](../EP-0027-US-0001-tutorial-start.md#критерии-приёмки).

## Проверка

Целевые тесты Engine.Tests и Client.Tests по метаданным, списку сценариев, запуску Tutorial, навигации боя и итогу боя; финальные `dotnet build DeepSpaceSaga.sln` и `dotnet test DeepSpaceSaga.sln`. Результаты пишутся под `D:/DeepSpaceSaga/artifacts/validation/ep0027-tutorial-start/`.

## Результат

Реализованы метаданные `hidden`, сценарий Tutorial, фабрика, маршрут «Новой игры» без сохранений, режим обучения в бою и документация экранов и инженерного контракта. Коммитов нет, изменения в рабочем дереве ветки `codex/tutorial-start`.

Проверки 2026-11-14 (артефакты под `D:/DeepSpaceSaga/artifacts/validation/ep0027-tutorial-start/`):

- `ScenarioModuleCleanupTests` — 5/5, оба варианта (`source`, `deployed`): `batch-c-cleanup-test/cleanup.trx`.
- `dotnet build DeepSpaceSaga.sln` — 0 предупреждений, 0 ошибок: `final/build.log`.
- `dotnet test DeepSpaceSaga.sln`: Contracts 199/199; Motion 1282/1283; Engine 2340/2952; Client 2465/2473; Performance 6/16; EconomyBalance 58/61. Лог и TRX: `final/test.log`, `final/*.trx`.
- Сверка с базой (`git stash -u`, та же ветка без изменений): 633 из 634 падений есть и на базе — масштаб расстояний, `insufficient_voyage_fuel`, отчёты Performance, ForbiddenCalls, карта Client, LongVoyage. Списки: `final/failed-final.txt`, `final/base-compare/failed-base.txt`.
- Единственное новое падение `ScenarioGroupTranslationTests.AllShippedScenariosCovered` (`final/failed-new.txt`) закрыто: `Tutorial` добавлен в ожидаемый список. Тест и `ScenarioMetadataHiddenTests` — 5/5: `final/group-translation-fix/`.
- Остальные 633 падения существовали до этих изменений.
- Приёмку выполняет пользователь; нативная проверка окна не выполнялась.

## Журнал

2026-11-14 — пользователь одобрил расширение allowlist 2026-11-14: скрытый Tutorial ломал EveryNewGameEntryLoadsWithoutRealtimeCombat. В `files_touched` добавлен `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ScenarioModuleCleanupTests.cs`.

2026-11-14 — пользователь одобрил расширение allowlist 2026-11-14: Tutorial ломал ScenarioGroupTranslationTests.AllShippedScenariosCovered; добавлен только в ожидаемый массив (:168), V2StartCases/InlineData не трогаем. В `files_touched` добавлен `tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs`.
