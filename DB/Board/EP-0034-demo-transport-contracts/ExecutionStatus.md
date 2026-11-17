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
| EP-0034-US-0006-TK-0001-contracts | `759692d7` | implemented, story tests PASS |
| EP-0034-US-0006-TK-0002-content | `81e358c1` | implemented, story tests PASS |
| EP-0034-US-0006-TK-0003-engine | `7aec0af0`, `4c1e3689` | implemented, story tests PASS |
| EP-0034-US-0006-TK-0005-localized-feedback | `559ce508` | implemented, story tests PASS |
| EP-0034-US-0006-TK-0004-client | `ec009bf5` | implemented, story tests PASS |
| EP-0034-US-0002-TK-0001-contracts | `7533bdd1` | implemented, story tests PASS |
| EP-0034-US-0002-TK-0002-engine | `d40e99f1` | implemented, story tests PASS |
| EP-0034-US-0002-TK-0004-localized-feedback | `82eade83` | implemented, story tests PASS |
| EP-0034-US-0002-TK-0003-client | `29f221bf` | implemented, story tests PASS |
| EP-0034-US-0003-TK-0001-contracts | `79c4ba94` | implemented, story tests PASS |
| EP-0034-US-0003-TK-0002-engine | `8b05db86` | implemented, story tests PASS |
| EP-0034-US-0003-TK-0004-localized-feedback | `fef0ae26` | implemented, story tests PASS |
| EP-0034-US-0003-TK-0003-client | `d972d175` | implemented, story tests PASS |
| EP-0034-US-0004-TK-0001-contracts | `130cf088` | implemented, story tests PASS |
| EP-0034-US-0004-TK-0002-engine | `0e3d4b9b` | implemented, story tests PASS |
| EP-0034-US-0004-TK-0004-localized-feedback | `84595dde` | implemented, story tests PASS |
| EP-0034-US-0004-TK-0003-client | `1bdf2bb2` | implemented, story tests PASS |
| EP-0034-US-0005-TK-0001-transport-roundtrip | `7cb9ac48` | implemented, story tests PASS |
| EP-0034-US-0005-TK-0002-transport-flow | `02646620` | implemented, story tests PASS |
| EP-0034-US-0006-TK-0006-feasible-deadlines | commit with this ticket ID (SHA via `git log`) | implemented, focused + story-area regressions run (defect found in US-0005) |

US-0001 evidence: [story validation](EP-0034-US-0001-passenger-ready-start/EP-0034-US-0001-passenger-ready-start.md#story-validation-2026-11-17). Native/product gates OPEN.
Save format: no bump in US-0001 (passenger capacity is derived, not persisted); A-P2 not triggered yet.
Shared files touched by US-0001 (EP-0033 merge watch): `DemoCampaign.cs` (Contracts, `partial` keyword), `SimulationEngine.DemoCampaign.cs` (projection token), `SimulationEngine.DemoStart.cs`, `DemoCampaignData.cs`, `StarterCarrierManifest.cs`, `Scenarios/Demo/scenario.json`, `English.json`/`Russian.json` (block after `Demo.Rearm.*`), `ShipScreen.cs`, `ScenarioEngineTests.cs`, `ModuleHealthBootstrapTests.cs`, `DemoEp0032Us0004Tk0002Tests.cs`.
Open issue: EP-0032 demo tests hard-code `D:/DeepSpaceSaga/DSS` content and fail while EP-0033 content is in progress there.

US-0006 evidence: [story validation](EP-0034-US-0006-station-job-board/EP-0034-US-0006-station-job-board.md#story-validation-2026-11-17). Native/product gates OPEN.
Save format: no bump in US-0006 — `demoCampaign.stationJobs` is a new optional section of the schema-1 campaign data (older demo saves publish the current day on Load); A-P2 still not triggered.
Shared files touched by US-0006 (EP-0033 merge watch): `SimulationEngine.DemoCampaign.cs` (two added lines: explicit `PrepareStationJobsCommand` after the shared partial hook, `PublishStationJobs` at the top of `TickDemoCampaign`; the single-implementation partial hooks stay free — EP-0033 implements them and changes their signature), `DemoCampaignData.cs` (`stationJobs` property, one validation call, `ValidateStationJobs` + `StationJobsStateData` at the end of the file), `DemoCampaignDefinitions.cs` (jobs schema records + validation, cargo reference check), `English.json`/`Russian.json` (`Demo.Jobs.*` block after `Demo.Passengers.unavailable`), `SkiaWindow.cs` (`OpenContractsAsync` only), `GameSessionScreen.cs` (J button / Ctrl+J lines, mechanics panel width ×3), `ContractsScreen.cs`, tests `DemoEp0032Us0001Tk0002Tests.cs` (shared `ContentRoot` now this checkout), `DemoEp0032ContentReviewTests.cs` (jobs literal + Root), `DemoEp0032ReceiptReviewTests.cs`, `DemoEp0032ReviewTests.cs`, `DemoEp0032Us0004Tk0003Tests.cs`. EP-0034-only new files: `StationJobsContracts.cs`, `SimulationEngine.StationJobs.cs`, `Data/Demo/jobs.json`, `StationJobsPresentation.cs`, `GameSessionScreen.Journal.cs`.
Open issue resolved for this branch: the shared EP-0032 test `ContentRoot` reads this checkout since `4c1e3689` (client locale tests `DemoEp0032Us0001Tk0004Tests`/`DemoEp0032Us0002Tk0004Tests` still read `D:/DeepSpaceSaga/DSS` locale files; outside this story's area).

US-0002 evidence: [story validation](EP-0034-US-0002-passenger-contract/EP-0034-US-0002-passenger-contract.md#story-validation-2026-11-17). Native/product gates OPEN.
Save format: no bump in US-0002 — `demoCampaign.passengerContracts` is a new optional section of the schema-1 campaign data (boarded passengers are ordinary `ship.Passengers`); A-P2 still not triggered.
Passenger contract API for EP-0033-US-0005-crew-upkeep: acceptance = `AcceptJobOfferCommand` on a passenger offer (boards people into `ship.Passengers`, ids `{contractId}:pax-{n}`); projection `DemoTransportSnapshot.PassengerContracts` (`PassengerContract`, `PassengersAboard`, `PassengerDeliveryStates`); people aboard also in `PassengerCapacitySnapshot.PassengerCabins`; rations already include passengers (`ConsumeScheduledRations`); payout posting `passenger:{contractId}` (`VoyageAmountKind.PassengerPayout`).
Shared files touched by US-0002 (EP-0033 merge watch): `SimulationEngine.DemoCampaign.cs` (three added lines: boarding prepare after `PrepareStationJobsCommand`, `BoardPassengers` after the commit, `DeliverPassengerContracts` after `PublishStationJobs`; shared partial hooks untouched), `DemoCampaignData.cs` (`passengerContracts` property, one validation call, `ValidatePassengerContracts` + `PassengerContractData` at the end), `SimulationEngine.cs` (one Load line `ValidatePassengerContractsForLoad`), `SimulationEngine.Carrier.cs` (cabin reservation counts passengers), `SimulationEngine.PassengerCapacity.cs` (one projection member), `SimulationEngine.StationJobs.cs` (committed seats exclude boarded passengers), `Data/Demo/jobs.json` (passenger deadline multipliers), `English.json`/`Russian.json` (`Demo.PassengerContracts.*` block after `Demo.Jobs.Reason.battle_active`), `ContractsScreen.cs` (passenger section), test `DemoEp0034Us0006Tk0003Tests.cs` (legacy edit also removes `passengerContracts`). EP-0034-only new files: `PassengerContractsContracts.cs`, `SimulationEngine.PassengerContracts.cs`, `PassengerContractsPresentation.cs`, tests `DemoEp0034Us0002Tk0001/Tk0002/Tk0003/Tk0004Tests.cs`.
Open issue for US-0004: the cargo deadline multiplier (2500‰ of the max-speed ETA) is as infeasible as the old passenger values were (see US-0002 TK-0002 A-04); US-0004 owns cargo delivery and should recalibrate it with a real flight.
Known baseline (not this epic): `EconomyTimeContinuityTests` ×7 and `ClusterVoyageIntegrationTests` ×7 fail identically at `1f3b1eb1` (carrier state on a ship without hull profile; insufficient voyage fuel).

US-0003 evidence: [story validation](EP-0034-US-0003-contract-failure/EP-0034-US-0003-contract-failure.md#story-validation-2026-11-17). Native/product gates OPEN.
Save format: no bump in US-0003 — `demoCampaign.transportFailures` is a new optional section of the schema-1 campaign data; A-P2 still not triggered.
Transport failure API (reusable by US-0004 cargo and EP-0033): `CancelTransportCommand(contractId)`; projection `DemoTransportSnapshot.TransportFailures` (`Failures`, `Terms`, `ContractDebt`); engine helpers `IsTransportJob`, `TransportPenalty(payout, permille)`, `ResolveTransportFailures` (also closes Active cargo obligations on expiry/loss with an empty passenger list), `RepayTransportDebt(failures, available, out collected)` — US-0004 should call it from cargo payouts like `DeliverPassengerContracts` does; penalty permille in `jobs.json` `penalties` (250/250). Tick transitions (expiry, loss, disembarkation) bump campaign `Revision`/`CommitSequence` so the client autosaves; a command sent with the previous revision gets `stale_revision`. Passenger delivery state gains `failed`; `PassengerContractsSnapshot.PassengersAboard` now also counts failed-contract passengers still waiting to disembark (relevant to EP-0033-US-0005 crew upkeep).
Shared files touched by US-0003 (EP-0033 merge watch): `SimulationEngine.DemoCampaign.cs` (three added lines: `PrepareTransportCancellation` after boarding prepare, `ApplyTransportCancellation` after the commit, `ResolveTransportFailures` after `DeliverPassengerContracts`; shared partial hooks untouched), `DemoCampaignData.cs` (`transportFailures` property, one validation call, `ValidateTransportFailures` + `TransportFailureData` at the end), `SimulationEngine.cs` (one Load line `ValidateTransportFailuresForLoad`), `SimulationEngine.PassengerCapacity.cs` (one projection member), `SimulationEngine.PassengerContracts.cs` (debt repayment from payouts, `failed` state, aboard count), `Content/DemoCampaignDefinitions.cs` (optional `DemoJobPenalties` + validation), `Data/Demo/jobs.json` (`penalties`), `English.json`/`Russian.json` (`Demo.PassengerContracts.State.failed` + `Demo.TransportFailures.*` after `Demo.PassengerContracts.State.unavailable`), `ContractsScreen.cs` (failures section, cancel buttons), `PassengerContractsContracts.cs` (`PassengerDeliveryStates.Failed`), tests `DemoEp0034Us0002Tk0002Tests.cs`, `DemoEp0034Us0006Tk0003Tests.cs`, `DemoEp0034Us0002Tk0004Tests.cs`. EP-0034-only new files: `TransportFailureContracts.cs`, `SimulationEngine.TransportFailure.cs`, `TransportFailurePresentation.cs`, tests `DemoEp0034Us0003Tk0001/Tk0002/Tk0003/Tk0004Tests.cs`.
Known limitation: when a single snapshot interval spans both a deadline and a meal boundary, rations are consumed before the expiry is processed (same ordering as US-0002 delivery); the deviation is bounded by the snapshot interval.

US-0004 evidence: [story validation](EP-0034-US-0004-cargo-contract/EP-0034-US-0004-cargo-contract.md#story-validation-2026-11-17). Native/product gates OPEN.
Save format: no bump in US-0004 — `demoCampaign.cargoContracts` is a new optional section of the schema-1 campaign data; A-P2 still not triggered.
Cargo contract API (for US-0005 and EP-0033): acceptance = `AcceptJobOfferCommand` on a cargo offer (seals the obligation's `MassKg` of `ItemTypeId`; no module cargo stack is created); projection `DemoTransportSnapshot.CargoContracts` (`CargoContract`, `CargoHoldStates` aboard/overdue/delivered/return_pending/returned/lost, hold split `ReservedMassKg`/`PlayerCargoMassKg`/`HoldCapacityKg`/`FreeHoldKg`); engine helpers `ContractCargoReservedKg(data)` and `PlayerFreeHoldKg(ship)` — any new path that adds player cargo to the carrier hold must respect `PlayerFreeHoldKg`; payout posting `cargo:{contractId}` (ledger column `PassengerPayout`, i.e. contract income); failures/penalty/debt reuse US-0003 (`CancelTransportCommand`, `RepayTransportDebt`). Cargo delivery and return in the tick bump `Revision`/`CommitSequence`. Cargo deadline content multiplier recalibrated 2500 → 12000‰ (resolves the US-0002 open issue).
Shared files touched by US-0004 (EP-0033 merge watch): `SimulationEngine.DemoCampaign.cs` (four added lines: `PrepareCargoSealing` and `PrepareCargoCancellationReturn` after `PrepareTransportCancellation`, `DeliverCargoContracts` after `DeliverPassengerContracts`, `ReturnFailedContractCargo` after `ResolveTransportFailures`; shared partial hooks untouched), `DemoCampaignData.cs` (`cargoContracts` property, one validation call, `ValidateCargoContracts` + `CargoContractData`), `SimulationEngine.cs` (one Load line `ValidateCargoContractsForLoad`; the legacy Buy capacity line also checks `PlayerFreeHoldKg`), `SimulationEngine.TradeExecution.cs` (`FreeCargoKg`, Buy capacity check), `SimulationEngine.StationJobs.cs` (cargo blocker uses `ContractCargoReservedKg`), `SimulationEngine.PassengerCapacity.cs` (one projection member), `Data/Demo/jobs.json` (`cargoPermille`), `English.json`/`Russian.json` (`Demo.CargoContracts.*` block after `Demo.TransportFailures.Reason.battle_active`; `Demo.TransportFailures.CancelConfirm` text now mentions contract cargo), `ContractsScreen.cs` (hold line under the board title, cargo section, `DrawPassengers` returns y). EP-0034-only new files: `CargoContractsContracts.cs`, `SimulationEngine.CargoContracts.cs`, `CargoContractsPresentation.cs`, tests `DemoEp0034Us0004Tk0001/Tk0002/Tk0003/Tk0004Tests.cs`.
Known limitation: dialogue cargo grants (`DialogueEffectTransaction`) check only module capacity, not the contract reservation; hold module damage can drop capacity below the reservation without destroying sealed cargo.

US-0005 evidence: [story validation](EP-0034-US-0005-transport-continuation/EP-0034-US-0005-transport-continuation.md#story-validation-2026-11-17). Native/product gates OPEN ([native checklist](EP-0034-US-0005-transport-continuation/EP-0034-US-0005-transport-continuation.md#native-checklist-ep-0034-us-0005-tk-0002-2026-11-17), NOT RUN).
Save format: no bump in US-0005 — no new sections; every EP-0034 section round-trips through real save files at all voyage boundaries (A-P2 still not triggered).
Behaviour changes for EP-0033 and later stories: a passenger payout (`DeliverPassengerContracts`) is now a campaign commit — `Revision`/`CommitSequence` + 1, like cargo delivery/return and transport failures — so the autosave captures it; any command sent with the pre-payout revision gets `stale_revision` (the Contracts screen shows the localized reason and the retry with the current snapshot succeeds). Finance labels of the contract ledger column: `Finance.PassengerPayout`/`Finance.PassengerPenalty` now read "Contract payouts/penalties" / «Выплаты/Штрафы по контрактам» (keys unchanged).
Shared files touched by US-0005 (EP-0033 merge watch): `SimulationEngine.PassengerContracts.cs` (candidate in `DeliverPassengerContracts` gains the revision/sequence bump), `English.json`/`Russian.json` (values of `Finance.PassengerPayout` and `Finance.PassengerPenalty` only). `ContractsScreen.cs`, `FinanceScreen.cs`, `SkiaWindow.cs`, `DemoCampaignData.cs`, `SimulationEngine.CargoContracts.cs` unchanged. EP-0034-only new files: tests `DemoEp0034Us0005Tk0001Tests.cs` (Engine), `DemoEp0034Us0005Tk0002Tests.cs` (Client).
Known baseline outside the US-0004 filters, confirmed pre-existing against `75c801cf` sources: Engine `ClusterSaveStateTests` ×18, `FullMapLocalLoadTests` ×2, `MapEnvironmentSaveTests` ×1 (`solarSystem: invalid distance scale`, `insufficient_voyage_fuel`); Client `Ep0029Us0012Tk0001Tests.CheckpointFailureKeepsRetryWithoutResettlement` (IOException creating the temp `Saves` directory).
Known limitations kept: dialogue cargo grants (`AddCargoItem`) ignore the contract reservation — no demo content uses that effect; hold damage below the reservation keeps sealed cargo and the single payout (verified), no destruction rule (battle stories). Balance observation for EP-0041 playtest: the three-burn flight SPC-0005 → SPC-0002 takes ≈ 9 h of calendar time against a 2.5 h ordinary deadline (the reverse route ≈ 1.2 h); deadlines from the max-speed ETA ignore turn geometry.

Defect fix EP-0034-US-0006-TK-0006-feasible-deadlines (2026-11-17, [card](EP-0034-US-0006-station-job-board/EP-0034-US-0006-TK-0006-feasible-deadlines/EP-0034-US-0006-TK-0006-feasible-deadlines.md)): the US-0005 balance observation above was a real AC-02 defect of US-0006 — deadlines from the max-speed ETA were infeasible for a real flight (SPC-0005 → SPC-0002: 541.7 min flight vs 150 min ordinary deadline; urgent also infeasible on SPC-0005 → SPC-0006 and SPC-0006 → SPC-0007). New rule: deadline = max(minimum, ceil((maneuverGameTimeMs + ceil(ETA × cruisePermille / 1000)) × type‰ / 1000)); `jobs.json` `deadlines`: ordinary 1300, urgent 1100, cargo 1300, minimum 1 h, `maneuverGameTimeMs` 32 400 000 (9 h), `cruisePermille` 3200 (both optional, absent = old formula, validated `>= 0` / `>= 1000`). A real three-burn flight on every publishable ordered pair now fits ordinary/cargo with ≥ 34% and urgent with ≥ 13.5% margin. Payouts unchanged; saved offers/obligations keep frozen deadlines on Load; no save-format or fingerprint change. Regressions equal the baseline above; the single extra Client failure `DemoEp0032Us0004Tk0004Tests.PublicProfileUsesDemoForTutorialAndHidesDevelopmentScenarios` reads the sibling checkout `D:/DeepSpaceSaga/DSS` (now on `codex/ep-0035-demo-pirate-encounters`) and does not touch changed files. Evidence: `D:/DeepSpaceSaga/artifacts/validation/EP-0034/tk0006-deadlines-20261117T155842/`.
Shared files touched (EP-0033 merge watch): `Content/DemoCampaignDefinitions.cs` (two optional members of `DemoJobDeadlines` + one validation line), `Data/Demo/jobs.json` (`deadlines` values). EP-0034-only: `SimulationEngine.StationJobs.cs`, test `DemoEp0034Us0006Tk0006Tests.cs`.
