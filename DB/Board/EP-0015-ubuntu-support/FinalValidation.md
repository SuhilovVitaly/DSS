# EP-0015 — audit проверок

Дата:2026-10-26. Это текущий checkpoint, не финальный PASS. Реальные результаты отделены от запланированных/ожидающих gates.

Artifact root: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/.

| Проверка | Evidence относительно artifact root | Факт |
|---|---|---|
| Полный финальный Windows SDK8 corpus |20261026-sdk8-final/results/summary.json; coverage-audit.json |5429/5429: Client2349, Contracts273, Economy61, Engine2486, Motion251, Performance9;0 failures/skips/invalid timeouts. Независимо сверены exact discovery/TRX identities |
| Исходный SDK8 baseline Windows/Ubuntu | 20261026-baseline/ci-121437890935.log и ci-121437891188.log | Одинаковые compiler failures; tests SDK8 тогда NOT RUN |
| Contracts exact-case runsettings | 20261026-runsettings-contracts/summary.json |273/273 реальных cases |
| Windows Motion baseline |20261026-baseline/Motion/baseline.trx |251/251 |
| Финальный headless на одном commit |20261026-headless-final-ci/comparison.json; audit.py; artifacts.json |CI40295143260 source0ef5866f: Windows2022/Ubuntu22/Ubuntu24 по3080/3080: Engine2486, Contracts273, Motion251, Economy61, Performance9. Все15 jobs и exact local discovery/TRX coverage PASS |
| Full Engine Windows + Ubuntu24 baseline |20261026-engine-ci-baseline/windows/extracted/summary.json;20261026-engine-ci-baseline/ubuntu/extracted/20261026T075417Z-2602/summary.json |2484/2484 на каждой ОС,181 leaf partition, SDK8; commit e9a8d236, до contentRoot changes |
| Client fonts full |20261026-fonts/client-fonts.trx |2326/2326 |
| Trade viewport/input + full layout regression |20261026-layout/trade-fit.trx; client-layout-full.trx |106/106 и2329/2329 |
| AppPaths full regression |20261026-app-paths/client-full.trx |2346/2346 |
| Final Windows Client |20261026-final-client-absolute-windows/summary.json |2349/2349,24 exact-case partitions, source af3faf63 после всех runtime fixes; C#12 final solution build0 warnings/errors |
| Current Engine defect and repair |20261026-final-engine-windows/summary.json;20261026-content-compatibility/portrait-compat.trx |2485 passed/1 failed in2486; unchanged independent portrait test then passes1/1 on fix0ef5866f. Full fixed-source run20261026-final-engine-fixed-windows subsequently PASS2486/2486 in181 partitions |
| External content root |20261026-external-content/external-content-final.trx |93/93 |
| Bundled OpenAL on Windows |20261026-audio-bundled/bundled-null.trx; bundled-unavailable.trx |6/6 и1/1, реальный native init/PCM buffers и no-device path |
| Bundled OpenAL на Ubuntu22/24 |20261026-linux-audio-final/ubuntu22-job.log; ubuntu24-job.log |CI40299896867 source2cb8fb67: оба Client jobs PASS, native group6/6 и no-device1/1 на каждой ОС; explicit Play/Stop test добавлен следующим af3faf63 и повтор ещё ожидается |
| Folder launcher |20261026-shell/shell.trx |11/11; actual Ubuntu file manager NOT RUN |
| Windows shell reuse regression |20261026-shell-reuse/before.trx; after.trx; format.log |Воспроизведение1 failed/4 passed →12/12, scoped format0; null handle допустим только Windows shell |
| Portable PS scripts Windows |20261026-assets/windows/summary.json |Оба запускаются;4 PNG hashes, neck/alpha/collar guards и visual preview |
| Current native Windows composite |20261026-layout-composite/windows/matrix.json |8 cases×6 screens,48 PNG |
| Ubuntu native after dependencies |20261026-linux-native-deps/extracted/native/matrix.json |8/8; software OpenGL llvmpipe; old standalone Trade composite margin gate superseded |
| Real production startup/save/load Windows |20261026-publish/windows-startup.json и .png |PASS с изолированным профилем,14 кадров |
| Self-contained cross-publish |20261026-publish/cross-publish.log; cross-inventory.json; native-dependencies.json |Сборка/состав PASS |
| Clean Ubuntu24 self-contained launch |20261026-linux-distribution-ci/extracted/distribution/summary.json; clean-ubuntu/startup.json; hosttrace.log |PASS: без системного .NET, read-only /opt/dss, unprivileged UID1001,13 real GPU frames/main menu, production Save/Load, XDG config/data; CI40291292917 |
| Missing native rejection |20261026-publish/verify-complete.log; verify-incomplete.log |Полный пакет0, безSkia1; файл восстановлен |
| Adaptive partition orchestration |20261026-adaptive-runner/adaptive-verification.json |273 реальных Contracts cases;3 injected invalid parent timeouts,6 actual leaves; не performance measurement |
| Documentation links/formats |20261026-project-docs/documentation-check.json |0 broken links,0 non-Markdown files |

## Покрытие критериев

PASS ниже относится к явно указанной проверке и source checkpoint; не означает human approval или завершение всего эпика.

| Story / AC | Проверка и текущая граница |
|---|---|
| US-0001 AC-01 | SDK8 solution build PASS в Ubuntu CI;0 compiler warnings/errors, Windows C#12 final build PASS |
| US-0001 AC-02 | Windows2022/Ubuntu22/Ubuntu24 на одном исправленном0ef5866f PASS: Engine2486, Contracts273, Motion251, Economy61. Exact local discovery/TRX и method multiplicities сверены; counters одинаковы |
| US-0001 AC-03 | Средовые compile/native/file-lock failures сохранены и исправлены, тесты не скрыты. Client без DISPLAY явно NOT RUN; CI предоставляет Xvfb |
| US-0001 AC-04 | Performance9/9 на Windows2022/Ubuntu22/Ubuntu24 source0ef5866f PASS; process/git invocation сохранены |
| US-0001 AC-05 | Реальный check-linux.sh используется всеми Linux CI shards, TRX и exact coverage сохранены |
| US-0002 AC-01 | X11/Xvfb actual OpenGL startup и self-contained Main PASS; compositor XWayland PENDING CI |
| US-0002 AC-02 | File-backed font roles, glyph tests и actual native metrics PASS; shared lifetime подтверждён full Client |
| US-0002 AC-03 | Windows8×6 native captures PASS. Ubuntu22 current8×6 PASS и comparison8/8 PASS; Ubuntu24 current composite comparison8/8 PASS. До-фонтный baseline и human visual acceptance NOT RUN |
| US-0002 AC-04 | Ubuntu graphical-session Client2346 full cases PASS на earlier source; Windows final2349 PASS, latest Linux source repeat PENDING |
| US-0003 AC-01 | PCM frequencies/durations/Nyquist and Windows native null playback PASS; Linux bundled-loader/native init6/6 PASS на Ubuntu22/24, explicit Play/Stop gate af3faf63 ожидается. Physical audibility NOT RUN |
| US-0003 AC-02 | Windows explicit unavailable-device1/1 PASS; exception contained, one warning. Ubuntu22/24 no-device по1/1 PASS на2cb8fb67 |
| US-0003 AC-03 | Argument boundaries, launch failure and panel feedback PASS; Windows Explorer reuse regression reproduced/fixed12/12. Actual Ubuntu file manager NOT RUN |
| US-0003 AC-04 | Оба pwsh scripts реально запускаются Windows/Ubuntu; button/portrait/gallery byte-identical, contact-sheet text AA difference измерена |
| US-0004 AC-01 | Реальный clean Ubuntu24 self-contained executable без dotnet PASS, read-only installation и production Save/Load |
| US-0004 AC-02 | XDG config/data в clean container PASS; AppPaths and writer integration targeted39/39, full Windows Client2349/2349 |
| US-0004 AC-03 | Migration/no-overwrite/original retention/marker tests PASS в AppPaths tests и full Client; old source Linux full Client также PASS |
| US-0004 AC-04 | Актуальный Ubuntu.md и связанные current contracts опубликованы, local links PASS |
| US-0005 AC-01 | DocumentationImpact ledger и датированная история опубликованы; current contracts обновлены |
| US-0005 AC-02 |5 stories/16 tickets, DAG и84 document links PASS; все16 implementation commits отдельно опубликованы и remote SHA проверены |
| US-0005 AC-03 | Graph manifest1241/current semantics63 inputs PASS; disposable compiled/cache/ZIP cleanup записан, root audit повторяется в конце |

## Сравнение native

File-backed font roles/families одинаковы. Windows/Linux advances различаются максимум приблизительно0.0014px; сравнение использует1/64px tolerance, а не недостижимое exact float equality. Geometry/viewport должны совпадать. QuoteId включает session nonce и сравнивается только на наличие; selected item, price и quantities сравниваются как данные. Первоначальный comparison.json с tolerance0.0001 и exact QuoteId намеренно остаётся failed diagnostic, не PASS.

Composite comparison Ubuntu22/Windows PASS8/8:20261026-layout-composite/comparison-ubuntu22.json, воспроизводимый compare.py. Exact panel bounds/wrapped lines и confirmed quote facts совпадают; max font advance delta0.00137px при tolerance1/64px. Self visual review проверил RU scale1.5 trade/map/debug на обеих ОС; это не human product approval. Ubuntu24 comparison также PASS8/8:20261026-layout-composite/comparison-ubuntu24.json. До-фонтный native baseline и физическая/human приёмка остаются NOT RUN. Cross-OS asset comparison: runtime button, portrait и gallery byte-identical; contact-sheet preview отличается только текстовым rasterization. В contact-sheet-pixels.json:5248/337312 pixels, mean absolute channel difference0.11116; button regions pixel-identical. Это не утверждение о совпадении всех четырёх encoded hashes.

## Граф

Используется tools/graphify/rebuild.py, output20261026-final-graph/graph. Семантика:3 host agents,63 input files,109 nodes/120 edges/3 hyperedges; fragment source hashes, endpoints, allowed confidence и доступные line bounds проверяются audit.py/final-audit.json. Coverage selective; AST/Markdown structure — полный выбранный corpus. AST parser предупредил о syntax recovery в неизменённом ModulePanelLocalizationTests.cs:25; это ограничение parser, не compiler PASS/FAIL. Финальный экспорт после absolute audio loader fix:16912 nodes,53848 edges,554 communities по869 code и372 Markdown;1275 unresolved AST edges и871 canonical self-loop явно удалены,3151 external/reference-only symbols не имеют local location. 1241 source hash и63 semantic hash актуальны; missing sources/out-of-range/dangling/self-loops0. Generated ledgers исключены из hashes явно; запись результатов не делает экспорт цикличным.

## Self-review и cleanup

Self-review подтверждён найденными/исправленными compile, glyph, fit, Nyquist и loader дефектами; независимое review не заявляется. Game rules, save schema19/18 и RNG не менялись.

20261026-cleanup/owned-bin-metrics.json идентифицировал261 task-owned JSONL по двум EP-0015 build SHA и UTC; файлы удалены из Client.Tests/bin,533 чужих старых файла сохранены. pip-cache/python-libs генерации шрифтов удалены после использования. Оригинальные variable-font inputs сохранены как referenced derivation evidence с font-manifest.json. Репозиторный Interface.log создан23 октября и не изменялся этим заданием. disposable-compiled.json фиксирует удаление583 файлов временных compiled outputs. duplicates-and-caches.json фиксирует удаление двух graph caches и9 дублирующих ZIP; извлечённые logs/TRX/JSON/PNG и tested Linux tar.gz с release-sha256.json сохранены. Retained audit.py — воспроизводимый инструмент этого отчёта, не production/test project. Окончательный artifact/root audit повторяется перед завершением.

Ubuntu22 full non-Engine corpora:20261026-ubuntu22-ci/comparison.json — Contracts273/273, Motion251/251, EconomyBalance61/61, Performance9/9; CI40291596107 source116ccf91.

Absolute audio-loader follow-up:20261026-linux-audio-diagnosis/extracted/audio-interface.log сохраняет реальный Ubuntu24 failure40293853513; предпочтение soft:true не устранило lookup. Код2cb8fb67 использует абсолютный bundled path для publish root/portable RID.20261026-audio-playback/native-playback.trx6/6 и unavailable.trx1/1 подтверждают Windows real native Play/Stop и no-device. Linux repeat на2cb8fb67 PASS6/6 +1/1 на Ubuntu22/24 (20261026-linux-audio-final/*-job.log); af3faf63 explicit Play/Stop gate ещё ожидается.20261026-final-build/workflow-caps.json проверяет точное значение/indentation5-minute limits для двух standalone CI steps; PyYAML недоступен, полный YAML parser PASS не заявляется.

20261026-final-client-absolute-windows/summary.json:2349/2349 на sourceaf3faf63, включая все runtime fixes.20261026-final-engine-fixed-windows/summary.json:2486/2486 в181 exact partitions, source4995e0be (Engine source после него не менялся).

Настоящий минимальный toolchain:20261026-sdk8-final/verified-sdk.json — SDK8.0.427 из официального Microsoft metadata, SHA512 совпал, Authenticode Valid. results/restore.log и build.log PASS,0 warnings/errors. Весь corpus PASS5429/5429; coverage-audit.json независимо сверяет все exact identities с discovery/TRX, failures/skips/invalid timeouts0. Native/openal-null.trx6/6 с Play/Stop и openal-unavailable.trx1/1 PASS. После завершения удалены6414 файлов private SDK/CLI-home/http cache (1193350040 bytes), процессы этого SDK отсутствовали:20261026-cleanup/private-sdk.json. Игнорируемые project.assets обновлены обычным restore с исходным SDK10/cache (20261026-post-sdk-cleanup/restore-default-final.log exit0; ограниченный sandbox attempt exit1 сохранён). Системный SDK10 не изменён.

XWayland coverage implemented in b587c608: clean-package script starts headless Weston13/pixman plus XWayland bridge, requires bridge PID and second real startup/save-load JSON.20261026-xwayland-gate/validation.json подтверждает Python AST/Bash syntax; actual CI40301785332 и explicit-activation fix5ac2de3d (CI40305139664) ожидаются, не PASS. Отсутствовавший --xwayland выявлен self-review; исправленная команда и Python AST/Bash -n проверены в20261026-xwayland-explicit/validation.json. Первоначальный Bash запуск ограничен MSYS sandbox, повтор вне него exit0. Native Wayland иarm64 вне подтверждаемого x64/X11/XWayland scope; не являются дополнительными критериями завершения этого эпика.

Same-commit headless audit:20261026-headless-final-ci/audit.py проверяет15 небольших artifacts, их ZIP SHA256 сверены с GitHub digest и ZIP удалены после извлечения. Counts совпадают на всех трёх ОС (3080 каждый), каждый local discovery case встречается в leaf TRX ровно с нужной multiplicity. Дополнительная слишком строгая проверка буквальных cross-OS display names обнаружила только infinity representation (∞/Infinity) и platform-newline/truncation в одном JSON theory input; это записано в cross-os-display-differences.json, exact_cross_os_display_names=false. Test methods/multiplicities и требуемые passed counts одинаковы, тесты/assertions не менялись.
