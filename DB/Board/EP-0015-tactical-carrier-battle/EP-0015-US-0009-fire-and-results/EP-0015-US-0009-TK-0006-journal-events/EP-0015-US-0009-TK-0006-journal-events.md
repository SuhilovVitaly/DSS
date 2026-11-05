---
epic: EP-0015-tactical-carrier-battle
story: EP-0015-US-0009-fire-and-results
ticket: EP-0015-US-0009-TK-0006-journal-events
title: Record authoritative journal events
stage: done
layer: engine
depends_on: ["EP-0015-US-0009-TK-0005-journal-contract"]
files_touched: 2
serves: [AC-03]
created: 2026-11-06
revision: 1
---

# Record authoritative journal events

## Why

The Client cannot recover old shot/target details from the latest salvo. Record facts when Engine resolves the actual salvo, in Display §6 order.

## Code context

- src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs: typed event production and outcome check phase.
- tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs: real session shot/target order, details, retained prior events.

## Acceptance

Keep typed immutable snapshots and existing event ordinals. Captured evidence must survive later turns and preserve the real event order without changing Engine rules, RNG, outcomes or world state. Use Display §6 from the approved Markdown; do not require a reference simulator.

Build, scoped format, relevant owning tests, diff review and separate immediate push to the authorized origin with SHA verification. Diagnostic outputs only under the epic validation root.

## Execution evidence — 2026-11-06

Engine appends exact chance/rear flags in shot order, simultaneous applied damage in first-hit order, disabled/destroyed records by side then carrier, and actual system changes with engine metadata. Terminal event retains the phase where victory was checked. Real full seed-1 session verified retained old records and all payloads against actual shots and before/after units; separate controlled Engine fixture verifies Move outcome phase. No rules or RNG changes.

D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle/20261106-journal-events-01/: build/format/diff/self-review PASS; 380 Engine +173 Client +19 Motion source guard PASS, including unchanged reference/session goldens.
