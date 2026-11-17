---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0008-fog-of-war-debug
title: Туман войны и консоль
stage: draft
approval_kind: workflow
implementation_status: implemented
priority: P0
dependencies: [EP-0035-US-0007-carrier-sensors, EP-0035-US-0001-visible-threats]
created: 2026-11-17T11:23:54Z
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; current execution and validation: [ExecutionStatus](../ExecutionStatus.md).

# Туман войны и консоль

## Связь с техническим заданием
Поручение 2026-11-17: сенсор, +20%, normal default-true fog + explicit debug-false override и консоль. Дословный запрос — в [эпике](../Documentation.md#grooming-2026-11-17). Материализация разрешена; implementation не запрошен.

## User story
Я начинаю с открытой карты и переключаю туман через консоль. Сенсоры и знание пиратов одинаковы в обоих режимах. Обзор мира не даёт AI всеведения.

## Зависимости
EP-0035-US-0007-carrier-sensors, EP-0035-US-0001-visible-threats

## Completion evidence
- AC-01: Normal defaulttrue без явного override; console читает/меняет значение, lifetime override D-35-40.
- AC-02: False показывает пиратов вне сенсора, true скрывает live contacts; future orders/loadout скрыты.
- AC-03: В обоих режимах sensor contacts, AI, движение, RNG совпадают; debug marker не даёт detection alert.
- AC-04: Пауза, неверный ввод, отсутствие/stale session и reset проверены.

## Acceptance criteria
AC-01…04 выше обязательны, headless не закрывает native/product.

## Decisions and assumptions
D-35-01 (пользователь): сенсор — установленный модуль носителя с радиусом.
D-35-02 (пользователь): стартовый R_player = 1.20 * R_pirate обоих обычных составов. Это стартовая комплектация, не вечный бонус игроку.
D-35-03 (пользователь): SUPERSEDED D-35-37: normal FogOfWarEnabled=true; explicit owner debug override=false через консоль. Открытая карта — визуализация, не обнаружение и не знание AI.
D-35-04: поручено добавить тикеты; продолжается грумминг, production implementation не запрошен.

A-35-01: пассивный всенаправленный сенсор, обнаружение при R>0 и distance<=R без RNG. Disabled/destroyed/отсутствующий модуль даёт R=0; несколько рабочих модулей дают max. Активное сканирование и новая модель энергопотребления вне scope.
A-35-02 SUPERSEDED D-35-37: normal defaulttrue, explicit debugfalse; lifetime override D-35-40.
A-35-03: fog фильтрует динамические пиратские маркеры, не известные станции/географию. Last-known маркеры пока вне scope.
D-35-05: патруль по точкам принят для демо. Остаток G-35-01: конкретные точки/стоянки и выбор других целей ещё обсуждаются. Потеря контакта/возврат приняты D-35-17; скорости приняты D-35-15/16.
G-35-02 [CLOSED D-35-20]: радиусы 200/240/264 км утверждены. G-35-02M отдельно: физические параметры установки модуля ещё нужно записать перед content implementation; это не неопределённая дальность.

## Grounding
- src/DeepSpaceSaga.Engine/Content/ModuleTypeDefinition.cs:5 — каталог модулей; sensor radius пока отсутствует.
- src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:922 — ModuleImplementationDto.
- src/DeepSpaceSaga.Client/Data/Ships/starter-carrier.json:80 — установлен Scanner ресурсов; сохранить его.
- src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup с SightRadius/PatrolSpeed; это схема, не подтверждение движения.
- src/DeepSpaceSaga.Engine/Scenario/StarterCarrierManifest.cs:13 — manifest установленных модулей.
- src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs:83 — projection и hooks sections.
- src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — CreateDefault; fog-команды нет.
- src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs:21 — IDebugCommand, async session context.
- EP-0030 Documentation.md:16 — поставленная консоль, её реализацию не дублировать.

## Ограничения и gaps
G-35-01 остаётся для следующего обсуждения. G-35-02 закрыт D-35-20; G-35-02M отдельно ограничивает готовность установки content, не дальность. После EP-0034 сверить фактический Demo manifest.

## Approved ticket map
Workflow карта; draft строки не являются готовыми к исполнению.
| Ticket | Результат | Prerequisites |
|---|---|---|
| [EP-0035-US-0008-TK-0001-contracts](EP-0035-US-0008-TK-0001-contracts/EP-0035-US-0008-TK-0001-contracts.md) | Контракт тумана войны | EP-0035-US-0001-TK-0001-contracts |
| [EP-0035-US-0008-TK-0002-engine](EP-0035-US-0008-TK-0002-engine/EP-0035-US-0008-TK-0002-engine.md) | Туман войны без изменения сенсоров и AI | EP-0035-US-0008-TK-0001-contracts, EP-0035-US-0007-TK-0004-sensor-runtime, EP-0035-US-0001-TK-0003-engine |
| [EP-0035-US-0008-TK-0003-console-and-map](EP-0035-US-0008-TK-0003-console-and-map/EP-0035-US-0008-TK-0003-console-and-map.md) | Команда fog_of_war и маркеры карты | EP-0035-US-0008-TK-0002-engine, EP-0035-US-0001-TK-0004-client |

## Review log
2026-11-17T11:23:54Z — материализация по прямому поручению «Добавляй нужные тикеты … мы продолжаем грумминг». D от пользователя, A/G отдельно. История draft до завершения грумминга. Implementation NOT STARTED; tests/native/product NOT RUN / OPEN. US-0006 выполняется последней.


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
## Additional ticket map D-35-40
- [TK0004 app overrides](EP-0035-US-0008-TK-0004-app-overrides/EP-0035-US-0008-TK-0004-app-overrides.md): client host after contracts.
- [TK0005 session overrides](EP-0035-US-0008-TK-0005-session-overrides/EP-0035-US-0008-TK-0005-session-overrides.md): local-client bootstrap after host/US12debug engine.
US8 теперь5 tickets; EP35 всего16stories/64tickets. Actual host path UI/SkiaWindow.cs.
