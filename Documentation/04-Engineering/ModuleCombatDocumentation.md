# EP-0009 documentation impact inventory — 2026-10-12

## Current EP-0012 contract — 2026-10-21

EP-0012 now supplies point-aim projectiles, geometric blast damage, manual point-PRO, evasion and Save18. Guided contact/roll/mode/auto/burst and Save17 statements below are historical. Module armor/structure and deterministic ownership remain. Current balance and human/native limitations are explicitly open. [Point-aim combat contract](PointAimCombat.md).

## Historical record (superseded combat rules)

All combat formulas, saves, commands, conclusions and permissions below describe their original dated stage. They are not the current writer/runtime or fresh acceptance. Non-combat invariants remain applicable.

This inventory is based on the final module-combat contracts and searches for Hull HP, EP-0008 hit formula, operator skills, one-flight/auto-attempt rules, Tetrarch-only damage, Save16 and historical range/speed constants. The current source of truth is [EngineRequirements §Phase Four](../01-Requirements/EngineRequirements.md#phase-four-module-combat); the concise engineering description is [ModuleCombat](ModuleCombat.md). Dated EP-0006/0007/0008 results are retained as history rather than rewritten as newly measured outcomes.

| Document / group | Disposition |
|---|---|
| `Documentation/01-Requirements/EngineRequirements.md`, `FirstReleaseRequirements.md` | Updated current contract, formula, module health, Save17 and explicit EP-0008 supersession; historical phase retained. |
| `Documentation/00-Process/CLAUDE.md`, `Documentation/README.md` | Updated navigation, content sources and current writer; dated EP-0008/Save16 text labeled historical. |
| `Documentation/02-FirstRelease/Mechanics/TetrarchClass.md` | Added combat-scenario nine-module/light-armor/four-crew loadout and removed Hull HP as current death rule. |
| `Documentation/02-FirstRelease/Mechanics/CommandPanels.md`, `TacticalMapAndManeuvering.md` | Added modes, active flights, module condition and configurable AI/ranges; EP-0008 details remain historical. |
| `Documentation/02-FirstRelease/Screens/GameSession.md`, `Defeat.md`, `Save.md`, `Load.md` | Updated selected module display, journal, module death and Save17 rejection/recovery. Native UI remains NOT RUN. |
| `Documentation/04-Engineering/ModuleCombat.md` | New current architecture, data, save and acceptance summary. |
| `Documentation/04-Engineering/BasicTorpedoCombat.md`, `CountermeasureCombat.md` | Added supersession links above dated EP-0006/0007/0008 behavior and old constants. |
| `Board/EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md` | Added “Superseded by EP-0009” section with §2.1 link; original test/native/FPS evidence retained. |
| `Board/EP-0009-battle/Documentation.md`, `Tickets.md`, story/ticket cards, `ImplementationStatus.md`, `PublicationStatus.md`, `evidence/balance.md`, `evidence/native.md` | Checked 12-story/38-ticket structure and outcomes; native/human and graph limitations preserved. Current ticket and publication markers updated as each ticket is committed. |
| `src/DeepSpaceSaga.CombatSim/README.md` | Explains new production-data sampler, independent approximations and CLI reproduction. |
| `Documentation/03-Design/TacticalMapSpecification.md` | Reviewed: historical map design statements and dated EP-0008 note. Current combat presentation is now linked from GameSession/ModuleCombat; no old result rewritten. |
| `Documentation/04-Engineering/AiMapEnvironment.md`, `TradingAndSolarSystem.md` | Reviewed: Save15 and AiMap/trading implementation reports are dated EP-0004 history, unaffected by module rules. Current Save17 documented above. |
| `Documentation/04-Engineering/PhaseThreeCombatAcceptance.md`, `PhaseThreeCombatDocumentation.md` | Reviewed: dated EP-0008 automated/native evidence, not EP-0009 acceptance; left intact. |
| `Documentation/06-Tooling/GameSessionScreenUI.md`, root `AGENTS.md`/`CLAUDE.md`, `Documentation/00-Process/DocumentationSystem.md`, `tools/DeepSpaceSaga.Performance/README.md` | Reviewed: workflow/UI harness descriptions contain no current EP-0009 combat contract requiring edits. Root README absent. |
| EP-0006/0007 Board `Documentation.md` and `Tickets.md`, other dated engineering/backlog/asset reports | Reviewed as historical ticket/review records; current contract linked through canonical requirements and new ModuleCombat page, so historical outcomes were not rewritten. |

The historical `Data/CombatDesign` tree and simulator source were absent; the new sampler's README states its limitations. The graph remains unchanged by user instruction. [Balance evidence](../../Board/EP-0009-battle/evidence/balance.md) records the 100-seed Engine discrepancy; [native matrix](../../Board/EP-0009-battle/evidence/native.md) records every deferred real-window case as NOT RUN. This documentation ticket does not turn those gates into PASS.

## Продолжение EP-0010 — 2026-10-16

[Боевые доклады](CombatReports.md) добавляют presentation facts E01–E07 поверх этого модульного боя и optional CombatReportsState Version1 внутри Save17. Формулы, конфигурация, journal, RNG и баланс EP9 сохраняются. [Evidence/публикация](../../Board/EP-0010-combat-reports/ImplementationStatus.md); native/human/FPS OPEN. Graphify обновлён в EP10; прежний skip остаётся датированной историей EP9. Самостоятельные игровые запуски агентом запрещены пользователем.
