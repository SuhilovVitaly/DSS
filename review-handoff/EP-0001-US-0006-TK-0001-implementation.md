# EP-0001-US-0006-TK-0001 — implementation

Chain: EP-0001-trading-system → EP-0001-US-0006-repeatable-trading-voyage → EP-0001-US-0006-TK-0001-round-trip-engine-proof. Stage: approved. Final review status: APPROVED.

## Implemented

- Extended D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\RepeatableTradingVoyageTests.cs with all eight named proofs: two complete A → B → A rounds, real-content seed corpus 1/17/42, local quote rejection, partial sale/duplicate handling, debt rejection, old/new port-fee schedule, lifecycle retry and snapshot cadence.
- Extended D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\TradingVoyageFixture.cs with command-driven flight, real dialogue, monotonic calendar/physical time, exact receipt-backed cargo/stock/Credits assertions, designated cargo module checks, bounded waits and no mid-voyage reload.
- Repaired the separately authorized generator dependency in D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\Scenario\TradingMapGeometryGenerator.cs and covered it in D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\TradingMapGeometryTests.cs. New stations now receive the source station's port tariff, so their real docking dialogue can complete.
- Created the final review at D:\DeepSpaceSaga\DSS\review-handoff\EP-0001-US-0006-TK-0001-review.md.

## Acceptance criteria

- AC-01: two rounds in one Engine, physical navigation and real seed corpus; Approach preserves speed and does not Dock.
- AC-02: old quote and unquoted trade rejected in flight, old A quote rejected at B and after returning to A, fresh quote executes at destination.
- AC-03: receipt quantities and totals match cargo, station stock and player Credits; other cargo preserved; partial sale leaves one unit; duplicate command has no second effect.
- AC-04: debt blocks Undock without new voyage; old port does not renew in flight; B creates one stay/fee and one due renewal; duplicate Undock/Dock preserve lifecycle and payment.

## Commands and observed results

- dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~RepeatableTradingVoyageTests|FullyQualifiedName~ApproachCommandTests|FullyQualifiedName~PortFeeScheduleTests|FullyQualifiedName~TradeCommandTests": 63 passed, 0 failed.
- dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-restore: 922 passed, 0 failed.
- dotnet build src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj --no-restore: passed, 0 warnings, 0 errors.
- dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include [four changed code files]: passed.
- git diff --check: passed. git diff --name-only: four changed code files and the updated review report; this new implementation report is untracked and appears in git status --short.

## Review resolution and residual gaps

The review's original P1 generator and missing named-proof findings were fixed. Two P2 assertion gaps discovered during review were fixed and retested: rejected B quote state and absence of old-port renewal event. Final review has no open findings. The fuel reservation remains nullable under US-0009; this ticket asserts stable retry state without claiming fuel cost or profit. Client UI evidence belongs to TK-0002/TK-0003 and is not claimed here.

No Board document was edited. No commit or push was performed in this run; HEAD was already 5d59abf when work resumed.
