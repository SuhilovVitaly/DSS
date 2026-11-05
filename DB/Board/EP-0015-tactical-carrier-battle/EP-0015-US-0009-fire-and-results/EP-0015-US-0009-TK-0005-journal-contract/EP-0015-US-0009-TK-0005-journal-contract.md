---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0009-fire-and-results
ticket: EP-0015-US-0009-TK-0005-journal-contract
title: Journal contract payloads
stage: done
layer: contracts
depends_on: ["EP-0015-US-0009-TK-0002-results-phase"]
files_touched: 2
serves: [AC-03]
created: 2026-11-06
revision: 1
---

# Journal contract payloads

## Why

The full retained journal must include exact shot chance, rear flag, per-target application, unit state and damaged system metadata. Optional additive payloads retain compatibility with earlier DTO consumers.

## Code context

- src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs: append enum member and typed payloads.
- tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs: legacy decoding and exact new payload round trip.

## Acceptance

Keep typed immutable snapshots and existing event ordinals. Captured evidence must survive later turns and preserve the real event order without changing Engine rules, RNG, outcomes or world state. Use Display §6 from the approved Markdown; do not require a reference simulator.

Build, scoped format, relevant owning tests, diff review and separate immediate push to the authorized origin with SHA verification. Diagnostic outputs only under the epic validation root.

## Execution evidence — 2026-11-06

Appended typed optional shot/damage/unit metadata and DamageApplied ordinal 13. Legacy event JSON decodes with null metadata and existing ordinals unchanged; new snapshot round trip retains exact chance bits, rear flag, damage and engine details. Reconciled the ticket index against canonical front matter while adding journal prerequisites. Engine production follows TK-0006.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-journal-contract-01/: format/build/diff/self-review PASS; Contracts284, Client173, Engine378, Motion guard19 PASS.
