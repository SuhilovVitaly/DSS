---
epic: EP-0035-demo-pirate-encounters
story: EP-0035-US-0013-squad-battle-assembly
title: Два пиратских носителя и их дроны в одном бою
stage: draft
approval_kind: workflow
implementation_status: not-started
dependencies: [EP-0035-US-0012-pirate-squadrons, EP-0035-US-0003-world-battle]
created: 2026-11-17
current_review: complete
revision: 1
source_request: "Эскадра пока два Логова; оба носителя и все дроны одновременно на поле; вероятность10% меняется консолью."
---

> Current authority: D-35-50 и текущая сводка эпика supersede исторические G/A ниже. Закрытые product gaps не блокируют исполнение автоматически; actual source/allowlist review и acceptance остаются обязательными. Implementation NOT STARTED.

# Два пиратских носителя и их дроны в одном бою

## User story
Как игрок, я встречаю одну карту-фишку эскадры из двух Логов, а в бою вижу оба носителя и их дронов одновременно. Identity, наблюдение и потери сохраняются на всех переходах.

## Decisions and gaps
D-35-21: один автоматический modal report + pause на stable group за кампанию; повторный контакт без modal/pause.
D-35-22/23/24: squad one token, min operational speed, simultaneous battle, default chance10% с debug настройкой.
D-35-25: DEMO строго2xЛогово,6+6 drones; ранее2–3/mixed отложено. Player carrier — третий носитель на поле. Танкред остаётся одиночным типом.
A-35-11: независимый draw на создание каждой новой группы,90% одиночная/10% squad; изменение влияет на future groups, не contact/Load.
G-35-12 CLOSED D-35-32 geometry/sensors; presentation two types+one approximate total — A; G-35-13: override lifetime; initial generation CLOSED D-35-34; G-35-14 CLOSED D-35-26: last-carrier death disables surviving drones; G-35-15: частичный исход и world survivors. Применимые G блокируют исполнение, не создание draft.

## Grounding
src/DeepSpaceSaga.Engine/Content/DemoCampaignDefinitions.cs:23 — DemoPirateGroup имеет один LoadoutId. TacticalBattle/Content/TacticalBattleDefinitions.cs:52 — BattleSide имеет один Carrier. TacticalBattle/TacticalBattleAssembly.cs:28 — сборка читает side.Carrier. TacticalBattle/TacticalBattleState.cs:100–102 — Midpoint/Carrier(side) опираются на одиночного носителя. TacticalBattle/TacticalBattle.cs:54–56 — outcome проверяет Carrier(side). TacticalBattle/Ai/TacticalCommander.cs:10 — защита выбирает Carrier(side). Client/UI/DebugConsole/DebugCommandCatalog.cs:6 — регистрация команд.

## Acceptance criteria
- AC-01: src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs,src/DeepSpaceSaga.Contracts/TacticalBattleRequests.cs
- AC-02: src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs,src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs,src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs
- AC-03: src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs,src/DeepSpaceSaga.Engine/TacticalBattle/OwnedBattleAssembly.cs,src/DeepSpaceSaga.Engine/SimulationEngine.WorldEncounterBattle.cs,src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs
- AC-04: src/DeepSpaceSaga.Client/Data/TacticalBattle/scenarios.json

## Non-goals
Третий enemy carrier, mixed demo squad, новый эпик, production implementation и утверждение orphan policy без пользователя.

## Dependencies
EP-0035-US-0012-pirate-squadrons, EP-0035-US-0003-world-battle. Точный порядок тикетов ниже; документация US0006 после всех feature stories.

## Approved ticket map
Workflow materialization, не human approval технических A/G.
| Ticket | Layer | Result |
|---|---|---|
| [EP-0035-US-0013-TK-0001-contracts](EP-0035-US-0013-TK-0001-contracts/EP-0035-US-0013-TK-0001-contracts.md) | contracts | Боевые identity и ownership |
| [EP-0035-US-0013-TK-0002-schema-engine](EP-0035-US-0013-TK-0002-schema-engine/EP-0035-US-0013-TK-0002-schema-engine.md) | engine | Схема нескольких носителей стороны |
| [EP-0035-US-0013-TK-0003-assembly-engine](EP-0035-US-0013-TK-0003-assembly-engine/EP-0035-US-0013-TK-0003-assembly-engine.md) | engine | Сборка всех участников эскадры |
| [EP-0035-US-0013-TK-0004-content](EP-0035-US-0013-TK-0004-content/EP-0035-US-0013-TK-0004-content.md) | content-data | Совместимые боевые сценарии |

## Review and validation
Board grooming only. Dependencies/links/limits проверяются после материализации. Implementation/build/tests/native/product NOT RUN. Дальнейший drift owning files требует обновления allowlist перед implementation.


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
## D-35-49: истечение лимита ходов — поражение игрока
Пользователь ответил «да» на вопрос «Истечение лимита ходов тоже считаем поражением?».
EP35 campaign outcome/UI считает existing Timeout/SeriesTurnCap поражением игрока, как Draw по D-35-48. Сохранить actual hull/ammo/disabled/destroyed/pilot state; не фабриковать уничтожение носителя/дронов или смерть пилотов. Применить штатные defeat consequences exactlyonce, без victory rewards, затем durable finished checkpoint до map/end по D-35-47. Сам label defeat не вводит новый gameover при живом carrier.
G-35-20T CLOSED. Продуктовый грумминг EP35 завершён; технические A/allowlist/schema drift проверяет owning implementation, это не новые user blockers. Implementation NOT STARTED, tests/build/native/product acceptance NOT RUN/OPEN.
Tests NOT RUN: SeriesTurnCap_PlayerDefeat; TimeoutPreservesActualDamageAndPilotState; Timeout_NoVictoryRewards; TimeoutSettlement_ExactlyOnce; TimeoutFinishedCheckpoint_DurableBeforeRelease; TimeoutReplayReceipt_NoReapply.