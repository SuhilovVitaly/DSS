# EP-0015 — исполнение

Дата: 2026-10-26. **Все 16 тикетов реализованы и опубликованы; окончательная проверка продолжается. Приёмка открыта.**

Canonical Board содержит 5 историй и 16 тикетов, включая необходимые дополнительные prerequisites и завершающую документационную историю. Stage draft сохранён: пользователь явно разрешил реализацию и публикацию. Self-review и автоматизированные проверки не означают одобрение пользователя.

## Текущий результат

| История | Опубликованный результат | Ожидающее доказательство |
|---|---|---|
| US-0001 | SDK 8 compilation, portability fixes, exact-case baseline и bounded CI | Автоматизированные AC подтверждены на одном commit для Windows и обеих Ubuntu |
| US-0002 | Linux Skia, комплектные шрифты, общий lifetime, Trade fit/input | Запуск через настоящий Weston/XWayland; остальная native matrix и cross-OS comparison подтверждены |
| US-0003 | OpenAL worker/cache/epochs, folder launcher, переносимые PS tools | Linux explicit Play/Stop gate после af3faf63; loader/native init и no-device на обеих Ubuntu PASS. Физическая слышимость и Ubuntu file manager не проверены |
| US-0004 | contentRoot, пользовательские XDG/Windows/portable paths, миграция, publisher и Linux guide | Повтор clean-package на окончательном runtime source и XWayland |
| US-0005 | Актуальные контракты, история документации, source-audited graph | Финальная синхронизация фактических CI результатов, cleanup и публикация evidence |

Последний опубликованный checkpoint: 5ac2de3d2dd4e5a8bfa8aa6cca83246dabf3df04. [PublicationStatus](PublicationStatus.md) содержит соответствия тикет → SHA → push; [Tickets](Tickets.md) — карту критериев и зависимостей.

## Проверенные результаты

- Финальные локальные Engine 2486/2486 и Client 2349/2349 PASS; Engine source включает portrait compatibility fix, Client — все runtime fixes. Дополнительный полный прогон именно SDK 8.0.427 завершён: 5429/5429, 0 failed/skipped; Client 2349, Contracts 273, EconomyBalance 61, Engine 2486, Motion 251, Performance 9. Exact identities discovery/TRX независимо сверены в20261026-sdk8-final/coverage-audit.json. Restore/build SDK 8: 0 warnings/errors.
- Windows native audio: 6/6 с реальным OpenAL Play/Stop через null device и 1/1 для unavailable device, также под SDK 8. Linux lookup failure на старом checkpoint сохранён; новый абсолютный loader проходит native group6/6 и no-device1/1 на обеих Ubuntu (source2cb8fb67), explicit Play/Stop gate ожидается.
- Native layout matrix: Windows, Ubuntu 22.04 и Ubuntu 24.04 по 8 cases × 6 screens. Обе cross-OS comparisons PASS 8/8: geometry, wrapped lines и confirmed quote facts совпадают, font advance delta ≤ 0.00137 px при tolerance 1/64 px.
- Portable PS scripts реально запущены на Windows/Ubuntu: button, portrait и gallery byte-identical; отличие сглаживания текста contact-sheet измерено и явно сохранено.
- Actual production Main на Windows и clean Ubuntu 24.04 без системного .NET PASS: настоящий OpenGL main menu, production save/load, read-only install, UID 1001 и изолированные XDG paths. Это предыдущий опубликованный checkpoint; final runtime/XWayland repeat ожидается.
- External content root 93/93; Windows folder reuse regression воспроизведён и исправлен, 12/12. Migration/no-overwrite/original retention проверяются AppPaths tests и полным Client corpus.
- Документация: 0 broken links, Markdown-only; 5 stories/16 tickets, dependency DAG и карта покрытия проверены. [DocumentationImpact](DocumentationImpact.md) содержит изменённые контракты и историю.

Все точные checkpoints, команды, реальные artifact paths и ограничения перечислены в [FinalValidation](FinalValidation.md). Artifact root: D:/DeepSpaceSaga/artifacts/validation/ep-0015-ubuntu-support/.

## Открытые gates

Финальные native CI стоят в очереди за длительными corpus jobs. Same-commit headless corpus уже PASS на Windows и обеих Ubuntu:20261026-headless-final-ci/comparison.json. Они не отменяются; PENDING не означает PASS. Все дочерние build/test процессы ограничены 300 секундами, длинные corpora разбиты по exact test identities.

NOT RUN/OPEN: до-фонтный Windows native baseline (Codex-managed worktree tool ответил Not a git repository для chat cwd), физическая мышь/клавиатура, слышимость на Windows/PulseAudio/PipeWire, реальный файловый менеджер Ubuntu, human/product acceptance и независимое review. Xvfb/llvmpipe подтверждает настоящий software OpenGL. Native Wayland и arm64 вне подтверждаемого x64/X11/XWayland scope и не добавлены как новые критерии эпика.

## История исправлений

2026-10-26: 661f3fe8 перенёс внешний пакет в Board и добавил US-0005. Реальный SDK 8 baseline выявил CS0121, nullable diagnostics и затем CS1998; ошибки сохранены и исправлены отдельными коммитами. SDK 10 baseline не подменяет SDK 8.

Native/self-review выявил glyph gap для ♦, Trade crop 1280×720, Nyquist boundary 22050 Hz, missing graphics dependencies, default OpenAL resolver failure, directory-only portrait compatibility и Windows Explorer reuse. Все исправления опубликованы отдельно; игровые правила, RNG и SaveFormat 19/18 не менялись. Для XWayland отдельный fix явно включает Weston --xwayland по официальному Ubuntu Noble weston(1); Python AST/Bash syntax PASS, actual native CI ещё требуется.

## Ограничение вспомогательной операции

Автоматическая проверка разрешений отклонила попытку отменить устаревший CI через сохранённые Git credentials: извлечение credentials и внешняя отмена не были отдельно разрешены. Операция не выполнена, credentials не извлекались, обход не применялся. Отмена не требуется для deliverable; проверки продолжаются с ожиданием очереди.
