---
epic: EP-0005-optimization
story: EP-0005-US-0002-tactical-map-render-pipeline
ticket: EP-0005-US-0002-TK-0003-read-only-map-painter
title: "Сделать рисование потребителем готовой сцены"
stage: draft
layer: client
depends_on: ["EP-0005-US-0002-TK-0002-scene-geometry-prepare"]
files_touched: 6
serves: [AC-0003]
source_finding: A03
evidence_status: architecture-proposal
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0002-TK-0003-read-only-map-painter

## Зачем

Повторная отрисовка должна воспроизводить готовый кадр и не продвигать presentation state.

## Решения и полномочия

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Сделать рисование потребителем готовой сцены. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapRenderer.cs` — создать в этом тикете / после зависимости
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/GridRenderer.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapRendererTests.cs` — создать в этом тикете / после зависимости

Исключение из ориентира <=5 файлов: 6 файлов образуют один атомарный контракт этой находки. Изменение должно согласованно охватить потребителей и проверки; разделение оставило бы промежуточный несовместимый контракт. Расширение allowlist за этот список требует обновления draft.

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0002-TK-0002-scene-geometry-prepare

## Публичный API

План: TacticalMapRenderer.Draw(SKCanvas canvas, TacticalMapSceneGeometry scene, double uiTimeSeconds).

## Порядок реализации

1. Переключить GameSessionScreen на Update -> Prepare -> Draw. Painter получает готовую scene и UI time для pulse/hover анимации.
2. Перенести draw-only операции из прежних renderer методов. Запретить внутри Draw prediction, route sampling, label placement, mutation hit targets, session calls и I/O.
3. Сохранить порядок слоёв, цвета, alpha, clipping и resource disposal. Добавить recording-canvas/counter проверки и ручное сравнение raster/GPU кадров.

## Критерии приёмки — AC-0003

- [ ] `Drawing_same_scene_does_not_mutate_state`: Повтор Draw оставляет frame/scene/revisions неизменными.
- [ ] `Draw_does_not_run_geometry_or_io`: Счётчики запрещённых операций равны нулю.
- [ ] `Painter_preserves_layer_order_and_clipping`: Golden/recording fixtures покрывают пересечения marker/path/label/UI.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapRendererTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs src/DeepSpaceSaga.Client/UI/GridRenderer.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapRendererTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
