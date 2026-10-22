# EP-0014 execution order

17 tickets, six stories. Sequential publication; final US-0006 includes full repository documentation impact audit and Graphify. Dependencies follow the preceding row; story-level dependencies describe final acceptance, while additive schema work can precede UI.

| Ticket | Layer | Story criteria | Dependency | State |
|---|---|---|---|---|
| [EP-0014-US-0004-TK-0001-panel-contracts](EP-0014-US-0004-common-panel/EP-0014-US-0004-TK-0001-panel-contracts/EP-0014-US-0004-TK-0001-panel-contracts.md) | contracts | AC-04 |  | implemented |
| [EP-0014-US-0004-TK-0002-panel-projection](EP-0014-US-0004-common-panel/EP-0014-US-0004-TK-0002-panel-projection/EP-0014-US-0004-TK-0002-panel-projection.md) | engine | AC-04 | EP-0014-US-0004-TK-0001-panel-contracts | implemented |
| [EP-0014-US-0004-TK-0003-priority-and-time](EP-0014-US-0004-common-panel/EP-0014-US-0004-TK-0003-priority-and-time/EP-0014-US-0004-TK-0003-priority-and-time.md) | client | AC-01, AC-02 | EP-0014-US-0004-TK-0002-panel-projection | implemented |
| [EP-0014-US-0004-TK-0004-module-health](EP-0014-US-0004-common-panel/EP-0014-US-0004-TK-0004-module-health/EP-0014-US-0004-TK-0004-module-health.md) | client | AC-03, AC-04, AC-06 | EP-0014-US-0004-TK-0003-priority-and-time | pending |
| [EP-0014-US-0004-TK-0005-dead-code](EP-0014-US-0004-common-panel/EP-0014-US-0004-TK-0005-dead-code/EP-0014-US-0004-TK-0005-dead-code.md) | client | AC-05 | EP-0014-US-0004-TK-0004-module-health | implemented |
| [EP-0014-US-0001-TK-0001-profile-memory](EP-0014-US-0001-torpedo-panel/EP-0014-US-0001-TK-0001-profile-memory/EP-0014-US-0001-TK-0001-profile-memory.md) | client | AC-02 | EP-0014-US-0004-TK-0005-dead-code | implemented |
| [EP-0014-US-0001-TK-0002-profile-rows](EP-0014-US-0001-torpedo-panel/EP-0014-US-0001-TK-0002-profile-rows/EP-0014-US-0001-TK-0002-profile-rows.md) | client | AC-01, AC-04 | EP-0014-US-0001-TK-0001-profile-memory | implemented |
| [EP-0014-US-0001-TK-0003-stats-and-scatter](EP-0014-US-0001-torpedo-panel/EP-0014-US-0001-TK-0003-stats-and-scatter/EP-0014-US-0001-TK-0003-stats-and-scatter.md) | client | AC-03, AC-04 | EP-0014-US-0001-TK-0002-profile-rows | implemented |
| [EP-0014-US-0001-TK-0004-reload-ring](EP-0014-US-0001-torpedo-panel/EP-0014-US-0001-TK-0004-reload-ring/EP-0014-US-0001-TK-0004-reload-ring.md) | client | AC-05 | EP-0014-US-0001-TK-0003-stats-and-scatter | implemented |
| [EP-0014-US-0001-TK-0005-blocked-aim](EP-0014-US-0001-torpedo-panel/EP-0014-US-0001-TK-0005-blocked-aim/EP-0014-US-0001-TK-0005-blocked-aim.md) | client | AC-06, AC-07 | EP-0014-US-0001-TK-0004-reload-ring | implemented |
| [EP-0014-US-0002-TK-0001-countermissile-panel](EP-0014-US-0002-countermissile-panel/EP-0014-US-0002-TK-0001-countermissile-panel/EP-0014-US-0002-TK-0001-countermissile-panel.md) | client | AC-01, AC-02, AC-03, AC-04, AC-05 | EP-0014-US-0001-TK-0005-blocked-aim | implemented |
| [EP-0014-US-0003-TK-0001-engine-panel](EP-0014-US-0003-engine-panel/EP-0014-US-0003-TK-0001-engine-panel/EP-0014-US-0003-TK-0001-engine-panel.md) | client | AC-01, AC-02, AC-03, AC-04 | EP-0014-US-0002-TK-0001-countermissile-panel | implemented |
| [EP-0014-US-0005-TK-0001-legacy-launcher-fields](EP-0014-US-0005-snapshot-compatibility/EP-0014-US-0005-TK-0001-legacy-launcher-fields/EP-0014-US-0005-TK-0001-legacy-launcher-fields.md) | contracts | AC-02, AC-03 | EP-0014-US-0003-TK-0001-engine-panel | pending |
| [EP-0014-US-0005-TK-0002-legacy-defense-fields](EP-0014-US-0005-snapshot-compatibility/EP-0014-US-0005-TK-0002-legacy-defense-fields/EP-0014-US-0005-TK-0002-legacy-defense-fields.md) | contracts | AC-02, AC-03 | EP-0014-US-0005-TK-0001-legacy-launcher-fields | pending |
| [EP-0014-US-0005-TK-0003-save-continuation](EP-0014-US-0005-snapshot-compatibility/EP-0014-US-0005-TK-0003-save-continuation/EP-0014-US-0005-TK-0003-save-continuation.md) | engine | AC-01, AC-04, AC-05 | EP-0014-US-0005-TK-0002-legacy-defense-fields | pending |
| [EP-0014-US-0006-TK-0001-localization](EP-0014-US-0006-localization-and-guide/EP-0014-US-0006-TK-0001-localization/EP-0014-US-0006-TK-0001-localization.md) | content-data | AC-01, AC-02 | EP-0014-US-0005-TK-0003-save-continuation | pending |
| [EP-0014-US-0006-TK-0002-project-documentation](EP-0014-US-0006-localization-and-guide/EP-0014-US-0006-TK-0002-project-documentation/EP-0014-US-0006-TK-0002-project-documentation.md) | documentation | AC-03, AC-04 | EP-0014-US-0006-TK-0001-localization | pending |
