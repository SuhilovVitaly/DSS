---
epic: EP-0009-module-combat
story: EP-0009-US-0002-module-and-ship-catalog
ticket: EP-0009-US-0002-TK-0003-structure-normalization
title: "structure-normalization"
stage: draft
layer: content-data
depends_on: ["EP-0009-US-0002-TK-0002-engine-catalog"]
files_touched: "ground-before-edit"
serves: ["US2-AC2"]
created: 2026-10-12
---

# EP-0009-US-0002-TK-0003-structure-normalization

STATUS: IMPLEMENTED; separate publication follows

Execution scope before edit: all 12 JSON files under `src/DeepSpaceSaga.Client/Data/Modules/**` (15 implementations), including the three-cabin living quarters and the new engine/defense variants; new `tests/DeepSpaceSaga.Client.Tests/ModuleStructureCatalogTests.cs`; existing Client tests that assert former structure maxima; this card plus EP-0009 status records. This exceeds the historical five-file estimate because the acceptance criterion says every module. `Scenarios/Default`, `Default_500`, `Docked`, `Undocked`, `PlayerShipOnly`, and `MarketProfiles` contain `structurePoints` >150 and are inventory-only here; actual scenario migration is assigned to US-0009-TK-0003. No Graphify/game/native runs.

Scope correction before scenario edit: after normalizing catalog maxima, real `CreateEngineFromSettingsFile` and scenario bootstrap tests fail with `structurePoints 400 is outside 0..150`. To preserve a loadable intermediate project, move the narrow above-max normalization of those six scenario JSON files into this ticket. The later US-0009-TK-0003 still owns combat loadouts, armorClass, crew skills, and all other scenario migration. Related tests with fixed old health values will be updated only as failures demonstrate.

Implementation: 15 module implementations across 12 catalog files now have structurePointsMax150 (10 JSON files changed; 2 already complied). Bridge navigation computer has `combatRole: bridge`. Six shipped scenario files had only above-max structurePoints 400/180 normalized to150; the later scenario ticket must retain these values while adding combat-specific data. Content regression checks all types and verifies D22 one/two-hit arithmetic from configured launcher damage, piercing, and light armor; it does not claim runtime damage behavior. Focused Client26/26; Engine non-corpus1946/1946; Client full1860/1860 after updating the intentional Default_500 source hash; solution build0 warnings/errors; scoped format/diff PASS. Graphify/game/native NOT RUN per user.

**Задача.**
1. `structurePointsMax` = 150 у всех модулей `Data/Modules/**`, включая `living.quarters.mk1.three-cabins` из EP-0008.
2. `combatRole: "bridge"` у мостика.
3. Текущие `structurePoints` > 150 в сценариях исправляются в US-0009-TK-0003. Здесь только составляется список мест.

**Файлы:** JSON модулей (больше 5 — по A12, с обоснованием), `tests/DeepSpaceSaga.Client.Tests/ModuleStructureCatalogTests.cs` (новый).
**Тесты:** `All_modules_structure_150`, `Rule_one_hit_unarmored_two_hits_light`.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0002-module-and-ship-catalog.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
