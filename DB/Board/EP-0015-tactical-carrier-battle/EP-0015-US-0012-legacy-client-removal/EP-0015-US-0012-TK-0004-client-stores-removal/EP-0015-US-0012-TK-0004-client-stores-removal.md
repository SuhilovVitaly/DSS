---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0012-legacy-client-removal
ticket: EP-0015-US-0012-TK-0004-client-stores-removal
title: client-stores-removal
stage: done
layer: client
depends_on: ["EP-0015-US-0012-TK-0003-client-panels-removal"]
files_touched: 32
serves: [AC-02, AC-05]
created: 2026-11-05
revision: 3
---

# client-stores-removal

## Why
Хранилища и приём презентации без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/GameSession/{CombatEffectStore,CombatForecastCache,CombatReportAssets,CombatReportFormatter,CombatReportStore,CombatTrajectoryProjector}.cs` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/{CombatReportInbox,SnapshotBuffer.Presentation}.cs` | приём боевой презентации | Удалить |
| `src/DeepSpaceSaga.Client/UI/SpaceMapColorResolver.cs` | боевые цвета | Убрать по инвентаризации |

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
Client.Tests. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.
## Scope correction before edits — 2026-11-06

Remove the eight named stores and presentation sources. Their direct session-handle, snapshot, map and window consumers must switch to ordinary world snapshots in the same compilable cut. Preserve generic sequence reconciliation, command receipts, latest snapshot timestamps, game speed tracking, NPC heading samples, held maneuver releases and the separate Tactical Battle transport. Map marker color selection already dropped retired types in TK-0003; settings-only palette entries will be removed with the old visual settings.

Direct consumers:
- src/DeepSpaceSaga.Client/SnapshotBuffer.cs
- src/DeepSpaceSaga.Client/GameSessionHandle.cs
- src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Maneuver.cs
- src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.ManualAxes.cs

Retired test dependencies will be listed before editing tests.
## Test scope correction before edits

Delete suites that target removed stores, old combat trajectory, timing or presentation protocol:
- tests/DeepSpaceSaga.Client.Tests/CombatEffectTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatForecastCacheTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatPresentationTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportAssetsTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportInboxTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatReportStoreTests.cs
- tests/DeepSpaceSaga.Client.Tests/CombatTrajectoryTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0010TK0002Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0004Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0003Tests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0001Tests.cs

Retain world inputs, ordinary navigation and NPC prediction tests, load/defeat and dialogue routing, debug tuning with stale-frame rejection, and pure locale checks. In `NpcObservedTrajectoryTests` retire only these old protocol/forecast tests: `Combat_forecast_target_matches_the_visible_observed_model`, `PresentedFramesOnlyAffectTrend`, `CatchUpConsumesEveryPresentedFrame`, `LegacyAndPresentationObserveEquivalentMovement`, `FuturePacketAndDuplicateCannotRevealAi`, `EquivalentPresentedFramesIgnoreRenderFrequency`. The equivalent world motion and resets remain covered by the other tests in that suite.

Mixed files:
- tests/DeepSpaceSaga.Client.Tests/CombatReportFormatterTests.cs
- tests/DeepSpaceSaga.Client.Tests/DebugTuningTransportTests.cs
- tests/DeepSpaceSaga.Client.Tests/DefeatNavigationTests.cs
- tests/DeepSpaceSaga.Client.Tests/DialogueIntegrationTests.cs
- tests/DeepSpaceSaga.Client.Tests/PhaseThreeSessionRoundtripTests.cs
- tests/DeepSpaceSaga.Client.Tests/NpcObservedTrajectoryTests.cs
- tests/DeepSpaceSaga.Client.Tests/PointCombatUS0016TK0001Tests.cs

## Execution evidence — 2026-11-06

Removed legacy effects/forecast/trajectory/report stores and client presentation buffering. GameSessionHandle uses ordinary world snapshots and retains command results and simulation speed tracking; the independent tactical connection is unchanged. The full Client suite passed (2086/2086), with solution build (0 warnings/errors), scoped format and diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-stores-regression-01`. Other projects' full post-removal regression and native/human acceptance remain open.
