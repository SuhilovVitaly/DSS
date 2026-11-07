---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
title: "Удаление старого боя в Client"
stage: done
dependencies: [EP-0015-US-0011-legacy-removal-prep]
---

# Удаление старого боя в Client

## Связь с техническим заданием

Источник правил, данных и эталонов — `VData/Planning/V4.0` (Spec, Data, Reference). Решения и gaps — [Documentation.md](../Documentation.md). Требования ниже — целевой результат, не утверждение о реализации.

## User story

Как игрок, я больше не вижу на тактической карте элементов старого боя (прицел торпед и ПРО, панель угроз, уклонение, снаряды, сводка и отчёты боя); Client — верхний слой — удаляется первым, поэтому после каждого тикета игра собирается и работает.

## Зависимости

[EP-0015-US-0011-legacy-removal-prep](../EP-0015-US-0011-legacy-removal-prep/EP-0015-US-0011-legacy-removal-prep.md)

## Completion evidence

- **AC-01.** Карта: удалены `GameSessionScreen.{Aiming, Combat, CombatForecast, CombatReports, CounterAimPreview, Encounter, ThreatAlerts, ThreatPanel, TorpedoAimPreview, Countermeasures, PointProjectiles}`; боевые секции `GameSessionScreen.DebugTuning`; тактическая карта и маневрирование (EP-0013) работают.

- **AC-02.** Панели и хранилища: удалены `Controls/{CombatJournalPanel, CombatReportPanel, CombatReportPanelLayout, CombatSummaryPanel, ThreatPanel}`, `CombatEffectStore`, `CombatForecastCache`, `CombatReportAssets`, `CombatReportFormatter`, `CombatReportStore`, `CombatTrajectoryProjector`, `CombatReportInbox`, `SnapshotBuffer.Presentation`; боевые части `CommandsPanel`, `ModuleDiagram`, `ObjectInfoPanel`, `SpaceMapColorResolver`, `DebugTuningPanel` (секции launchers) убраны по инвентаризации; остальные панели работают.

- **AC-03.** Оболочка: боевые части `SkiaWindow` (CombatVisualSettings, CombatInputSettings, CombatAlertAudio, отчёты, поражение по бою), `GameSessionHandle` (боевая презентация), `SessionConnectionLoader` (`Combat.LoadIncompatible`) убраны; удалены `UI/CombatAlertAudio.cs`, `UI/CombatVisualSettings.cs`, `CombatInputSettings.cs`, `CombatMetricsWriter.cs`, `Data/UI/{combat-input, combat-visuals, combat-report-visuals}.json`, `Images/UI/CombatReports/*` и правило копирования в `DeepSpaceSaga.Client.csproj:78`; `DefeatScreen` — по решению инвентаризации.

- **AC-04.** Локаль: строки старого боя (≈ 344) удалены из `Russian.json` и `English.json`; наборы ключей совпадают.

- **AC-05.** После каждого тикета решение собирается и вся регрессия зелёная (R-09).

Это план будущих наблюдений, а не выполненная проверка. Automated/render, native/input и product/human acceptance фиксируются раздельно.

## Ограничения и gaps

Списки уточняются по RemovalInventory (US-0011 TK-0001).

## Тикеты

| Ticket | Layer | Story criteria |
|---|---|---|
| [EP-0015-US-0012-TK-0001-client-map-removal-1](EP-0015-US-0012-TK-0001-client-map-removal-1/EP-0015-US-0012-TK-0001-client-map-removal-1.md) | client | AC-01, AC-05 |
| [EP-0015-US-0012-TK-0002-client-map-removal-2](EP-0015-US-0012-TK-0002-client-map-removal-2/EP-0015-US-0012-TK-0002-client-map-removal-2.md) | client | AC-01, AC-05 |
| [EP-0015-US-0012-TK-0003-client-panels-removal](EP-0015-US-0012-TK-0003-client-panels-removal/EP-0015-US-0012-TK-0003-client-panels-removal.md) | client | AC-02, AC-05 |
| [EP-0015-US-0012-TK-0004-client-stores-removal](EP-0015-US-0012-TK-0004-client-stores-removal/EP-0015-US-0012-TK-0004-client-stores-removal.md) | client | AC-02, AC-05 |
| [EP-0015-US-0012-TK-0005-client-shell-removal](EP-0015-US-0012-TK-0005-client-shell-removal/EP-0015-US-0012-TK-0005-client-shell-removal.md) | client | AC-03, AC-05 |
| [EP-0015-US-0012-TK-0006-client-assets-locale-removal](EP-0015-US-0012-TK-0006-client-assets-locale-removal/EP-0015-US-0012-TK-0006-client-assets-locale-removal.md) | client | AC-03, AC-04, AC-05 |

## Historical planning review

Draft; human approval отсутствует. Решения и сообщения согласования записываются в review log эпика.

All six Client tickets implemented. Latest full Client suite 2016/2016 PASS; full cross-project post-removal regression and native/human acceptance remain open.
