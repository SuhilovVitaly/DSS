# Default world saves before real-time combat removal

Captured on `ac565d15eb726dd4fff901d51a3434f621488103` with the Save19 Engine writer, `ScenarioLoader.Serialize`, the production registry from `src/DeepSpaceSaga.Client/Settings.json` and deterministic master seed 42. The writer names its metadata `quicksave`; the source scenario is `Scenarios/Default/scenario.json`.

- **Idle:** load the original Default scenario with seed 42, pause the deterministic clock, capture the authoritative save.
- **Combat:** add the first actual hostile actor from `CombatMukha` 400 world units to the right and 250 up from the Default player. Issue the normal medium torpedo and wide countermeasure commands and capture at world time 1000 ms, motion time 100 ms. Both commands executed; the writer captured two torpedoes (player and hostile), one countermissile, one hostile runtime actor, and combat report sequence 4.
- **Save18:** the same authoritative idle/active captures, projected to the older accepted schema following `DebugTuningSaveTests`: version 18, no debug tuning or projectile `launcherModuleTypeId`/`originalFlight` provenance, serialized by the current writer. These are compatibility projections, not claims of executing the historical Save18 writer.

The historical capture procedure was `SaveFixturesTests.RecordPreRemovalFixturesWhenExplicitlyRequested` at the pre-removal source recorded above; set `DSS_RECORD_SAVE_FIXTURES` to a dedicated absolute validation output folder and filter to that test. Never regenerate these migration fixtures from the Save20 writer. Read-only verification tests run normally.

Catalog fingerprint in these captures: `FB95784357DF3BF74B871D1FCEBD834698E206102EF8D1FCC31EE2E4E56F5678` (catalog/rules version 1/1). Writer-byte hashes at capture:

| File | SHA-256 |
|---|---|
| Default-Save18.json | `3FA8FA43D28231A2F7DE2A8A27E5AAA1C62298BE1E4A8645EFC738BF56B4FC42` |
| Default-Save18-combat.json | `E414D808FB97A8E0C42E58AAC8F0E7B481977B441928E6E7BBF179AEB16B96EC` |
| Default-Save19.json | `2880776836E784AA51E44EF18288E401F5B344846F3EC3759E0CC26B369C0CA7` |
| Default-Save19-combat.json | `F155FEE965132077E7F99865E6826EC39FE32BA7AB7F0C0F8550D58CFC5A3998` |

These files are read-only inputs to the current Save20 migration tests. See [SaveFormat](../../../../DB/Documentation/04-Engineering/SaveFormat.md).
