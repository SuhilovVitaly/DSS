# EP-0015 — audit проверок

Дата:2026-10-26. Это текущий checkpoint, не финальный PASS. Реальные результаты отделены от запланированных/ожидающих gates.

Artifact root: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/.

| Проверка | Evidence относительно artifact root | Факт |
|---|---|---|
| Исходный SDK8 baseline Windows/Ubuntu | 20261026-baseline/ci-121437890935.log и ci-121437891188.log | Одинаковые compiler failures; tests SDK8 тогда NOT RUN |
| Contracts exact-case runsettings | 20261026-runsettings-contracts/summary.json |273/273 реальных cases |
| Windows Motion baseline |20261026-baseline/Motion/baseline.trx |251/251 |
| Full Engine Windows + Ubuntu24 baseline |20261026-engine-ci-baseline/windows/extracted/summary.json;20261026-engine-ci-baseline/ubuntu/extracted/20261026T075417Z-2602/summary.json |2484/2484 на каждой ОС,181 leaf partition, SDK8; commit e9a8d236, до contentRoot changes |
| Client fonts full |20261026-fonts/client-fonts.trx |2326/2326 |
| Trade viewport/input + full layout regression |20261026-layout/trade-fit.trx; client-layout-full.trx |106/106 и2329/2329 |
| AppPaths full regression |20261026-app-paths/client-full.trx |2346/2346 |
| Final Windows Client |20261026-final-client-windows/summary.json |2349/2349,24 exact-case partitions, source4995e0be; C#12 final solution build0 warnings/errors |
| Current Engine defect and repair |20261026-final-engine-windows/summary.json;20261026-content-compatibility/portrait-compat.trx |2485 passed/1 failed in2486; unchanged independent portrait test then passes1/1 on fix0ef5866f. New full fixed-source run20261026-final-engine-fixed-windows is pending, not PASS |
| External content root |20261026-external-content/external-content-final.trx |93/93 |
| Bundled OpenAL on Windows |20261026-audio-bundled/bundled-null.trx; bundled-unavailable.trx |6/6 и1/1, реальный native init/PCM buffers и no-device path |
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

## Native comparison

File-backed font roles/families одинаковы. Windows/Linux advances различаются максимум приблизительно0.0014px; сравнение использует1/64px tolerance, а не недостижимое exact float equality. Geometry/viewport должны совпадать. QuoteId включает session nonce и сравнивается только на наличие; selected item, price и quantities сравниваются как данные. Первоначальный comparison.json с tolerance0.0001 и exact QuoteId намеренно остаётся failed diagnostic, не PASS.

Финальные composite Linux captures и сравнение повторяются по опубликованному harness. До-фонтный native baseline и физическая/human приёмка остаются NOT RUN. Cross-OS asset comparison: runtime button, portrait и gallery byte-identical; contact-sheet preview отличается только текстовым rasterization. В contact-sheet-pixels.json:5248/337312 pixels, mean absolute channel difference0.11116; button regions pixel-identical. Это не утверждение о совпадении всех четырёх encoded hashes.

## Граф

Используется tools/graphify/rebuild.py, output20261026-final-graph/graph. Семантика:3 host agents,63 input files,108 nodes/117 edges/3 hyperedges; fragment source hashes, endpoints, allowed confidence и доступные line bounds проверяются audit.py/final-audit.json. Coverage selective; AST/Markdown structure — полный выбранный corpus. AST parser предупредил о syntax recovery в неизменённом ModulePanelLocalizationTests.cs:25; это ограничение parser, не compiler PASS/FAIL. Финальный экспорт после shell fix:16909 nodes,53839 edges,552 communities по869 code и372 Markdown;1273 unresolved AST edges и871 canonical self-loop явно удалены,3150 external/reference-only symbols не имеют local location. 1241 source hash и63 semantic hash актуальны; missing sources/out-of-range/dangling/self-loops0. Generated ledgers исключены из hashes явно; запись результатов не делает экспорт цикличным.

## Self-review и cleanup

Self-review подтверждён найденными/исправленными compile, glyph, fit, Nyquist и loader дефектами; независимое review не заявляется. Game rules, save schema19/18 и RNG не менялись.

20261026-cleanup/owned-bin-metrics.json идентифицировал261 task-owned JSONL по двум EP-0015 build SHA и UTC; файлы удалены из Client.Tests/bin,533 чужих старых файла сохранены. pip-cache/python-libs генерации шрифтов удалены после использования. Оригинальные variable-font inputs сохранены как referenced derivation evidence с font-manifest.json. Репозиторный Interface.log создан23 октября и не изменялся этим заданием. disposable-compiled.json фиксирует удаление583 файлов временных compiled outputs. duplicates-and-caches.json фиксирует удаление двух graph caches и9 дублирующих ZIP; извлечённые logs/TRX/JSON/PNG и tested Linux tar.gz с release-sha256.json сохранены. Retained audit.py — воспроизводимый инструмент этого отчёта, не production/test project. Окончательный artifact/root audit повторяется перед завершением.

Ubuntu22 full non-Engine corpora:20261026-ubuntu22-ci/comparison.json — Contracts273/273, Motion251/251, EconomyBalance61/61, Performance9/9; CI40291596107 source116ccf91.
