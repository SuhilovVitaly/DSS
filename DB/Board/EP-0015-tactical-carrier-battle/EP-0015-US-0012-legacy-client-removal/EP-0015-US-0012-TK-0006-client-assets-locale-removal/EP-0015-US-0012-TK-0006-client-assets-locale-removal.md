---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
ticket: EP-0015-US-0012-TK-0006-client-assets-locale-removal
title: client-assets-locale-removal
stage: done
layer: client
depends_on: ["EP-0015-US-0012-TK-0005-client-shell-removal"]
files_touched: 36
serves: [AC-03, AC-04, AC-05]
created: 2026-11-05
revision: 3
---

# client-assets-locale-removal

## Why
Ресурсы и локаль Client без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/UI/{combat-input,combat-visuals,combat-report-visuals}.json` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/Images/UI/CombatReports/` | иконки отчётов | Удалить |
| `src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj` | копирование CombatReports (`:78`) | Убрать правило |
| `src/DeepSpaceSaga.Client/Data/Locale/{Russian,English}.json` | строки старого боя | Удалить |

## Public API after the change
—

## Implementation steps
1. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
2. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Client.Tests; тест симметрии ключей. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03, AC-04, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Retire the three old UI JSON files, eight report images, explicit copy rule and realtime combat text. Keep and rename ordinary map, engine/module, command groups, maneuver errors and generic fields into the World namespace. TacticalBattle.* and Session.* remain independent. Remove debug text used only by retired launchers; validate whole RU/EN key and placeholder parity. Replace obsolete locale-only tests with the current contract; preserve current panel/input assertions.

Delete:
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-in-range.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-ship-destroyed.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-ship-hit.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-torpedo-intercepted.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/enemy-torpedo-launch.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-countermeasure-launch.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-ship-damaged.png
- src/DeepSpaceSaga.Client/Images/UI/CombatReports/player-torpedo-launch.png
- src/DeepSpaceSaga.Client/Data/UI/combat-input.json
- src/DeepSpaceSaga.Client/Data/UI/combat-visuals.json
- src/DeepSpaceSaga.Client/Data/UI/combat-report-visuals.json
- tests/DeepSpaceSaga.Client.Tests/CombatReportFormatterTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0004Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0016TK0001Tests.cs

Edit direct consumers:
- src/DeepSpaceSaga.Client/Localization.cs
- src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj
- src/DeepSpaceSaga.Client/Data/Locale/English.json
- src/DeepSpaceSaga.Client/Data/Locale/Russian.json
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.EnginePanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelText.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/PanelTime.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs
- tests/DeepSpaceSaga.Client.Tests/ManeuverHintsTests.cs
- tests/DeepSpaceSaga.Client.Tests/ModulePanelEngineTests.cs
- tests/DeepSpaceSaga.Client.Tests/ModulePanelLocalizationTests.cs
- tests/DeepSpaceSaga.Client.Tests/ModulePanelHealthTests.cs
- tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs
- tests/DeepSpaceSaga.Client.Tests/PhaseThreeLocaleTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningLocalizationTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0002Tests.cs
- tools/DeepSpaceSaga.Performance/ModulePanelNativeEvidence.cs

Retire the two tests specific to the deleted palette JSON: CombatPaletteContentTests.cs and CountermeasurePaletteTests.cs.

## Execution evidence — 2026-11-06

Removed three old UI data files, eight report images and their copy rule. Verified absence in deployed Client.Tests output (`assets.json`). RU/EN each have 546 keys with matching placeholders; no Combat.* or CombatReports.* remains, and surviving world text uses World.*. Client 2016/2016 PASS, solution build 0 errors/warnings, scoped format and diff PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-assets-regression-02`. Linux tactical workflows 42136456099, 42137064341 and 42137723668 PASS for panels, stores and shell. Full final cross-project and native/human acceptance remain open.
