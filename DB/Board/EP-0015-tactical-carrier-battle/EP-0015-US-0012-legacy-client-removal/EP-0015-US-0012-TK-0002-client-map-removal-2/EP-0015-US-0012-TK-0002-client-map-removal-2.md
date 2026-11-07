---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
ticket: EP-0015-US-0012-TK-0002-client-map-removal-2
title: client-map-removal-2
stage: done
layer: client
depends_on: ["EP-0015-US-0012-TK-0001-client-map-removal-1"]
files_touched: 10
serves: [AC-01, AC-05]
created: 2026-11-05
revision: 3
---

# client-map-removal-2

## Why
Карта без старого боя, часть 2.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatAlerts.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ThreatPanel.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.TorpedoAimPreview.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Countermeasures.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.PointProjectiles.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.DebugTuning.cs` | панель тюнинга | Убрать боевые секции |

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
Client.Tests регрессия карты. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-01, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

The five named realtime render/input partials were removed together with their shared private state in TK-0001 (verified SHA `bb267c7d9e9d08fcdf7f97b4142e60030f98278e`). This ticket removes debug tuning weapon presentation, keeps editable engine rows, actual maneuver bindings, help, capture and resets, and redirects the three historical debug test destinations to the current run. Existing authoritative tuning schema remains through the Engine removal tickets.
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.DebugTuning.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/DebugTuningPanel.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningPanelLayoutTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningLocalizationTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningDefaultsFileTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningFileWriterTests.cs
- src/DeepSpaceSaga.Client/Data/Locale/English.json
- src/DeepSpaceSaga.Client/Data/Locale/Russian.json

Direct consumer: 	ests/DeepSpaceSaga.Client.Tests/ManeuverHintsTests.cs verifies only the current maneuver help; old threat panel assertions belong to the retired map.

## Execution evidence — 2026-11-06

Debug tuning now shows six engine parameters in two sections and lists only live maneuver/debug bindings in RU and EN. Existing authoritative reset, edits, persistence and hover/input coverage remain. Redirected historical test output paths to the configured validation root. Windows build 0 warnings/errors, format and diff PASS; 256 selected Client cases PASS in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-map-removal-tuning-02` (includes locale renders, map input and tactical entry). Native/human acceptance and full post-removal regression remain open.
