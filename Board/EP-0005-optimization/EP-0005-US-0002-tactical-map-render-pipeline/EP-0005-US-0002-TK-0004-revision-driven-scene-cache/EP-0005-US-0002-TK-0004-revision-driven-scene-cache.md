---
epic: EP-0005-optimization
story: EP-0005-US-0002-tactical-map-render-pipeline
ticket: EP-0005-US-0002-TK-0004-revision-driven-scene-cache
title: "Добавить адресную инвалидацию и ограниченные кэши"
stage: draft
layer: client
depends_on: ["EP-0005-US-0002-TK-0003-read-only-map-painter"]
files_touched: 5
serves: [AC-0004]
source_finding: A04
evidence_status: architecture-proposal
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0002-TK-0004-revision-driven-scene-cache

## Зачем

После разделения стадий можно пересчитывать только изменившиеся части и ограничить работу видимыми контактами.

## Решения и полномочия

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Добавить адресную инвалидацию и ограниченные кэши. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapStateUpdater.cs` — создать в этом тикете / после зависимости
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneBuilder.cs` — создать в этом тикете / после зависимости
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneCache.cs` — создать в этом тикете / после зависимости
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSpatialIndex.cs` — создать в этом тикете / после зависимости
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapSceneCacheTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0002-TK-0003-read-only-map-painter

## Публичный API

План: client-internal revision keys и bounded TacticalMapSceneCache; формат snapshot/save не меняется.

## Порядок реализации

1. Описать dependency matrix: world/pose/route/trail/camera/layout/settings/locale/selection revisions. UI time не инвалидирует геометрию; selection не перестраивает статический spatial index.
2. Для static contacts использовать индекс, для dynamic contacts обновлять изменившиеся bounds. Query расширяется на marker radius; line clipping учитывает сегменты с обоими концами вне viewport.
3. Ограничить кеши текущей сессией и установленным capacity; removal/eviction/dispose освобождают ресурсы. Измерить rebuild counters; не вводить GPU render textures без отдельного evidence.

## Критерии приёмки — AC-0004

- [ ] `Unchanged_frame_reuses_geometry`: Пауза и неизменный view дают ноль повторных geometry builds.
- [ ] `Each_revision_invalidates_only_dependents`: Табличный тест всех revisions проверяет правильные invalidation sets.
- [ ] `Cache_is_bounded_and_spatial_query_is_complete`: Удалённые объекты очищены; индекс не пропускает пересекающие viewport элементы.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapSceneCacheTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapStateUpdater.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneBuilder.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneCache.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSpatialIndex.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapSceneCacheTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
