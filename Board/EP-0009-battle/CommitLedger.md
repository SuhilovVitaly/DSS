# EP-0009 published ticket ledger

Remote: `https://github.com/SuhilovVitaly/DSS`; branch: `base-fight`. Every ticket below has its own commit. Verification: `git ls-remote origin refs/heads/base-fight` matched local HEAD after each push; the final remote head is a descendant of every listed commit. The source is the linear Git history, not a prospective Board SHA.

| Step | Ticket | Commit SHA | Push |
|---:|---|---|---|
| 1 | EP-0009-US-0001-TK-0001-module-health-contracts | `6baef18139b177340ba329f272ac22835a96d8dc` | verified |
| 2 | EP-0009-US-0001-TK-0002-fire-contracts | `2efad4619528e97af27fd1131f4040e5fe8774d8` | verified |
| 3 | EP-0009-US-0001-TK-0003-combat-rules-content | `1bead7588ee439464f3d5fde3e3db4e534c20e37` | verified |
| 4 | EP-0009-US-0001-TK-0004-module-combat-schema | `e2bb1da9479db4087dbda250b2f36c9be49c6c73` | verified |
| 5 | EP-0009-US-0001-TK-0005-crew-skills-and-armor-schema | `f7eb4c4a8a694227c39bca25600239d0e961649a` | verified |
| 6 | EP-0009-US-0002-TK-0001-weapon-catalog | `65bdb36aa8d419dc5a3240a8d981297589933c25` | verified |
| 7 | EP-0009-US-0002-TK-0002-engine-catalog | `8bbe76d48f021273312cd09fce1c5e7736f38f15` | verified |
| 8 | EP-0009-US-0002-TK-0003-structure-normalization | `cf44b0c975b5185901858e971a9b99ddc05fb995` | verified |
| 9 | EP-0009-US-0002-TK-0004-hostile-ship-classes | `44fa559522d126f537d35c8f30552dab4968ec03` | verified |
| 10 | EP-0009-US-0003-TK-0001-module-health-runtime | `51aae0749b5af4132c9624dc19e7db19c2680864` | verified |
| 11 | EP-0009-US-0003-TK-0002-module-damage | `7d5f8db507b075810962cc47854e28b3139bcfa5` | verified |
| 12 | EP-0009-US-0003-TK-0003-ship-destruction | `1c28695238134114daa705d5fd9022d0ec2c5329` | verified |
| 13 | EP-0009-US-0003-TK-0004-disabled-module-effects | `fcdae04429d326513d75f7074fb207137c4d9b95` | verified |
| 14 | EP-0009-US-0004-TK-0001-chance-math | `3df87e2377bf882a0b6feb415a2ad49a8a8f3a37` | verified |
| 15 | EP-0009-US-0004-TK-0002-torpedo-hit-roll | `e2420ea361d53ff943bd52aff86fc31ba0540b76` | verified |
| 16 | EP-0009-US-0004-TK-0003-intercept-roll-skills | `acd5c646fedba322bb66a5626b80cb1bd8423d36` | verified |
| 17 | EP-0009-US-0005-TK-0001-launcher-cooldown | `1af75fb9bcb7721f219480ddaa36d5f9a428ddac` | verified |
| 18 | EP-0009-US-0005-TK-0002-pd-burst | `f8096e45af1fb11706c9507187e525e5c3c4811d` | verified |
| 19 | EP-0009-US-0005-TK-0003-pd-reload-and-attempts | `ff6fe1ef83cda0c632331f2f64b5728e8d704d87` | verified |
| 20 | EP-0009-US-0006-TK-0001-mode-runtime | `5f5a4af2907a5d57b0788fcc2f44eb591247bce6` | verified |
| 21 | EP-0009-US-0006-TK-0002-set-mode-command | `16255243beb7b270fc73c754bc12cae83d82f4d8` | verified |
| 22 | EP-0009-US-0006-TK-0003-mode-selector-ui | `db93b03e8ca76a6422a00a179f7fe97ffd8909ab` | verified |
| 23 | EP-0009-US-0007-TK-0001-manual-fire-burst | `673f4223a3bddb0ffc0a1521d017ec7a84704cec` | verified |
| 24 | EP-0009-US-0007-TK-0002-defense-panels-burst-ui | `7d293d4e0bb553816e28febf14e1ceaa8d071487` | verified |
| 25 | EP-0009-US-0008-TK-0001-npc-fire-cadence | `c82560134948160b8c7954cf6d7fb7772c040f34` | verified |
| 26 | EP-0009-US-0008-TK-0002-flank-approach-math | `6e858bcde45998e344be5b9c805dc6e2d0ef1839` | verified |
| 27 | EP-0009-US-0008-TK-0003-ai-params-from-scenario | `52008fcae257bbd0885da382c7f5557388c484f6` | verified |
| 28 | EP-0009-US-0009-TK-0001-scenario-combat-mukha | `97f7f552a4002d7820f4d0200635d11af15e1fed` | verified |
| 29 | EP-0009-US-0009-TK-0002-scenario-combat-shmel | `c4977e72e1a019de5fc86a137d922c0909d5fdf7` | verified |
| 30 | EP-0009-US-0009-TK-0003-scenarios-migrate-schema | `c223a640144bbb3495520b0a38d54c86eb4a26f7` | verified |
| 31 | EP-0009-US-0010-TK-0001-defeat-from-module-rule | `2a95d7d1adce4ef82fc9150c79b7427e30b11e57` | verified |
| 32 | EP-0009-US-0010-TK-0002-module-health-ui | `c85f4d20838e308fa1c940cc2d18a28ce9cee771` | verified |
| 33 | EP-0009-US-0010-TK-0003-combat-journal-lines | `1f816d6a6a6538ab5c6b7887877180b34753be15` | verified |
| 34 | EP-0009-US-0011-TK-0001-save-format-17 | `18bd3474ea7b4c2bb79ca3ce1e50a64d6f31c9ce` | verified |
| 35 | EP-0009-US-0011-TK-0002-combat-acceptance-and-balance | `3ccadfde0b7109dd7735cc43b1b8d3909c04aa1c` | verified |
| 36 | EP-0009-US-0011-TK-0003-simulator-on-game-data-and-native | `66ae0c9965a3c91a9c07b9413c3430973aa6022c` | verified |
| 37 | EP-0009-US-0012-TK-0001-update-project-documents | `65076b732aa286e2f53d1540483b3b37b131e5de` | verified |
| 38 | EP-0009-US-0012-TK-0002-refresh-graph-and-links | `e5584284afde13a90a6b2cf20fddf58f939b9f92` | verified |

Preparation: `c0dbfdf37d1e241f8dca151c09d3d635d848a4d3` (separate service commit). Post-review structural snapshot assertion repair for US-0010 TK-0002: `7f11147078bbbc296359dcc3df3b7ccc347bad77` (separate follow-up commit, pushed and remote verified). The ledger itself is finalized in a subsequent documentation-status commit; its own SHA is checked after that push.

The 38 ticket implementations and documentation chain are published. Product acceptance remains open for the documented Mukha balance discrepancy, native/game checks NOT RUN and Graphify rebuild SKIPPED by user instruction; full corpus-inclusive Engine run was interrupted after >10 minutes. See [implementation status](ImplementationStatus.md), [balance evidence](evidence/balance.md), [native matrix](evidence/native.md) and [navigation audit](evidence/navigation.md).
