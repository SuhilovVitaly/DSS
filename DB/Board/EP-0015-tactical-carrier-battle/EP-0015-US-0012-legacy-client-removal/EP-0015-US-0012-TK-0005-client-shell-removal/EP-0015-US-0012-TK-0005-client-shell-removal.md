---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
ticket: EP-0015-US-0012-TK-0005-client-shell-removal
title: client-shell-removal
stage: done
layer: client
depends_on: ["EP-0015-US-0012-TK-0004-client-stores-removal"]
files_touched: 41
serves: [AC-03, AC-05]
created: 2026-11-05
revision: 3
---

# client-shell-removal

## Why
Оболочка Client без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | настройки и звук боя, отчёты, поражение по бою | Убрать |
| `src/DeepSpaceSaga.Client/{GameSessionHandle,SessionConnectionLoader}.cs` | боевая презентация | Убрать |
| `src/DeepSpaceSaga.Client/UI/{CombatAlertAudio,CombatVisualSettings}.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/{CombatInputSettings,CombatMetricsWriter}.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs` | поражение | По решению инвентаризации |

## Public API after the change
—

## Implementation steps
1. Клавиши маневрирования уже в `ManeuverInputSettings` (US-0011 TK-0003).
2. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
3. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

The Client no longer has an old combat transport (TK-0004). Remove its legacy audio, world defeat screen, telemetry and settings; keep ordinary move/debug keys and dedicated tactical keys, pause/restore, load errors, regular native probes, and tactical outcome only inside its modal. Keep scenario/asset deletions in their owning later tickets.

Delete:
- src/DeepSpaceSaga.Client/CombatInputSettings.cs
- src/DeepSpaceSaga.Client/CombatMetricsWriter.cs
- src/DeepSpaceSaga.Client/UI/CombatAlertAudio.cs
- src/DeepSpaceSaga.Client/UI/CombatVisualSettings.cs
- src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatScreen.cs
- src/DeepSpaceSaga.Client/UI/Screens/Defeat/DefeatLayout.cs

Edit direct consumers:
- src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
- src/DeepSpaceSaga.Client/GameSessionHandle.cs
- src/DeepSpaceSaga.Client/SessionConnectionLoader.cs
- src/DeepSpaceSaga.Client/UI/KeyboardEdgeTracker.cs
- src/DeepSpaceSaga.Client/TacticalBattleInputSettings.cs
- src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs
- src/DeepSpaceSaga.Client/Data/Locale/English.json
- src/DeepSpaceSaga.Client/Data/Locale/Russian.json
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualAxes.cs
- tools/DeepSpaceSaga.Performance/SolarNativeEvidence.cs
- tools/DeepSpaceSaga.Performance/ModulePanelNativeEvidence.cs
- tools/DeepSpaceSaga.Performance/DebugTuningNativeEvidence.cs

Test dependencies will be listed before editing them.
## Test scope correction before edits

Delete legacy-only settings, metrics, audio and defeat test suites:
- tests/DeepSpaceSaga.Client.Tests/CombatInputSchemaV2Tests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatMetricsWriterTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatVisualSettingsTests.cs
- tests/DeepSpaceSaga.Client.Tests/CountermeasureVisualSettingsTests.cs
- tests/DeepSpaceSaga.Client.Tests/DefeatScreenTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0011TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0003Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0005Tests.cs
- tests/DeepSpaceSaga.Client.Tests/TacticalPaletteRoutingTests.cs

Keep window capacity/edge routing, save error routing, debug file tests, maneuver and tactical keyboard behavior with current settings types in:
- tests/DeepSpaceSaga.Client.Tests/CompactObjectLabelTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningPanelInputTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningFileWriterTests.cs
- tests/DeepSpaceSaga.Client.Tests/DefeatNavigationTests.cs
- tests/DeepSpaceSaga.Client.Tests/ManualManeuverInputTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0012TK0001Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PhaseThreeSessionRoundtripTests.cs
- tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ManeuverInputSettingsTests.cs
- tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TacticalBattleInputSettingsTests.cs

The keyboard edge buffer case previously in DefeatNavigationTests remains on the real window fixture without a retired defeat scenario.

## Execution evidence — 2026-11-06

The client shell no longer loads or invokes real-time combat input, audio, visuals, metrics, point-fire APIs or world defeat. Ordinary maneuver/debug keys and tactical shortcut collision handling are retained; incompatible/malformed save messages belong to Session. Real window routing buffer coverage remains. Client 2026/2026 PASS, build (0 warnings/errors), scoped format and diff check PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-shell-regression-01`. Assets and remaining old locale entries are in TK-0006.
