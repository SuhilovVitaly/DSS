---
epic: EP-0005-optimization
story: EP-0005-US-0003-documentation-and-graph
ticket: EP-0005-US-0003-TK-0002-graph-rebuild
stage: implemented
depends_on: [EP-0005-US-0003-TK-0001-documentation-sync]
---
# EP-0005-US-0003-TK-0002-graph-rebuild

Authorized by the 2026-10-09 execution request. Execute last, after dependencies, with separate validation, review, commit and immediate verified push.

Scope: Board/EP-0005-optimization, affected Documentation and repository Markdown, Graphify configuration and generated navigation artifacts. No gameplay changes in this ticket.

Implementation: Perform full Graphify rebuild of code, tests, Documentation and this epic, excluding media, build output and caches. Record corpus hashes, source commit, unresolved references and verify at least five representative code/document paths.

Acceptance: Fresh nonempty graph and manifest match inputs; representative navigation queries resolve to existing symbols; graph freshness and limitations documented.

Validation: inspect final diff, validate relative Markdown links and unique Board IDs/dependency DAG, git diff --check. Graph validation does not substitute runtime tests or native acceptance.

## Execution scope and evidence

Full current source/tests/tools code plus all Documentation and EP-0005 Markdown, root shims and tool README. Update .graphifyignore from historical EP-0007 scope. Media/build/cache/raw benchmark JSON excluded. Generated GraphRebuild.md and US-0003-Review.md are excluded from inputs to avoid self-referential report hashes; all ticket cards and the publication registry are included. Outputs are versioned in root graphify-out.

[GraphRebuild](../../GraphRebuild.md) records actual corpus/hashes, extraction/health, query traces and final validation. Stage indicates delivered navigation artifacts when this ticket is committed; it does not close the epic's performance or manual gates. Own commit SHA/push is verified externally after creation, per EpicExecutionPrompt.
