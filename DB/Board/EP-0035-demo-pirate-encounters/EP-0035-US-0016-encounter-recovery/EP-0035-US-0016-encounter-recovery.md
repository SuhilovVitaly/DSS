---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0016-encounter-recovery
title: Восстановление того же боя и durable последствия
stage: approved
approval_kind: workflow
implementation_status: not-started
dependencies: [EP-0035-US-0015-squad-battle-continuation, EP-0035-US-0017-world-integration]
revision: 1
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Восстановление того же боя и durable последствия

## User story / D-35-47
Пользователь: «Да, принимаем восстановление того же боя с начала после закрытия приложения или сбоя. Добавляй в тикеты».
Continue возвращает тот же captured encounter с начала, а не свободную карту. Durable prebattle checkpoint фиксирует exact player/company state, group/member/drone composition и seed. После win/loss последствия exactly once и durable checkpoint до выхода на карту/end; failed storage retry не пересчитывает settlement. Crash boundary oldpending replay либо durablefinished, промежуточный damage не сохраняется. G-35-19 CLOSED.

## Acceptance criteria
- AC01: checkpoint durable до tactical start; failure не начинает бой.
- AC02: restart same IDs/state/seed, no reroll/free escape.
- AC03: outcome once, durable before release; retry не restart/reapply.
- AC04: UI guards/recovery соответствуют normalwin/loss-only; никакого perturn resume.

## Grounding
SimulationEngine.cs:1113–1115 CaptureSaveStateCore запрещает active tactical save. LocalGameSessionConnection.cs:145–180 receipt/cache/retry continuation write. SortieSettlement.cs:17 atomic consequences before clear. Эти owner paths расширяются, не новая save subsystem.

## Ticket map
- [EP-0035-US-0016-TK-0001-engine-checkpoint](EP-0035-US-0016-TK-0001-engine-checkpoint/EP-0035-US-0016-TK-0001-engine-checkpoint.md): Pending encounter и фиксированный replay state, engine.
- [EP-0035-US-0016-TK-0002-durable-storage](EP-0035-US-0016-TK-0002-durable-storage/EP-0035-US-0016-TK-0002-durable-storage.md): Durable границы запуска и завершения боя, local-client.
- [EP-0035-US-0016-TK-0003-recovery-client](EP-0035-US-0016-TK-0003-recovery-client/EP-0035-US-0016-TK-0003-recovery-client.md): Продолжение pending боя и ошибки checkpoint, client.

## Non-goals / validation
No perturn saves, retreat, narrative. Implementation NOT STARTED; tests/native/product NOT RUN. Сначала US15, затем engine→storage→client; US6 docs последняя. Routine serialization/schema/version compatibility уточнить по owning save version, не назначать конкурирующую writer version.


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