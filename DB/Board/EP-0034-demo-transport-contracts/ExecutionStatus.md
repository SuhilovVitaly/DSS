# EP-0034 execution

Started 2026-11-17 from main `83782f77394c652435df2341977bf99df751ef82` (EP-0032 merged).
Branch: `feature/ep-0034-demo-transport-contracts`; remote: origin, https://github.com/SuhilovVitaly/DSS.
Checkout: `D:\DeepSpaceSaga\Dss-Main`. EP-0033 runs in parallel in `D:\DeepSpaceSaga\DSS`
on `feature/ep-0033-demo-company-services`; Board cards of EP-0034 are updated in this branch,
not in the EP-0033 working tree, so that each checkout keeps only its own changes.

The owner's request (2026-11-17): «бери EP-0034 в новой ветке после каждого тикета коммит и пуш,
в конце стори запускаются тесты только на область этого стори, в конце запускается общий пул
проверок. коммит и пуш разрешен».
Ticket commits before a story boundary are implemented with validation pending, never test PASS.
Story-scoped tests run at the end of each user story; the full solution build/test runs once at the end.

## Order and dependencies

US-0001 → US-0006 → US-0002 → US-0003 → US-0004 → US-0005 → US-0007 (documentation last).
External prerequisites EP-0032-US-0001/US-0003/US-0004 are merged in the base.
EP-0033-US-0005-crew-upkeep depends on EP-0034-US-0002; nothing in EP-0034 depends on EP-0033.

## Parallel-work assumptions

- A-P1: EP-0033 touches overlapping hub files (DemoCampaign contracts/engine, locale JSON,
  ShipScreen/FinanceScreen, csproj, EngineRequirements). EP-0034 keeps transport logic in its own
  types/partials where the ticket allows and makes additive edits in shared files to keep merges simple.
- A-P2: Save-format version bumps may collide with EP-0033; the later-merged branch re-numbers
  during merge. Recorded here so the merge reviewer checks it explicitly.

## Progress

| Ticket | Commit(s) | Status |
|---|---|---|
| EP-0034-US-0001-TK-0001-contracts | `89c2c929` | implemented, story tests PASS |
| EP-0034-US-0001-TK-0002-content | `af463f4e`, `a6cbce6d` | implemented, story tests PASS |
| EP-0034-US-0001-TK-0003-engine | `1329c059` | implemented, story tests PASS |
| EP-0034-US-0001-TK-0005-localized-feedback | `bc87d851`, `ba14ef23` | implemented, story tests PASS |
| EP-0034-US-0001-TK-0004-client | `406522f3` | implemented, story tests PASS |

US-0001 evidence: [story validation](EP-0034-US-0001-passenger-ready-start/EP-0034-US-0001-passenger-ready-start.md#story-validation-2026-11-17). Native/product gates OPEN.
Save format: no bump in US-0001 (passenger capacity is derived, not persisted); A-P2 not triggered yet.
Shared files touched by US-0001 (EP-0033 merge watch): `DemoCampaign.cs` (Contracts, `partial` keyword), `SimulationEngine.DemoCampaign.cs` (projection token), `SimulationEngine.DemoStart.cs`, `DemoCampaignData.cs`, `StarterCarrierManifest.cs`, `Scenarios/Demo/scenario.json`, `English.json`/`Russian.json` (block after `Demo.Rearm.*`), `ShipScreen.cs`, `ScenarioEngineTests.cs`, `ModuleHealthBootstrapTests.cs`, `DemoEp0032Us0004Tk0002Tests.cs`.
Open issue: EP-0032 demo tests hard-code `D:/DeepSpaceSaga/DSS` content and fail while EP-0033 content is in progress there.
