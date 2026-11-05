---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0013-legacy-engine-removal
title: "Удаление старого боя в Engine.LocalClient и Engine"
stage: draft
dependencies: [EP-0015-US-0012-legacy-client-removal]
---

# Удаление старого боя в Engine.LocalClient и Engine

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как разработчик, после удаления Client-потребителей я удаляю старый бой из Engine.LocalClient и Engine, чтобы бой V4 остался единственной боевой системой Engine, а небоевые механики (маневрирование, скорость, диалоги) работали как раньше.

## Зависимости

[EP-0015-US-0012-legacy-client-removal](../EP-0015-US-0012-legacy-client-removal/EP-0015-US-0012-legacy-client-removal.md)

## Completion evidence

- **AC-01.** Engine.LocalClient: `LocalGameSessionConnection` не реализует `ICombatPresentationConnection` и не держит канал `CombatPresentationUpdate` (`:13,22-24,98`); `SnapshotMailbox` не сливает `CombatReports` (`:17-35`).

- **AC-02.** Engine: удалены `SimulationEngine.{Combat, CombatJournal, CombatPersistence, CombatReport*, Countermeasures, Encounter, Evasion, HostileCombat, HostileFire, NpcDodge, PointAim, PointCombat*, WeaponOperators, DebugTuningProjectiles, DebugTuningReloads}`, `Engine/Combat/*`, боевые части `SimulationEngine.cs`, `Presentation`, `PlayerDefeat`, `Time` (`IsDefeated` по бою), боевые потоки `RngStreamNames`; `InternalsVisibleTo DeepSpaceSaga.CombatAnalyzer` убран. Нарезка по файлам — так, чтобы каждый срез собирался (partial-файлы ссылаются друг на друга, решает инвентаризация).

- **AC-03.** Контент Engine: `CombatRulesDefinition`, загрузка `combat-rules`, `GameDataRegistry.CombatRules` удалены, боевые поля `ModuleTypeDefinition` и `DebugTuningState` убраны; судьба `armorClasses` — по решению инвентаризации; `tools/DeepSpaceSaga.EconomyBalance/ClusterBalanceRun.cs:141` исправлен в том же тикете.

- **AC-04.** Небоевое почищено, не удалено: `TurnHold`, `SpeedHold`, `RuntimeMotion` (`ClearEvasionControls`/`Evasion`), `Dialogue/SimulationEngine.Dialogue.cs:209` (encounter); маневрирование, скорость и диалоги работают.

- **AC-05.** DebugTuning (EP-0013) — только небоевые оверрайды; схема версии +1; старые файлы с боевыми полями — понятный отказ, который Client пишет в InterfaceLog, остальные оверрайды применяются.

- **AC-06.** Save-DTO и проверки старого боя (`Scenario/{CombatReactionSaveData, CombatReportsSaveData, CombatReportsSaveValidation, CombatSaveData, CountermeasureSaveData, HostileCombatSaveData, PointCombatContinuationValidation, PointCombatSaveData}`) удалены; миграция 18/19 → 20 читает старые секции через минимальный legacy-reader и проходит на фикстурах US-0011.

- **AC-07.** После каждого тикета решение собирается и вся регрессия зелёная (R-09).

- **AC-08.** После удаления боевого кода Engine и его тестов и до удаления определений модулей: сценарии (`Scenarios/*/scenario.json`, в т.ч. `Default` `:192,209`) не ставят на корабли модулей старого боя; сценарии `CombatMukha`, `CombatMukhaSingle` удалены; New Game создаёт мир без боевых модулей; все сценарии загружаются.

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Списки уточняются по RemovalInventory (US-0011 TK-0001).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0013-TK-0001-local-client-removal](EP-0015-US-0013-TK-0001-local-client-removal/EP-0015-US-0013-TK-0001-local-client-removal.md) | engine-local-client | AC-01, AC-07 |
| [EP-0015-US-0013-TK-0002-engine-removal-1](EP-0015-US-0013-TK-0002-engine-removal-1/EP-0015-US-0013-TK-0002-engine-removal-1.md) | engine | AC-02, AC-07 |
| [EP-0015-US-0013-TK-0003-engine-removal-2](EP-0015-US-0013-TK-0003-engine-removal-2/EP-0015-US-0013-TK-0003-engine-removal-2.md) | engine | AC-02, AC-07 |
| [EP-0015-US-0013-TK-0004-engine-removal-3](EP-0015-US-0013-TK-0004-engine-removal-3/EP-0015-US-0013-TK-0004-engine-removal-3.md) | engine | AC-02, AC-07 |
| [EP-0015-US-0013-TK-0005-engine-removal-4](EP-0015-US-0013-TK-0005-engine-removal-4/EP-0015-US-0013-TK-0005-engine-removal-4.md) | engine | AC-02, AC-07 |
| [EP-0015-US-0013-TK-0006-scenario-module-cleanup](EP-0015-US-0013-TK-0006-scenario-module-cleanup/EP-0015-US-0013-TK-0006-scenario-module-cleanup.md) | content-data | AC-08, AC-07 |
| [EP-0015-US-0013-TK-0007-engine-content-removal](EP-0015-US-0013-TK-0007-engine-content-removal/EP-0015-US-0013-TK-0007-engine-content-removal.md) | engine | AC-03, AC-07 |
| [EP-0015-US-0013-TK-0008-engine-noncombat-cleanup](EP-0015-US-0013-TK-0008-engine-noncombat-cleanup/EP-0015-US-0013-TK-0008-engine-noncombat-cleanup.md) | engine | AC-04, AC-07 |
| [EP-0015-US-0013-TK-0009-debug-tuning-cleanup](EP-0015-US-0013-TK-0009-debug-tuning-cleanup/EP-0015-US-0013-TK-0009-debug-tuning-cleanup.md) | engine | AC-05, AC-07 |
| [EP-0015-US-0013-TK-0010-legacy-save-dto-removal](EP-0015-US-0013-TK-0010-legacy-save-dto-removal/EP-0015-US-0013-TK-0010-legacy-save-dto-removal.md) | engine | AC-06, AC-07 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
