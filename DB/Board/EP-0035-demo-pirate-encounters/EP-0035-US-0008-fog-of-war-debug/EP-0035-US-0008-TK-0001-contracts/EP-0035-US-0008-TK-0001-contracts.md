---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0008-fog-of-war-debug
ticket: EP-0035-US-0008-TK-0001-contracts
title: Контракт тумана войны
stage: approved
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: contracts
matching_test_project: tests/DeepSpaceSaga.Contracts.Tests
depends_on: [EP-0035-US-0001-TK-0001-contracts]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T11:23:54Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Контракт тумана войны

## Why
Выполнить owning слой [истории](../EP-0035-US-0008-fog-of-war-debug.md), не дублировать консоль EP-0030.

## Decisions
D-35-01…04 и дословный запрос — в [эпике](../../Documentation.md#grooming-2026-11-17). Board grooming разрешён; production не запрошен. Workflow approval не означает human review деталей.

## Assumptions
A-35-01…03 и G-35-01…02 перечислены в истории. API ниже — проект. Численный радиус G-35-02 закрыт D-35-20. Content остаётся draft только до фиксации отдельных технических параметров установки G-35-02M; радиус заново не согласовывать.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Contracts/FogOfWarContracts.cs | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Contracts/DemoCampaign.cs | Grounding в истории; новые Sensor/Fog файлы создаёт этот тикет. | Только шаги ниже, без соседних механик. |
| src/DeepSpaceSaga.Contracts/PirateGroupsContracts.cs | Owning prerequisite US0001-TK0001. | Раздельные признаки sensor detection/debug visibility. |
| src/DeepSpaceSaga.Contracts/DebugTuning.cs | Существующий DTO owner. | Transient session-start options для fog/chance; не save DTO. |
| tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0035Us0008Tk0001Tests.cs | Новый matching test file. | Только шаги ниже, без соседних механик. |

## Public API after the change
Проект: SetFogOfWarRequest(CommandId,ExpectedRevision,Enabled):DemoCampaignCommand; FogOfWarSnapshot(Enabled).
Проект transient DTO в DebugTuning.cs: SessionDebugOverrides(int SchemaVersion = 1, bool? FogOfWarEnabled = null, double? PirateSquadChance = null). PirateSquadChance измеряется в процентах0..100, только finite; неизвестная SchemaVersion и NaN/Infinity/out-of-range отклоняются до создания/публикации session без частичного применения. Null означает отсутствие explicit override: normal defaults FogOfWarEnabled=true и PirateSquadChance=10. Новая DTO version — только версия transient options, не Save writer version.

App host передаёт immutable options в LocalClient session factory/constructor до NewGame/Load и первого engine tick (owning wiring US0008-TK0005, затем host US0008-TK0004). NewGame применяет оба effective значения до seeded generation; Load сохраняет группы и применяет только fog. Lifetime — память процесса; DTO не является членом save/profile и не сериализуется на диск. Active fog меняется существующим SetFogOfWarRequest через ExecuteDemoCampaignAsync; expected revision/command receipt сохраняются. Изменение chance влияет только на следующий NewGame. Host отдельно хранит requested override; FogOfWarSnapshot отражает подтверждённое engine effective значение, failed/stale response не означает success.

## Implementation steps
1. Добавить command/snapshot normal defaulttrue, explicit debug overridefalse через существующий campaign transport.
2. Разделить IsSensorDetected и IsDebugVisible в pirate projection; открытая карта не раскрывает future orders/скрытый loadout.

## Served acceptance criteria
AC-01…04: вклад только слоя contracts; всю историю проверять после integration.

## Out of scope
Ресурсный Scanner, реализация патруля (владелец US0001/0002, G-35-01 закрыт), stealth/occlusion, тактическое отступление, экономика перевозок, новая save writer version, публикация.

## Invariants
EngineRequirements.md, «Архитектура и авторитет», «Время и единицы», «Мир и наблюдаемость»: Engine владеет контактами, 1 world unit=100 м, renderer не мутирует AI. Fog не меняет сенсор, RNG, движение и pursuit timers; не раскрывает future orders/скрытый loadout.

## Tests
Будущие проверки, NOT RUN:
- DemoEp0035Us0008Tk0001Tests.NormalDefault_IsTrue
- DemoEp0035Us0008Tk0001Tests.DebugVisibility_IsNotDetection

Из корня DSS создать уникальный абсолютный <run> внутри D:/DeepSpaceSaga/artifacts/validation/ep0035-sensors/.
Ограничить каждый test process tree 300000 ms, остановить дерево при timeout.
dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj -c Release --filter "FullyQualifiedName~DemoEp0035Us0008Tk0001Tests" --results-directory <run>/tests --logger trx
dotnet build src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj -c Release --artifacts-path <run>/build
dotnet format src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj --verify-no-changes --include src/DeepSpaceSaga.Contracts/FogOfWarContracts.cs src/DeepSpaceSaga.Contracts/DemoCampaign.cs tests/DeepSpaceSaga.Contracts.Tests/Demo/DemoEp0035Us0008Tk0001Tests.cs
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
## Transient session-start API D-35-40
Additional allowed contracts files: src/DeepSpaceSaga.Contracts/IGameSessionConnection.cs; src/DeepSpaceSaga.Contracts/DemoStartContracts.cs. Optional transient fog/chance bootstrap options, no save payload. Chance NewGame only; fog before first snapshot NewGame/Load. Existing matching contract test validates defaults/override.
## Audit: transient session options
Owning contracts включают transient immutable session-start options (nullable fog override и squad chance), normal defaults true/10%, validation finite0..100. US0008-TK0004/0005 используют эти DTO до первого tick; сериализация save не включает options. Public snapshot distinguishes requested/effective override; hidden NPC data не добавлять.


## Execution 2026-11-17
Added typed revisioned SetFogOfWarRequest, optional effective FogOfWarSnapshot(defaulttrue), and separate computed IsDebugVisible. Reuse US12 DemoDebugSettings for the in-process factory API instead of duplicating transient options: owning process memory is read before generation and every projection, so no startup frame can precede applying it. No save DTO field. Build PASS0 warnings/errors; diff PASS; evidence D:/DeepSpaceSaga/artifacts/validation/ep-0035/20261117-resume/us8-tk1-build.log. Tests NOT RUN until US8 boundary.
