# Production review corrections — 2026-11-17

User supplied production review and requested autonomous corrections, with only necessary tests at the end; no game launch. Existing clean branch retained. This is self-review and does not close owner acceptance.

| Finding | Resolution |
|---|---|
| Terminal Continue used F5 | Dedicated ContinueCheckpoint event loads campaign-continuation; F5/F9 unchanged |
| Terminal autosave erased recovery | Save living checkpoint before owned sortie; terminal writes leave it intact; latest-compatible search skips terminal worlds; manual terminal saves remain possible |
| Load overwrote newer autosave | Adopt loaded snapshot as baseline; first write needs gameplay revision change or new docking |
| Global mutable Demo fingerprint | Semantic cluster-only fingerprint, initial LF/CRLF legacy compatibility; optional future files do not invalidate existing worlds |
| Defeat message lost | Restore ShipDestroyed.Defeat, keep error overrides; remove checkpoint English from RU rollback text |
| Unexpected corrupt-candidate exceptions | Candidate failure isolation catches non-fatal exceptions; cancellation/OOM still propagate |
| F6 and permanent Saved overlay | Ignore battle retry, hide status in battle/terminal view, expire Saved after five seconds |
| Continue error opened Load | Keep origin view for Continue/Checkpoint failure, display error there |
| Credits removed in development | Disabled Credits retained outside demo profile |
| Invalid campaignProfile crashed startup | Safe development fallback with diagnostic for missing/invalid settings/profile |
| Broken Demo disabled unrelated scenarios | Load Demo definitions only for Demo worlds/saves |
| Reject-driven writes and growing receipts | Autosave observes gameplay Revision; receipt replay bounded to1024; older successful command replay is rejected by expected revision |
| Closing replaced confirmed answer | Keep already-returned authoritative response; suppress stale presentation; pending cancelled response remains cancellation, never claimed as applied/rejected |

Validation: Engine `FullyQualifiedName~DemoEp0032` 27/27 PASS; Client `FullyQualifiedName~DemoEp0032|FullyQualifiedName~ShipDestroyedScreenTests|FullyQualifiedName~ScreenEventTests|FullyQualifiedName~MenuLayoutTests` 133/133 PASS. Release compilation is included in each test run. No full suite or standalone solution build was requested or run. Scoped whitespace verification and git diff --check passed.

Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0032-review/20261117-check/engine4.log`, `client3.log`, `format-final.log`, `format-final.err`. Earlier nullable/fixture/type failures are retained in the same directory and are not PASS. Final Engine recheck followed splitting the regression tests by owning ticket. Generated graph from the original epic is historical evidence and was not rebuilt for this focused correction.

Disposable builds/results/empty fixture folders are removed after validation; only referenced logs retained. Manual acceptance stays OPEN. Per-ticket commits follow the original requested commit/push workflow. The supplied review supersedes earlier execution statements that treated continuation as quicksave or allowed terminal auto-overwrite. Current contract owner: ../../Documentation/04-Engineering/DemoCampaign.md.

## Publication

Branch: `codex/ep-0032-demo-campaign-continuity`; origin: `https://github.com/SuhilovVitaly/DSS`. Every corrective ticket was pushed immediately and remote SHA checked.

| Ticket | SHA | Push |
|---|---|---|
| EP-0032-US-0007-TK-0003 | `ef042daf7c4501b6af29ec6937585c562dbaf2a9` | verified |
| EP-0032-US-0007-TK-0002 | `ff4c2a0e44f1f7adbda59de24d837377d189505f` | verified |
| EP-0032-US-0007-TK-0005 | `fdd7e820409b7fae8373cc79f56a74835be42768` | verified |
| EP-0032-US-0004-TK-0004 | `4463a57f61e5d7d844b58ddf4b45f0a40d455021` | verified |
| EP-0032-US-0005-TK-0001 | `cd97c98a18675b1ec9f009f898116a67979ab455` | verified |
| EP-0032-US-0005-TK-0002 | `abf6937ddaf76aadad756faf9cefc035788f93e9` | verified |
| EP-0032-US-0006-TK-0001 | this documentation commit (resolve by Git subject) | verify after commit |
