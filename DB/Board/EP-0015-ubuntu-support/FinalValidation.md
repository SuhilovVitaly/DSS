# EP-0015 — итоговый audit проверок

Дата: 2026-10-26. Все 16 тикетов реализованы и опубликованы. **Automated и native проверки подтверждены; физическая/продуктовая приёмка открыта**. PASS относится к указанной проверке и source checkpoint, не означает одобрение пользователя.

Artifact root: `D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/`. Пути в таблицах ниже заданы относительно него. Canonical scope: 5 историй, 16 тикетов; [карта](Tickets.md), [публикация](PublicationStatus.md), [история документации](DocumentationImpact.md).

## Автоматизированные проверки

| Проверка | Evidence | Фактический результат |
|---|---|---|
| Полный Windows corpus, настоящий SDK 8.0.427 | `20261026-sdk8-final/results/summary.json`, `coverage-audit.json` | **5429/5429**, 0 failed/skipped/invalid timeouts; exact discovery/TRX identities независимо сверены |
| SDK и сборка | `20261026-sdk8-final/verified-sdk.json`, `sdk-source.json`, `results/restore.log`, `results/build.log` | Официальный Microsoft SDK, SHA512 совпал, Authenticode Valid; restore/build exit0, 0 warnings/errors |
| Headless на одном commit | `20261026-headless-final-ci/comparison.json`, `audit.py`, `artifacts.json` | Windows2022/Ubuntu22/Ubuntu24 по **3080/3080**, все 15 jobs PASS; CI40295143260 source0ef5866f |
| Graphical Client обеих Ubuntu | `20261026-final-linux-native/release-audit.json`, `20261026-client24-final-ci/checked-summary.json` |2349/2349 на Ubuntu22 (5ac2de3d) и Ubuntu24 (af3faf63); exact coverage PASS, source trees совпадают |
| Текущий Client Windows | `20261026-final-client-absolute-windows/summary.json` |2349/2349 на af3faf63; повтор с SDK8 выше также2349/2349 |
| External contentRoot | `20261026-external-content/external-content-final.trx` |93/93: реальный bootstrap, portraits, session save/load с внешней конфигурацией |
| AppPaths/migration/writers | `20261026-app-paths/app-paths-final.trx` |39/39; полный Client также проходит; portable, XDG, no-overwrite, сохранение исходников и marker |
| Windows Explorer reuse | `20261026-shell-reuse/before.trx`, `after.trx`, `format.log` | Repro1 failed/4 passed →12/12; scoped formatter exit0 |
| Adaptive orchestration | `20261026-adaptive-runner/adaptive-verification.json` |273 реальных Contracts cases, 3 injected invalid parent timeouts и6 actual leaves; не performance measurement |
| Форматирование и текстовый diff | scoped format logs в карточках; `20261026-final-build/build.log` | Изменённые C# области проверены; финальный C#12 build0 diagnostics. `git diff --check` PASS |

| Проект | Windows SDK8, финальный код | Windows2022/Ubuntu22/Ubuntu24, один0ef5866f |
|---|---:|---:|
| Client |2349 | Отдельный graphical CI, см. строку выше |
| Contracts |273 |273 на каждой ОС |
| EconomyBalance |61 |61 на каждой ОС |
| Engine |2486 |2486 на каждой ОС,181 leaf partitions |
| Motion |251 |251 на каждой ОС |
| Performance |9 |9 на каждой ОС, включая реальные dotnet/git процессы |

Windows full build source — b587c608; последующий5ac2de3d меняет только команду package validation, C# не меняется. Engine source после0ef5866f не менялся. Для headless independently проверены local discovery, selected identities и фактические leaf TRX; parent timeouts0. ZIP SHA256 всех15 artifacts совпал с GitHub digest, ZIP удалены после извлечения. Буквальные cross-OS display names различаются в7 theory cases: ∞/Infinity и platform newline/truncation одного JSON input. `cross-os-display-differences.json` сохраняет различия, exact_cross_os_display_names=false. Methods/multiplicities и требуемые passed counts совпадают; tests/assertions не менялись ради этой дополнительной проверки.

Команды: `python tools/ci/platform-baseline.py <absolute-results>`; Linux CI вызывает `xvfb-run -a bash tools/check-linux.sh <absolute-results>` с точным DSS_TEST_PROJECT. Каждый дочерний build/test процесс имеет300s cap и cleanup дерева; без DISPLAY/WAYLAND_DISPLAY Client явно NOT RUN с причиной, в CI Xvfb предоставлен. Полные corpora исполняются exact-case partitions, успешный процесс с0 tests не принимается. Scoped formatter использует `--verify-no-changes --include` только для изменённых файлов.

## Native и интеграция

| Проверка | Evidence | Результат |
|---|---|---|
| Windows OpenAL | `20261026-sdk8-final/native/openal-null.trx`, `openal-unavailable.trx`; `20261026-audio-playback/` |6/6 группы, включая настоящий native Play/Stop, и1/1 unavailable device; не проверка слышимости |
| Ubuntu22/24 OpenAL | `20261026-linux-audio-final/ubuntu22-playback-job.log`, `ubuntu24-playback-job.log` |CI40300209826 sourceaf3faf63: на каждой ОС6/6 с Play/Stop и1/1 no-device PASS; абсолютный bundled native context |
| Native layout matrix | `20261026-layout-composite/windows/matrix.json`, `ubuntu22/native/matrix.json`, `ubuntu24/native/matrix.json` | На каждой ОС8 cases ×6 screens,48 PNG: menu/save/load/trade/map/debug, RU/EN и scales0.8/1/1.2/1.5 |
| Cross-OS layout | `20261026-layout-composite/comparison-ubuntu22.json`, `comparison-ubuntu24.json`, `compare.py` | Обе comparisons PASS8/8: viewport, geometry, wrapped lines и confirmed quote facts |
| Portable PS tools | `20261026-assets/windows/summary.json`; Linux summaries в native CI artifacts | Оба scripts реально выполнены pwsh7 на Windows/Ubuntu; runtime button, portrait и gallery byte-identical |
| Actual Windows Main | `20261026-publish/windows-startup.json`, `.png` | Настоящий GPU main menu,14 frames и production session save/load; изолированный профиль |
| Clean Ubuntu24 X11 package | `20261026-linux-distribution-ci/extracted/distribution/summary.json` | CI40291292917: без системного dotnet, read-only /opt/dss, UID1001, XDG config/data,13 GPU frames и production save/load PASS |
| Native inventory/rejection | `20261026-publish/cross-inventory.json`, `native-dependencies.json`, `verify-complete.log`, `verify-incomplete.log` | Полный пакет exit0; без libSkiaSharp.so exit1, файл восстановлен |
| Final package/XWayland | `20261026-final-linux-native/extracted/distribution/summary.json`, `release-audit.json`, `release.json` |CI40305139664 source5ac2de3d PASS: clean Ubuntu24, actual X11 и Weston/XWayland по13 GPU frames, production save/load, PID4823 Xwayland и DISPLAY=:0 |

Layout harness использует настоящую paused session seed1, transport, SaveSlotRepository, production Game→Station→Trade stack и подтверждённый quote. File-backed font roles/families совпадают; максимальная разница advances0.00137px при tolerance1/64px. QuoteId содержит session nonce: сравнивается на наличие, price/selection/quantities — как данные. Слишком строгий первый oracle (0.0001px/exact QuoteId) сохранён как failed diagnostic в `20261026-linux-native-deps/comparison.json`, не PASS. Self visual review проверил RU1.5 trade/map/debug; это не human approval.

У contact-sheet preview отличается сглаживание текста: `contact-sheet-pixels.json` —5248/337312 pixels, mean absolute channel difference0.11116, max80, bbox[24,22,595,206]; button regions pixel-identical. Совпадение всех четырёх encoded hashes не заявляется. Font derivation inputs, OFL и `20261026-fonts/font-manifest.json` сохранены.

Package harness `python tools/ci/platform-publish.py <repository> <absolute-results>` проверяет tar execute bits/extraction, отсутствующий native и запускает executable в настоящем чистом Ubuntu24 Docker. Новый gate добавляет Weston13/pixman, явно --xwayland, требует реальный bridge PID и отдельный startup/save-load JSON. WAYLAND_DISPLAY снимается только у X11 клиента, DISPLAY берётся у compositor. Оба запуска сохраняют no-runtime/read-only/unprivileged/XDG условия,300s container cap и удаление собственного container. Weston — зависимость проверки. Native Wayland/arm64 вне подтверждаемого x64/X11/XWayland scope и не добавлены как новые критерии эпика.

## Покрытие acceptance criteria

| Story / AC | Подтверждение и граница |
|---|---|
| US-0001 AC-01 | Реальный SDK8 build solution на Windows и Ubuntu PASS, warnings-as-errors сохранён |
| US-0001 AC-02 | Четыре требуемых headless corpora проходят с одинаковыми counts на одном0ef5866f |
| US-0001 AC-03 | Средовые failures сохранены и исправлены; silent skip нет, no-display Client явно NOT RUN |
| US-0001 AC-04 | Performance9/9 на всех трёх ОС |
| US-0001 AC-05 | Реальный check-linux.sh используется Linux CI, exact coverage/TRX сохранены |
| US-0002 AC-01 | Actual X11 и Weston/XWayland startup PASS в clean Ubuntu24 |
| US-0002 AC-02 | Bundled fonts/roles/glyphs/shared lifetime и native metrics PASS |
| US-0002 AC-03 | Ubuntu22/24 против Windows layouts8/8 PASS; до-фонтный native baseline и human visual acceptance NOT RUN |
| US-0002 AC-04 | Graphical Ubuntu22/Ubuntu24 Client по2349/2349, exact coverage PASS; source equivalence подтверждена |
| US-0003 AC-01 | PCM/frequency/duration/Nyquist и native Play/Stop Windows/Ubuntu PASS; физическая слышимость NOT RUN |
| US-0003 AC-02 | No-device1/1 на всех трёх ОС; worker exception contained и once-warning checks PASS |
| US-0003 AC-03 | Argument boundaries/failure/path feedback и Explorer reuse PASS12/12; настоящий Ubuntu file manager NOT RUN |
| US-0003 AC-04 | Оба PowerShell scripts реально выполнены cross-OS, сравнения outputs сохранены |
| US-0004 AC-01 | Final5ac2de3d пакет на clean Ubuntu24 без .NET: X11/XWayland и настоящий save/load PASS |
| US-0004 AC-02 | XDG в реальном clean container, AppPaths/writer tests и full Client PASS |
| US-0004 AC-03 | No-overwrite/original retention/one-time marker migration PASS |
| US-0004 AC-04 | Current Ubuntu guide, dependencies/commands/user paths и local links проверены |
| US-0005 AC-01 | Current contract impact и dated history опубликованы, gameplay/RNG/SaveFormat19/18 не менялись |
| US-0005 AC-02 |5 stories/16 tickets, ID/DAG/counts/links проверены; все implementation commits separately pushed и remote verified |
| US-0005 AC-03 | Граф source hashes, final-root-audit.json и evidence-index.json PASS; task-owned temp/cache/processes0, один final release retained |

## История failures и исправлений

- Настоящий SDK8 baseline обнаружил CS0121, nullable diagnostics и затем CS1998. `20261026-baseline/ci-121437890935.log`, `ci-121437891188.log` сохраняют исходный failure; поздний SDK10 PASS не подменяет SDK8. Исправления опубликованы по отдельным тикетам.
- Self-review исправил missing ♦ glyph, Trade crop1280×720 и общую fit/input transform, точную Nyquist boundary22050Hz. Targeted/full tests и native matrix проходят.
- Linux graphics dependencies добавлены после реального native failure. Default OpenAL resolver и затем предпочтение soft:true не работали на Ubuntu24 (`20261026-linux-audio-diagnosis/extracted/audio-interface.log`). Абсолютный root/portable-RID путь исправляет lookup; реальные6/1 подтверждены обеими Ubuntu.
- Существующий independent portrait test выявил directory-only discovery regression: `20261026-final-engine-windows/summary.json`2485 passed/1 failed; `20261026-content-compatibility/portrait-compat.trx`1/1 после0ef5866f, затем полные2486/2486 на всех ОС.
- Windows Explorer reuse: null Process после успешного shell launch ошибочно считался failure; воспроизведено и исправлено12/12. Unix null/exit failures продолжают отображать путь.
- В первой XWayland validation команде пропущен --xwayland; self-review исправил в5ac2de3d по Ubuntu Noble weston(1). CI40301785332 затем воспроизвёл DISPLAY: unbound variable (`20261026-xwayland-gate/ubuntu22-before-activation-job.log`); остальные Client steps PASS. Первоначальный Bash sandbox attempt ограничен MSYS, повтор вне него exit0; native подтверждение исправления PASS в CI40305139664, X11/XWayland по13 frames; реальные startup PNG просмотрены.

Все source assertions и существенные изменения проверены self-review по Board/архитектурным инвариантам. Независимое review не заявляется.

## Граф и cleanup

`20261026-final-graph/graph/verification.json`, `source-manifest.json`, `final-audit.json` и воспроизводимый `audit.py`:1241 source hashes,63 fresh host semantic inputs,5 stories/16 tickets,84 Board+Documentation files,0 broken links, Markdown-only. Семантика3 agents:109 nodes/120 edges/3 hyperedges, selective coverage и unmeasured token usage. Полный выбранный structural corpus:869 C# files,372 Markdown;16912 nodes/53848 edges/554 communities.1275 unresolved AST edges и871 canonical self-loops удалены явно,3151 reference-only symbols без local location. Missing/out-of-range/dangling/self-loops0. Recovery warning неизменённого ModulePanelLocalizationTests.cs:25 — ограничение parser, не compiler failure. Generated ledgers исключены из source hashes для предотвращения циклов; recorded HEAD pre-export, hashes описывают действительное working tree.

`20261026-cleanup/owned-bin-metrics.json`: удалён261 task-owned JSONL по EP-0015 build SHA/UTC,533 старых чужих файла сохранены. `disposable-compiled.json`: удалены583 временных compiled files. `duplicates-and-caches.json`, `late-duplicate-archives.json`: удалены дублирующие ZIP и прежние caches. Font pip-cache/python-libs удалены, referenced inputs/license/manifest сохранены. Repo Interface.log от23 октября не изменялся.

`private-sdk.json`: после успешных5429 tests удалены6414 private SDK/CLI-home/http-cache files,1193350040 bytes; процессов этого SDK не оставалось. `20261026-post-sdk-cleanup/restore-default-final.log` exit0 обновил ignored project.assets к обычному cache исходного SDK10; ограниченный sandbox attempt exit1 сохранён. Системный SDK не изменён. Окончательный root/process/cache audit PASS: `20261026-cleanup/final-root-audit.json`, `evidence-index.json`. Task process candidates/cache/compiled/duplicate ZIP/root temp0; DSS/artifacts и parent Interface.log отсутствуют, repo Interface.log остаётся от23 октября. Повторно созданный cache последнего graph rebuild удалён, два cleanup records в final-graph-cache.json. Superseded earlier5d5bd31b tar удалён после проверки hash, historical metadata/logs сохранены (superseded-release.json); оставлен один final5ac2de3d tar. Evidence index перечисляет фактические run directories и retained reproduction scripts.

## Открытая приёмка

NOT RUN/OPEN: до-фонтный native Windows baseline (create_worktree tool вернул Not a git repository для chat cwd; manual source copy/checkout не применялись), физическая мышь/клавиатура, слышимость Windows/PulseAudio/PipeWire, настоящий Ubuntu file manager и human/product acceptance. Xvfb/llvmpipe — настоящий software OpenGL, не hardware/physical evidence.

Автоматическая проверка разрешений отклонила вспомогательную отмену устаревшего CI через извлечение Git credentials: извлечение и внешняя отмена не были отдельно разрешены. Операция не выполнена, credentials не извлекались, обход не применялся. Нужные результаты получены с ожиданием очереди.

Финальный release: `20261026-final-linux-native/DeepSpaceSaga-linux-x64.tar.gz`,93058886 bytes, SHA256 `5b25bb3503150ec213e3cdc4cc6fe9f11d2b035fdbee14b2b372ca76bc7d5dea`. GitHub ZIP digest совпал, локально сохранены один tar и148 evidence files, compiled copies/дублирующие PNG/ZIP исключены. Native ELF signatures, tar execute bit, фактические13+13 frames/XDG/no-runtime/save-load и exact Client coverage независимо проверены в release-audit.json. Producer host — Ubuntu22, Docker target — чистая Ubuntu24; это не смешивает версии runtime.

`20261026-publication/source-equivalence.json` проверяет18 Git tree/blob objects: весь src/tests и baseline/layout tools на5ac2de3d совпадают с проверенным af3faf63; headless components совпадают с0ef5866f. Это source equality, не binary identity (build metadata различается). Поэтому повторный Ubuntu24 shard latest workflow не нужен для доказательства неизменённого Client кода: Ubuntu24 af3faf63 уже PASS2349/2349, а новая package validation проверена отдельно на настоящей Ubuntu24. При фиксации evidence часть повторных matrix jobs ещё queued/running; зелёный статус всего составного workflow не заявляется.
