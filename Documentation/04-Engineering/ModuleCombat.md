# Module combat — EP-0009 current contract

Implemented on `base-fight` after EP-0008. The [canonical Engine requirements](../01-Requirements/EngineRequirements.md#phase-four-module-combat) and [EP-0009 specification](../../Board/EP-0009-battle/EP-0009-module-combat.md) define the rules; this page describes the shipped boundaries and evidence. Earlier [basic torpedo](BasicTorpedoCombat.md), [countermeasure](CountermeasureCombat.md) and [EP-0008](PhaseThreeCombatAcceptance.md) reports remain dated history.

## Runtime and content

`Engine` owns module armor/structure, disable timers, damage, destruction, attack/defense rolls, named RNG, hostile AI and terminal defeat. `Contracts` carries authoritative snapshots, commands and journal facts. `Motion` provides shared guidance and relative-orbit math; `Client` only displays facts and sends commands. Production data lives under `src/DeepSpaceSaga.Client/Data/Modules`, `Data/Combat/combat-rules.json`, `Data/Combat/fire-modes.json`, `Data/Ships/ship-classes.json` and `Scenarios/CombatMukha|CombatShmel`.

Chance is `clamp(50 + attack - defense, 0, 100)` for torpedo contact and PRO intercept. Operators contribute current accuracy/maneuverability skills to their assigned systems. A torpedo can miss at contact, independently of its guided route. Module selection uses the configured mass weighting. Modules have separate armor and structure; a hit can temporarily disable a surviving module. Destruction of a critical bridge/engine or the configured fraction of modules ends the ship. `HullCombat` is no longer the damage source.

Torpedo mode `standard`, `maneuverability` or `overheat` is captured for the shot; mode changes, reload, breakdown and active flight identities remain authoritative. Launchers can have multiple active torpedoes, subject to readiness. Automatic and manual PRO share physical deadlines and actual projectile identity; the old single auto-attempt flag is removed. NPCs use their configured flank/radius AI and module/operator data. The two combat scenarios have three hostile Mukha or Shmel ships respectively; Shmel carries a single-barrel PRO. Existing scenarios keep their explicitly migrated skills/armor, rather than silently inheriting the new encounter loadout.

## Save/Load and UI

Current save writer is **SaveFormatVersion 17**, with defense payload 3, combat payload 2 and hostile state 2. It stores module durability, disable state, launcher mode/readiness/breakdown/active flights, PRO queues, AI parameters and named RNG state/counters. Save files of version 16 or earlier are rejected before replacing the current world; NewGame scenario version 0 is not a legacy save migration. Saving a defeated session remains prohibited. The selected-ship panel and map bar show authoritative module condition, and the combat journal shows hit/intercept rolls, module damage, skips, breakdown and death causes. Unknown enemy modules remain masked.

## Evidence and open acceptance

[Implementation status](../../Board/EP-0009-battle/ImplementationStatus.md), [100-seed Engine balance](../../Board/EP-0009-battle/evidence/balance.md) and [native matrix](../../Board/EP-0009-battle/evidence/native.md) separate automated proof from open acceptance. The real Engine won 67/100 CombatMukha and 51/100 CombatShmel runs under the documented test policy; Mukha is 14.2 percentage points below the historical simulator reference. The old Python simulator source was absent; the [new independent sampler](../../src/DeepSpaceSaga.CombatSim/README.md) reads production JSON but uses simplified geometry and does not certify Engine equivalence. Game/native runs, real-window UI, human balance judgement and Graphify rebuilding were explicitly deferred by the user. They are NOT RUN/SKIPPED, not PASS.

## Продолжение EP-0010 — 2026-10-16

[Боевые доклады](CombatReports.md) добавляют presentation facts E01–E07 поверх этого модульного боя и optional CombatReportsState Version1 внутри Save17. Формулы, конфигурация, journal, RNG и баланс EP9 сохраняются. [Evidence/публикация](../../Board/EP-0010-combat-reports/ImplementationStatus.md); native/human/FPS OPEN. Graphify обновлён в EP10; прежний skip остаётся датированной историей EP9. Самостоятельные игровые запуски агентом запрещены пользователем.
