---
epic: EP-0009-module-combat
story: EP-0009-US-0011-save-and-combat-acceptance
ticket: EP-0009-US-0011-TK-0002-combat-acceptance-and-balance
title: "combat-acceptance-and-balance"
stage: draft
layer: engine
depends_on: ["EP-0009-US-0011-TK-0001-save-format-17"]
files_touched: "tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs; tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs; DB/Board/EP-0009-battle/evidence/balance.md; DB/Board/EP-0009-battle/ImplementationStatus.md; DB/Board/EP-0009-battle/PublicationStatus.md"
serves: ["US11-AC2"]
created: 2026-10-12
---

# EP-0009-US-0011-TK-0002-combat-acceptance-and-balance

STATUS: HEADLESS PASS; BALANCE DISCREPANCY DOCUMENTED; NATIVE NOT RUN

**Файлы:** `tests/DeepSpaceSaga.Engine.Tests/ModuleCombatEndToEndTests.cs` (новый), `tests/DeepSpaceSaga.Engine.Tests/CombatBalanceReport.cs` (новый; категория `Balance`, вне быстрого набора), `DB/Board/EP-0009-module-combat/evidence/balance.md`.

## Actual scope and result

The planned evidence path uses the epic ID as a directory, but the actual Board directory is `EP-0009-battle`; the evidence file therefore lives at `DB/Board/EP-0009-battle/evidence/balance.md`. The two tests use the shipped scenario files, production registry, actual hostile AI, player `Fire` commands, module-hit journal and defeat outcome. No game process or native UI was started.

The focused two-scenario proof passed. The separate `Category=Balance` run passed 2/2 over seeds 1..100 per scenario. `CombatMukha` won 67% versus historical simulator 81.2% (−14.2 pp, outside ±10 pp); `CombatShmel` won 51% versus 49.5% (+1.5 pp). Each scenario had one 600-second timeout, recorded as no victory. This discrepancy is expressly non-blocking under US11-AC2; the historical simulator and its exact `AttackAI` source are absent from this checkout. See [balance evidence](../../evidence/balance.md) for policy, metrics and limitations.

Checks: focused `FullyQualifiedName~ModuleCombatEndToEndTests` 2/2 PASS after final code change; `Category=Balance` 2/2 PASS (200 Engine battles); scoped `dotnet format --verify-no-changes` PASS. The long Balance category is separate from the quick regression set. Full Engine corpus remains NOT COMPLETE from the earlier scenario migration; native/game NOT RUN.

Self-review: the driver fires only when the authoritative launcher is ready, uses stable seed/target order and stops on real defeat or destruction of all three NPCs. It does not fake hostile outcomes or consume an independent RNG. The driver is a reproducible test policy, not an assertion that it matches the historical simulator's player decision policy. Game/native and human acceptance remain OPEN; Graphify rebuild SKIPPED by user instruction.

## Canonical context and execution

[Specification](../../EP-0009-module-combat.md) sections2–4 define D01–D33, A01–A07, event order, values and boundaries. [Parent story](../EP-0009-US-0011-save-and-combat-acceptance.md). [Current execution decisions](../../ExecutionDecisions.md) supersede historical baseline/push/Graphify instructions. Exact production/test paths and current APIs must be recorded before editing; planned file count is not the runtime scope. Every ticket receives tests appropriate to its change, self-review, a separate commit and immediate verified push to base-fight. No game/native/Graphify runs.
