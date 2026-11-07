---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0014-legacy-foundation-removal
ticket: EP-0015-US-0014-TK-0003-contracts-fields-removal
title: contracts-fields-removal
stage: done
layer: contracts
depends_on: ["EP-0015-US-0014-TK-0002-contracts-dto-removal"]
files_touched: 24
serves: [AC-02, AC-05]
created: 2026-11-05
revision: 3
---

# contracts-fields-removal

## Why
Боевые поля в общих DTO.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/{AuthoritativeSnapshot,PlayerCommand,InstalledModuleSnapshot,CommandResult}.cs` | боевые поля и коды | Убрать |
| `src/DeepSpaceSaga.Contracts/{SpaceObjectType,SimulationSpeed,DebugTuningFileFormat,SessionOutcomeSnapshot}.cs` | боевые значения | Убрать по инвентаризации |

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
Contracts.Tests; сборка решения. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-02, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before edits — 2026-11-06

Remove the remaining realtime scalars, reason codes, object markers, speed gate and compiled consumers. Keep structure, disabled world controls, map navigation and debug tuning compatibility. Save18/19 world schema checks keep their internal raw Wreck spelling until the archive/content boundary cleanup in TK-0004.

- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs
- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs
- Edit: src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapMarkerPolicy.cs
- Edit: src/DeepSpaceSaga.Contracts/AuthoritativeSnapshot.cs
- Edit: src/DeepSpaceSaga.Contracts/CommandResult.cs
- Edit: src/DeepSpaceSaga.Contracts/InstalledModuleSnapshot.cs
- Edit: src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs
- Edit: src/DeepSpaceSaga.Contracts/SimulationSpeed.cs
- Edit: src/DeepSpaceSaga.Contracts/SpaceObjectType.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs
- Edit: src/DeepSpaceSaga.Engine/SimulationEngine.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/CombatMapVisualTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/CompactObjectLabelTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0013TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0020TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/SpaceMapColorResolverTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ScenarioModuleCleanupTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TorpedoInspectionTests.cs
- Edit: tests/DeepSpaceSaga.Contracts.Tests/InstalledModuleSnapshotTests.cs
- Edit: tests/DeepSpaceSaga.Contracts.Tests/ModulePanelContractTests.cs
- Edit: tests/DeepSpaceSaga.Contracts.Tests/PointCombatUS0020TK0006Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModuleHealthProjectionTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/ObjectInfoPanelTests.cs — preserve generic module visibility policy.

## Execution evidence — 2026-11-06

Removed realtime armor, damage timers, criticality, speed gate, obsolete reason codes and retired object type constants from public Contracts and their compiled consumers. Kept stable ordinary simulation speeds, structure, disabled engine reason and world geometry. Scoped format, zero-warning build and diff check; 175 Contracts, 1,267 Motion, 2,024 Engine (non-corpus), 1,961 Client, 42 fast Economy and 9 Performance checks passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-contract-fields-02/`.
