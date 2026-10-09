---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0013-reconciled-route-join
title: "Проверить стык сглаженного корабля и Approach"
stage: implemented
layer: client
depends_on: []
files_touched: 4
serves: [AC-0013]
source_finding: R02
evidence_status: risk-to-verify
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0013-reconciled-route-join

## Зачем

Риск: correction меняет marker, но начало Approach берётся из PredictPose(route,elapsed). Нужен integration reproduction.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Сначала воспроизвести риск и записать evidence. Если риск не подтверждается, сохранить regression/probe и обоснованный результат not-reproduced; оптимизацию не внедрять без evidence.

Целевой результат: Проверить стык сглаженного корабля и Approach. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/NavigationTrajectoryProjector.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/ApproachRouteProjectionTests.cs` — существует

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

No public API change.

## Порядок реализации

1. Создать new-snapshot/resume с ненулевой visual correction и confirmed route; сравнить marker и начало линии.
2. При подтверждении добавить presentation-only join от rendered pose к неизменной authoritative route. Не сдвигать endpoint или все samples.
3. Сохранить maneuverPointCount и физическую семантику; connector исчезает после reconciliation. При not-reproduced не менять renderer.

## Критерии приёмки — AC-0013

- [x] `Reconciliation_keeps_marker_joined_to_route`: В ходе correction marker связан с маршрутом.
- [x] `Visual_join_preserves_authoritative_endpoint`: Route endpoint остаётся точным.
- [x] `Resume_connector_disappears_after_correction`: Evidence показывает reproduce/fix либо not-reproduced.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs`, `tests/DeepSpaceSaga.Client.Tests/ApproachRouteProjectionTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/NavigationTrajectoryProjector.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs tests/DeepSpaceSaga.Client.Tests/ApproachRouteProjectionTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Reproduction and scope 2026-10-09
Both integration probes reproduce a >1-world-unit gap between the captured rendered marker and the drawn authoritative route, on fresh snapshot and resume. Add a separate presentation-only two-point navigation-join, leaving every original route sample and maneuverPointCount unchanged. It disappears when rendered pose matches route start. Expand scope to GameSessionScreen.GeometryCache.cs to retain join geometry and maneuver boundary. Review also found the cache ignored the projector return value for legacy Approach (which can return a different list); repair this confirmed integration defect in the same navigation path and cover it with regression.

## Execution evidence 2026-10-09
Separate cached navigation-join geometry connects rendered pose to the unchanged analytical start. The painter and detached capture consume this same connector; profiling attributes the first visible point to the marker. Endpoint, route samples and maneuver boundary remain unchanged. Cache now retains the projector-returned list for legacy Approach and forwards maneuverPointCount to the painter.
Before fix both new-snapshot/resume probes failed because the marker differed from the line start and no connector was drawn. After fix: navigation/smoothness/route tests 79/79; full Client 1787/1787; Release 0 warnings/errors; scoped whitespace verification and diff check pass. Tests assert actual captured draw paths, exact endpoint, connector removal after correction, unchanged full sample sequence/maneuver boundary, and actual legacy Approach rendering. No Motion/Engine physics changes. Native/manual matrix remains OPEN.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.

## AC-to-test naming map

Planned names are AC labels; actual tests consolidate related transitions without dropping assertions:

- `Visual_join_preserves_authoritative_endpoint` → `Visual_join_preserves_every_authoritative_sample_and_maneuver_boundary` (combined behavioral fixture).
- `Resume_connector_disappears_after_correction` → `Reconciliation_keeps_marker_joined_to_route_and_join_expires` (combined behavioral fixture).
