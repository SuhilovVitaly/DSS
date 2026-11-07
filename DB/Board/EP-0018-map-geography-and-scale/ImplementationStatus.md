# Исполнение EP-0018

Статус на2026-11-08: IN PROGRESS. Опубликовано27/29тикетов; stories1–7 функционально реализованы; открыты US8 evidence и US9 документация. Следующий: US0009TK0002; затем navigation/graph TK3.

## Git и полномочия

Базаc43e8af5cfbc76844e6a9109921fbae41153d370; рабочая ветка и подтверждённый upstream
codex/ep-0016-tactical-battle-skins / origin/codex/ep-0016-tactical-battle-skins.
Remote https://github.com/SuhilovVitaly/DSS. Изначально только untracked EP-0018.
Пользователь дополнительно прямо разрешил публикацию всего EP-0018 в эту ветку.
Два первоначальных auto-review push rejection устранены этим прямым подтверждением;
обходов не было. Чужие файлы не менялись, managed worktrees не создавались.

| Тикет/подготовка | SHA | Push / проверка |
|---|---|---|
| Board/gates |2bb0ec17c67a05093d99e55af063086904bd8aee|PASS, ancestor опубликованного head|
| US-0001-TK-0001|7d42085a273bc86cff19b9aa083ff8060fe159f8|PASS, remote head SHA проверен|
| Полный scope финальной документации|0a8695f2d021a3cf90ef186b60b07bca12855fdd|PASS, remote head SHA проверен|
| US-0001-TK-0002|dded58417c116e8c13ba88c630fde87e19fe48ae|PASS, remote head SHA проверен|
| US-0001-TK-0003|042ae85c0c988cb61ec566028e0de6d2845c629c|PASS, remote head SHA проверен|

Полные IDs/порядок29тикетов — Tickets.md. Следующий — US-0001-TK-0004-bootstrap;
затем US0002→0003→0004→0005→0006→0007→0008→0009. Content activation толькоUS0004TK0004.
US0009 последняя: полный inventory документации во всём репозитории, текущие
контракты/README/Board/navigation и offline graph после итогового diff.

## Реализованный результат

TK1: additive Geography DTO, camelCase, missing/nulllegacy и v2 snapshot roundtrip.
TK2: versioned config/gen1/2 solar ingress, minimumStationDays и consistency,
named geography/v2 stage; v1golden неизменён.
TK3: detached единое circular размещение всех stationIDs, включая Default legacy,
pair/nearest/diameter/roles/payload проверки и finite deterministic failure.
Player/resources/связи нескольких кластеров/savevalidation/client ещё впереди.
Default фактически использует6000м/с:17280wu/day; descriptor захватывает реальный
operational Vmax, примерные4000м/с не подменяют catalog. Shipped config остаётсяv1.

## Проверки и evidence

Все outputs под D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/.
Все команды из D:/DeepSpaceSaga/DSS, test watchdog300000ms с Kill(true).

| Тикет/run | Наблюдаемый результат | Сохранённое evidence |
|---|---|---|
| TK1 /20261107-descriptor|focused5/5; Contracts189/189; Release build0warnings/errors; scoped format2files; diff PASS|focused-final.trx/log, contracts-full.trx/log, build.log, format-contracts-unsandboxed.log, format-tests.log|
| TK2 /20261107-generation-input|schema/bootstrap12/12; Engine безLongRunning/Corpus2070/2070; Release build0warnings/errors; scoped format3files; diff PASS|focused-final.trx/log, engine-non-corpus.trx/log, build-unsandboxed.log, format-engine.log, format-tests-final.log|
| TK3 /20261107-local-placement|placement+v1cluster/schema/seed24/24; Release build0warnings/errors; scoped format3files; diff PASS|focused-final.trx/log, build.log, format-engine.log, format-tests.log|

Точные команды focused/build/format и ACcoverage — соответствующие карточки.
RegressionTK2: dotnet test tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
-c Release --no-build --no-restore --filter "Category!=LongRunning&FullyQualifiedName!~Corpus"
--results-directory D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-generation-input
--logger "trx;LogFileName=engine-non-corpus.trx".
Это не полный corpus: начальный broad run намеренно остановлен для шардирования
untagged100seed tests, не засчитанPASS. Полный corpus остаётсяUS0008.

Предварительные неуспехи сохранены как history: descriptor/focused.trx/log/err —
sandbox testhost connection abort90s; format-contracts.log — sandboxMSBuild error;
focused-unsandboxed.trx/log/err —2FAIL нового ожидания массива, исправленного на
существующий IsDefault. Final5/5 и189/189 после исправленияPASS. generation-input/build.log
— sandbox buildFAIL без compilererrors; build-unsandboxed.logPASS. No hidden PASS.

Self-review каждого diff по owningC/AC: findings нет; независимого review не было.
Board local-link path audit42Markdown:0missing, git diff --checkPASS.
Native, physical input, human/product, FPS NOT RUN. Эпик не завершён.
Task-owned stalled testhost32764 уже завершён; широкий процесс48092 остановлен
с деревом. Временные scripts/cache/slots/worktrees не создавались. Retained evidence
перечислено выше; пустые stderr/лишние предварительные outputs убираются при cleanup.
## US0001 bootstrap и story review

TK4 завершён локально; commit/push следует сейчас, собственный SHA проверяется Git.
Реальный v2 Engine27/27focused, Engine non-corpus2076/2076PASS; build/format/diffPASS.
Evidence run20261107-bootstrap: focused-final.trx/log содержит Default42snapshot,
66pair distances/12nearest; engine-non-corpus.trx/log; build.log; format-engine.log;
format-tests-final.log. Широкие2076прошли до добавления только xUnit evidence output;
после него повторены27focused и scoped test format, productionкод не менялся.

Story1 код реализован, но общая приёмка Save/Load ещё US4: история остаётся
implemented_pending_compatibility_gate. Следующийтикет — US0002TK0001radial lanes,
после публикации TK4. Остаток25тикетов, corpus/native/FPS/human NOT RUN.
## Radial lanes — US0002TK0001

US0001TK0004 опубликован: e930ec2d2380eeb0756d26bc3953b2fd83dd3b28, exactremoteSHA PASS.
US0002TK0001 локально завершён:22/22focused, build0warnings/errors, scopedformat/diff
PASS. Evidence20261107-radial-lanes/focused-verified.trx/log и build/format logs.
Геометрия проверена до runtime публикации; первоначальные2FAIL выявили зависимость
от межкластерного graphTK0002 (не скрыты/не засчитаныPASS). Live narrowbelt atomic
rejection остаётся проверенным. Следующий тикетTK0002 обязан включить Engine
integration3–5кластеров и strong directed connectivity/cargo, до storyclosure.
Минимум при любых фазах доказан аналитически через радиусы, samples0/1/30/365 —
дополнительная проверка. После публикации этого тикета останется24тикета.
## Trading graph — US0002TK0002

RadialTK0001 опубликован9efe2cb75f190be1531e34feb312d713b08aec25, exactremoteSHA PASS.
GraphTK0002 локально завершён:25/25focused, build0warnings/errors, scopedformat/diff
PASS. Live3/5cluster Engine integration и strong directed reachability подтвердили
закрытие предыдущей runtime graph зависимости. Every multi-cluster station>=2outbound.
Evidence20261107-cluster-graph/focused-final.trx/log, build/format logs.
Следующийтикет — US0002TK0003AI/environment/POI integration. После этой публикации
будет6/29тикетов и23остаётся; story1 compatibility и story2 итоговые gates ещё открыты.
## Полная система — US0002TK0003

GraphTK0002 опубликован018c1115891043458083e27c92a6fa3e755fc2be, exactremoteSHA PASS.
FullsystemTK0003 завершён локально:36/36focused и2085/2085Engine non-corpusPASS,
Releasebuild0warnings/errors и scopedformat/diffPASS. Evidence20261107-full-system:
before.trx/log/err(2FAIL), focused-final.trx/log, engine-non-corpus.trx/log,
build.log, format-engine.log, format-tests.log. Исправлен строго, безbypass,
v1-only finalvalidator; AI/environment/POI алгоритмы не потребовали изменений.
Story2 код реализован, совместимостьфайлов/save-regeneration ещёUS4, поэтому
implemented_pending_compatibility_gate. После публикации7/29тикетов,22остаётся.
Следующийтикет — US0003TK0001 resources relocation/knowledge/exactly-once.
## Ресурсы — US0003TK0001

FullsystemTK0003 опубликован1f92b482ff63cc0420f468a6e2fa802f295b4be3, remoteSHA PASS.
ResourcesTK0001 локально завершён:15/15focused и2088/2088Engine non-corpusPASS,
build0warnings/errors, scopedformat5files/diffPASS. Evidence20261107-resources:
focused-final.trx/log, engine-non-corpus.trx/log, build.log, format-engine.log,
format-tests.log. Failurehistory сохранена в before/declared-before/
declared-valid-before/relocation-before/focused TRX/logs; детали в owningticket.
Исправлены owneroffset2044км и скрытаяcompositionутечка, сохранены capturedrules/RNG.
V2 static legacy corridor reservation отменена как несовместимая с сохранением
sourceoffset; annulus/spacing/clearance/manifest validation остаётся. C03уточнён.
После этой публикации8/29тикетов,21остаётся. Следующий — US0003TK0002scenario starts.
## Сценарии — US0003TK0002

ResourcesTK0001 опубликован95ec544646290332abfd69ae662548c8922cbe8d, remoteSHA PASS.
ScenarioTK0002 завершёнлокально:42/42focused, Releasebuild0warnings/errors,
scopedformat3files/diffPASS. Включены24cases(6scenario×2/4км/с×50/75d), explicitpayload,
реальныйApproach/Dock/departure/duplicatefuel. PlayerShipOnly сохраняет первоначальную
solarpoint, найденныйсдвиг исправленпослеbeforeFAIL; seed NPC nullname expectation
исправлено какtest-onlycontractclarification. Evidence20261107-scenario-starts.
После публикации9/29тикетов,20остаётся. Перед voyage/savecontinuity нужны строгие
file-save ingress и TradingMapv2 validation изUS4; порядокзависимостей следует
уточнить до следующейреализации, не выдавать failingsaveload заPASS.
ScenarioTK0002 опубликован2544f8ea14666cc49a1e3f221e8d948df306fcf7; remoteSHA PASS. Dependencyorder исправлен вTickets.md и карточках;20тикетов остаётся. Текущий следующий — US0004TK0001 strictsaveingress.

US0004TK0001: 37/37 focused, build/format/diff PASS; save-versions evidence as in ticket. v1/v2 materialized no-regeneration and atomic negative checks. Self-review complete; independent/human not performed.

US0004TK0001 published8d5b3febfc463889f9e8b693205faa3ff79bd671 verified. US0004TK0002: focused65, non-corpus2133 PASS, build/format/diff PASS; evidence trade-validation. Next voyage gate.

US0004TK0002 published9193557a548ebdf7ccabe942eb688f2d288d59c2 verified. US0003TK0003 actual voyages focused36/36 PASS; evidence voyages, build/format/diff PASS. No remote market execution introduced.

US0003TK0003 publishedae34d85cf98b708c1049baf5efc1302815aa6042 verified. US0004TK0003 focused-verified33/33 PASS; build/format/diff PASS; local-roundtrip evidence. Temporary slot cleanup verified.

US0004TK0003 published78156d4f59e2e033ce681da7d1f2e75b25609a53 verified. US0004TK0004 production v2 active; focused20, Client2072, Engine non-corpus2148 PASS; build/format/diff PASS. Final acceptance remains US8. UI scale work next.

US0004TK0004 published2380c4b45ca1b632773723612f2453305aefa0a0 verified. US0005TK0001 local scale pure policy focused17 PASS; build/format/diff PASS; evidence local-policy.

US0005TK0001 published4e1521ed1b37e2fdf65849955da16fde019c5812 verified. US0005TK0002 camera focused52/fullClient2093 PASS, build/format/diff PASS; evidence camera. Native remains US8.

US0005TK0002 published81183d40d45c066e4a4f686520a506c0423bc012 verified. US0005TK0003 focused20 PASS, build/format/diff PASS, local-scale-content evidence; temporary settings removed. US5 implemented pending native/human acceptance.

US0005TK0003 publishedc71528defe18cd7117ab636ac567fba85531ea70 verified. US0006TK0001 semantic map detail focused30 PASS; build/format/diff PASS, map-detail evidence.

US0006TK0001 publishedf2f1f02ebf8552aa8928925bd90611832da95c21 verified. US0006TK0002 orbital focused7 PASS; build/format/diff PASS, orbit-detail evidence.

US0006TK0002 published9d6e9a66f4b6d287c512b81c490fa289db4d4298 verified. US0006TK0003 fullClient2106 PASS, focused84/compatibility27 PASS; build/format/diff PASS, map-labels evidence. Original failures and build-lock retained as evidence.

US0006TK0003 publishedf2a4afae4026d238f16a86b28bda0e3b251ecf25 verified. US0006TK0004 focused77/fullClient2109 PASS; build/format/diff PASS, detail-lifecycle evidence. US6 implemented pending rendered/native/human acceptance.

US0006TK0004 publisheda47d95d6151eab3bff1b925bd983cd84962197d4 verified. US0007TK0001 focused46 PASS, build/format/diff PASS; map-navigation evidence, frame-context isolation and explicit station-local action.

US0007TK0001 publishedad6458245c586f294fbaacc45e470ee72a74cee5 verified. US0007TK0002 focused20/fullClient2115 PASS; build/format/diff PASS, current-estimates evidence. Functional stories1–7 implemented; corpus/rendered/native/documentation gates remain.

US0007TK0002 published5324df5570c0a117bf53f1a4a8ab3ec685ba0df5 verified. US0008TK0001 aggregate2788 unique Engine cases PASS:2158main+540legacy+90v2; all100seed/scenario and64human boundary combinations checked, no timeout. Source hashes, per-run TRX and metrics indexed in20261108-corpus-validation/Summary.json. Build/format/diff PASS. Stories1–7 statuses reconciled to implemented_pending_acceptance, file-save gates technically closed.


US8TK2: raster tooling/evidence complete, six scenarios/18worlds/864PNG; report tests6/6, UI17/17, Client2117/2117. Evidence20261108-raster-verified/RasterReport.md and MatrixSummary.json.24 cluster-fit high-UI/small-window semantic detail gaps remain OPEN; native/human/baseline separate.


US8TK3:12native scripted routes complete,108PNG, actual Save/Load and cleanup; native p95 all12≤12.5ms, p99 legacy verdict failed3. NativeReport.md/NativeMatrixSummary.json in20261108-native-tool. Tool tests9/9. Motion1266/1267:unchanged baseline guard3findings audited at c43e8af5. Physical/human/baseline/24raster detail gaps OPEN. Functional/evidence stories1–8 implemented, final documentation story remains.

US9TK1:12 curated owners synchronized;13 changed Markdown/84links/29tickets/38DAG nodes,0errors. Current contracts contain no run history. Next final Board inventory/handoff.
