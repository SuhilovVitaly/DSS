# EP-0009 — execution evidence

## Resumption — 2026-10-12

EP-0008 final ticket6b7186444b027a03671e62ba61e86623e00762a2 published and remote verified. Dependency is delivered under explicit user deferral of native/human gates and cancellation of Graphify. Current scope: autonomous38-ticket chain, separate commit/push each, no game/native/graph launches. Standard cards and final documentation story prepared. [Technical assumptions](ExecutionDecisions.md). First implementation: US-0001-TK-0001 module-health-contracts. Historical blocker below is resolved under this revised scope.

## Historical baseline report — 2026-10-11

# EP-0009 — проверка базы исполнения

Дата: 2026-10-11 (Asia/Jerusalem).

Статус: **BLOCKED — обязательная зависимость EP-0008 не завершена**.
Реализация EP-0009 не начата; ни один из 38 тикетов не завершён.

## Контракт и блокер

[Спецификация EP-0009](EP-0009-module-combat.md), revision 2, требует перед
US-0001 взять HEAD с полностью завершённым EP-0008. Первый тикет
EP-0009-US-0001-TK-0001-module-health-contracts имеет зависимость `EP-0008 done`.
Текущая база эту зависимость не удовлетворяет. Замена боевых правил EP-0008
не заменяет требуемую инфраструктуру ручной ПРО, враждебного ИИ,
терминального поражения и сохранений.

## Проверенные факты

| Проверка | Фактический результат |
|---|---|
| `git status --short --branch` до работы | `base-fight...origin/base-fight`; только неотслеживаемая папка `Board/EP-0009-battle/` |
| `git rev-parse HEAD` | `99e15a2414b753d91eca9a2cbbe0a1309cb23c80` |
| `git remote get-url origin` | `https://github.com/SuhilovVitaly/DSS` |
| Upstream | `origin`, `refs/heads/base-fight` |
| `git ls-remote --heads origin` | После повтора с разрешённым сетевым доступом успешно; `base-fight` указывает на тот же `99e15a2` |
| `git fetch origin` | Успешно; `origin/main` обновлена до `781ffa714e5b0aebcfd88a9197516e25bf2636a8` |
| `git log origin/main -6 --oneline` | Последний коммит — merge PR #88 из `base-fight`; завершения EP-0008 нет |
| `git diff --stat origin/base-fight origin/main -- src tests` | Пусто: исходники и тесты совпадают |
| `git log --all --oneline --grep=EP-0008` | Подготовка `198d20a`, TK-0001 `4233f52`, TK-0002 `02c89d4`; других коммитов с EP-0008 в сообщении нет |
| `Scenario/ScenarioData.cs:32` | `CurrentSaveFormatVersion = 15`; требуемая база EP-0008 предусматривает 16 |
| `SimulationEngine.cs:2328` — диспетчер команд | Обрабатывает `DefenseCommandTypes.Enable/Disable`, но не `DefenseCommandTypes.Fire`; строковая константа `defense.fire` в Contracts не доказывает runtime-реализацию |
| Другие локальные ветки и worktree | Ветки EP-0004/EP-0005 относятся к другим эпикам; detached worktree `44bf` находится на `5d59abf` (EP-0001), содержит посторонние изменения, не изменялся |

Историческая запись о непубликации TK-0002 в
[отчёте EP-0008](../EP-0008-player-defense-and-hostile-combat/ImplementationStatus.md)
устарела в части push: текущая удалённая `base-fight` уже содержит TK-0002.
Это не означает завершения последующих тикетов EP-0008.

## Выполненная работа и проверки

- Повторно прочитаны требования к базе, порядок исполнения и первый тикет.
- Проверены локальные и удалённые ссылки Git, актуальный исходный код и
  совпадение исходников `origin/main` с `origin/base-fight`.
- Добавлен только этот отчёт. Исходная спецификация пользователя сохранена.
- Production-код и тесты не изменялись. Build, тесты и native acceptance:
  **NOT RUN** — реализация остановлена на проверке обязательной зависимости.
- Коммиты и push EP-0009 отсутствуют; последний опубликованный тикет EP-0009:
  **нет**.

## Условие продолжения

Получить проверенную базу с завершённым EP-0008, затем заново проверить API,
пути и критерии EP-0009 на этой базе. Начать с
`EP-0009-US-0001-TK-0001-module-health-contracts` после обязательного чтения
документации и подготовки Board. Завершающую US-0012 оформить в стандартной
структуре и расширить до всех требований §6 `EpicExecutionPrompt.md`.
Все 12 историй и 38 тикетов EP-0009 остаются открытыми.

## EP-0009-US-0001-TK-0001 — 2026-10-12

Additive module-health and journal Contracts implemented. Focused3/3 and full Contracts188/188, solution build0 warnings/errors, scoped format/diff PASS. Existing hull API intentionally remains until US-0003 consumer migration (A08); no module-health gameplay claim yet. Separate publication follows.

## EP-0009-US-0001-TK-0002 — 2026-10-12

TK-0001 6baef18139b177340ba329f272ac22835a96d8dc published/remote verified. Additive launcher/defense/torpedo/command/preview Contracts implemented with four JSON regressions. Initial full Contracts7 failures from default/empty ImmutableArray equality corrected with nullable queue identity; final Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Old active-flight/hit-chance fields remain until live consumer migration; gameplay acceptance not claimed. Separate publication follows.

## EP-0009-US-0001-TK-0003 — 2026-10-12

TK-0002 2efad4619528e97af27fd1131f4040e5fe8774d8 published/remote verified. Combat rules/fire modes production JSON loaded and validated through Settings/EngineContentLoader/GameDataRegistry. Focused9/9; non-corpus Engine1931/1931 before final additional validation, focused9/9 after; solution build0 warnings/errors, scoped format/diff PASS. Initial new-file EOL formatter failure corrected. Runtime consumption follows. Separate publication follows.

## EP-0009-US-0001-TK-0004 — 2026-10-12

TK-0003 1bead7588ee439464f3d5fde3e3db4e534c20e37 published/remote verified. Additive module schema for launcher, defense, engine, and combat role; JSON ingestion and category/range checks. Focused63/63 after final test addition; non-corpus Engine1936/1936 before final test-only addition; solution build0 warnings/errors. Scoped format/diff PASS. Legacy content without new fields remains loadable until US-0002 catalog migration; gameplay not yet changed. Separate publication follows. Graphify/game/native NOT RUN per current user instruction.

## EP-0009-US-0001-TK-0005 — 2026-10-12

TK-0004 e2bb1da9479db4087dbda250b2f36c9be49c6c73 published/remote verified. Crew accuracy/maneuverability skills and module armorClass schema with runtime and Save/Load preservation. Focused19/19; first broad Engine non-corpus1943/1944 had one pause-timing failure unrelated to changed paths; that test passed alone and full clean repeat1944/1944. Solution build0 warnings/errors; scoped format/diff PASS. New combat math follows in later tickets. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0002-TK-0001 — 2026-10-12

TK-0005 f7eb4c4a8a694227c39bca25600239d0e961649a published/remote verified. US-0001 story review recorded partial AC2/AC3 until later catalog/runtime migration. Weapon catalog now contains EP-0009 launcher and defense values plus the new single-barrel defense variant. Historical EP-0008 combat fixtures pin their original weapon values; shipped data is checked separately in Client tests. New locale keys cover previously added combat event enum values. Focused Client catalog19/19; Engine non-corpus1944/1944; Client full1857/1857; solution build0 warnings/errors; scoped format/diff PASS. Initial Engine88/Client30 failures came from old fixed catalog assumptions, addressed in tests/fixtures. Runtime multiflight/burst behavior still follows in later tickets. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0002-TK-0002 — 2026-10-12

TK-0001 65bdb36aa8d419dc5a3240a8d981297589933c25 published/remote verified. Basic engine now reaches6km/s with maneuverability10; new Strizh8km/s, acceleration800m/s², turn12°/s, mass4000kg, maneuverability40. Runtime command/propulsion selection, hostile restore and solar generation use module.engine category, with compatibility for legacy programmatic registries lacking category metadata. Actual Strizh command and solar Vmax regression passed. Engine non-corpus1946/1946, Client full1857/1857, solution build0 warnings/errors, scoped format/diff PASS. Production scenario loadout and combat maneuverability use follow later. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0002-TK-0003 — 2026-10-12

TK-0002 8bbe76d48f021273312cd09fce1c5e7736f38f15 published/remote verified. All15 module types now have structurePointsMax150, bridge has combatRole bridge. Six shipped scenarios had above-max structurePoints lowered to150 early because standard Engine bootstrap otherwise failed; this subset is recorded in US-0009-TK-0003. Content one/two-hit arithmetic regression is a data gate only; runtime damage follows later. Focused Client26/26; Engine non-corpus1946/1946; Client full1860/1860; solution build0 warnings/errors, scoped format/diff PASS. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0002-TK-0004 — 2026-10-12

TK-0003 cf44b0c975b5185901858e971a9b99ddc05fb995 published/remote verified. Ship class content and Engine schema now contain only TypeId; Mukha and Shmel class ids load. Legacy hull runtime remains under A08, sourcing scenario/save HP or default450 until US-0003. Hostile scenario layouts/images will be integrated by US-0009 scenario tickets; US2-AC3 remains OPEN. Focused Engine99/99 and Client4/4 before final Client test addition; Engine non-corpus1943/1943, Client full1861/1861; solution build0 warnings/errors, scoped format/diff PASS. Full Engine corpus run was cancelled after slow generation and replaced by non-corpus suite. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0003-TK-0001 — 2026-10-12

TK-0004 44fa559522d126f537d35c8f30552dab4968ec03 published/remote verified. US-0002 story review recorded partial AC2/AC3 until runtime hit logic and scenario layouts. Installed module runtime now initializes armor/structure from combat rules and scenario, projects module health, and preserves mutable armor/disable/destroy fields in JSON Save/Load. Existing hull impact path remains until TK-0003 under A08, with `No_hull_state` still OPEN; this ticket is an additive intermediate step, not completed US3-AC1. New focused3/3; Engine non-corpus1946/1946 before legacy-null serialization repair, focused Engine21/21 after; initial Client full1860/1861 found null-vs-zero legacy armor serialization, corrected and focused1/1, final Client full1861/1861. Scoped format/diff PASS. Graphify/game/native NOT RUN per user.

## EP-0009-US-0003-TK-0002 — 2026-10-12

TK-0001 51aae0749b5af4132c9624dc19e7db19c2680864 published/remote verified. Mass-weighted module hit selection, isolated persisted RNG, captured armor parameters, D09 armor/structure split, disable timer and module journal events implemented. Hull damage still runs in parallel until TK-0003 removes the last consumer under A08; current combat therefore remains transitional. Focused ModuleDamage5/5, initial Engine non-corpus1950/1951 exposed old five-event journal assertion, updated; final Engine non-corpus1951/1951, Client full1861/1861, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. No two-surviving-hit disable extension test yet. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0003-TK-0003 — 2026-10-12

TK-0002 7d5f8db507b075810962cc47854e28b3139bcfa5 published/remote verified. D11 module-death threshold and critical roles now drive a unified `DestroyShip` path for combat and station security; the path emits `ShipDestroyed` with a captured reason, creates a wreck, and triggers EP-0008 player defeat exactly once. Runtime hull HP staging/projection and `ship.tetrarch` destruction hardcode were removed. Engine non-corpus1956/1956 and Client1859/1859 PASS after migrating historical HP assertions and scenarios. Solution build0 warnings/errors, scoped formatter and diff PASS. Client completed-impact resume now has one parameter case instead of three; two-hit UI destruction and wreck behavior remain covered by BasicCombatUiFlowTests. Separate publication follows. Graphify/game/native NOT RUN per user.

## EP-0009-US-0003-TK-0004 — 2026-10-12

TK-0003 1c28695238134114daa705d5fd9022d0ec2c5329 published/remote verified. Physically disabled/destroyed modules reject commands with distinct reason codes; hit interrupts active cycles and clears pending PD burst shots. Disabled engines coast without NPC maneuvers, and NPC planning resumes at timer expiry. Hostile Save/Load readiness is checked at the restored motion time. Focused DisabledModule6/6; Engine non-corpus1962/1962, Client1859/1859, Contracts192/192; solution build0 warnings/errors, scoped format/diff PASS. US-0003 story review: AC1 runtime module health and removal of `_hullCombat` PASS; AC2 weighted hit and D09 PASS, a dedicated two-surviving-hit timer-extension regression remains OPEN; AC3 D11 combat/security and EP-0008 terminal path PASS; AC4 command/PD/engine behavior PASS in headless tests. Native/game acceptance NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0004-TK-0001 — 2026-10-12

TK-0004 fcdae04429d326513d75f7074fb207137c4d9b95 published/remote verified. Live automatic/manual defense and preview use `CombatChanceMath` with configured base50 and direct countermeasure accuracy skill. Frozen EP-0008 save validation is preserved; arbitrary custom chance bases cannot roundtrip in Save v16 until rules identity is recorded. Historical EP-0008 fixture rules are pinned for their old golden outcomes. Engine non-corpus1961/1961, Client1859/1859, solution build0 warnings/errors, scoped format/diff PASS. Torpedo attack and maneuverability skills follow in TK-0002/TK-0003; US4-AC1 formula/callers PASS in headless coverage, full story acceptance OPEN. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0004-TK-0002 — 2026-10-12

TK-0001 3df87e2377bf882a0b6feb415a2ad49a8a8f3a37 published/remote verified. D05 launch-time attack and contact-time engine/pilot defense are implemented with independent `combat.torpedo.hit.v1` RNG, journal outcome, persisted state/counter and miss termination without damage. Non-ship obstacle contact does not consume this roll. Focused4/4 including Save/Load continuation and partition invariance; Engine non-corpus1969/1969, Client1859/1859, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Broad Engine and Client suites were repeated and passed after journal-counter validation hardening. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0004-TK-0003 — 2026-10-12

TK-0002 e2420ea361d53ff943bd52aff86fc31ba0540b76 published/remote verified. Torpedo maneuverability directly includes its operator's `ManeuverabilitySkill`, frozen at launch; PD directly includes its operator's `AccuracySkill`, frozen at countermeasure launch. Manual/auto interception and preview share D04, with preview Attack/Defense/BaseChance populated. Production 58+20 vs 50+20 gives 580; 0% and 100% encounters still each consume one interception RNG draw. Focused Engine62/62 and example/preview/boundaries13/13; Engine non-corpus1972/1972, Client1859/1859, solution build0 warnings/errors, scoped format/diff PASS. US-0004 story review: AC1/AC2/AC3 PASS in headless coverage. Custom chance-base Save v16 roundtrip remains OPEN; native/game acceptance NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0005-TK-0001 — 2026-10-12

TK-0003 acd5c646fedba322bb66a5626b80cb1bd8423d36 published/remote verified. Launcher cooldown starts at launch (15000 MotionTimeMs), with independent active-flight list, repeat player/NPC launch, targeted self-destruct, Client reloading state, and legacy singular-field Save/Load migration. Focused multi-flight and idempotence checks passed. Engine non-corpus1970/1970, Client1859/1859, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Game/native NOT RUN and Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0005-TK-0002 — 2026-10-12

TK-0001 1af75fb9bcb7721f219480ddaa36d5f9a428ddac published/remote verified. Two-barrel defense now queues a second shot at +2000 MotionTimeMs; the combat scheduler orders pending shots after contacts/expiries and before new launches. It skips a removed target or unavailable module, tracks overlapping countermeasure IDs, and persists the queue through Save/Load. Focused burst/skip/roundtrip3/3; Engine non-corpus1973/1973, Client1859/1859, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. TK-0003 owns reload-from-first-shot and attempt policy. Game/native NOT RUN and Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0005-TK-0003 — 2026-10-12

TK-0002 f8096e45af1fb11706c9507187e525e5c3c4811d published/remote verified. D13 readiness now starts with the first barrel; D15 auto targeting checks live guiding countermeasures and retries after a miss. The runtime attempted flag is removed. Save v16 writes an empty legacy attempt list until US-0011 and restores missing readiness from older saves. Production 14500 ms boundary passed; Engine non-corpus1972/1972, Client1859/1859, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Headless US-0005 story review: AC1 cooldown/multi-flight PASS, AC2 burst/skip/Save-Load PASS, AC3 launch-based reload/retry/arbitration PASS. Game/native NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0006-TK-0001 — 2026-10-12

TK-0003 ff6fe1ef83cda0c632331f2f64b5728e8d704d87 published/remote verified. Fire-mode bonuses are frozen at torpedo launch; Maneuverability gives 48% interception in the production fixture and 18750 ms reload. Overheat uses a separate persisted `combat.launcher.breakdown.v1` RNG and can break the launcher after launching; docking clears the broken flag. Focused mode3/3 and docking/dialogue/mode26/26; Engine non-corpus1975/1975, Client1859/1859, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Mode selection and UI remain TK-0002/TK-0003; native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0006-TK-0002 — 2026-10-12

TK-0001 5f5a4af2907a5d57b0788fcc2f44eb591247bce6 published/remote verified. Production `torpedo.setMode` now dispatches through Engine, category command allowlist and content. Out of combat it selects immediately; in combat it adds one base reload cycle to the current readiness deadline. Processed IDs persist through Save/Load; invalid category and mode reject. Focused Engine4/4 covers pause, enemy/torpedo combat, PD rejection and duplicate after reload. An initial broad Engine run exposed an old two-command catalog assertion and was interrupted; focused repeat of the repaired assertion passed. Initial Client full1842/1859 exposed only fixed command-list/count assumptions; those assertions were updated to the three-command contract. Final Client1859/1859, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0006-TK-0003 — 2026-10-12

TK-0002 16255243beb7b270fc73c754bc12cae83d82f4d8 published/remote verified. The torpedo panel now cycles Standard/Maneuverability/Overheat using an addressed `ModeId` command, blocks repeated submissions until authoritative confirmation or rejection, and displays localized mode, remaining reload seconds and breakdown-until-dock state. Broken launchers disable Fire and mode selection. Focused panel21/21 and related UI125/125; initial full Client had seven old exact-status/icon-list assertions, then the repeat had four remaining exact-status expectations and one unrelated timed-out market scenario. After updating all status assertions, the market scenario passed alone5/5. Remaining Client1855/1855 plus isolated market5/5 PASS, solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0007-TK-0001 — 2026-10-12

TK-0003 db93b03e8ca76a6422a00a179f7fe97ffd8909ab published/remote verified. US-0006 self-review: AC1 captured bonuses/reload and AC2 separate breakdown RNG/dock reset PASS headlessly; AC3 command timing/idempotence/PD rejection and UI mode/timer/broken status PASS headlessly. Native/window interaction remains OPEN by user instruction. For US-0007, manual `defense.fire` already used the shared two-barrel queue; this ticket adds distinct `not_ready` on cooldown and permits same-module relaunch after ReadyAt while older countermeasures still guide. Module disabled/destroyed remain distinct. Engine focused27/27, affected Client24/24, Contracts192/192, solution build0 warnings/errors, scoped format/diff PASS. Game/native NOT RUN and Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0007-TK-0002 — 2026-10-12

TK-0001 673f4223a3bddb0ffc0a1521d017ec7a84704cec published/remote verified. Two independent defense panels now project their own barrel count, queued second-shot timer and launch-based readiness timer from confirmed MotionTimeMs; the existing D06 chance breakdown and active-flight details remain. Target UI9/9 and Client1856/1856 excluding the slow market scenario, which passed alone5/5 at the previous ticket; solution build0 warnings/errors, scoped format/diff PASS. US-0007 self-review: AC1 shared two-barrel manual burst, readiness/disabled/destroyed guards, duplicate and zero-chance behavior PASS headlessly; AC2 independent panels/timers and D06 preview PASS headlessly. Game/native OPEN by user instruction; Graphify rebuild SKIPPED. Separate publication follows.

## EP-0009-US-0008-TK-0001 — 2026-10-12

TK-0002 7d293d4e0bb553816e28febf14e1ceaa8d071487 published/remote verified. Hostile launch still uses shared 15-second cooldown and now requires a predicted intercept within 0.8 of captured `torpedoMaxFlightTimeMs`. Every newly launched torpedo has a captured physical expiry (production120s); deterministic expiry removes flight, adds journal/impact facts and survives Save/Load. Nullable old-save lifetime remains accepted until Save17 migration. New NPC/TTL3/3; battle regression376/376 after adapting five old impossible-launch or old-reason fixtures; Contracts192/192, Client1856/1856 excluding the slow market scenario (separately passed5/5 in US-0006 TK-0003), solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0008-TK-0002 — 2026-10-12

TK-0001 c82560134948160b8c7954cf6d7fb7772c040f34 published/remote verified. Pure `HostileOrbitMath.PlanAttack` now handles D33 distance-scaled flank with moving-target lead, old orbit steering inside the configured band, and close-range retreat. Missing band delegates exactly to EP-0008 `Plan`. Added stable `FlankApproach`/`Retreat` phase values and Client labels. Historical Python AttackAI source is absent, so simulator equivalence remains unverified. Motion focused17/17 and full173/173, Contracts194/194, related Engine27/27 and Client10/10, solution build0 warnings/errors, scoped format/diff PASS. Scenario parameters and live integration are TK-0003. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0008-TK-0003 — 2026-10-12

TK-0002 6e858bcde45998e344be5b9c805dc6e2d0ef1839 published/remote verified. Scenario NPC `ai` (`standoffKm`, `bandKm`, `flankOffsetDeg`) is validated and passed to the live hostile planner; the absent block preserves EP-0008 defaults. Parameters survive save/load and configured radius is accepted by hostile-state preflight. Focused Engine9/9, related hostile37/37 (before final orbit assertion), solution build0 warnings/errors, scoped format/diff PASS. Mukha-specific content follows in US-0009. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0009-TK-0001 — 2026-10-12

TK-0003 52008fcae257bbd0885da382c7f5557388c484f6 published/remote verified. Added selectable `CombatMukha`: Tetrarch against three Strizh-powered, unarmored Mukhas with configured 50±10 km attack band and −35/0/+35 flank offsets. Player has nine light-armored modules, three weapon operators plus a pilot, all skill20; the production catalog gains a four-cabin living-quarters variant. Two solar-only Client tests now select scenarios from the solar generation allowlist, because combat encounters intentionally have no solar map. The initial broad run failed those two assumptions; targeted repeat4/4 and broad repeat1863/1863 (excluding slow market suite) PASS. Final content2/2, solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0009-TK-0002 — 2026-10-12

TK-0001 97f7f552a4002d7820f4d0200635d11af15e1fed published/remote verified. Added selectable `CombatShmel` from the Mukha encounter: three `ship.shmel` with an extra existing single-barrel PRO module, active auto-defense and a distinct skill20 operator for each module. Production loader and three hostile AI states verified headlessly; focused content4/4, related scenario/map12/12, solution build0 warnings/errors, scoped format/diff PASS. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0009-TK-0003 — 2026-10-12

TK-0002 c4977e72e1a019de5fc86a137d922c0909d5fdf7 published/remote verified. Six older scenarios now carry explicit zero accuracy/maneuverability and `none` armor, preserving their gameplay; the two new combat scenarios retain light player armor and skill20. Removed obsolete crew skills from scenario DTO, runtime/save projection and fixture code. Strict JSON rejection of obsolete fields is tested. Updated passive catalog count for the prior four-cabin addition and the shipped-scenario list for both new encounters. Engine operator/defense38/38, scenario/catalog/market21/21, cross-domain sample4/4 and Client scenario36/36 PASS, solution build0 warnings/errors, scoped format/diff PASS. Three long broad Engine runs were interrupted; first two exposed now-corrected failures, while the third reported no failures before interruption. Full post-fix Engine status remains NOT COMPLETE. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0010-TK-0001 — 2026-10-12

TK-0003 c223a640144bbb3495520b0a38d54c86eb4a26f7 published/remote verified. US-0009 self-review: AC1/AC2 both combat starts listed/load with specified geometry, module/crew loadouts and active hostile AI; AC3 six legacy scenarios load under the new skills/armor schema and reject old skill fields. Native/human battle acceptance and full post-fix Engine suite remain OPEN. This ticket adds end-to-end boundary proof for threshold3/9 and critical-engine defeat, exact clock stop, one terminal transition, no later combat events, and saving blocked. Existing Engine path needed no production change. Related Engine17/17, solution build0 warnings/errors, scoped format/diff PASS. Native defeat window NOT RUN, Graphify rebuild SKIPPED. Separate publication follows.

## EP-0009-US-0010-TK-0002 — 2026-10-12

TK-0001 2a95d7d1adce4ef82fc9150c79b7427e30b11e57 published/remote verified. Known player/NPC ships now project authoritative per-module armor, structure, disable deadline and destruction; unknown NPC ships expose no module health. The selected-ship panel shows those values with a remaining disable timer and constrained, scrollable layout. Map labels use an aggregate armor+structure module bar, independent of hull HP and ship class. After repairing two UI regressions and adapting snapshot equality plus a stale scenario hash, focused Engine1/1, Client panel/map52/52 and broad Client1873/1873 excluding the slow market route suite PASS. Solution build0 warnings/errors, scoped format/diff PASS. US10-AC2 headless PASS; native UI NOT RUN and Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0010-TK-0003 — 2026-10-12

TK-0002 c85f4d20838e308fa1c940cc2d18a28ce9cee771 published/remote verified. The journal presents captured torpedo attack/defense/chance/roll, interception's new formula, module armor/structure before→after, destroyed module, ship-death cause, skipped burst reason and Overheat breakdown in English/Russian. Journal/locale22/22 and broad Client1876/1876 excluding the slow market route suite PASS; a transient route-allocation failure passed alone8/8 and in the final broad repeat. Solution build0 warnings/errors, scoped format/diff PASS. US-0010 self-review: AC1 defeat path, AC2 module panel/bar, AC3 journal lines PASS headlessly; native defeat/UI acceptance NOT RUN, Graphify rebuild SKIPPED. Separate publication follows.

## EP-0009-US-0011-TK-0001 — 2026-10-12

TK-0003 1f816d6a6a6538ab5c6b7887877180b34753be15 published/remote verified. Save v17 writes defense3/combat2/hostile2, retains module durability and disable state, weapon readiness/mode/broken/active flights, defense bursts, named combat RNG states/counters and NPC AI; malformed v17 continuation and v16-or-earlier save files are rejected before world replacement. Removed dead auto-attempt and singular-flight fields; hit-chance preflight now accepts 0..100. A full Contracts run exposed old default-ImmutableArray equality tests from TK-0002, repaired here. Engine save/compatibility167/167 and related combat326/326, Contracts194/194, Client save8/8 and broad1876/1876 excluding slow market route suite PASS; solution build0 warnings/errors, scoped format/diff PASS. Full Engine corpus and native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0011-TK-0002 — 2026-10-12

TK-0001 18bd3474ea7b4c2bb79ca3ce1e50a64d6f31c9ce published/remote verified. Added real-scenario Engine end-to-end proof and an opt-in Balance category that ran 100 seeds each on CombatMukha and CombatShmel. Focused2/2 and Balance2/2 PASS. Results: Mukha67% versus historical81.2% (−14.2 pp, outside tolerance); Shmel51% versus49.5% (+1.5 pp); one 600-second unfinished battle per scenario. The discrepancy is recorded in `evidence/balance.md` and does not block US11-AC2 under the epic. Native/game NOT RUN, Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0011-TK-0003 — 2026-10-12

TK-0002 3ccadfde0b7109dd7735cc43b1b8d3909c04aa1c published/remote verified. The referenced historical Python simulator and `Client/Data/CombatDesign` are absent; created a clearly labeled independent sampler that reads the current production module/combat/ship/scenario JSON and validates their references. Python unittest4/4 and headless CLI100-seed runs on both scenarios PASS. New sampler win counts Mukha73/100 and Shmel34/100 are not historical or authoritative Engine outcomes. Native matrix at `evidence/native.md` remains entirely NOT RUN by user instruction; US11-AC3 remains OPEN. Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0012-TK-0001 — 2026-10-12

TK-0003 66ae0c9965a3c91a9c07b9413c3430973aa6022c published/remote verified. Updated canonical Engine/first-release requirements, process guide/index, Tetrarch/map/panel mechanics, GameSession/Save/Load/Defeat screens, historical torpedo/PRO reports and EP-0008 supersession. Added `Documentation/04-Engineering/ModuleCombat.md` and `ModuleCombatDocumentation.md` impact inventory, and synchronized EP-0009 Board navigation. Historical EP-0008 test/native findings remain dated; current EP-0009 Engine balance discrepancy and NOT RUN native matrix are linked rather than upgraded to acceptance. Relative file targets across21 changed Markdown files PASS, Board12 stories/38 tickets, targeted stale-current scan and diff check PASS. Full anchor/nav audit follows in TK-0002. Graphify rebuild SKIPPED per user. Separate publication follows.

## EP-0009-US-0012-TK-0002 — 2026-10-12

TK-0001 65076b732aa286e2f53d1540483b3b37b131e5de published/remote verified. Final link audit found28 stale references to three historic EngineRequirements anchors; explicit compatibility anchors added. Repeat:593 local Markdown links,0 missing targets/anchors; Board12 stories/38 unique tickets, all local dependencies valid, DAG acyclic. See `evidence/navigation.md`. Graphify rebuild SKIPPED and game/native NOT RUN by direct user instruction; no graph or native PASS is claimed. The Mukha balance gap and incomplete full Engine corpus remain recorded in prior evidence. Separate publication follows.

## Final cross-epic review repair — 2026-10-12

US12 TK-0002 e5584284afde13a90a6b2cf20fddf58f939b9f92 published/remote verified: all38 ticket commits are on origin/base-fight. A corpus-inclusive Engine attempt surfaced five stale snapshot-record equality assertions, then ran >10 minutes without a final result and was interrupted; it is NOT COMPLETE. Focused reproduction5/5 failed before repair and passed5/5 after structural full-snapshot comparison; broad Engine without corpus and Balance passed2005/2005, while Balance had separately passed200 real battles. Scoped format and diff check PASS. This follow-up addresses test assertions introduced by US10 TK-0002 module projection and will be published as a separate repair commit. Native/game NOT RUN and Graphify rebuild SKIPPED by user instruction; Mukha balance discrepancy remains documented and user/product acceptance OPEN.
