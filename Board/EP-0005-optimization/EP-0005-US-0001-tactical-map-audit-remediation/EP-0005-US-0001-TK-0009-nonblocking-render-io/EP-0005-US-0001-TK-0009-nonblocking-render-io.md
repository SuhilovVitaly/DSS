---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0009-nonblocking-render-io
title: "Убрать файловые операции из интерактивного рисования"
stage: implemented
layer: client
depends_on: ["EP-0005-US-0001-TK-0007-coherent-frame-diagnostics"]
files_touched: 4
serves: [AC-0009]
source_finding: F11
evidence_status: audit-finding
priority: P3
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0009-nonblocking-render-io

## Зачем

JSON serialization/write синхронны; ResolveObjectImage декодирует изображение в Render при первом hover.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Убрать файловые операции из интерактивного рисования. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Diagnostics.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSnapshot.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapAsyncIoTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0001-TK-0007-coherent-frame-diagnostics

## Публичный API

No public API change.

## Порядок реализации

1. Кратко копировать согласованный frame; serialize/write выполнять bounded worker с atomic rename и уникальными именами.
2. Image decode выполнять вне render thread через bounded cache; до готовности placeholder, публикация результата на UI thread.
3. Отмена при уничтожении экрана, обработка ошибок один раз, ограниченные queue/cache и запрет публикации disposed ресурсов.

## Критерии приёмки — AC-0009

- [x] `Slow_capture_writer_does_not_block_render`: Заблокированный fake writer не блокирует следующий Render.
- [x] `Image_decode_publishes_after_render`: Заблокированный decoder не блокирует hover.
- [x] `Closing_screen_cancels_pending_io`: Cancel не оставляет испорченный final JSON или опубликованный disposed image.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapAsyncIoTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Diagnostics.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSnapshot.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapAsyncIoTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09
Expand to Controls/AsyncObjectImageCache.cs, GameSessionScreen.cs and UI/ScreenStack.cs: bounded decode ownership needs a reusable cache and permanent removal must cancel work without cancelling modal deactivation. Introduce IDisposable cancellation now; TK-0010 extends it to all owned graphics resources. Existing asynchronous snapshot writer is retained with cancellation before atomic publication.
Additional scope: PiratePresentationTests.cs must await real asynchronous image decoding before its existing red/blue pixel mirror assertions; first-frame placeholder is now intentional. Preserve all four relation/type cases and pixel expectations.

## Execution evidence 2026-10-09
Object images now decode outside Render through a UI-owned cache (128 ready/negative entries, 16 pending requests, one active decoder). Layout publishes completed images; Draw only reads them and uses placeholders until ready. Cache teardown cancels queued work and disposes late native results without blocking the UI. Permanent ScreenStack removal calls IDisposable; modal deactivation preserves the screen.
Snapshot capture retains its single-worker bound and detached document. Cancellation is checked before serialization and atomic final rename; temporary files are cleaned in finally. Completed cancelled work is not published back to the removed screen. An already completed atomic rename may remain as a complete JSON; no partial final document is exposed.
Validation: focused panel/snapshot/stack/async suite 65/65; initial full run exposed four obsolete synchronous pirate-image fixtures. After preserving pixel assertions and awaiting decode: focused 11/11, full Client 1767/1767. Review added protected diagnostic logging on decoder errors; final targeted async tests 5/5 including failure caching. Release 0 warnings/errors, scoped whitespace verification and diff check pass. Full rerun after the final error-only guard is deferred to next ticket's full suite. Native/manual matrix NOT RUN/OPEN.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.

## AC-to-test naming map

Planned names are AC labels; actual tests consolidate related transitions without dropping assertions:

- `Closing_screen_cancels_pending_io` → `Slow_capture_writer_does_not_block_render_and_closing_screen_cancels_publication` (combined behavioral fixture).
