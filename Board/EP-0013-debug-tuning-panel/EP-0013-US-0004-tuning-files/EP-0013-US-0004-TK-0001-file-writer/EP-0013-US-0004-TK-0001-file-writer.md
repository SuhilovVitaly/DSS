---
epic: EP-0013-debug-tuning-panel
story: EP-0013-US-0004-tuning-files
ticket: EP-0013-US-0004-TK-0001-file-writer
stage: done
layer: Client
depends_on: EP-0013-US-0003-TK-0002-startup-file
---

# EP-0013-US-0004-TK-0001-file-writer

Implement the matching supplied revision2 ticket: file-writer. See [epic contract](../../Documentation.md).

## Code context and scope

Resolve exact files and dependency APIs before starting this ticket. No implementation claimed. At most the supplied file cap; record any necessary scope correction here before editing.

Resolved scope2026-10-22: new Client/DebugTuningFileWriter.cs; GameSessionHandle.cs;
UI/SkiaWindow.cs; new Client.Tests/DebugTuningFileWriterTests.cs. Four files instead
of three are needed for the real receive path and session/shutdown lifecycle.
Observe only the contiguous confirmed Buffer.Latest revision, coalesce writes at
250ms, flush final data before closure, defer replacement-session writes until
adoption, and suspend/flush the preceding writer before reading startup tuning.
On failed load reactivate the unchanged previous session. Keep rendering free of I/O.

Shutdown review adds one line in CombatMetricsWriter.cs: its worker disposal must
use ConfigureAwait(false), since the window now waits for complete session cleanup
and the debug writer's final revision before process exit. A captured UI continuation
inside metrics disposal could otherwise deadlock that required shutdown wait.

## Validation

Run matching named acceptance tests, affected project regression, build, scoped formatting and diff check. Tests hard-capped300000ms. Record observed results; no inferred PASS. One commit after successful completion; immediately push after every commit. Verify the target branch and remote before pushing, then confirm that the remote branch SHA matches local HEAD. A ticket is not published until this verification succeeds.

## Execution2026-10-22

The real receive loop observes only contiguous confirmed Buffer.Latest tuning
revisions. The background writer emits all25 values, schema1, invariant numbers,
UTF-8 without BOM, two-space JSON and UTC timestamp. Atomic same-directory
temp/replace keeps the preceding file valid on failure and cleans temporary files.
Writes coalesce with strict>=250ms completion spacing; final flush retries an
unwritten revision and disposal waits for it. I/O errors remain visible in status/log.

Candidate session writers remain inactive until adoption. Load suspends/flushes
the previous writer before reading startup tuning and reactivates it on failure.
Session disposals share completion; the window tracks preceding cleanup across
Main Menu/New Game/Load and waits at shutdown. Metrics disposal does not capture
the UI context. Disk I/O remains outside rendering.

US4-AC1/AC2: five new tests cover all25 values, exactly20 scheduled updates in1s
with<=5 commits and>=250ms spacing, final revision20 on disposal, injected atomic
replacement failure/retry, candidate adoption and actual Engine->connection->buffer
file output, including concurrent session disposal. Initial relative-delay stress
input exceeded1s due Windows timer drift; the test now schedules absolute50ms
deadlines without weakening the write-count assertion.

Client regression **61/61 PASS**; final lifecycle/metrics regression **56/56 PASS**.
Client build0warnings/errors; scoped format and ticket-scoped diff check **PASS**.
Global diff check currently finds a blank EOF line in externally modified process
AGENTS; that foreign edit is preserved and excluded. Native window exit is NOT RUN.
Self-review complete; defaults export/reset button remain their following tickets.

Evidence under `D:/DeepSpaceSaga/artifacts/validation/ep0013/`:
`writer-regression-final-20261022T114541765`, `writer-lifecycle-final-20261022T114906135`,
`writer-build-20261022T115135416`, `writer-format-20261022T114857455`.
