---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0014-legacy-foundation-removal
ticket: EP-0015-US-0014-TK-0004-content-cleanup
title: content-cleanup
stage: done
layer: content-data
depends_on: ["EP-0015-US-0014-TK-0003-contracts-fields-removal"]
files_touched: 24
serves: [AC-03, AC-05]
created: 2026-11-05
revision: 3
---

# content-cleanup

## Why
Контент без старого боя.

## Decisions
Решения пользователя D-USER-01…13 и правила боя `VData/Planning/V4.0` — см. [Documentation.md](../../Documentation.md). Этот тикет их не меняет.

## Assumptions
Baseline `main @ fb39a0cf`; формат сохранения — текущий на момент тикета (`SaveFormat.md`: Save19 до US-0011 TK-0004, Save20 после); `VData/Planning/V4.0` и `tools/DeepSpaceSaga.CarrierWingSim` в checkout (D-USER-07). Точные сигнатуры уточняются по исходникам перед реализацией.

## Code context
| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Combat/combat-rules.json` | старый бой | Удалить |
| `src/DeepSpaceSaga.Client/Data/Modules/{TorpedoLauncher,CountermeasureLauncher}/, src/DeepSpaceSaga.Client/Data/Commands/{TorpedoLauncher,CountermeasureLauncher}/` | боевые модули | Удалить |
| `src/DeepSpaceSaga.Client/Data/{module-types.json,Ships/ship-classes.json}` | типы | Убрать боевые |
| `src/DeepSpaceSaga.Client/Settings.json` | combatRules | Убрать |

## Public API after the change
—

## Implementation steps
1. Проверить загрузкой каждый сценарий.
2. Точный список файлов, символов и тестов — из `RemovalInventory.md` (US-0011 TK-0001); перед началом обновить Code context.
3. Срез компилируемый: перед коммитом сборка решения и все тестовые проекты зелёные (R-09); тесты удаляемого кода удаляются в этом же тикете, общие — переписываются без ослабления.

## Out of scope
Изменения вне описанного результата, баланс и правила боя сверх `VData/Planning/V4.0`.

## Invariants
Engine владеет исходами, RNG и командами; Contracts неизменяемы; общая математика — в Motion; Client отображает допущенные снимки и не вызывает Engine из Render. Числа боя — только из `Data/TacticalBattle`. Правила исполнения R-01…R-09 ([Documentation.md](../../Documentation.md#правила-исполнения)).

## Tests
Client.Tests content discovery. Сборка, scoped format, `git diff --check`. Процессы тестов с process-tree cap 300000 ms, результаты — в `<artifacts>/validation/ep-0015-tactical-carrier-battle/<unique-run>/ (на Windows по умолчанию <artifacts> = D:/DeepSpaceSaga/artifacts)`.

## Definition of Done
Критерии AC-03, AC-05 истории выполнены и проверены; diff просмотрен; фактические результаты записаны в карточку; отдельный коммит тикета создан; push — сразу, и только в одобренный внутренний remote (`*.amat.com` или внутренний IP); если `origin` указывает на внешний хост (github.com, gitlab.com, bitbucket.org и т. п.) — не пушить, остановиться и сообщить пользователю (D-USER-08, D-USER-09, R-06).

## Self-containment check
Родительская история задаёт критерии, эпик — решения и источники; правила и числа — `VData/Planning/V4.0/Spec` и `Data`. Перед расширением файлов — обновить Code context.

## Scope correction before edits — 2026-11-06

Registry-coupled rules, launchers, commands and laser were removed in US-0013 TK-0007. Current classes keep their identifiers only. Audit source and deployment, then remove the final armor, hull HP, auto-defense and attack AI fields from authored and Save20 world rows. Validate those historical fields at the legacy reader before discarding them. Preserve general crew assignment/skills, structure and economy validation.

- Edit: src/DeepSpaceSaga.Client/Scenarios/MarketProfiles/scenario.json
- Edit: src/DeepSpaceSaga.Engine/Scenario/LegacyCombatSaveReader.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/SaveMigration20.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs
- Edit: src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/CombatSessionRoundtripTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/LocalSessionIntegrationTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/ManualManeuverInputTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PhaseThreeSessionRoundtripTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatSessionFixture.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUiFixture.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0001Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/PointCombatUS0019TK0005Tests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ScenarioModuleCleanupTests.cs
- Edit: tests/DeepSpaceSaga.Client.Tests/TetrarchLoadoutTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/EconomyTimeContinuityTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ModuleHealthBootstrapTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/PointCombatUS0018TK0002Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/SaveMigration20Tests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/ScenarioArmorSchemaTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/TorpedoImpactTests.cs
- Edit: tests/DeepSpaceSaga.Engine.Tests/WeaponOperatorSchemaTests.cs
- Edit: tools/DeepSpaceSaga.Performance/DebugTuningNativeEvidence.cs
- Edit: tools/DeepSpaceSaga.Performance/ModulePanelNativeEvidence.cs

## Execution evidence — 2026-11-06

All six authored and deployed New Game entries load with current world content; verified the retired definitions and Settings key are absent and the three established hull class IDs remain as identity only. Current ScenarioData and Save20 emit no legacy armor, hull HP, attack AI, auto-defense or disable timer. Historical rows validate those fields before dropping them; the Save19 armor fixture retains 123 structure points and refuses negative armor. Scoped format, clean build/diff; 175 Contracts, 1,267 Motion, 2,024 Engine non-corpus, 1,963 Client, 42 fast Economy and 9 Performance tests passed in `D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-world-content-final-02/`.
