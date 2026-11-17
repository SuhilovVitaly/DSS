---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0008-fog-of-war-debug
ticket: EP-0035-US-0008-TK-0003-console-and-map
title: Команда fog_of_war и маркеры карты
stage: approved
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0035-US-0008-TK-0002-engine, EP-0035-US-0001-TK-0004-client, EP-0035-US-0008-TK-0004-app-overrides]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T11:23:54Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Команда fog_of_war и маркеры карты

## Why
Выполнить owning слой [истории](../EP-0035-US-0008-fog-of-war-debug.md), не дублировать консоль EP-0030.

## Decisions
D-35-01…04 и дословный запрос — в [эпике](../../Documentation.md#grooming-2026-11-17). Board grooming разрешён; production не запрошен. Workflow approval не означает human review деталей.

## Assumptions
A-35-01…03 и G-35-01…02 перечислены в истории. API ниже — проект. Численный радиус G-35-02 закрыт D-35-20. Content остаётся draft только до фиксации отдельных технических параметров установки G-35-02M; радиус заново не согласовывать.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/DebugConsole/FogOfWarDebugCommand.cs | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Client/UI/Demo/PirateGroupsPresentation.cs | Создаётся US-0001. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0035Us0008Tk0003Tests.cs | Новый matching test file. | Только шаги ниже, без соседних механик. |

## Public API after the change
Проект: fog_of_war [true|false]; без аргумента — текущее значение.

## Implementation steps
1. Зарегистрировать команду и help; отправлять SetFogOfWarRequest через GameSessionHandle.ExecuteDemoCampaignAsync, не напрямую в Engine.
2. Неверные аргументы=>отказ; no-session main-menu host override допустим. Stale/rejected active application не выдавать за applied success.
3. False: отличимый обзорный маркер вне сенсора без detection alert. True: убрать такие markers, live tooltip positions и hit targets.
4. Проверить паузу, обратное переключение и смену session; UI не сохраняет старый override.

## Served acceptance criteria
AC-01…04: вклад только слоя client; всю историю проверять после integration.

## Out of scope
Ресурсный Scanner, реализация патруля (владелец US0001/0002, G-35-01 закрыт), stealth/occlusion, тактическое отступление, экономика перевозок, новая save writer version, публикация.

## Invariants
EngineRequirements.md, «Архитектура и авторитет», «Время и единицы», «Мир и наблюдаемость»: Engine владеет контактами, 1 world unit=100 м, renderer не мутирует AI. Fog не меняет сенсор, RNG, движение и pursuit timers; не раскрывает future orders/скрытый loadout.

## Tests
Будущие проверки, NOT RUN:
- DemoEp0035Us0008Tk0003Tests.Help_IncludesCommand
- DemoEp0035Us0008Tk0003Tests.InvalidInputAndNoSession_NoMutation
- DemoEp0035Us0008Tk0003Tests.StaleSession_NoFalseSuccess
- DemoEp0035Us0008Tk0003Tests.Map_HidesNonDetected
- DemoEp0035Us0008Tk0003Tests.DebugMarker_NoAlert

Из корня DSS создать уникальный абсолютный <run> внутри D:/DeepSpaceSaga/artifacts/validation/ep0035-sensors/.
Ограничить каждый test process tree 300000 ms, остановить дерево при timeout.
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0008Tk0003Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --verify-no-changes --include src/DeepSpaceSaga.Client/UI/DebugConsole/FogOfWarDebugCommand.cs src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs src/DeepSpaceSaga.Client/UI/Demo/PirateGroupsPresentation.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0035Us0008Tk0003Tests.cs
JSON проверять loader tests; native/product acceptance отдельно.

## Definition of Done
- Steps выполнены в allowlist, served AC покрыты вкладом слоя и integration evidence.
- Focused tests/build/format проходят либо записаны конкретные исходные failures.
- API, invariants, out-of-scope соблюдены; нет скрытой работы/допущений.
- Численные радиусы D-35-20 соблюдены; отдельно G-35-02M технические параметры установки записаны до исполнения content.
- Пользовательский путь проверяем, headless не подменяет native.

## Self-containment check
Prerequisites, API, files, steps и проверки перечислены. Grounding/решения в истории; при drift, особенно Demo manifest после EP-0034, сначала уточнить Board. G-35-01 о патруле не решается этим тикетом.


## D-35-37: player fog включён, owner debug обзор выключает fog
Пользователь: «туман войны для игрока будет включен. я пока хочу видеть без тумана войны. Давай в этом эпике не будем обсудать сюжет».
Normal player demo default FogOfWarEnabled=true. Owner/developer использует явный debug override false через console для обзора карты. Прежние D-35-03 universal defaultfalse и A-35-02 resetfalse SUPERSEDED; unconditional false при Load/NewGame запрещён. Без явного override normal/legacy/new session safe default=true.
No-fog меняет только разрешённую debug projection, не sensor acquisition/AI knowledge/pursuit/доклады/генерацию loadout. Returntrue скрывает out-of-sensor live positions/tooltip/hit targets. Console показывает actual value/override status.
G-35-17 CLOSED D-35-40: app process memory, survives NewGame/Load, discarded on exit; defaults true/10.
Сюжет и named captain вне текущего EP35 grooming. Прежний G-35-16 перенесён в deferred integration note EP39, НЕ обязательный blocker EP35; EP39 не изменён. Не утверждено, что captain является пятой группой, появляется заранее или является исключением. Текущее D остаётся4 ORDINARY группы на старте/no respawn.
Tests NOT RUN: NormalNewGame_FogTrue; ExplicitDebugFalse_RevealsOnlyProjection; ReturnTrue_HidesUndetectedLiveContacts; LegacyAndNewSession_DefaultTrueWithoutOverride; FogOverride_NoSensorEventsOrAiKnowledge; OverrideLifetime_AccordingToResolvedG35_17.
## D-35-40…42: overrides, оператор и состоявшийся перехват
D-35-40 пользователь: «Да, настройки действуют до закрытия приложения, между запусками не сохраняются».
Host хранит explicit overrides только в app process memory: fog немедленно и после NewGame/Load; chance только следующий NewGame. Console main menu read/set без session допустим. Restart=>fogtrue/chance10; saves/profiles не содержат overrides. Failed/stale active application не выдаётся за effective success. Options применяются до первого engine tick/snapshot. G-35-13 lifetime и G-35-17 CLOSED.
D-35-41 пользователь: «Да, принимаем по наличию в экипаже и назначению, без смен и усталости».
Irina activecrew+sensor assignment=>264км,±1/±2; absent/unassigned=>script192км,±2/±4. Broken/disabled sensor=>0 coverage независимо от operator/script. No shifts/fatigue/injury schema. Reassignment восстанавливает бонус, не сбрасывает consumed groups; новые контакты расширенного радиуса идут через standard batch/autopause. G-35-06 CLOSED.
D-35-42 пользователь: «Да, принимаем: если перехват состоялся, после доклада и паузы начинается неизбежный бой».
При реальном pirate sensor contact и distance<=20км authoritative atomic capture фиксирует encounter. Player detection alone не capture. Для новой группы report+pause по batch policy; dismissal сохраняет Speed0; explicitcontinue начинает бой. Уже captured нельзя отменить сменой курса на паузе. Previously reported group не создаёт повторный automatic modal; используется существующий battle transition. Save/Load сохраняет pending capture, не даёт free escape. G-35-09 normal single-group inside-capture CLOSED; остаток: simultaneous capture CLOSED D-35-45: select earliest, no merge; recovery CLOSED D-35-47.
Tests NOT RUN: MainMenuOverrides_NoSession; OverridesSurviveNewGameLoadUntilExit; Restart_NormalDefaults; Save_NoOverrides; Chance_ExistingGroupsUnchanged; IrinaAbsentUnassigned_Fallback; AssignedIrina_Bonus; BrokenSensor_NoDetection; Reassignment_NoDuplicateReport; NewlyRevealedGroup_NormalBatch; CaptureThenReport_StaysPaused; ExplicitContinue_StartsUnavoidableBattle; PausedCourse_CannotUndoCapture; LoadPendingCapture_NoEscape; PlayerDetectionOnly_NoCapture.
Placement: all human station spawn400км A-35-17, START hard800км D-35-43/A-35-19; конкретные patrol routes ещё балансируются.