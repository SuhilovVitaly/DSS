# EP-0010 native / visual acceptance

Status: **PARTIAL / OPEN**. On2026-10-16 the user explicitly instructed: **«никогда не запускай самостоятельно игру»**. No further game/native-window/harness runs are permitted autonomously. Headless tests/builds do not close this gate.

A previously started native process had already finished before the instruction. Process inspection found no remaining reports-window process. Its existing artifacts are retained honestly as a single partial sample, not full acceptance.

| Check | Evidence | State |
|---|---|---|
|1280x720/UI100/English, ObjectInfo open/closed,0/200 reports | [native JSON](native/1280-1-English.json), four matching PNGs | Automated native checks PASS |
|All8 art variants in native card size | native/1280-1-English.variant-0.png through variant-7.png | Artifacts generated; assistant partial visual inspection; human OPEN |
|Full1280/1920 x UI100/120/150 x RU/EN x0/200 x open/closed |48 offscreen combinations in Client acceptance tests | Headless PASS; native remaining cases NOT RUN per user policy |
|Accordion/focus/input isolation | Same native JSON checks, report unit/integration tests | Single native sample PASS; complete manual matrix OPEN |
|NewGame/load success/failure/modal/defeat | Engine/Client automated lifecycle tests | Automated PASS; native manual NOT RUN |
|Human visual/gameplay acceptance | Owner review | OPEN |

The native harness uses an unchanged production CombatMukha seed1 paused world and explicitly injected presentation fixtures. It does not prove live CombatMukha balance or native Save/Load/defeat. It ran64 frames for3.40seconds. It did not measure600-frame performance. Screenshots do not prove scanout.

Manual harness source: tools/DeepSpaceSaga.Performance/CombatReportsNativeEvidence.cs. It is prepared for owner-operated review only. **Agent must not invoke it autonomously.** No claim of complete native acceptance.
