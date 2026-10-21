# EP-0010 — статус реализации и приёмки

2026-10-16. **IMPLEMENTED / HEADLESS VERIFIED / ACCEPTANCE OPEN**. Пользовательский результат: E01–E07 в правой панели, captured facts/докладчики, RU/EN,8 illustrations,200-card accordion, надёжная доставка и Save17 continuation. Полностью принятым эпик не объявляется.

| Область | Текущий результат |
|---|---|
| Scope | 12 историй /30 тикетов:27 исходных +3 corrective |
| Production implementation US01–US10 | IMPLEMENTED; self-review complete |
| Assets |8/8 PNG1024×576, catalog/output/publish; assistant QA, human OPEN |
| Contracts |210/210 PASS |
| Engine |2197/2197 PASS, full headless suite,29min |
| Client |1973/1973 PASS, excluding5 baseline station-deadline scenarios |
| Scope regression |226/226 PASS; all confirmed EP10 failures repaired |
| Build |Full DeepSpaceSaga.sln,0warnings/0errors |
| Format/diff |All epic-changed C# scoped dotnet format PASS; git diff --check PASS |
| US11 automated proof |Production CombatMukha +rare E01–E07 outcomes, delivery/persistence/UI matrix PASS |
| US11 native/human/FPS |PARTIAL/OPEN; one old64-frame sample; no600-frame perf measurement |
| US12 documentation/Graphify |IMPLEMENTED: inventory,428links/45anchors/30-ticket DAG/R01–R24 PASS;844-input Graphify verified |
| Commit/push |29 prior ticket commits published and verified; US12 final documentation commit published last; own SHA verified through Git after creation |

**Инструкция пользователя: «никогда не запускай самостоятельно игру».** Больше игровых окон/native/performance harness агент не запускает. Native matrix, manual Save/Load/Defeated, human art/gameplay, GPU/scanout/80FPS остаются OPEN. [Native evidence](evidence/NativeAcceptance.md), [Performance](evidence/Performance.md).

Полная торговая theory не зелёная: current10/11 в классе, baseline4/5 theory;15s refuel deadline воспроизведён на исходном af594de (другой station subtype в повторе, что указывает на общий временной бюджет). Это сторонняя baseline-проблема, не исправлялась. Финальная Client suite явно исключает ровно5 theory cases; её результат не выдается за полный1978/1978. Raw TRX — evidence/regression.

[Тикет → SHA → push](Publication.md) · [инвентаризация документов](DocumentationInventory.md) · [audit](DocumentationAudit.json) · [финальное собственное ревью](FinalReview.md) · [текущая архитектура](../../Documentation/04-Engineering/CombatReports.md) · [Graphify](../../Documentation/06-Tooling/TradingSolarGraphify.md).

## Graphify и документация

Свежий graph13087nodes/45407edges/416communities по713code+131Markdown.844SHA256 inputs verified;0missing/invalid locations/dangling/selfloops.992unresolved edges и2494unlocatedreference nodes сохранены отдельным audit; selective14mechanisms, не exhaustive semantic coverage. HTML агрегирует communities. Снижение объёма старой семантики EP1–4 явно задокументировано, AST всего выбранного кода свежий. Локальные query CombatReportInbox/CombatReports и semantic path outbox→mailbox→buffer→inbox→history проверены. Подробнее — tooling doc и src/graphify-out/verification.json. Link audit428relative/45anchors,12stories/30tickets,unknown deps0/cycles0/R01–R24 covered. Временная baseline worktree удалена после записи evidence, остальные worktrees сохранены.

## Итоговые команды и evidence

- `dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj --no-build --no-restore --nologo --verbosity minimal --blame-hang-timeout 60s`:2197/2197,29min; [сохранённый terminal result](evidence/regression/final-headless.txt).
- `dotnet test tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj --no-build --no-restore --nologo --verbosity minimal --logger "trx;LogFileName=contracts-final.trx" --results-directory Board/EP-0010-combat-reports/evidence/regression`:210/210, [TRX](evidence/regression/contracts-final.trx).
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-build --no-restore --nologo --verbosity minimal --filter "FullyQualifiedName!~StationMarketDemoContentTests.Every_demo_station_docks_and_trades_via_session" --logger "trx;LogFileName=client-final-corrected.trx" --results-directory Board/EP-0010-combat-reports/evidence/regression --blame-hang-timeout 60s`:1973/1973,1m17s, [TRX](evidence/regression/client-final-corrected.trx).
- Focused filter CombatReport / CommandsPanelSkeletonTests / ApproachCameraIntegrationTests / ObjectInfoPanelTests / TacticalMapLabelBoundsTests:226/226, [TRX](evidence/regression/hover-resize-repair.trx).
- `dotnet build DeepSpaceSaga.sln --no-restore --nologo --verbosity minimal -p:UseSharedCompilation=false -p:SkipPortraitPackCheck=true`:0warnings/0errors. Portrait pack check explicitly skipped; no new portrait assets added. Eight new illustrations validated independently in US8.
- `dotnet format DeepSpaceSaga.sln --verify-no-changes --no-restore --include <all changed .cs from git diff af594de> --verbosity quiet`:PASS; `git diff --check`:PASS.

US11 corrections:TK04 report rectangle in free map;TK05 footer/toolbar reservation and affected snapshot/navigation fixtures;TK06 HUD hover/tiny-width wrapping and remaining free-map fixtures. All have separate committed/pushed cards and regression evidence. Story-level reviews are in each US document; final self-review is not independent reviewer or human acceptance.

## Ticket evidence

Фактические результаты тикетов; SHA и push — в Publication.md. Planning audit остаётся историей, не runtime evidence.

| Ticket | Implementation | Automated | Native/human |
|---|---|---|---|
| [EP-0010-US-0001-TK-0001-immutable-report](EP-0010-US-0001-report-contracts/EP-0010-US-0001-TK-0001-immutable-report/EP-0010-US-0001-TK-0001-immutable-report.md) | IMPLEMENTED | 13 focused / 207 suite PASS | OPEN / по scope |
| [EP-0010-US-0001-TK-0002-snapshot-batch](EP-0010-US-0001-report-contracts/EP-0010-US-0001-TK-0002-snapshot-batch/EP-0010-US-0001-TK-0002-snapshot-batch.md) | IMPLEMENTED | 3 focused / 210 suite PASS | OPEN / по scope |
| [EP-0010-US-0002-TK-0001-report-outbox](EP-0010-US-0002-range-reports/EP-0010-US-0002-TK-0001-report-outbox/EP-0010-US-0002-TK-0001-report-outbox.md) | IMPLEMENTED | 4 focused PASS; build/format PASS | OPEN / по scope |
| [EP-0010-US-0002-TK-0002-first-range-entry](EP-0010-US-0002-range-reports/EP-0010-US-0002-TK-0002-first-range-entry/EP-0010-US-0002-TK-0002-first-range-entry.md) | IMPLEMENTED | 7 focused + 4 outbox + 14 regressions PASS | OPEN / по scope |
| [EP-0010-US-0003-TK-0001-captured-reporters](EP-0010-US-0003-launch-reports/EP-0010-US-0003-TK-0001-captured-reporters/EP-0010-US-0003-TK-0001-captured-reporters.md) | IMPLEMENTED | 5 focused + 11 regressions PASS | OPEN / по scope |
| [EP-0010-US-0003-TK-0002-confirmed-launches](EP-0010-US-0003-launch-reports/EP-0010-US-0003-TK-0002-confirmed-launches/EP-0010-US-0003-TK-0002-confirmed-launches.md) | IMPLEMENTED | 5 focused; 26 report/combat PASS | OPEN / по scope |
| [EP-0010-US-0004-TK-0001-intercept-results](EP-0010-US-0004-combat-result-reports/EP-0010-US-0004-TK-0001-intercept-results/EP-0010-US-0004-TK-0001-intercept-results.md) | IMPLEMENTED | 5 focused; 26 reports PASS | OPEN / по scope |
| [EP-0010-US-0004-TK-0002-contact-summary](EP-0010-US-0004-combat-result-reports/EP-0010-US-0004-TK-0002-contact-summary/EP-0010-US-0004-TK-0002-contact-summary.md) | IMPLEMENTED | 6 focused; 60 report/combat PASS | OPEN / по scope |
| [EP-0010-US-0005-TK-0001-save-schema-validation](EP-0010-US-0005-report-save-continuation/EP-0010-US-0005-TK-0001-save-schema-validation/EP-0010-US-0005-TK-0001-save-schema-validation.md) | IMPLEMENTED | 13 focused PASS | OPEN / по scope |
| [EP-0010-US-0005-TK-0002-capture-and-restore](EP-0010-US-0005-report-save-continuation/EP-0010-US-0005-TK-0002-capture-and-restore/EP-0010-US-0005-TK-0002-capture-and-restore.md) | IMPLEMENTED | 7 focused; 83 report/save/combat PASS | OPEN / по scope |
| [EP-0010-US-0006-TK-0001-mailbox-report-merge](EP-0010-US-0006-reliable-report-delivery/EP-0010-US-0006-TK-0001-mailbox-report-merge/EP-0010-US-0006-TK-0001-mailbox-report-merge.md) | IMPLEMENTED | 4 focused + 8 reliability PASS | OPEN / по scope |
| [EP-0010-US-0006-TK-0002-buffer-pending-reports](EP-0010-US-0006-reliable-report-delivery/EP-0010-US-0006-TK-0002-buffer-pending-reports/EP-0010-US-0006-TK-0002-buffer-pending-reports.md) | IMPLEMENTED | 5 focused; 28 delivery/buffer PASS | OPEN / по scope |
| [EP-0010-US-0007-TK-0001-history-state](EP-0010-US-0007-report-history-and-text/EP-0010-US-0007-TK-0001-history-state/EP-0010-US-0007-TK-0001-history-state.md) | IMPLEMENTED | 5 focused; 14 report/client PASS | OPEN / по scope |
| [EP-0010-US-0007-TK-0002-localized-report-text](EP-0010-US-0007-report-history-and-text/EP-0010-US-0007-TK-0002-localized-report-text/EP-0010-US-0007-TK-0002-localized-report-text.md) | IMPLEMENTED | 10 focused; 24 report/client PASS | OPEN / по scope |
| [EP-0010-US-0008-TK-0001-launch-art](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0001-launch-art/EP-0010-US-0008-TK-0001-launch-art.md) | IMPLEMENTED | 3 PNG dimensions/visual QA PASS | OPEN / по scope |
| [EP-0010-US-0008-TK-0002-defense-art](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0002-defense-art/EP-0010-US-0008-TK-0002-defense-art.md) | IMPLEMENTED | 3 PNG dimensions/visual QA PASS | OPEN / по scope |
| [EP-0010-US-0008-TK-0003-damage-art](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0003-damage-art/EP-0010-US-0008-TK-0003-damage-art.md) | IMPLEMENTED | 2 PNG; all8 dimensions/visual QA PASS | OPEN / по scope |
| [EP-0010-US-0008-TK-0004-catalog-and-cache](EP-0010-US-0008-report-illustrations/EP-0010-US-0008-TK-0004-catalog-and-cache/EP-0010-US-0008-TK-0004-catalog-and-cache.md) | IMPLEMENTED | 4 focused; 28 report/client PASS | OPEN / по scope |
| [EP-0010-US-0009-TK-0001-card-rendering](EP-0010-US-0009-report-accordion/EP-0010-US-0009-TK-0001-card-rendering/EP-0010-US-0009-TK-0001-card-rendering.md) | IMPLEMENTED | 5 focused; 33 report/client PASS | OPEN / по scope |
| [EP-0010-US-0009-TK-0002-accordion-input](EP-0010-US-0009-report-accordion/EP-0010-US-0009-TK-0002-accordion-input/EP-0010-US-0009-TK-0002-accordion-input.md) | IMPLEMENTED | 5 focused; 38 report/client PASS | OPEN / по scope |
| [EP-0010-US-0010-TK-0001-right-column-layout](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-TK-0001-right-column-layout/EP-0010-US-0010-TK-0001-right-column-layout.md) | IMPLEMENTED | 10 focused; 48 report/client PASS | OPEN / по scope |
| [EP-0010-US-0010-TK-0002-screen-render-and-input](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-TK-0002-screen-render-and-input/EP-0010-US-0010-TK-0002-screen-render-and-input.md) | IMPLEMENTED | 5 focused; 53 report/client PASS | OPEN / по scope |
| [EP-0010-US-0010-TK-0003-session-reset-and-defeat](EP-0010-US-0010-tactical-screen-integration/EP-0010-US-0010-TK-0003-session-reset-and-defeat/EP-0010-US-0010-TK-0003-session-reset-and-defeat.md) | IMPLEMENTED | 6 focused; 67 report/reliability PASS | OPEN / по scope |
| [EP-0010-US-0011-TK-0001-engine-scenario-proof](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-TK-0001-engine-scenario-proof/EP-0010-US-0011-TK-0001-engine-scenario-proof.md) | IMPLEMENTED | 5 E2E; 57 Engine report PASS | OPEN / по scope |
| [EP-0010-US-0011-TK-0002-client-integration-proof](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-TK-0002-client-integration-proof/EP-0010-US-0011-TK-0002-client-integration-proof.md) | IMPLEMENTED | 10 acceptance; 82 Client focused PASS | OPEN / по scope |
| [EP-0010-US-0011-TK-0003-native-visual-and-performance](EP-0010-US-0011-report-acceptance/EP-0010-US-0011-TK-0003-native-visual-and-performance/EP-0010-US-0011-TK-0003-native-visual-and-performance.md) | EVIDENCE RECORDED / OPEN | One native sample; full matrix/performance NOT RUN | OPEN / по scope |
| [EP-0010-US-0012-TK-0001-project-documentation](EP-0010-US-0012-documentation-and-handoff/EP-0010-US-0012-TK-0001-project-documentation/EP-0010-US-0012-TK-0001-project-documentation.md) | DOCUMENTED | Link/ID/DAG/Graphify audit | Native/human gates retained OPEN |

## Дополнительные corrective tickets

| Ticket | Implementation | Automated | Publication |
|---|---|---|---|
| US11/TK04 map viewport | IMPLEMENTED | Report/free-map cases PASS | cfb91938, verified push |
| US11/TK05 HUD/fixtures | IMPLEMENTED |166 focused PASS |47b60d29, verified push |
| US11/TK06 hover/resize | IMPLEMENTED |226 focused +1973 full filtered PASS |a1a4dd1f, verified push |

## История исполнения — checkpoints2026-10-16 (superseded current status выше)

Запрошено полное автономное выполнение по Board/EpicExecutionPrompt.md. Подготовительное чтение и аудит продолжаются; runtime ещё не изменён, тикеты не завершены. Последняя история US0012 расширена до §6, включая Graphify.

- Baseline HEAD: af594de8465421f45cd362e82c6c1dbfdd8631ed; branch base-fight, upstream origin/base-fight.
- Origin: https://github.com/SuhilovVitaly/DSS. Сетевой доступ из sandbox не разрешил DNS; разрешённый повтор git ls-remote подтвердил тот же удалённый SHA.
- Исходное дерево: изменены Documentation/01-Requirements/EngineRequirements.md и Documentation/README.md; неотслеживаемые целевой Board и TacticalCombatReports.md. Это входные материалы пользователя; tracked правки пока сохраняются вне служебного коммита. Целевой Board и его исходное ТЗ фиксируются как утверждённая база исполнения.
- Commit/push разрешены текущим запросом; planning-only запреты карточек заменены этим поручением. Нативные и человеческие gates остаются OPEN до фактических проверок.
- Обязательное полное чтение Documentation и эпика ещё не завершено. Production implementation не начинается до завершения preflight.

### Исторические checkpoints

Подготовительный f6cd7ea41a705aca7d6c25689c96de5bc35f1306 опубликован в origin/base-fight и SHA подтверждён git ls-remote. Первичный auto-review отказ push снят после read-only GitHub проверки: connected user=owner SuhilovVitaly, repository permissions admin/push=true. Обязательное чтение Documentation и карточек выполнено частями; текущая реализация соответствует contract US0001. Baseline Contracts194/194, после первого DTO207/207. Полный solution baseline в sandbox завершился exit1 без diagnostics; scoped test в sandbox завис на IPC и был отменён, разрешённый host run PASS.

US0001 review: оба AC проверены, Contracts210/210, build/format/diff PASS. TK0001 a49debeb6a6df8b01b150487b6d002420a53cfcd —push и remote SHA PASS. TK0002 e944f2827c7ab294e6e7b05c153a5236fd4650ac опубликован, remote SHA PASS. US0002 выполняется.

US0002: TK0001 5f682d2dfe8e47ba5dda007390e12c31c3e3b2ad published/remote verified. TK0002 E01 implemented and self-reviewed; focused11 + existing combat14 PASS. Full Engine suite cancelled without result; not a green gate.

US0002 TK0002 00d3c9d8afdfe6ba992e126bffc68c207ccd06fc published/remote verified; story self-review complete. US0003 identity helper implemented, 16 report cases PASS.

US0003 TK0001 4b6b1d0d6419ff51eef8c026ad9d300a2645e0ac published/remote verified. TK0002 launches implemented, 26 cases PASS, story self-review complete.

US0003 launch TK0002 5cf06b1a04ec892aab59a7a380fc030eb1aa50ca published/remote verified. US0004 E05 implemented, report suite26 PASS.

US0004 TK0001 caadaa025d86a6388ac1a714092cc81a87aa70d6 published/remote verified. Contact summaries implemented, story self-review complete.

US0004 summary 518efe5495721825e2fefc562ad5004a7f345fe5 published/remote verified. US0005 schema13 PASS; continuation capture/restore next.

US0005 schema 5ad677422f317281f0630e7a0a99bede6ff764cd published/remote verified. Capture/restore implemented and self-reviewed, 83 cases PASS.

US0005 capture/restore 9900015d413b898af49b58ee18b6896b0905d390 published/remote verified. Mailbox merge implemented, 12 cases PASS.

US0006 mailbox 8cbb8ad96bd08508ae76b31b76a06c4cab5130e7 published/remote verified. Inbox implemented and story self-reviewed, 28 cases PASS.

US0006 inbox 344aae55db58a91dd60253e1217d51756c89bdb5 published/remote verified. History store implemented, 14 cases PASS.

US0007 history 0eb073a59c85646889538f586cccef3eb7481c84 published/remote verified. Formatter implemented, story self-review complete, 24 cases PASS.

US0007 formatter03eccb6d7a51092fe2068f5155621f67d297a431 published/remote verified. First three art assets built-in generated and visually inspected; human art acceptance OPEN.

US0008 launch-art b320f74850b6ba70388ee4d24fa35bca1265d202 published/remote verified. Defense/hit art assistant visual QA PASS, human acceptance OPEN.

US0008 defense-art844942856d29d1f5c745ca5c25b369d7b696f1d3 published/remote verified. All8 art assets prepared; human acceptance OPEN.

US0008 damage-art ce47dac2100b2d5d7cccfac4e7efdf2d7c449214 published/remote verified. Catalogue/cache implemented and story self-reviewed; human art gate remains OPEN.

US0008 catalogue/cache acfdce903df3e7829a40b234fe1f7e35048cced9 published/remote verified. Card rendering implemented;33 report cases PASS.

US0009 card render7a62c7b7554170a7f9c50c590a8623101c64022d published/remote verified. Input implemented, story self-review complete,38 cases PASS.

US0009 input31b83e0b22948ddd645ae9689a7364b14086510a published/remote verified. Right-column geometry implemented;48 report cases PASS.

US0010 right-column d2a177051d9c430ff00a628fd641a521c8626b18 published/remote verified. Screen render/input integrated,53 cases PASS.

US0010 screen integration6ff6199e32f7cc9bcab2a040e074e9d09cfaca93 published/remote verified. Lifecycle implemented and story self-reviewed;67 cases PASS.

US0010 lifecycle81fa61f0a593b909719563c16bd6cef839b297d8 published/remote verified. Engine acceptance production CombatMukha + rare outcomes PASS.

US0011 Engine proof276bc6331e89b65412713ccf02eaf2840cc9f186 published/remote verified. Client integration/matrix82 PASS; native remains separate.

Corrective US0011-TK0004: report rectangle added to free-map obstacles, new6 + existingMapLayer3 PASS. Full Client before repair1948/1971,23failures; remaining navigation fixtures and station timeout investigation open. Client proof9f022367fec29f41f15e8cb20c9a1f232bf26b02 published/remote verified.

CorrectiveTK0004 cfb91938c2c1409213d535911cc11e7ed73e7bf6 published/remote verified. TK0005 toolbar reservation and affected fixtures repaired;166 cases PASS, including all12 failures reproduced by scoped pre-fix run. Raw evidence in evidence/regression.

TK0005 47b60d2902c8577642d51af3894682104b9673f9 published/remote verified. User explicitly forbids autonomous game/native launches from2026-10-16 onward. Native/performance gate stays OPEN. Existing64-frame1280/UI100/EN sample retained; no active native process remained. Station timeout reproduced on source af594de, baseline evidence retained.

TK0003 5d2d9357a4c08b303ce7285ee1bfa7178f5346bd published/remote verified. TK0006 fixed HUD hover, empty-string tiny-width wrapping and free-map fixtures;226 focused cases PASS, scoped format/diff PASS. Native remains OPEN by explicit user policy.


## Superseded by EP-0012 — 2026-10-21

Guided/chance/mode/auto/burst combat and Save17/16 rules in this dated report are superseded by [point-aim combat](../../Documentation/04-Engineering/PointAimCombat.md). Module armor/structure, report delivery and observed-motion privacy remain. Historical SHA/test/FPS/native results and original permissions are preserved as evidence of their time, not fresh acceptance. EP12 actual native matrix does not close earlier human/palette/FPS gates; production dodge balance remains outside its required corridors. [Current implementation and publication](../EP-0012-combat-rebuild/ImplementationStatus.md).
