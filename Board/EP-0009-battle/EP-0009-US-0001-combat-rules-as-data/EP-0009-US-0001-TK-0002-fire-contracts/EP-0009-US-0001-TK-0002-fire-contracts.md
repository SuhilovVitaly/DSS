---
epic: EP-0009-module-combat
story: EP-0009-US-0001-combat-rules-as-data
ticket: EP-0009-US-0001-TK-0002-fire-contracts
title: "fire-contracts"
stage: draft
layer: contracts
depends_on: ["EP-0009-US-0001-TK-0001-module-health-contracts"]
files_touched: "ground-before-edit"
serves: ["US1-AC2", "US1-AC3"]
created: 2026-10-12
---

# EP-0009-US-0001-TK-0002-fire-contracts

STATUS: NOT STARTED

**Задача.**
1. `LauncherCombatSnapshot` получает `ModeId`, `ReadyAtMotionTimeMs`, `IsBrokenUntilDock`, `ActiveTorpedoIds[]`. `ActiveTorpedoObjectId` (EP-0006/0008) удаляется.
2. `DefenseSnapshot` получает `Barrels`, `PendingBurstShots[]` (время, ID торпеды, номер ствола) и `ReadyAtMotionTimeMs`. Перезарядка EP-0008 «после удаления» в контракте больше не описывается.
3. `TorpedoSnapshot`: константа `HitChancePercent` (100 в EP-0006) заменяется полем `HitChanceTenths`. Захваченная атака хранится рядом с `CapturedManeuverability` из EP-0008.
4. Константа `TorpedoLauncherCommandTypes.SetMode = "torpedo.setMode"` и параметр `ModeId` в `PlayerCommand`, если на HEAD нет общего поля параметра.
5. `DefenseLaunchPreview` (EP-0008, `02c89d4`) получает разложение шанса: атака, защита, база.

**Файлы:** `CombatSnapshot.cs`, `CountermeasureSnapshot.cs` (или файл с `DefenseSnapshot`), `PlayerCommand.cs`, `tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs`, `tests/DeepSpaceSaga.Contracts.Tests/DefenseLaunchPreviewTests.cs`.
**Тесты:** `Launcher_mode_ready_and_multiple_torpedoes_roundtrip`, `Defense_burst_queue_roundtrip`, `Torpedo_hit_chance_not_constant`, `Preview_has_attack_defense_breakdown`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0001-combat-rules-as-data.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
