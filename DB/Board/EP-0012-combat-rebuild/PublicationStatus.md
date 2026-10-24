# EP-0012 publication ledger

Branch `codex/ep-0012-combat-rebuild`; remote `origin` = `https://github.com/SuhilovVitaly/DSS`. Baseline `c894925d187c90a658d1e25958feed69a45d7e41`. Every numbered ticket has a separate commit followed immediately by push and remote SHA verification. Service/corrective commits are additional and do not replace numbered publications. All listed earlier SHAs exist and are ancestors of final HEAD. Technical publication is separate from open balance/human acceptance.

Final ticket73 cannot embed its own cryptographic SHA without changing that SHA. Its row names this commit as HEAD; after publication, `git rev-parse HEAD` must equal `git ls-remote origin refs/heads/codex/ep-0012-combat-rebuild`. Exact final SHA and clean-tree verification are returned in the final response and external publication audit, without rewriting published history.

| Step | Ticket | Commit SHA | Push |
|---:|---|---|---|
| 1 | [EP-0012-US-0001-TK-0001-projectile-contracts](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-TK-0001-projectile-contracts/EP-0012-US-0001-TK-0001-projectile-contracts.md) | `090b3635822f100bbe11105e2840f585d1294ec8` | origin verified per ticket |
| 2 | [EP-0012-US-0001-TK-0002-command-contracts](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-TK-0002-command-contracts/EP-0012-US-0001-TK-0002-command-contracts.md) | `be0078caf61fe4f08ed92a081ce932f5a441f9d6` | origin verified per ticket |
| 3 | [EP-0012-US-0001-TK-0003-encounter-contracts](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-TK-0003-encounter-contracts/EP-0012-US-0001-TK-0003-encounter-contracts.md) | `c3978c9cb3dda2c030bbe10e42d6c3839827387c` | origin verified per ticket |
| 4 | [EP-0012-US-0001-TK-0004-combat-rules-schema](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-TK-0004-combat-rules-schema/EP-0012-US-0001-TK-0004-combat-rules-schema.md) | `c19dafdf185ac279133cb78afcdef795969912d5` | origin verified per ticket |
| 5 | [EP-0012-US-0001-TK-0005-launcher-profile-schema](EP-0012-US-0001-contracts-and-schema/EP-0012-US-0001-TK-0005-launcher-profile-schema/EP-0012-US-0001-TK-0005-launcher-profile-schema.md) | `f06c235c84d5010d8497731b68be549930874b77` | origin verified per ticket |
| 6 | [EP-0012-US-0017-TK-0001-combat-event-contracts](EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-TK-0001-combat-event-contracts/EP-0012-US-0017-TK-0001-combat-event-contracts.md) | `7abd93815e6765586f5cc684752cec887ca18c4e` | origin verified per ticket |
| 7 | [EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts](EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts/EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts.md) | `012963afb6a531e767d5e6ef531cc60c5ddd4ae8` | origin verified per ticket |
| 8 | [EP-0012-US-0003-TK-0001-blast-math](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-TK-0001-blast-math/EP-0012-US-0003-TK-0001-blast-math.md) | `b197271fe27c121bb98bd31ad81d0b1af1c0ab2d` | origin verified per ticket |
| 9 | [EP-0012-US-0006-TK-0001-blast-threat-math](EP-0012-US-0006-threats-and-evasion/EP-0012-US-0006-TK-0001-blast-threat-math/EP-0012-US-0006-TK-0001-blast-threat-math.md) | `52355e437b9395a24fc9e46e83358e61ea1bdb6f` | origin verified per ticket |
| 10 | [EP-0012-US-0018-TK-0004-shared-point-forecast](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-TK-0004-shared-point-forecast/EP-0012-US-0018-TK-0004-shared-point-forecast.md) | `5c9cac3f0104c2baa6209fb37c618a62bb82fd4e` | origin verified per ticket |
| 11 | [EP-0012-US-0003-TK-0002-torpedo-point-launch](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-TK-0002-torpedo-point-launch/EP-0012-US-0003-TK-0002-torpedo-point-launch.md) | `ce2dcf9d55c9656cd32a6158551647dcb002ec5d` | origin verified per ticket |
| 12 | [EP-0012-US-0003-TK-0003-torpedo-area-detonation](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-TK-0003-torpedo-area-detonation/EP-0012-US-0003-TK-0003-torpedo-area-detonation.md) | `e1e34582d43d87194c0352e8103f31d02572adca` | origin verified per ticket |
| 13 | [EP-0012-US-0003-TK-0004-manual-detonation](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-TK-0004-manual-detonation/EP-0012-US-0003-TK-0004-manual-detonation.md) | `40ded2cbb65ee98eca483b889238e465a1f041bf` | origin verified per ticket |
| 14 | [EP-0012-US-0004-TK-0001-countermissile-point-launch](EP-0012-US-0004-point-aim-countermissiles/EP-0012-US-0004-TK-0001-countermissile-point-launch/EP-0012-US-0004-TK-0001-countermissile-point-launch.md) | `8a92cac30bfae7e7c148362254fce9a51baf97fa` | origin verified per ticket |
| 15 | [EP-0012-US-0004-TK-0002-countermissile-detonation](EP-0012-US-0004-point-aim-countermissiles/EP-0012-US-0004-TK-0002-countermissile-detonation/EP-0012-US-0004-TK-0002-countermissile-detonation.md) | `d834c15e8341c273a82fda39375a3dd7fb27f280` | origin verified per ticket |
| 16 | [EP-0012-US-0006-TK-0002-evade-command](EP-0012-US-0006-threats-and-evasion/EP-0012-US-0006-TK-0002-evade-command/EP-0012-US-0006-TK-0002-evade-command.md) | `2cd6a430af37d4a8196a8ca5ca03c11ac7bcec2f` | origin verified per ticket |
| 17 | [EP-0012-US-0006-TK-0003-continuous-turn-in-combat](EP-0012-US-0006-threats-and-evasion/EP-0012-US-0006-TK-0003-continuous-turn-in-combat/EP-0012-US-0006-TK-0003-continuous-turn-in-combat.md) | `04e173b7f4fc3ac30380c53f6bdab45e8c70aaa3` | origin verified per ticket |
| 18 | [EP-0012-US-0007-TK-0001-orbit-lead-fix](EP-0012-US-0007-mukha-ai-v3/EP-0012-US-0007-TK-0001-orbit-lead-fix/EP-0012-US-0007-TK-0001-orbit-lead-fix.md) | `5db5347c9b628db31c5d16ca8bbf93926739e979` | origin verified per ticket |
| 19 | [EP-0012-US-0007-TK-0002-npc-aim-and-fire](EP-0012-US-0007-mukha-ai-v3/EP-0012-US-0007-TK-0002-npc-aim-and-fire/EP-0012-US-0007-TK-0002-npc-aim-and-fire.md) | `2cddcf035e23c8e29fbb67f0cbda9e4b9c4d47cf` | origin verified per ticket |
| 20 | [EP-0012-US-0007-TK-0003-npc-evade-and-reaction](EP-0012-US-0007-mukha-ai-v3/EP-0012-US-0007-TK-0003-npc-evade-and-reaction/EP-0012-US-0007-TK-0003-npc-evade-and-reaction.md) | `d340dccbfce90182d04f8f37c17e2252b8d75f9b` | origin verified per ticket |
| 21 | [EP-0012-US-0008-TK-0001-encounter-state](EP-0012-US-0008-encounter-and-time/EP-0012-US-0008-TK-0001-encounter-state/EP-0012-US-0008-TK-0001-encounter-state.md) | `a4a9acd31dce15ad17b5cce7ffb412723e229a8c` | origin verified per ticket |
| 22 | [EP-0012-US-0008-TK-0002-combat-speed-lock](EP-0012-US-0008-encounter-and-time/EP-0012-US-0008-TK-0002-combat-speed-lock/EP-0012-US-0008-TK-0002-combat-speed-lock.md) | `ca3eda12838d90908f6a64e4b53528eea059230c` | origin verified per ticket |
| 23 | [EP-0012-US-0008-TK-0003-encounter-stats](EP-0012-US-0008-encounter-and-time/EP-0012-US-0008-TK-0003-encounter-stats/EP-0012-US-0008-TK-0003-encounter-stats.md) | `1db6e9c3682b337ff2431152a819c1e6aa575552` | origin verified per ticket |
| 24 | [EP-0012-US-0018-TK-0003-point-combat-forecasts](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-TK-0003-point-combat-forecasts/EP-0012-US-0018-TK-0003-point-combat-forecasts.md) | `b23f38cd7a28634f7af1c44b7632f89b886fe8d3` | origin verified per ticket |
| 25 | [EP-0012-US-0017-TK-0002-engine-report-producers](EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-TK-0002-engine-report-producers/EP-0012-US-0017-TK-0002-engine-report-producers.md) | `891fe86209c41d8031b279780ecf3eacfb83ebc6` | origin verified per ticket |
| 26 | [EP-0012-US-0005-TK-0001-snapshot-masking](EP-0012-US-0005-snapshot-privacy/EP-0012-US-0005-TK-0001-snapshot-masking/EP-0012-US-0005-TK-0001-snapshot-masking.md) | `479fef0765e7981c0a078cea2f9b00e7f419ac57` | origin verified per ticket |
| 27 | [EP-0012-US-0005-TK-0002-hidden-fields-leak-test](EP-0012-US-0005-snapshot-privacy/EP-0012-US-0005-TK-0002-hidden-fields-leak-test/EP-0012-US-0005-TK-0002-hidden-fields-leak-test.md) | `533db328038655df67260e15582d391de1fc5ecf` | origin verified per ticket |
| 28 | [EP-0012-US-0017-TK-0003-local-client-delivery](EP-0012-US-0017-event-and-command-integration/EP-0012-US-0017-TK-0003-local-client-delivery/EP-0012-US-0017-TK-0003-local-client-delivery.md) | `e0822e55287708680c6d0f885dfcd99af4d7ac2e` | origin verified per ticket |
| 29 | [EP-0012-US-0019-TK-0003-client-forecast-cache-migration](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-TK-0003-client-forecast-cache-migration/EP-0012-US-0019-TK-0003-client-forecast-cache-migration.md) | `5a129a9bbf4780f9959a1ecc3ff512d73048414c` | origin verified per ticket |
| 30 | [EP-0012-US-0010-TK-0001-combat-visuals-v3](EP-0012-US-0010-tactical-map/EP-0012-US-0010-TK-0001-combat-visuals-v3/EP-0012-US-0010-TK-0001-combat-visuals-v3.md) | `a6dfe06add453668b0c3f3b95c9bef697b4c5168` | origin verified per ticket |
| 31 | [EP-0012-US-0010-TK-0002-projectiles-and-blasts-render](EP-0012-US-0010-tactical-map/EP-0012-US-0010-TK-0002-projectiles-and-blasts-render/EP-0012-US-0010-TK-0002-projectiles-and-blasts-render.md) | `44a8c6fbc2ede01ed4b9852f8903238b82225ff9` | origin verified per ticket |
| 32 | [EP-0012-US-0010-TK-0003-persistent-range-rings](EP-0012-US-0010-tactical-map/EP-0012-US-0010-TK-0003-persistent-range-rings/EP-0012-US-0010-TK-0003-persistent-range-rings.md) | `dbee25bf9e0455003a0f6c2c1c875fc34cbee1f3` | origin verified per ticket |
| 33 | [EP-0012-US-0010-TK-0004-own-path-forecast-and-fit](EP-0012-US-0010-tactical-map/EP-0012-US-0010-TK-0004-own-path-forecast-and-fit/EP-0012-US-0010-TK-0004-own-path-forecast-and-fit.md) | `b0e219218c748c22601ceb336a429282d3fe9c8f` | origin verified per ticket |
| 34 | [EP-0012-US-0011-TK-0001-aim-mode-and-input-map](EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-TK-0001-aim-mode-and-input-map/EP-0012-US-0011-TK-0001-aim-mode-and-input-map.md) | `5478dfefb3a1d13d65ae3a1297b19c600ba89e7a` | origin verified per ticket |
| 35 | [EP-0012-US-0011-TK-0002-torpedo-aim-preview](EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-TK-0002-torpedo-aim-preview/EP-0012-US-0011-TK-0002-torpedo-aim-preview.md) | `99c5f13b31864f602a0c9999bcd6eca6f5470736` | origin verified per ticket |
| 36 | [EP-0012-US-0011-TK-0003-countermissile-aim-preview](EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-TK-0003-countermissile-aim-preview/EP-0012-US-0011-TK-0003-countermissile-aim-preview.md) | `d9fb3a66144d950b62bb6c92d9a3d7ec57db6e12` | origin verified per ticket |
| 37 | [EP-0012-US-0011-TK-0004-launch-commands-and-reasons](EP-0012-US-0011-aiming-and-launch/EP-0012-US-0011-TK-0004-launch-commands-and-reasons/EP-0012-US-0011-TK-0004-launch-commands-and-reasons.md) | `762723844c69b1a455ff037274e95e0a6fec8842` | origin verified per ticket |
| 38 | [EP-0012-US-0012-TK-0001-evade-input-and-threat-select](EP-0012-US-0012-maneuver-and-threat-ui/EP-0012-US-0012-TK-0001-evade-input-and-threat-select/EP-0012-US-0012-TK-0001-evade-input-and-threat-select.md) | `4ed46b208c9e14a8adf838dc1587133a70abccfd` | origin verified per ticket |
| 39 | [EP-0012-US-0012-TK-0002-threat-panel](EP-0012-US-0012-maneuver-and-threat-ui/EP-0012-US-0012-TK-0002-threat-panel/EP-0012-US-0012-TK-0002-threat-panel.md) | `f2b2b08daa61ca6ae253ca58280854f0e4a2ddfd` | origin verified per ticket |
| 40 | [EP-0012-US-0012-TK-0003-threat-alerts](EP-0012-US-0012-maneuver-and-threat-ui/EP-0012-US-0012-TK-0003-threat-alerts/EP-0012-US-0012-TK-0003-threat-alerts.md) | `52c22c428249e3cf1c68a75d3e473e9a6a8d9411` | origin verified per ticket |
| 41 | [EP-0012-US-0013-TK-0001-module-diagram](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-TK-0001-module-diagram/EP-0012-US-0013-TK-0001-module-diagram.md) | `e8871cb460b1302e076c68187973ffc0baeab6b9` | origin verified per ticket |
| 42 | [EP-0012-US-0013-TK-0002-weapon-panels-v3](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-TK-0002-weapon-panels-v3/EP-0012-US-0013-TK-0002-weapon-panels-v3.md) | `ce323d30fc8e999e2436f2ac814a56e07b42e5e4` | origin verified per ticket |
| 43 | [EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels/EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels.md) | `7ca95b70d6708405b9792fc31dc66f42e4eef311` | origin verified per ticket |
| 44 | [EP-0012-US-0013-TK-0004-journal-and-reports-v3](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-TK-0004-journal-and-reports-v3/EP-0012-US-0013-TK-0004-journal-and-reports-v3.md) | `ff74e9bbe6a584b96e483fbd875fff862ff3f0df` | origin verified per ticket |
| 45 | [EP-0012-US-0013-TK-0005-combat-start-timer-summary](EP-0012-US-0013-combat-state-and-feedback/EP-0012-US-0013-TK-0005-combat-start-timer-summary/EP-0012-US-0013-TK-0005-combat-start-timer-summary.md) | `69c5d34090e3072fa35eedb8ce0d60be1bbb2ae9` | origin verified per ticket |
| 46 | [EP-0012-US-0016-TK-0001-combat-localization](EP-0012-US-0016-combat-localization/EP-0012-US-0016-TK-0001-combat-localization/EP-0012-US-0016-TK-0001-combat-localization.md) | `d112cb2536790a7b65d4a98ea6f716a4a450023f` | origin verified per ticket |
| 47 | [EP-0012-US-0009-TK-0003-save-format-18](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-TK-0003-save-format-18/EP-0012-US-0009-TK-0003-save-format-18.md) | `babcc7c6a4903e6271382a253c6fcba1b55908c4` | origin verified per ticket |
| 48 | [EP-0012-US-0009-TK-0004-save18-capture-and-restore](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-TK-0004-save18-capture-and-restore/EP-0012-US-0009-TK-0004-save18-capture-and-restore.md) | `d55a19b02a54f7f81b3f3fc9c902198f2155b81d` | origin verified per ticket |
| 49 | [EP-0012-US-0009-TK-0005-save18-defense-validation](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-TK-0005-save18-defense-validation/EP-0012-US-0009-TK-0005-save18-defense-validation.md) | `cc94ea5f8b403c71f1d9b918cb61f5d801351e0b` | origin verified per ticket |
| 50 | [EP-0012-US-0002-TK-0001-torpedo-launchers-v3](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-TK-0001-torpedo-launchers-v3/EP-0012-US-0002-TK-0001-torpedo-launchers-v3.md) | `32dc20289c598edbd59b342db3fd11b621c119c4` | origin verified per ticket |
| 51 | [EP-0012-US-0002-TK-0002-countermissile-launcher-v3](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-TK-0002-countermissile-launcher-v3/EP-0012-US-0002-TK-0002-countermissile-launcher-v3.md) | `4c760ea66b7da27e6842a3d8fa77b61096a8f1ce` | origin verified per ticket |
| 52 | [EP-0012-US-0002-TK-0003-engines-v3](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-TK-0003-engines-v3/EP-0012-US-0002-TK-0003-engines-v3.md) | `5454d93d3c5791fe3b3f5886402732d22fed6670` | origin verified per ticket |
| 53 | [EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands/EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands.md) | `233774316c91175a35b3cf61cfa5f999364b4417` | origin verified per ticket |
| 54 | [EP-0012-US-0002-TK-0005-combat-rules-v3](EP-0012-US-0002-combat-catalog-v3/EP-0012-US-0002-TK-0005-combat-rules-v3/EP-0012-US-0002-TK-0005-combat-rules-v3.md) | `d251a5a384ba0ef8383bc1a04fb71ab15735fed1` | origin verified per ticket |
| 55 | [EP-0012-US-0019-TK-0005-maneuver-command-catalog](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-TK-0005-maneuver-command-catalog/EP-0012-US-0019-TK-0005-maneuver-command-catalog.md) | `2dabedc7f45cfee7c6db6fd7c59a543152bfe8bd` | origin verified per ticket |
| 56 | [EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3/EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3.md) | `3aaf0807dda5a1c06fad2c68269c2a14f4a24c4c` | origin verified per ticket |
| 57 | [EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios](EP-0012-US-0009-scenarios-and-saves/EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios/EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios.md) | `a89133245b9ad7bb44289f7ef6a08df9faa53903` | origin verified per ticket |
| 58 | [EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios/EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios.md) | `201e1d7ab1b97ddcc92edcd9cfc94f7d770a9d34` | origin verified per ticket |
| 59 | [EP-0012-US-0019-TK-0002-scenario-engine-fixtures](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-TK-0002-scenario-engine-fixtures/EP-0012-US-0019-TK-0002-scenario-engine-fixtures.md) | `d9bce90fa1bae496ede798a87604f9688745d8c4` | origin verified per ticket |
| 60 | [EP-0012-US-0019-TK-0004-client-combat-regressions](EP-0012-US-0019-scenario-and-client-regressions/EP-0012-US-0019-TK-0004-client-combat-regressions/EP-0012-US-0019-TK-0004-client-combat-regressions.md) | `bbe9e62deb8b51ddd61616fbdd00ea7da5e93ff2` | origin verified per ticket |
| 61 | [EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests/EP-0012-US-0018-TK-0002-migrate-mode-runtime-tests.md) | `c640d3b8aaef326ee73c2007faa28ba8754dc7dc` | origin verified per ticket |
| 62 | [EP-0012-US-0004-TK-0003-remove-auto-pd](EP-0012-US-0004-point-aim-countermissiles/EP-0012-US-0004-TK-0003-remove-auto-pd/EP-0012-US-0004-TK-0003-remove-auto-pd.md) | `77436b16d1821a42de1d85f190bb1e776f4a859c` | origin verified per ticket |
| 63 | [EP-0012-US-0018-TK-0001-remove-interception-helper](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-TK-0001-remove-interception-helper/EP-0012-US-0018-TK-0001-remove-interception-helper.md) | `9aaa48f198fa57aea56d922ec36cc21faef86f5e` | origin verified per ticket |
| 64 | [EP-0012-US-0003-TK-0005-remove-homing-roll-modes](EP-0012-US-0003-point-aim-torpedoes/EP-0012-US-0003-TK-0005-remove-homing-roll-modes/EP-0012-US-0003-TK-0005-remove-homing-roll-modes.md) | `fc72732e5fa357eb127d0e29a2a2d13268b7bf23` | origin verified per ticket |
| 65 | [EP-0012-US-0018-TK-0005-module-combat-regressions](EP-0012-US-0018-legacy-combat-cleanup/EP-0012-US-0018-TK-0005-module-combat-regressions/EP-0012-US-0018-TK-0005-module-combat-regressions.md) | `6eab4dbe9672d82018e794fa3bf8951be1f7a2e8` | origin verified per ticket |
| 66 | [EP-0012-US-0014-TK-0001-production-balance-harness](EP-0012-US-0014-balance-and-acceptance/EP-0012-US-0014-TK-0001-production-balance-harness/EP-0012-US-0014-TK-0001-production-balance-harness.md) | `e4a50aac64878719a238a6a5098d0b7709b4e3cd` | origin verified per ticket |
| 67 | [EP-0012-US-0014-TK-0002-tools-analyzer](EP-0012-US-0014-balance-and-acceptance/EP-0012-US-0014-TK-0002-tools-analyzer/EP-0012-US-0014-TK-0002-tools-analyzer.md) | `ee6763f43963ca990d651820bfe82e9b699ee22f` | origin verified per ticket |
| 68 | [EP-0012-US-0014-TK-0003-native-acceptance](EP-0012-US-0014-balance-and-acceptance/EP-0012-US-0014-TK-0003-native-acceptance/EP-0012-US-0014-TK-0003-native-acceptance.md) | `cec1c54cdf21c2a8f5b70fb8d30e39413e985e6b` | origin verified per ticket |
| 69 | [EP-0012-US-0015-TK-0001-update-documents-1](EP-0012-US-0015-project-documentation/EP-0012-US-0015-TK-0001-update-documents-1/EP-0012-US-0015-TK-0001-update-documents-1.md) | `31291a2c5147e3ddf7ab996adb44b470da002c0d` | origin verified per ticket |
| 70 | [EP-0012-US-0015-TK-0002-update-documents-2](EP-0012-US-0015-project-documentation/EP-0012-US-0015-TK-0002-update-documents-2/EP-0012-US-0015-TK-0002-update-documents-2.md) | `a6e676ec7cf0f8b26efc6d38833ac661d3fabc74` | origin verified per ticket |
| 71 | [EP-0012-US-0015-TK-0003-update-documents-3](EP-0012-US-0015-project-documentation/EP-0012-US-0015-TK-0003-update-documents-3/EP-0012-US-0015-TK-0003-update-documents-3.md) | `294df70ce3445ca6bed2aaff956bc7f96da55f92` | origin verified per ticket |
| 72 | [EP-0012-US-0015-TK-0004-update-documents-4](EP-0012-US-0015-project-documentation/EP-0012-US-0015-TK-0004-update-documents-4/EP-0012-US-0015-TK-0004-update-documents-4.md) | `296c2d58eeb7d67d94b75bbd267e1c705efb49a1` | origin verified per ticket |
| 73 | [EP-0012-US-0015-TK-0005-refresh-graph-and-links](EP-0012-US-0015-project-documentation/EP-0012-US-0015-TK-0005-refresh-graph-and-links/EP-0012-US-0015-TK-0005-refresh-graph-and-links.md) | `HEAD (this final ticket commit; exact SHA verified after push)` | verified after commit/push; see final response and external audit |

## Additional service and corrective publications

The complete commit list after baseline follows. All are ancestors of final HEAD, preserving actual sequence and prior repairs. The final ticket commit is the containing HEAD.

```text
cb6d67253d6fe192ee4f4bbcc24002b0cc6fa5af EP-0012: preserve approved planning package and begin execution journal
090b3635822f100bbe11105e2840f585d1294ec8 EP-0012-US-0001-TK-0001-projectile-contracts: separate own and observed point flights
be0078caf61fe4f08ed92a081ce932f5a441f9d6 EP-0012-US-0001-TK-0002-command-contracts: add explicit point launch and evade inputs
c3978c9cb3dda2c030bbe10e42d6c3839827387c EP-0012-US-0001-TK-0003-encounter-contracts: add encounter counters and public enemy status
c19dafdf185ac279133cb78afcdef795969912d5 EP-0012-US-0001-TK-0004-combat-rules-schema: load strict point combat rules without fire modes
f06c235c84d5010d8497731b68be549930874b77 EP-0012-US-0001-TK-0005-launcher-profile-schema: validate point profiles and publish typed capabilities
7abd93815e6765586f5cc684752cec887ca18c4e EP-0012-US-0017-TK-0001-combat-event-contracts: carry public blast and shotdown facts
012963afb6a531e767d5e6ef531cc60c5ddd4ae8 EP-0012-US-0017-TK-0004-turn-and-summary-command-contracts: add explicit hold and results controls
9de88536c6c311d0b464c622e6e73fe3c76ace9a EP-0012 execution: prepare runtime before activating point catalogs
b197271fe27c121bb98bd31ad81d0b1af1c0ab2d EP-0012-US-0003-TK-0001-blast-math: add deterministic point projectile geometry
52355e437b9395a24fc9e46e83358e61ea1bdb6f EP-0012-US-0006-TK-0001-blast-threat-math: evaluate observed relative blast danger
5c9cac3f0104c2baa6209fb37c618a62bb82fd4e EP-0012-US-0018-TK-0004-shared-point-forecast: forecast point flights without hidden enemy aims
809bd18e3d2d226927efe7438eccb74a2d9d1c12 EP-0012-US-0017-TK-0001-combat-event-contracts: localize new event consumers
ce2dcf9d55c9656cd32a6158551647dcb002ec5d EP-0012-US-0003-TK-0002-torpedo-point-launch: execute deterministic captured point launches
e1e34582d43d87194c0352e8103f31d02572adca EP-0012-US-0003-TK-0003-torpedo-area-detonation: resolve ordered fractional area blasts
40ded2cbb65ee98eca483b889238e465a1f041bf EP-0012-US-0003-TK-0004-manual-detonation: detonate own live torpedoes through area damage
8a92cac30bfae7e7c148362254fce9a51baf97fa EP-0012-US-0004-TK-0001-countermissile-point-launch: launch counter profiles through shared point executor
d834c15e8341c273a82fda39375a3dd7fb27f280 EP-0012-US-0004-TK-0002-countermissile-detonation: destroy nearby projectiles before torpedo blasts
999d584949c3a80284b04c9495c339069e0f7a4f EP-0012 execution: defer legacy deletion until save and client migrations
2cd6a430af37d4a8196a8ca5ca03c11ac7bcec2f EP-0012-US-0006-TK-0002-evade-command: execute observed-trace evasions with shared course return
04e173b7f4fc3ac30380c53f6bdab45e8c70aaa3 EP-0012 US0006 TK0003: execute continuous held turns
5db5347c9b628db31c5d16ca8bbf93926739e979 EP-0012 US0007 TK0001: bound orbit lead by reachable radius
2cddcf035e23c8e29fbb67f0cbda9e4b9c4d47cf EP-0012 US0007 TK0002: aim and launch NPC point torpedoes
d340dccbfce90182d04f8f37c17e2252b8d75f9b EP-0012 US0007 TK0003: schedule NPC dodge reactions once per threat
a4a9acd31dce15ad17b5cce7ffb412723e229a8c EP-0012 US0008 TK0001: track authoritative encounter lifecycle
ca3eda12838d90908f6a64e4b53528eea059230c EP-0012 US0008 TK0002: enforce combat speed and preserve clock fractions
1db6e9c3682b337ff2431152a819c1e6aa575552 EP-0012-US-0008-TK-0003-encounter-stats: count authoritative battle facts
90f128c6a1b63c71efd248baaa494580e70badf8 EP-0012 preparation: migrate point consumers before production catalog activation
ebf66d6585269f22477e422c4f4df0a074008e57 Fix EP-0012-US-0018-TK-0004-shared-point-forecast runtime parity
b23f38cd7a28634f7af1c44b7632f89b886fe8d3 EP-0012-US-0018-TK-0003-point-combat-forecasts: isolate captured and observed prediction
891fe86209c41d8031b279780ecf3eacfb83ebc6 EP-0012-US-0017-TK-0002-engine-report-producers: publish point combat facts privately
91b5c79ab12c1325bee68d1778b4525843a806e2 Fix EP-0012-US-0008-TK-0003-encounter-stats for surviving hostile flights
479fef0765e7981c0a078cea2f9b00e7f419ac57 EP-0012-US-0005-TK-0001-snapshot-masking: guard point combat payload privacy
6842980485c140e2b3328b722e8186065f396a36 EP-0012-US-0008-TK-0002-combat-speed-lock: publish confirmed speed in compact frames
533db328038655df67260e15582d391de1fc5ecf EP-0012-US-0005-TK-0002-hidden-fields-leak-test: verify serialized full and compact privacy
e0822e55287708680c6d0f885dfcd99af4d7ac2e EP-0012-US-0017-TK-0003-local-client-delivery: verify real point transport and removal
5a129a9bbf4780f9959a1ecc3ff512d73048414c EP-0012-US-0019-TK-0003-client-forecast-cache-migration: admit point paths and encounter state
a6dfe06add453668b0c3f3b95c9bef697b4c5168 EP-0012-US-0010-TK-0001-combat-visuals-v3: load validated point combat styles from JSON
44a8c6fbc2ede01ed4b9852f8903238b82225ff9 EP-0012-US-0010-TK-0002-projectiles-and-blasts-render: render admitted point flights and facts
dbee25bf9e0455003a0f6c2c1c875fc34cbee1f3 EP-0012-US-0010-TK-0003-persistent-range-rings: persist per-launcher point range visibility
b0e219218c748c22601ceb336a429282d3fe9c8f EP-0012-US-0010-TK-0004-own-path-forecast-and-fit: show five-second own path and fit battle with F
5478dfefb3a1d13d65ae3a1297b19c600ba89e7a EP-0012-US-0011-TK-0001-aim-mode-and-input-map: wire point aiming through actual input and command submission
99c5f13b31864f602a0c9999bcd6eca6f5470736 EP-0012-US-0011-TK-0002-torpedo-aim-preview: render physical aim and guard friendly fire
d9fb3a66144d950b62bb6c92d9a3d7ec57db6e12 EP-0012-US-0011-TK-0003-countermissile-aim-preview: snap observed traces and show conditional interception
762723844c69b1a455ff037274e95e0a6fec8842 EP-0012-US-0011-TK-0004-launch-commands-and-reasons: preserve pending scene binding and localized refusals
2d59fe4b659ac3341d4fc5431d7218d520faccfc EP-0012-US-0011-TK-0004-launch-commands-and-reasons: localize dispatcher refusals
4ed46b208c9e14a8adf838dc1587133a70abccfd EP-0012-US-0012-TK-0001-evade-input-and-threat-select: wire held maneuver controls and observed threat selection
f2b2b08daa61ca6ae253ca58280854f0e4a2ddfd EP-0012-US-0012-TK-0002-threat-panel: show admitted threats and wire real counter and evade actions
52c22c428249e3cf1c68a75d3e473e9a6a8d9411 EP-0012-US-0012-TK-0003-threat-alerts: render accessible threat signals and session-bound native audio
e8871cb460b1302e076c68187973ffc0baeab6b9 EP-0012-US-0013-TK-0001-module-diagram: show authoritative own module health and remove hull HP bar
ce323d30fc8e999e2436f2ac814a56e07b42e5e4 EP-0012-US-0013-TK-0002-weapon-panels-v3: show reload rings, operator scatter and point launch controls
7ca95b70d6708405b9792fc31dc66f42e4eef311 EP-0012-US-0013-TK-0003-enemy-badges-and-hit-labels: show public results and preserve destroyed enemy wreck status
ff74e9bbe6a584b96e483fbd875fff862ff3f0df EP-0012-US-0013-TK-0004-journal-and-reports-v3: render public combat facts and protect own-only health details
69c5d34090e3072fa35eedb8ce0d60be1bbb2ae9 EP-0012-US-0013-TK-0005-combat-start-timer-summary: integrate encounter HUD, acknowledgement and defeat statistics
d112cb2536790a7b65d4a98ea6f716a4a450023f EP-0012-US-0016-TK-0001: localize combat presentation in Russian and English
c6569d44d31123416d0bc07c64050b6a8920525f EP-0012: require Save18 before point catalog activation
babcc7c6a4903e6271382a253c6fcba1b55908c4 EP-0012-US-0009-TK-0003: define Save18 point combat continuation schema
d55a19b02a54f7f81b3f3fc9c902198f2155b81d EP-0012-US-0009-TK-0004: persist and restore deterministic point combat
cc94ea5f8b403c71f1d9b918cb61f5d801351e0b EP-0012-US-0009-TK-0005: reject inconsistent point combat saves atomically
746814a0efdb01fa40f29f27ba6d68bec312bddf EP-0012: stage catalog components before atomic v3 activation
32dc20289c598edbd59b342db3fd11b621c119c4 EP-0012-US-0002-TK-0001: prepare verified v3 torpedo launcher catalog
4c760ea66b7da27e6842a3d8fa77b61096a8f1ce EP-0012-US-0002-TK-0002: prepare verified v3 countermissile catalog
5454d93d3c5791fe3b3f5886402732d22fed6670 EP-0012-US-0002-TK-0003: prepare Tetrarch and Strizh engine catalog
233774316c91175a35b3cf61cfa5f999364b4417 EP-0012-US-0002-TK-0004-remove-modes-and-legacy-commands: stage point commands and matching categories
d251a5a384ba0ef8383bc1a04fb71ab15735fed1 EP-0012-US-0002-TK-0005-combat-rules-v3: atomically activate production point combat catalog
2dabedc7f45cfee7c6db6fd7c59a543152bfe8bd EP-0012-US-0019-TK-0005-maneuver-command-catalog: expose typed combat controls through canonical engine category
3aaf0807dda5a1c06fad2c68269c2a14f4a24c4c EP-0012-US-0009-TK-0001-scenario-combat-mukha-v3: activate approved formation and prove maximum-speed pursuit
a89133245b9ad7bb44289f7ef6a08df9faa53903 EP-0012-US-0009-TK-0002-remove-shmel-and-migrate-scenarios: retire Shmel scenario and unused single launcher
201e1d7ab1b97ddcc92edcd9cfc94f7d770a9d34 EP-0012-US-0019-TK-0001-migrate-other-combat-scenarios: migrate shipped scenarios and preserve calibrated fixture invariants
d9bce90fa1bae496ede798a87604f9688745d8c4 EP-0012-US-0019-TK-0002 migrate real scenario regression fixtures
bbe9e62deb8b51ddd61616fbdd00ea7da5e93ff2 EP-0012-US-0019-TK-0004 migrate point UI regressions and route middle mouse
48c462b549754ff45f7049070f8e26c3f43b449e EP-0012 order legacy consumer migration before runtime removal
c640d3b8aaef326ee73c2007faa28ba8754dc7dc EP-0012-US-0018-TK-0002 migrate mode consumers to captured point profiles
77436b16d1821a42de1d85f190bb1e776f4a859c EP-0012 US4-TK3 retire automatic defense runtime
9aaa48f198fa57aea56d922ec36cc21faef86f5e EP-0012 US18-TK1 remove interception probability helper
fc72732e5fa357eb127d0e29a2a2d13268b7bf23 refactor(combat): remove guided combat and legacy consumers
6eab4dbe9672d82018e794fa3bf8951be1f7a2e8 fix(combat): preserve player command order at NPC boundaries
e4a50aac64878719a238a6a5098d0b7709b4e3cd test(combat): publish production balance harness and 2000-seed evidence
ee6763f43963ca990d651820bfe82e9b699ee22f feat(combat): add production analyzer and pin installed SDK
cec1c54cdf21c2a8f5b70fb8d30e39413e985e6b EP-0012 US0014 TK0003: record native evidence and fix projectile labels and victory summary
31291a2c5147e3ddf7ab996adb44b470da002c0d EP-0012 US0015 TK0001: document implemented point combat and superseded engineering contracts
a6e676ec7cf0f8b26efc6d38833ac661d3fabc74 EP-0012 US0015 TK0002: synchronize reports and point forecast documentation
294df70ce3445ca6bed2aaff956bc7f96da55f92 EP-0012 US0015 TK0003: update tactical controls scenarios and session save documentation
296c2d58eeb7d67d94b75bbd267e1c705efb49a1 EP-0012 US0015 TK0004: synchronize canonical requirements and project-wide combat guidance
60418af19fdf2634f4d7bb27ecb2d075c28882b3 EP-0012 US0019 TK0001 fix: preserve point combat rules in economy registry variants
```

## US20 addition — 2026-10-21

Six technical fixes individually published, followed by US15-TK0006 documentation. [Current implementation, exact SHA ledger, checks and residual gates](EP-0012-US-0020-bugs/ExecutionStatus.md). Historical73-ticket records above remain unchanged.
