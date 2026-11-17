---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0015-squad-battle-continuation
ticket: EP-0035-US-0015-TK-0004-save-engine
title: Сохранение и продолжение эскадры
stage: draft
implementation_status: implemented-tests-pending
layer: engine
matching_test_project: tests/DeepSpaceSaga.Engine.Tests
depends_on: [EP-0035-US-0015-TK-0003-settlement-engine]
files_touched: 5
serves: [AC-04]
created: 2026-11-17
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Сохранение и продолжение эскадры

## Why and decisions
[История](../EP-0035-US-0015-squad-battle-continuation.md) — bounded вклад AC-04. D-35-21…25 и A/G истории применяются; продуктовые G закрыты итоговыми решениями, техническая готовность требует проверки prerequisites/allowlists по текущим source owners.

## Code context and allowlist
- src/DeepSpaceSaga.Engine/Scenario/DemoCampaignData.cs — существующий owning source, только описанный scope.
- src/DeepSpaceSaga.Engine/SimulationEngine.DemoCampaign.cs — существующий owning source, только описанный scope.
- src/DeepSpaceSaga.Engine/SimulationEngine.WorldEncounterBattle.cs — owning prerequisite/new planned extension; проверить фактический путь перед исполнением.
- src/DeepSpaceSaga.Engine/SimulationEngine.PirateGroups.cs — owning prerequisite/new planned extension; проверить фактический путь перед исполнением.
- tests/DeepSpaceSaga.Engine.Tests/Demo/Ep0035Us15Tk4Tests.cs — новый matching test.

Grounding: src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup имеет один LoadoutId. TacticalBattle/Content/TacticalBattleDefinitions.cs:52 — BattleSide имеет один Carrier. TacticalBattle/TacticalBattleAssembly.cs:28 — сборка читает side.Carrier. TacticalBattle/TacticalBattleState.cs:100–102 — Midpoint/Carrier(side) опираются на одиночного носителя. TacticalBattle/TacticalBattle.cs:54–56 — outcome проверяет Carrier(side). TacticalBattle/Ai/TacticalCommander.cs:10 — защита выбирает Carrier(side). Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — регистрация команд.

## Public API / implementation steps
Roundtrip до контакта, после discovery, до/после settlement сохраняет stable IDs, materialized per-member composition, report consumed flag, lastKnown/state/waypoint и frozen speed baseline. Не reroll/restart battle/reapply receipt. Active tactical battle save поддержать только согласно existing save lifecycle; если сейчас запрещён, не обещать его молча, записать compatibility policy.

Сначала сверить prerequisite DTO/schema; затем реализовать описанную часть только в allowlist. Если слой требует шестой файл, обновить Board decomposition до изменения production. Новые имена — проект, не утверждение существующего API.

## Invariants
Один layer, Engine authoritative; stable group/member/drone IDs. Сенсор200/240/264км, capture20км и один automatic report/group сохраняются. Fog off не discovery; нет reroll. Не считать side равным carrier. Два enemy Логова плюс player carrier. Открытые G не заменять случайным решением.

## Tests and validation plan
NOT RUN. Добавить focused Ep0035Us15Tk4Tests: positive two-Lair path; invalid/duplicate identity rejected; legacy singleton compatibility; repeat/Load idempotence для изменяемой части. Проверить конкретный AC-04 и отсутствие утечки hidden loadout. Для spawn — fixed seed,0/100% boundaries,10% default, rejected NaN/out-of-range без mutation; для battle — уничтожение первого carrier при живом втором не завершает сторону; для client — выбор каждого ID и3 carrier snapshot.
Запуск после реализации из DSS: dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj -c Release --filter FullyQualifiedName~Ep0035Us15Tk4Tests --results-directory <absolute-run>/tests. Output root D:/DeepSpaceSaga/artifacts/validation/ep0035-squadrons/<unique-run>; timeout300000ms, не PASS. Build owning project и format изменённых C#; JSON loader tests. Native UI/product отдельно, не заменяются unit tests.

## Out of scope and Definition of Done
Другие layers,3/mixed enemy squad и самостоятельный выбор orphan policy вне scope. Для завершения реализации требуется evidence: AC проверены, stable save/identity и regression singleton соблюдены. Сейчас tests/native/product NOT RUN/OPEN; закрытые продуктовые G не переоткрывать. US0006 документирует фактическую реализацию последней. Никакого commit/push/build при текущем grooming.


## Текущее расширение эскадр — D-35-21…26
D-35-22: одна фишка группы, Vmax=min operational Vmax её носителей.
D-35-23: все носители и дроны эскадры одновременно на одном поле.
D-35-24: шанс эскадры10%, меняется debug console. Предлагаемый синтаксис pirate_squad_chance [0..100], без аргумента current; help/валидация обязательны.
D-35-25 supersedes раннее2–3/mixed DEMO: «так по поводу боя. пока эскадры только два логова и оба выводим в бой». Ровно два Логова, по6 дронов =12, плюс носитель игрока (всего3 carriers на поле). Танкред остаётся одиночным типом. Без повреждений squad Vmax=1.10 frozen start player maximum, operational min сохраняется.
D-35-26: «при уничтожении всех носителей дроны получают статус выведены из строя как при стрельбе». Гибель первого из двух не отключает дроны. После последнего носителя стороны все её surviving drones становятся существующим Disabled, speed0; Destroyed остаётся Destroyed, отключение не означает смерть пилота. Симметрично для player и pirate side. Atomic/idempotent transition, затем outcome и skip последующих shots по existing invariants.
Grounding: TacticalBattleState.cs:9 TacticalDroneStatus Air/Disabled/Destroyed; TacticalBattleSalvo.cs:237–239 shot disable задаёт Disabled,Speed0,statistics; :152–153 skip disabled/destroyed shooter.
A-35-11: независимый seeded draw на каждое создание новой группы,90% singleton/10% squad; chance действует только future creation, не sensor/load; RNG/result сохраняются без reroll.
G-35-12 CLOSED D-35-32: одна точка группы, max operational radius без сложения; report layout — A.
G-35-13 lifetime CLOSED D-35-40; ordinary group count4 CLOSED D-35-36; singleton weights CLOSED D-35-35; initial/norespawn CLOSED D-35-34.
G-35-14 CLOSED D-35-26; orphan policy больше не открыта.
G-35-15 normal retreat CLOSED/excluded D-35-46: exit onlywin/loss; crash recovery CLOSED D-35-47.
Ранние2–3/mixed варианты, выбор2vs3 и orphan proposals — superseded; не scope демо и не текущие G.
D-35-26 tests NOT RUN: FirstCarrierDeath_LeavesDronesActive; LastCarrierDeath_DisablesSurvivors; DestroyedDrone_Unchanged; Disable_NoPilotDeath; PlayerAndEnemySymmetric; RepeatedResolution_Idempotent; SubsequentShot_ShooterDisabled. Reuse existing Disabled semantics and statistics; never fabricate Hull damage to trigger disable.
## D-35-45/46: последовательные бои и только победа/поражение
Пользователь: «Да, принимаем последовательные бои без присоединения соседних групп. Добавляй в тикеты. Давай теперь разберём выход из незавершённого боя, загрузку и сохранение последствий. Из боя выйти можно проиграв или победив».
D-35-45: несколько разных groups пересекают capture threshold=>выбрать earliest continuous crossing event; exact tie решается детерминированно по stable groupId (технический deterministic tiebreak). Только выбранная группа входит в бой, максимум2enemy carriers при squad2Логово; соседи не присоединяются/не подкрепляют. World motion frozen в бою. После результата world остаётся paused; explicitresume может позволить другой группе захватить игрока по обычным условиям. Нет postwin immunity.
G-35-18 CLOSED. Не путать arbitration с batch first-discovery: одна сводка шага может перечислить несколько новых групп, но только selected captured group участвует в текущем бою.
D-35-46: normal EP35 campaign battle заканчивается только win/loss. Нет retreat/abort/обычного выхода или загрузки другого мира, позволяющих уйти из ongoing battle. UI/command guard authoritative, не только скрытая кнопка. Debug/tutorial exit_battle существующие semantics — отдельный явный context; не разрешать ему незаметно обходить normal campaign capture и не менять tutorial tooling без owner scope.
G-35-19 CLOSED D-35-47: durable pending same-battle restart; no perturn resume, no freeworld.
Grounding LocalGameSessionConnection.cs:145–180: accepted settlement receipt cached by BattleId, повтор retry только continuation write; SortieSettlement.cs:17 применяет outcome до clear session. Переиспользовать existing exactly-once consequences; не считать Save failure повторной наградой.
Tests NOT RUN: EarliestContinuousCapture_WinsArbitration; ExactTie_StableGroupId; NeighborGroup_NotMerged; Battle_FreezesWorldMotion; ReturnWorld_Paused; Resume_AnotherCaptureAllowedNoImmunity; OngoingCampaignBattle_RejectNormalExitAndEscapeLoad; TutorialDebugContext_ExplicitSeparation; SettlementReplay_ExactlyOnce.
## D-35-47: восстановление captured боя с начала
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя».
Durable prebattle pending checkpoint ДО tactical start: committed capture, fixed group/member/drone composition, exact player/company prebattle state, stable encounter/battleId и replay seed. Continue после close/crash запускает тот же бой с начала, не navigable world. Ordinary midbattle save/load/menu retreat запрещены; perturn resume не вводится.
Win/loss settlement exactly once, затем durable finished checkpoint до map/end. Failed write удерживает pending result/retry storage, не restart/reapply. Crash при atomic persistence даёт либо old durable pending battle replay, либо durable finished state. Intermediate damage persistence не обещается.
G-35-19 CLOSED. US0016 owns engine checkpoint, local-client durable storage и client recovery. Legacy aborted settlement не используется для ordinary EP35 unfinished battle; debug/tutorial context отдельно.
Tests NOT RUN: checkpoint-before-start; same composition/playerstate/seed replay; no navigable escape; failedwrite retryonly; atomicoldpending-orfinished; exactlyonce rewards/losses; no pending loss via autosave.
## D-35-48: ничья считается поражением игрока
Пользователь: «ничья равно поражению. игрок проиграл».
Existing Draw, включая NoCombatCapability и simultaneousdefeat, отображается/учитывается как player defeat для EP35 campaign outcome. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать hull0/смерть пилотов ради label. Победные награды не выдавать; exactly-once settlement/durable boundary D-35-47 сохраняются. Поражение боя само по себе не вводит новый gameover при живом player carrier; existing destroyed-player terminal semantics сохраняются.
G-35-20 Draw CLOSED D-35-48; G-35-20T Timeout CLOSED D-35-49. Interactive SeriesTurnCap также player defeat.
Tests NOT RUN: DrawNoCombatCapability_PlayerDefeat; SimultaneousDefeat_PlayerDefeat; DrawDoesNotFabricateDamageOrPilotDeath; Defeat_NoVictoryReward; OutcomeReceiptIdempotent. Timeout policy CLOSED D-35-49; проверки только запланированы, не PASS реализации.
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.
## D-35-50: завершённая группа больше не участвует в мире
Пользователь: «После завершённого боя убирать группу с карты».
После завершённого EP35 боя (win/loss, включая Draw и Timeout/SeriesTurnCap) участвовавшая pirate group становится retired: исчезает с карты, не патрулирует, не обнаруживает игрока и не может начать новый encounter. Это логическое завершение участия группы, не искусственное уничтожение: actual carrier hull/ammo и drone/pilot statuses сохраняются в settlement/history. Соседние группы не меняются; для них обычный перехват на resume сохраняется без иммунитета.
Retirement входит в exactly-once settlement receipt и durable finished checkpoint. До завершённого settlement, включая pending battle/recovery или отказ записи результата, navigable world не публикуется; повтор callback/Load не создаёт группу заново и не повторяет последствия. Continue pending по D-35-47 восстанавливает тот же бой с начала; retirement не служит выходом из незавершённого боя. A-35-20 и остаток G-35-15 о surviving world group закрыты этим решением; no-respawn сохраняется.
Acceptance / tests NOT RUN: WinLossDrawTimeout_SelectedGroupRetired; SurvivingHullAndPilotStateNotFabricated; RetiredGroupNeverCapturesAgain; NeighborGroupsRemainActive; RetirementAndReceiptExactlyOnce; PendingRecoveryDoesNotRetireEarly; FinishedCheckpointRestoresRetirement. UI использует authoritative retired state, а не локальное скрытие маркера.

Execution: expand Scenario/WorldEncounterData.cs with strict bounded result-history validation. Check unique bindings/unit IDs, exact group/member/drone ownership, retired/history equivalence and finished latest encounter. Legacy absent optional fields remain valid; provisional EP35 version1 world remains rejected. Active-turn save remains rejected; US16 supplies durable prebattle restart checkpoint. Roundtrip completed result and reject corrupted receipt before replacing live world.

Build PASS us15-tk4-build.log. Story tests after this push: Engine Ep0035Us15/Ep0035Us3/Ep0035Us13/SortieSettlement; Client Ep0035Us15/Ep0035Us3/ResultsPhaseTests/PanelHeaderTests. Native NOT RUN.
