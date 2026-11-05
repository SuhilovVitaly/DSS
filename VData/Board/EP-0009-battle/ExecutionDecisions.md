# EP-0009 execution decisions — 2026-10-12

Baseline:6b7186444b027a03671e62ba61e86623e00762a2, verified published in origin/base-fight (https://github.com/SuhilovVitaly/DSS). EP-0008 implementation/self-review/documentation delivered; native13/14, FPS/human acceptance remains OPEN. User explicitly authorizes EP-0009 under that deferral. Graph rebuilding cancelled by user; no further Graphify extraction, game launch or native runs. Automated tests/builds remain allowed. Each ticket receives a separate commit and immediate push with remote SHA verification. The historical Bitbucket/no-GitHub statement in revision2 is superseded by direct user authorization.

## Technical assumptions, not answers attributed to the user

- O2: use recommended global basic engine6000m/s, with voyage/fuel/economy regression; Strizh is a distinct type.
- O3: all catalog modules structure150, updating fixtures/data only with observable rule tests.
- O4: Mukha/Shmel each have a distinct crew member per module; skills20 in combat scenarios.
- O5: simulator reads real game content. Referenced C:/DSS/docs/fight/CombatDesign-v1, Client/Data/CombatDesign and src/DeepSpaceSaga.CombatSim are absent in the inspected checkout. Build an independent simulator when its ticket arrives; do not claim reproduction of unavailable historical400-run evidence. No fabricated legacy comparison.
- O6: existing pirate retains EP-0008 controller defaults while receiving new module values.
- O7: retain two range parameters, both150km.
- A08 (implementation sequencing): provide additive Contracts fields first, retaining the actively used hull/single-projectile fields until their real consumers migrate in US-0003/0005. Remove those compatibility fields and tests in the migrating ticket. The planned immediate deletion breaks later Engine content tickets before runtime migration; a working intermediate build is preferable. This does not introduce alternate gameplay or a mock. Final module health and multiple-projectile contracts must have no obsolete hull/one-projectile API. Additive DTO defaults must not be presented as a completed runtime feature.
- A09 (scenario capacity): new four-person combat loadout requires four living places; add an explicit four-cabin variant in combat scenarios rather than silently overfilling the EP-0008 three-cabin module. Existing PlayerShipOnly three-operator setup remains until its migration ticket.

## Preparation scope

Materialize12 story/38 ticket cards, Documentation.md/Tickets.md, this decision record, current ImplementationStatus/PublicationStatus. Update monolithic specification with current execution overrides while preserving historical simulator results. Record final EP-0008 ticket publication in its PublicationStatus. No production changes in preparation.
