# Исполнение EP-0018

На 2026-11-08 технически реализованы и опубликованы29/29тикетов всех9историй.
Последний тикет: EP-0018-US-0009-TK-0003, SHA686682380ebf4fdc357bd8bd239ca98f25b000f8.
Удалённый SHA подтверждён; текущий служебный receipt фиксирует публикацию.
Продуктовая приёмка остаётся OPEN по перечисленным ниже gates.

## Результат и границы

Новые миры используют v2: все человеческие станции, включая исходные Default,
размещаются по общему frozen масштабу; min1d, nearest1–3d, rigid clusters и radial lanes.
Operational Vmax захватывается при генерации (поставленный engine.basic6км/с).
Ресурсы, роли/ID/payload, источники RNG, старты50–75d и Docked offset сохранены.
Save20 принимает согласованные v1/v2; Load не генерирует мир и не меняет масштаб
по текущему двигателю. Local/Region/Cluster/System, scoped orbits/labels и явный
Station local работают по одному представленному миру. Линейка frozen, ETA текущий.

## Git и публикация

База c43e8af5cfbc76844e6a9109921fbae41153d370. Remote
https://github.com/SuhilovVitaly/DSS; ветка/upstream
codex/ep-0016-tactical-battle-skins / origin/codex/ep-0016-tactical-battle-skins.
Пользователь прямо разрешил публикацию всего EP-0018; повторных разрешений не нужно.
Начальные auto-review push rejection устранены этим подтверждением. Ни force push,
ни переписывания истории не было. Исходно только untracked EP18; чужая работа сохранена.
Managed worktrees не создавались. Каждый опубликованный тикет немедленно push,
HEAD сравнивался с git ls-remote. Собственный SHA последнего документационного
коммита фиксируется последующим publication receipt, не выдумывается заранее.

Evidence prefix: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/.
В колонке run опущен этот общий prefix; YYYYMMDD — часть реального имени каталога.

| Тикет | SHA | Push/проверка | Основной run |
|---|---|---|---|
| [EP-0018-US-0001-TK-0001-geography-descriptor](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0001-geography-descriptor/EP-0018-US-0001-TK-0001-geography-descriptor.md) | 7d42085a273bc86cff19b9aa083ff8060fe159f8 | PASS: remote SHA проверен | 20261107-descriptor |
| [EP-0018-US-0001-TK-0002-v2-generation-input](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0002-v2-generation-input/EP-0018-US-0001-TK-0002-v2-generation-input.md) | dded58417c116e8c13ba88c630fde87e19fe48ae | PASS: remote SHA проверен | 20261107-generation-input |
| [EP-0018-US-0001-TK-0003-all-station-placement](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0003-all-station-placement/EP-0018-US-0001-TK-0003-all-station-placement.md) | 042ae85c0c988cb61ec566028e0de6d2845c629c | PASS: remote SHA проверен | 20261107-local-placement |
| [EP-0018-US-0001-TK-0004-atomic-world-bootstrap](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0004-atomic-world-bootstrap/EP-0018-US-0001-TK-0004-atomic-world-bootstrap.md) | e930ec2d2380eeb0756d26bc3953b2fd83dd3b28 | PASS: remote SHA проверен | 20261107-bootstrap |
| [EP-0018-US-0002-TK-0001-radial-lane-placement](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-TK-0001-radial-lane-placement/EP-0018-US-0002-TK-0001-radial-lane-placement.md) | 9efe2cb75f190be1531e34feb312d713b08aec25 | PASS: remote SHA проверен | 20261107-radial-lanes |
| [EP-0018-US-0002-TK-0002-cluster-link-graph](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-TK-0002-cluster-link-graph/EP-0018-US-0002-TK-0002-cluster-link-graph.md) | 018c1115891043458083e27c92a6fa3e755fc2be | PASS: remote SHA проверен | 20261107-cluster-graph |
| [EP-0018-US-0002-TK-0003-full-system-integration](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-TK-0003-full-system-integration/EP-0018-US-0002-TK-0003-full-system-integration.md) | 1f92b482ff63cc0420f468a6e2fa802f295b4be3 | PASS: remote SHA проверен | 20261107-full-system |
| [EP-0018-US-0003-TK-0001-station-resource-relocation](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-TK-0001-station-resource-relocation/EP-0018-US-0003-TK-0001-station-resource-relocation.md) | 95ec544646290332abfd69ae662548c8922cbe8d | PASS: remote SHA проверен | 20261107-resources |
| [EP-0018-US-0003-TK-0002-scenario-start-bindings](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-TK-0002-scenario-start-bindings/EP-0018-US-0003-TK-0002-scenario-start-bindings.md) | 2544f8ea14666cc49a1e3f221e8d948df306fcf7 | PASS: remote SHA проверен | 20261107-scenario-starts |
| [EP-0018-US-0003-TK-0003-voyage-geography](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-TK-0003-voyage-geography/EP-0018-US-0003-TK-0003-voyage-geography.md) | ae34d85cf98b708c1049baf5efc1302815aa6042 | PASS: remote SHA проверен | 20261107-voyages |
| [EP-0018-US-0004-TK-0001-versioned-save-validation](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0001-versioned-save-validation/EP-0018-US-0004-TK-0001-versioned-save-validation.md) | 8d5b3febfc463889f9e8b693205faa3ff79bd671 | PASS: remote SHA проверен | 20261107-save-versions |
| [EP-0018-US-0004-TK-0002-legacy-trade-map-validation](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0002-legacy-trade-map-validation/EP-0018-US-0004-TK-0002-legacy-trade-map-validation.md) | 9193557a548ebdf7ccabe942eb688f2d288d59c2 | PASS: remote SHA проверен | 20261107-trade-validation |
| [EP-0018-US-0004-TK-0003-local-world-roundtrip](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0003-local-world-roundtrip/EP-0018-US-0004-TK-0003-local-world-roundtrip.md) | 78156d4f59e2e033ce681da7d1f2e75b25609a53 | PASS: remote SHA проверен | 20261107-local-roundtrip |
| [EP-0018-US-0004-TK-0004-activate-v2-content](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0004-activate-v2-content/EP-0018-US-0004-TK-0004-activate-v2-content.md) | 2380c4b45ca1b632773723612f2453305aefa0a0 | PASS: remote SHA проверен | 20261107-content-activation |
| [EP-0018-US-0005-TK-0001-local-view-policy](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-TK-0001-local-view-policy/EP-0018-US-0005-TK-0001-local-view-policy.md) | 4e1521ed1b37e2fdf65849955da16fde019c5812 | PASS: remote SHA проверен | 20261107-local-policy |
| [EP-0018-US-0005-TK-0002-initial-camera-and-zoom](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-TK-0002-initial-camera-and-zoom/EP-0018-US-0005-TK-0002-initial-camera-and-zoom.md) | 81183d40d45c066e4a4f686520a506c0423bc012 | PASS: remote SHA проверен | 20261107-camera |
| [EP-0018-US-0005-TK-0003-local-scale-content](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-TK-0003-local-scale-content/EP-0018-US-0005-TK-0003-local-scale-content.md) | c71528defe18cd7117ab636ac567fba85531ea70 | PASS: remote SHA проверен | 20261107-local-scale-content |
| [EP-0018-US-0006-TK-0001-semantic-map-detail](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0001-semantic-map-detail/EP-0018-US-0006-TK-0001-semantic-map-detail.md) | f2f1f02ebf8552aa8928925bd90611832da95c21 | PASS: remote SHA проверен | 20261107-map-detail |
| [EP-0018-US-0006-TK-0002-orbital-layer-policy](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0002-orbital-layer-policy/EP-0018-US-0006-TK-0002-orbital-layer-policy.md) | 9d6e9a66f4b6d287c512b81c490fa289db4d4298 | PASS: remote SHA проверен | 20261107-orbit-detail |
| [EP-0018-US-0006-TK-0003-map-label-priorities](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0003-map-label-priorities/EP-0018-US-0006-TK-0003-map-label-priorities.md) | f2a4afae4026d238f16a86b28bda0e3b251ecf25 | PASS: remote SHA проверен | 20261107-map-labels |
| [EP-0018-US-0006-TK-0004-smooth-detail-transitions](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0004-smooth-detail-transitions/EP-0018-US-0006-TK-0004-smooth-detail-transitions.md) | a47d95d6151eab3bff1b925bd983cd84962197d4 | PASS: remote SHA проверен | 20261107-detail-lifecycle |
| [EP-0018-US-0007-TK-0001-cluster-focus-navigation](EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-TK-0001-cluster-focus-navigation/EP-0018-US-0007-TK-0001-cluster-focus-navigation.md) | ad6458245c586f294fbaacc45e470ee72a74cee5 | PASS: remote SHA проверен | 20261107-map-navigation |
| [EP-0018-US-0007-TK-0002-current-travel-estimates](EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-TK-0002-current-travel-estimates/EP-0018-US-0007-TK-0002-current-travel-estimates.md) | 5324df5570c0a117bf53f1a4a8ab3ec685ba0df5 | PASS: remote SHA проверен | 20261107-current-estimates |
| [EP-0018-US-0008-TK-0001-seed-geography-corpus](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-TK-0001-seed-geography-corpus/EP-0018-US-0008-TK-0001-seed-geography-corpus.md) | 03f78e81ad55dc78cf20ee34a4054e9b93e89bf8 | PASS: remote SHA проверен | 20261108-corpus-validation |
| [EP-0018-US-0008-TK-0002-rendered-map-evidence](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-TK-0002-rendered-map-evidence/EP-0018-US-0008-TK-0002-rendered-map-evidence.md) | 51d6f99ea9ef73669c9c6c14f981081a0e413f68 | PASS: remote SHA проверен | 20261108-raster-verified |
| [EP-0018-US-0008-TK-0003-native-map-evidence](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-TK-0003-native-map-evidence/EP-0018-US-0008-TK-0003-native-map-evidence.md) | 6dd13cb3d619de8e8a7bc588bd238f3bac58537b | PASS: remote SHA проверен | 20261108-native-tool |
| [EP-0018-US-0009-TK-0001-current-map-contracts](EP-0018-US-0009-project-documentation/EP-0018-US-0009-TK-0001-current-map-contracts/EP-0018-US-0009-TK-0001-current-map-contracts.md) | f38fefad5f37ac8c67c9b191d441c7b9fc23ba61 | PASS: remote SHA проверен | 20261108-documentation/contracts-audit.json |
| [EP-0018-US-0009-TK-0002-board-handoff](EP-0018-US-0009-project-documentation/EP-0018-US-0009-TK-0002-board-handoff/EP-0018-US-0009-TK-0002-board-handoff.md) | 9f3081f3bd64230fe90a582dd98c9b83472070da | PASS: remote SHA проверен | 20261108-documentation/handoff-audit.json |
| [EP-0018-US-0009-TK-0003-documentation-navigation](EP-0018-US-0009-project-documentation/EP-0018-US-0009-TK-0003-documentation-navigation/EP-0018-US-0009-TK-0003-documentation-navigation.md) | 686682380ebf4fdc357bd8bd239ca98f25b000f8 | PASS: remote SHA проверен | 20261108-graph |

Подготовительные/service commits (также опубликованы):

- 2bb0ec17c67a05093d99e55af063086904bd8aee: EP-0018: preserve approved Board and record autonomous execution gates.
- 0a8695f2d021a3cf90ef186b60b07bca12855fdd: EP-0018: extend final documentation scope to execution prompt requirements.
- b375fbb88a30c9f5ac2af9ad4b5c9fa60b9a190d: EP-0018: order save ingress before active voyage continuation checks.

## Проверки и точное покрытие

Все новые commands/log/TRX/PNG/JSON находятся под указанным workspace prefix,
команды build/test выполнялись из D:/DeepSpaceSaga/DSS. Test/native child tree cap300000ms,
native internal cap40s; timeout никогда не PASS. Точные ранние команды/исходные FAIL
и исправления сохранены в owning карточках и Git, а не противоречащих текущих сводках.

| Область | Наблюдаемый результат | Retained evidence |
|---|---|---|
| DTO/Contracts |189/189; additive optional Geography roundtrip |20261107-descriptor/|
| Engine main |2158/2158; исключён отдельный LongRunning tactical combat |20261108-corpus-validation/engine-main.trx|
| V2 corpus |90 theory cases:6scenarios×5ranges×3stages;100unique seeds/scenario;64boundary combinations |20261108-v2-corpus-verified/Summary.json|
| Historical v1 corpus |540 theory cases; исходные полные Cartesian matrices,100seeds/config, sharded5×20 |20261108-legacy-corpus/Summary.json|
| Corpus audit |36finalTRX,2788unique passed IDs,0duplicates; independent trig/geometry, epoch/1/30/365d, repeat+Save/Load |20261108-corpus-validation/CorpusReport.md, Summary.json|
| File continuity |33real-file focused cases, six scenarios, changed current Vmax + invalid generation config ignored onLoad; active v1/v2 voyages, Save18/19 migrations |20261107-local-roundtrip/|
| Client full |2117/2117 after UI2 support,0skips |20261108-raster-tool/client-full.trx|
| Raster |6scenarios×3seeds×4windows×3UI×4views=864PNG,18world snapshots;5warmup/30samples each; all6reports validated |20261108-raster-verified/RasterReport.md, MatrixSummary.json|
| Native |12Default cases,4windows×3UI; seed1/42/100 mapped by UI (not36seed Cartesian cases);108PNG/108phase snapshots; actual Dock/F5/F9/follow |20261108-native-tool/NativeReport.md, NativeMatrixSummary.json|
| Native timing |120warmup/600swap samples percase; p50≈10ms; all12p95≤12.5ms; old p99 guard FAIL3 |same native reports and per-case native-frames.json|
| Report guards |9/9tool tests;4actual tampered copies rejected; owning Release builds/scoped format/diff PASS |20261108-native-tool/verified.trx, negative-validation.log|
| Motion full |1266/1267:FAIL1, not PASS |20261108-final-regression/motion-full.trx|
| Motion baseline audit |same3actual guard findings in unchanged c43e8af5 source: TacticalBattleResults.cs15/53, TacticalBattleSalvo.cs95 |20261108-final-regression/MotionBaselineAudit.json|
| Current docs |12owners updated,84links/13changedMarkdown,29tickets/38DAG nodes,0errors |20261108-documentation/contracts-audit.json|
| Named tests |74completed-card named references resolve to actual methods; not a claim that every reference is a separate runtime test |20261108-documentation/NamedTestsAudit.json|

Independent corpus reports sampled human pair minimum1.1793579412496d;
nearest range1.1793579412496–1.45528054304949d; analytic inter-lane lower bound
1.07405626966427d. AI2/4 boundaries coupled to cluster3/5: no independent exhaustive
AI Cartesian claim. Main+census counts are not double-counted with pilot runs.

## Критерии эпика

| AC | Фактический статус и evidence |
|---|---|
| E18-AC-01 |Automated PASS: all-human min/own-nearest,6scenarios/100seeds + analytic proof |
| E18-AC-02 |Automated PASS: rigid circular parameters/radial lane proof + multi-epoch oracle |
| E18-AC-03 |Automated/native scripted PASS: payload/IDs/resources/start anchors/Dock; human separately OPEN |
| E18-AC-04 |Automated/native PASS: strict v1/v2 ingress, real materialized Load; malformed input atomic rejection |
| E18-AC-05 |Automated PASS: graph supply/demand/reachability, actual local/intercluster voyages/settlement, bounded AI; balance not assessed |
| E18-AC-06 |Automated/raster/native scripted PASS: Local extent, resize,4windows/UI2; physical acceptance OPEN |
| E18-AC-07 |Implementation/evidence present; product readability OPEN,24raster cluster-fit cases retain non-Cluster detail at small free viewport |
| E18-AC-08 |Repro artifacts/seed/hash/camera and native timings measured; baseline/human/physical OPEN; old p99 FAIL3 |
| E18-AC-09 |Current contracts/Board/navigation synchronized; graph verified; final publication receipt after commit |

## Решения исполнения

- Actual shipped speed6км/с сохранена; corpus2/4 использует registry overlays. Fallback4 толькодлямирабезdescriptor.
- V2 не резервирует legacy template corridor strips; сохраняет owner offsets/RNG, annulus/clearance/spacing. V1 прежний.
- Межкластерный граф использует общий outbound/connectivity contract; не требует двух локальных покупателей каждой станции.
- Save ingress выполнен до проверки active voyage continuation: исправленный DAG38nodes/29tickets.
- UI2 принят renderer tooling/constructor; обычный SkiaWindow и меню остаются0.8/1/1.2/1.5, предел явно описан.
- Unexposed solar/cluster retry counters не придуманы: config limits + actual AI diagnostics, unavailable отдельно.
- Native driver использует production fit/focus APIs и managed screen events; snapshots — authoritative observations,
  camera — completed render. Это не hardware input или точный OCR/pixel-time oracle.

## Открытые gates

Human/product readability, physical keyboard/mouse, independent review — NOT RUN.
Matched pre-change native baseline — OPEN.24cluster-fit raster detail gaps остаются
перечислены с PNG в MatrixSummary.json; Fit по свободной области и short-side LOD
могут выбрать System вместо Cluster. P95 target выполнен на указанной100Hz машине,
но3old-p99 verdict FAIL и физический scanout/GPU execution не измерены.
Motion source guard — доказанный pre-existing FAIL, не скрыт фильтром/ослаблением.
Самопроверка не независимое review; продуктовая приёмка не APPROVED.

## Уборка и завершение

Native owned slots удалены, interrupted slots проверены по marker после остановки
owned processes. Отчёты/PNG/TRX referenced above retained; pilot/failed runs исключены
из итоговых чисел и оставлены как failure evidence в owning reports. Нет DSS/artifacts,
ручных worktrees, root temp snapshots или source junctions. TK9.3 проверил
финальные links/hash/graph/cleanup и записал publication receipt. Текущий ledger
заменяет ранние противоречащие сводки; их история сохранена в Git и owning cards.

## TK9.3: final navigation / graph gate

Current guides/examples and EP2/3/4 integration links updated without rewriting
historical approval/results. Graph rebuilt after final included source/text;
744code+664Markdown=1408source hashes,15fresh agent semantic inputs,20422nodes,
55930edges,690communities. Retained20261108-graph/graph.html, graph.json,
GRAPH_REPORT.md, source-manifest.json, semantic-coverage.json, verification.json,
quality-audit.json and verify_graph.py. Source hashes and18stable export hashes PASS;
query smoke/required v2 source presence PASS; final derived dangling/selfloops0.

Graph limitations are explicit: partial parse of unchanged ModulePanelLocalizationTests.csL25,
1101unresolved AST edges pruned,2776reference-only unlocated symbols,
746canonical loops removed;1930same-endpoint undirected edges combined. Full original
relationships in extraction.json. This is not full semantic/runtime/acceptance proof.
Native/human/baseline/Motion/readability gates above unchanged.

Final navigation audit:62changedMarkdown/427links,29tickets/38DAG nodes,0errors
(20261108-documentation/navigation-audit.json). Cleanup audit:owned native slots0,
report-test temp dirs0,noDSS/artifacts,noRootgraphify-out,no manual worktrees;
Graph AST/semantic caches removed. Required reports/TRX/PNG/JSON and reproducible
validation scripts retained under workspace artifacts only. Final receipt follows
actual last-ticket commit/push; included source hashes remain valid across that
publication because status/impact/index/final-ticket receipts are explicitly excluded.

## Publication receipt — 2026-11-08

All29ticket SHAs in the table are real committed ancestors of the target branch;
each ticket was pushed before its successor. Last ticket686682380ebf4fdc357bd8bd239ca98f25b000f8
was compared against git ls-remote origin refs/heads/codex/ep-0016-tactical-battle-skins
and matched. DocumentationImpact/current index now reflects completed technical
execution, not human approval. This receipt is a separate Board-only service commit;
its own final HEAD and remote comparison are written after push to workspace
20261108-documentation/PublicationReceipt.json, avoiding self-referential invented SHAs.
Graph included source hashes remain current: this status/index/impact/final-ticket
receipt scope is explicitly excluded by the maintained rebuild contract.
