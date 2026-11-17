# EP-0033 execution

Started 2026-11-17 from main `83782f77394c652435df2341977bf99df751ef82`.
Branch: `feature/ep-0033-demo-company-services`; remote: origin, https://github.com/SuhilovVitaly/DSS.
The owner's current request authorizes implementation, one commit and immediate push per ticket.
Tests run at the end of each user story, overriding the original per-ticket test cadence.
Ticket commits before that boundary are implemented with validation pending, never test PASS.

## Order and dependencies

US-0001 → US-0002 → US-0003 → US-0004 → US-0005 → US-0006 (documentation last).
EP-0032 is merged in the base. EP-0034-US-0002 is not implemented in this checkout;
passenger integration and its story acceptance remain open until its real runtime exists.
Do not substitute a mock passenger-contract implementation.

## Documentation scope

US-0006 includes source-based impact inventory across the repository, current requirements,
mechanics, screens, save/content contracts, README, Board and offline Graphify.
Its initial three-file lists may be expanded with the impact inventory before editing.
Product/native/balance acceptance remains separate from automated checks.

## Progress

Preparation only. No implementation tests or native validation have run.
