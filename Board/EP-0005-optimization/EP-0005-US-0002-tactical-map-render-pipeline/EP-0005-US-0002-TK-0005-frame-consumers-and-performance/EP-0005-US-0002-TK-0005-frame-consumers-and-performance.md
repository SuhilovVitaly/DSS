---
epic: EP-0005-optimization
story: EP-0005-US-0002-tactical-map-render-pipeline
ticket: EP-0005-US-0002-TK-0005-frame-consumers-and-performance
title: "Согласовать input, capture и измерения с показанным кадром"
stage: implemented
layer: client
depends_on: ["EP-0005-US-0002-TK-0004-revision-driven-scene-cache"]
files_touched: 5
serves: [AC-0005]
source_finding: A05
evidence_status: automated-validated-native-open
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0002-TK-0005-frame-consumers-and-performance

## Зачем

Hit-test и capture должны описывать тот же кадр, который видит игрок. Нужны отдельные измерения Update/Prepare/Draw и итогового времени кадра.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Согласовать input, capture и измерения с показанным кадром. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Diagnostics.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSnapshot.cs` — существует
- `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapPipelineIntegrationTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0002-TK-0004-revision-driven-scene-cache

## Публичный API

План: диагностические stage metrics и presented FrameId. Existing transport/gameplay API неизменны.

## Порядок реализации

1. Input и capture читают последний представленный frame/scene ID, а не следующий подготовленный кадр. Проверить resize, modal, pause, locale и teardown.
2. Расширить существующую диагностику длительностями и counters Update/Prepare/Draw; отключённые counters не создают per-frame allocations. Не сохранять файлы на render thread.
3. Release benchmark: 500/5000 объектов, 1920x1080 и 3440x1440, paused/live/zoom/pan/route modes. Сохранить p50/p95/p99 CPU time, allocations, GC, path samples и cache builds. Отдельно реальный GPU manual smoke: цель 80 FPS, бюджет 12.5 ms; при превышении зафиксировать остаточный bottleneck.

## Критерии приёмки — AC-0005

- [x] `Input_and_capture_use_presented_frame`: ID и позиции input/capture совпадают с показанным кадром.
- [x] `Pipeline_handles_resize_modal_pause_and_disposal`: Integration fixtures покрывают переходы без stale geometry и ресурсов.
- [x] `Stage_instrumentation_has_bounded_overhead`: Операционные counters проверены автоматически; время и FPS подтверждаются benchmark evidence, не flaky unit assertions.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapPipelineIntegrationTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Diagnostics.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSnapshot.cs src/DeepSpaceSaga.Client/UI/SkiaWindow.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapPipelineIntegrationTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09

EpicExecutionPrompt authorizes expansion to the GameSession files `GameSessionScreen.MapView.cs`, `GameSessionScreen.GeometryCache.cs`, `GameSessionScreen.Combat.cs`, `GameSessionScreen.Profiling.cs`, `TacticalMapFrameProfile.cs`, `TacticalMapFrameState.cs`, `TacticalMapStateUpdater.cs`, `ObjectTrailBuffer.cs`, plus `tools/DeepSpaceSaga.Performance/Program.cs`, new `TacticalPipelineEvidence.cs`, and `README.md`. Existing consumer tests may be updated only where the explicit last-presented-frame contract changes pre-render interaction assumptions, with reasons recorded. Board benchmark/native reports belong to this ticket evidence. Frozen trail views use small copy-on-write pages, preserving exact diagnostic histories without copying every historical point on every frame.

Also allow `GameSessionScreen.Layout.cs`: freeze speed/panel data to the captured prediction during preparation; command/input reads outside Render retain current authoritative state.

## Execution evidence 2026-10-09

Implemented last-presented scene input/capture, owned reconciliation and copy-on-write frozen trail histories, coherent preparation snapshot/speed, and opt-in Update/Prepare/Draw counters. Disabled additional counters read no timestamps and create no additional metrics allocations. Commands outside Render continue to consume current authoritative state.

Validation: `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` passed 1826/1826 after the final snapshot coherence repair; four new integration tests passed. `dotnet build tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj -c Release --no-restore` passed with 0 warnings/errors (includes Client). Scoped `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include <changed C# files>` and `git diff --check` passed.

Final measured binary: 20 CPU/raster cases and 16 native window cases, 600 measured frames each. All 80 scripted native actions passed. Native p99 presentation 49.742–116.265 ms: 80 FPS gate FAILED/OPEN. Human manual and native modal/resize/locale acceptance NOT RUN. High-scale panel accessibility remains unaccepted. CPU zoom/allocation and legacy zero-cadence route costs remain documented limitations. Full methodology, raw JSON/PNG links and hashes: [PerformanceEvidence](../../PerformanceEvidence.md). [Story self-review](../../US-0002-Review.md). No shared simulation/save changes.

Implementation and automated ACs complete; performance/manual acceptance remains open. Publication is recorded in the registry after exact remote SHA verification.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.
