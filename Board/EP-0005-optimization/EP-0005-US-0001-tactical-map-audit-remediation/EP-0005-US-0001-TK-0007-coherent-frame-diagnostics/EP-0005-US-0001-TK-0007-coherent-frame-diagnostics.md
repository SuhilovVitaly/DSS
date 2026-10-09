---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0007-coherent-frame-diagnostics
title: "Сохранять диагностику одного завершённого кадра"
stage: implemented
layer: client
depends_on: []
files_touched: 4
serves: [AC-0007]
source_finding: F09
evidence_status: audit-finding
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0007-coherent-frame-diagnostics

## Зачем

LatestPrediction смешивается с renderStates прошлого кадра. Capture Project отличается от показанного viewport forecast: end X=2000 вместо 960.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Сохранять диагностику одного завершённого кадра. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Diagnostics.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSnapshot.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapSnapshotTests.cs` — существует

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

No public API change.

## Порядок реализации

1. При завершении Render зафиксировать frame sequence/timestamp/baseline и реально использованные paths.
2. Capture копирует показанный frame, не читает свежий SnapshotBuffer и не вызывает projectors. Сохранять enabled flags и только показанные траектории.
3. До первого frame возвращать явно обозначенный пустой capture; поднять schemaVersion при изменении значения fields и описать reader compatibility.

## Критерии приёмки — AC-0007

- [x] `Capture_uses_rendered_snapshot_when_new_snapshot_arrives`: Snapshot arrival между Render и click не смешивает baseline/poses.
- [x] `Capture_preserves_drawn_viewport_trajectory`: Saved endpoints равны показанным.
- [x] `Capture_respects_hidden_forecast`: Отключённый forecast не появляется в capture.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapSnapshotTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.Diagnostics.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapSnapshot.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapSnapshotTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09
Add GameSessionScreen.Profiling.cs to seal presented metadata at the actual end of Render. Existing async writer and copied same-render trajectories are baseline functionality; this ticket changes capture to the last completed displayed frame, including frozen camera/input state. Schema 3 adds HasPresentedFrame; schema 2 readers that tolerate additive fields remain structurally compatible, but capture timing semantics change.

Additional scope: GameSessionScreen.MapView.cs removes obsolete queued-capture button flags; GameSessionScreen.Combat.cs registers the actually drawn combat and preview paths as capture inputs. No combat projection math changes.

## Execution evidence

The new regression reproduced next-frame capture (expected snapshot 1, saved snapshot 2). Capture now detaches the last completed Render at click time: frozen prediction/timestamp/frame ID, camera/viewport/selection/cluster metadata, actual rendered poses and already-drawn ordinary/navigation/combat/preview paths. No projector or fresh SnapshotBuffer read occurs during capture. Input-only camera movement before the next Render does not alter the captured projection. Worker DTOs remain independent of subsequent frames.

Schema 3 retains existing state fields and adds hasPresentedFrame; before the first Render it is false, frameId is zero and snapshot/object/path data is empty. Old schema-2 structural readers that ignore new fields can read the shape, but must account for the change from next-render to last-completed-render timing. The repository performance reader does not reject schema 3. Forecast-off stays absent, hidden paths are not reconstructed. Existing profile tests now request capture after the frame they inspect.

Focused snapshot/combat regressions 20/20, Release build zero warnings/errors, scoped formatting and git diff --check passed. Native/manual acceptance OPEN. Existing bounded async writer/atomic rename are prior baseline functionality, not newly claimed work.

Full Client regression 1759/1759 passed (ep5-us1-tk7-client.trx). Diff review confirmed consistent baseline and owned worker data. Implementation complete; native OPEN.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.

## AC-to-test naming map

Planned names are AC labels; actual tests consolidate related transitions without dropping assertions:

- `Capture_preserves_drawn_viewport_trajectory` → `Capture_keeps_one_rendered_frame_when_snapshot_arrives_during_render` (combined behavioral fixture).
- `Capture_respects_hidden_forecast` → `Capture_keeps_one_rendered_frame_when_snapshot_arrives_during_render` (combined behavioral fixture).
