---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0016-encounter-recovery
ticket: EP-0035-US-0016-TK-0003-recovery-client
title: Продолжение pending боя и ошибки checkpoint
stage: approved
approval_kind: workflow
implementation_status: implemented-tests-pending
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0035-US-0016-TK-0002-durable-storage]
files_touched: 5
serves: [AC01, AC02, AC03, AC04]
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Original planning baseline; implementation status and published commits: see current execution ledger.

# Продолжение pending боя и ошибки checkpoint

## Why / decisions
[История](../EP-0035-US-0016-encounter-recovery.md), D-35-47. Continue после appclose/crash relaunch pending same battle from start без navigable map или reroll; ordinary midbattle save/load/menu retreat недоступны. Обработка failedwrite показывает retry pending operation, не кнопку escape. Finished result отпускает map/end только после durableack. Debug/tutorial exception сохраняет явный context и не меняет ordinary campaign. Не показывать recovery как perturn resume.

## Code context / allowlist
- src/DeepSpaceSaga.Client/GameSessionHandle.cs
- src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
- src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleFlow.cs
- src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs
- tests/DeepSpaceSaga.Client.Tests/Demo/Ep0035Us0016Tk3Tests.cs
Grounding: SimulationEngine.cs CaptureSaveStateCore; LocalGameSessionConnection.ExitTacticalBattleAsync/WriteSaveAsync; existing receipt retry byBattleId. WorldEncounterBattle — owning prerequisite. Test новый; LocalClient integration matching Engine.Tests (отдельного проекта нет).

## API / implementation
Использовать existing pending encounter DTO/state owning US3/15, serializable committed capture и stable receipt. Сначала сверить actual save version/transport, затем выполнить steps выше в allowlist. No concurrent writer version. Не создавать midbattle snapshot format. Если реальные owner endpoints требуют больше5files, пересмотреть Board до production.

## Tests / DoD
NOT RUN: PendingDurableBeforeStart; StartWriteFailure_NoBattle; RestartSameBattleIdsSeedComposition; ReplayUsesExactPrebattlePlayerState; OutcomeReceipt_ExactlyOnce; FailedResultWrite_RetryOnlyStorage; AtomicCrashBoundary_OldPendingOrFinished; ContinuePending_NoNavigableWorld; AutosaveCannotErasePending; NoMidbattleOrdinarySave. Использовать fault injection existing storage seams, не только mock happy path. Focused Ep0035Us0016Tk3Tests matching project; absolute outputs D:/DeepSpaceSaga/artifacts/validation/ep0035-recovery/<unique-run>,300000ms processcap; build/format scoped. Native Continue/error UX отдельно. DoD: AC evidence, no duplicate reward/loss/freeescape, no perturn claim, docs last. Planning не разрешает build/production/commit.


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
Execution scope includes GameSessionScreen auto-transition and SkiaWindow atomic screen adoption. Loaded active encounter is queried and adopted into campaign Flow before map screen can be rendered; no Begin rerun. Recovery view uses exact engine state. Existing checkpoint retry modal retains finished result. Campaign escape navigation is guarded even between report dismissal and modal activation. Tests cover matching recovered battle identity and no command resubmission.

Build PASS us16-tk3-build.log; tests after publication: Engine Ep0035Us16/Us15/Us3 and Client Ep0035Us16/Us15/Us3/BattleNavigationTests. No native launch. Recovery adopts engine session before UI navigation and verifies exact battle binding.
