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

STATUS: IMPLEMENTED — additive fire Contracts; final removals deferred to runtime migration

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

## Grounded scope before implementation — 2026-10-12

TK-0001 SHA6baef18139b177340ba329f272ac22835a96d8dc published/remote verified. `PlayerCommand` has no general mode field; `DefenseLaunchPreview` is its own Contracts file, so expand planned scope before edit. Exact files: src/DeepSpaceSaga.Contracts/CombatSnapshot.cs, CountermeasureSnapshot.cs, DefenseLaunchPreview.cs, PlayerCommand.cs; tests/DeepSpaceSaga.Contracts.Tests/CombatSnapshotTests.cs, DefenseLaunchPreviewTests.cs; this card, ImplementationStatus.md, PublicationStatus.md. Add snapshots as optional fields so Engine/Client compiled consumers continue through migration. `ActiveTorpedoObjectId` and constant `HitChancePercent` remain actively used until US-0005 and US-0004/0011 respectively (A08); final deletion criteria stay OPEN. Existing `Roll` is reused. No runtime change claimed.

## Execution evidence — 2026-10-12

Current Contracts now carry launcher mode/ready/broken/multiple-flight identity, defense barrel queue/ready, captured torpedo attack/chance, module-addressed `torpedo.setMode` parameter, and preview attack/defense/base. The new arrays are nullable: null preserves old snapshot identity; empty/nonempty are reserved for the migrated runtime. Existing engine/client still use old fields until US-0004/0005/0011 migration. Four named real-JSON tests PASS. First full Contracts run exposed seven `ImmutableArray` default/empty record-equality regressions; changed absent collections to nullable, then full Contracts192/192 PASS. Solution build0 warnings/errors; scoped format/diff PASS.

Self-review: no gameplay mode/burst/chance claim yet. `HitChancePercent` and `ActiveTorpedoObjectId` final removal remains OPEN and is assigned to consumer migration by A08. Per-ticket commit/push follows; own SHA recorded by next ticket after remote verification. No game/native/Graphify run.
