---
epic: EP-0005-optimization
story: EP-0005-US-0002-tactical-map-render-pipeline
ticket: EP-0005-US-0002-TK-0002-scene-geometry-prepare
title: "Выделить подготовку геометрии кадра"
stage: implemented
layer: client
depends_on: ["EP-0005-US-0002-TK-0001-frame-state-update"]
files_touched: 7
serves: [AC-0002]
source_finding: A02
evidence_status: automated-validated-native-open
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0002-TK-0002-scene-geometry-prepare

## Зачем

Кластеры, trails, forecast, labels и hit targets вычисляются вперемешку с Draw. Нужен проверяемый снимок готовой сцены.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Выделить подготовку геометрии кадра. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneGeometry.cs` — создать в этом тикете / после зависимости
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneBuilder.cs` — создать в этом тикете / после зависимости
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapSceneGeometryTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0002-TK-0001-frame-state-update

## Публичный API

План: TacticalMapSceneBuilder.Prepare(TacticalMapFrameState frame, TacticalMapViewInput view) -> TacticalMapSceneGeometry. View input включает все revision keys.

## Порядок реализации

1. Prepare принимает frame и immutable view input: camera, viewport, текущий layout, settings, locale и selection. Использовать существующие projectors, не копировать физику.
2. Подготовить видимые markers, clusters, trajectories, labels и hit candidates в детерминированном порядке слоёв. Вычитать camera в double перед float conversion.
3. Зафиксировать ownership и lifetime буферов: опубликованная геометрия валидна до окончания Draw/input/capture. На этом шаге shadow/adaptor seam; painter переключается в TK-0003.

## Критерии приёмки — AC-0002

- [x] `Scene_preparation_is_deterministic`: Одинаковые входы дают одинаковую геометрию и hit candidates.
- [x] `Scene_geometry_matches_current_render_contract`: Сохранены слои, viewport clipping, пути и label priorities.
- [x] `Large_coordinates_keep_camera_relative_precision`: При больших world coordinates близкие объекты сохраняют различимое положение.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapSceneGeometryTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneGeometry.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSceneBuilder.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapSceneGeometryTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09

EpicExecutionPrompt authorizes implementation and expansion. Add `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` for the production shadow publication after existing projectors and label placement complete. TK-0003 switches painting to the prepared publication. Existing projector output is retained, never resampled by the shadow builder.

Add `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailGeometry.cs` for a read-only cache key match needed to exclude stale offscreen trail entries from scene publication.

## Execution evidence 2026-10-09

Production shadow/adaptor publication added after the existing projectors and label preparation. Immutable scene includes frame identity, camera-relative markers and click candidates, layout obstacles, geometry settings, locale/selection, clusters, labels, projected/world paths and visible trail segments. Cached trail keys are verified before copying; retained previous scenes survive subsequent renders unchanged. Painter migration remains TK-0003.

Checks: new deterministic/contract/large-coordinate tests 3/3; full Client 1807/1807; final metadata addition rechecked 3/3; Release build and scoped format/diff checks pass. Native/GPU comparison OPEN.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.
