---
epic: EP-0005-optimization
story: EP-0005-US-0002-tactical-map-render-pipeline
ticket: EP-0005-US-0002-TK-0002-scene-geometry-prepare
title: "Выделить подготовку геометрии кадра"
stage: draft
layer: client
depends_on: ["EP-0005-US-0002-TK-0001-frame-state-update"]
files_touched: 5
serves: [AC-0002]
source_finding: A02
evidence_status: architecture-proposal
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0002-TK-0002-scene-geometry-prepare

## Зачем

Кластеры, trails, forecast, labels и hit targets вычисляются вперемешку с Draw. Нужен проверяемый снимок готовой сцены.

## Решения и полномочия

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Выделить подготовку геометрии кадра. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

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

- [ ] `Scene_preparation_is_deterministic`: Одинаковые входы дают одинаковую геометрию и hit candidates.
- [ ] `Scene_geometry_matches_current_render_contract`: Сохранены слои, viewport clipping, пути и label priorities.
- [ ] `Large_coordinates_keep_camera_relative_precision`: При больших world coordinates близкие объекты сохраняют различимое положение.

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

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
