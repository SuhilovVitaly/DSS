---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0014-legacy-foundation-removal
title: "Удаление старого боя в Motion, Contracts, контенте и инструментах"
stage: draft
dependencies: [EP-0015-US-0013-legacy-engine-removal]
---

# Удаление старого боя в Motion, Contracts, контенте и инструментах

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как разработчик, я удаляю последние следы старого боя в нижних слоях, контенте и инструментах, чтобы решение не содержало ни одного потребителя прежней системы.

## Зависимости

[EP-0015-US-0013-legacy-engine-removal](../EP-0015-US-0013-legacy-engine-removal/EP-0015-US-0013-legacy-engine-removal.md)

## Completion evidence

- **AC-01.** Motion: удалены `BlastMath`, `BlastThreatMath`, `CombatForecast`, `CombatRangeMath`, `EvasionMotionMath`, `TorpedoCollisionMath`, `HostileOrbitMath` (если инвентаризация не нашла других consumers).

- **AC-02.** Contracts: удалены `BattleReportSnapshot`, `CombatEncounterSnapshot`, `CombatJournalEntry`, `CombatPresentationUpdate` (вместе с `ICombatPresentationConnection`, объявленным в нём, `:31`), `CombatSnapshot`, `CountermeasureSnapshot`, `EvasionSnapshot`, `HostileCombatSnapshot`, `WeaponOperatorSnapshot`, `DefenseLaunchPreview`; убраны боевые поля `AuthoritativeSnapshot`, `PlayerCommand`, `InstalledModuleSnapshot` (`LauncherCombat`, `Operator`, `CombatRole`, `:61-76`), боевые reason codes `CommandResult` (`:130,139,166-169`), `SpaceObjectType.Countermeasure`, `SimulationSpeed.IsAllowedInCombat`, секция `launchers` `DebugTuningFileFormat`, `SessionOutcomeSnapshot` — по решению инвентаризации.

- **AC-03.** Контент: удалены `Data/Combat/combat-rules.json`, `Data/Modules/{TorpedoLauncher, CountermeasureLauncher}` (и `CombatLaser` по инвентаризации), `Data/Commands/{TorpedoLauncher, CountermeasureLauncher}`; `module-types.json`, `Ships/ship-classes.json`, `Settings.json` (`combatRules`) приведены в соответствие; все сценарии загружаются.

- **AC-04.** Инструменты: удалены `src/DeepSpaceSaga.CombatSim`, `tools/DeepSpaceSaga.CombatAnalyzer`, `tools/DeepSpaceSaga.Performance/{PointCombatNativeEvidence, CombatReportsNativeEvidence}.cs`; боевые части `DebugTuningNativeEvidence`, `ModulePanelNativeEvidence`, `SolarNativeEvidence`, `Program.cs` и `tools/DeepSpaceSaga.Performance.Tests` убраны; `tools/graphify/rebuild.py:95-99` не ссылается на удалённые файлы; `tools/DeepSpaceSaga.CarrierWingSim` не трогается; неотслеживаемые `tools/DeepSpaceSaga.CombatBalanceSim`, `tools/HexWingSimulator` — решение пользователя (G-07).

- **AC-05.** Поиск символов старого боя по `src`, `tests`, `tools` пуст (кроме решений «оставить» инвентаризации); решение собирается, вся регрессия зелёная (R-09).

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Списки уточняются по RemovalInventory (US-0011 TK-0001).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0014-TK-0001-motion-removal](EP-0015-US-0014-TK-0001-motion-removal/EP-0015-US-0014-TK-0001-motion-removal.md) | motion | AC-01, AC-05 |
| [EP-0015-US-0014-TK-0002-contracts-dto-removal](EP-0015-US-0014-TK-0002-contracts-dto-removal/EP-0015-US-0014-TK-0002-contracts-dto-removal.md) | contracts | AC-02, AC-05 |
| [EP-0015-US-0014-TK-0003-contracts-fields-removal](EP-0015-US-0014-TK-0003-contracts-fields-removal/EP-0015-US-0014-TK-0003-contracts-fields-removal.md) | contracts | AC-02, AC-05 |
| [EP-0015-US-0014-TK-0004-content-cleanup](EP-0015-US-0014-TK-0004-content-cleanup/EP-0015-US-0014-TK-0004-content-cleanup.md) | content-data | AC-03, AC-05 |
| [EP-0015-US-0014-TK-0005-tools-cleanup](EP-0015-US-0014-TK-0005-tools-cleanup/EP-0015-US-0014-TK-0005-tools-cleanup.md) | tooling | AC-04, AC-05 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.
