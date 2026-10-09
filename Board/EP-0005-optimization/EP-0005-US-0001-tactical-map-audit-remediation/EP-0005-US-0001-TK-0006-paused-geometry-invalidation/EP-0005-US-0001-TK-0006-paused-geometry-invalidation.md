---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0006-paused-geometry-invalidation
title: "Повторно использовать геометрию на паузе"
stage: implemented
layer: client
depends_on: ["EP-0005-US-0001-TK-0001-paused-authoritative-rebase"]
files_touched: 4
serves: [AC-0006]
source_finding: F08
evidence_status: audit-finding
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0006-paused-geometry-invalidation

## Зачем

Render повторяет подготовку карты под modal и на паузе. F03 уже кэширует ordinary turn forecast; этот ticket охватывает остальные слои.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Повторно использовать геометрию на паузе. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailGeometry.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/NavigationTrajectoryProjector.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapPausedWorkTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0001-TK-0001-paused-authoritative-rebase

## Публичный API

No public API change.

## Порядок реализации

1. Добавить history/camera/navigation revision keys. Не пересобирать неизменившуюся геометрию.
2. Продолжать UI-time status/reticle animation и hover. Pan/zoom/resize/new snapshot/target инвалидируют соответствующие слои.
3. Не замораживать весь фон безусловно: station time advance может обновить мир под modal.

## Критерии приёмки — AC-0006

- [x] `Paused_frames_reuse_geometry`: На одинаковом paused frame geometry build counters не растут.
- [x] `Paused_zoom_invalidates_screen_geometry`: Изменение камеры обновляет screen geometry в следующий кадр.
- [x] `Ui_animation_does_not_rebuild_motion`: Анимация UI не запускает физическое прогнозирование.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapPausedWorkTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailGeometry.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/NavigationTrajectoryProjector.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapPausedWorkTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09
Add ObjectTrailBuffer.cs for mutation revision and GameSessionScreen.GeometryCache.cs for bounded screen-owned cache helpers. Keep cache logic separate from the already large screen file. Cache geometry by actual history/pose and view, retaining UI-time drawing. The final pipeline story owns comprehensive stage extraction and spatial indexing.

## Execution evidence

History mutation revisions invalidate per-contact screen trail geometry. Trajectory caches retain at most the current player/selected/navigation forecasts and one navigation route, keyed by immutable pose plus camera/viewport. Removed histories and old forecast selections evict their entries. Paused label geometry uses snapshot/view/layout/selection keys; UI drawing and real-time effects continue each frame. New physical snapshots still run paused rebase and invalidate geometry.

Focused paused/smoothness/Approach/trail geometry checks: 44/44. Repeated 40-frame paused fixtures assert unchanged build counters for ordinary forecast and navigation; zoom rebuilds screen geometry and a paused time advance updates the pose. Release build zero warnings/errors, scoped formatting and diff checks passed. Full architecture/spatial-index extraction remains US-0002; native acceptance OPEN.

Full Client regression 1757/1757 passed (ep5-us1-tk6-client.trx). Implementation complete; native OPEN.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.

## AC-to-test naming map

Planned names are AC labels; actual tests consolidate related transitions without dropping assertions:

- `Paused_zoom_invalidates_screen_geometry` → `Paused_frames_reuse_geometry` (combined behavioral fixture).
- `Ui_animation_does_not_rebuild_motion` → `Paused_frames_reuse_geometry` (combined behavioral fixture).
