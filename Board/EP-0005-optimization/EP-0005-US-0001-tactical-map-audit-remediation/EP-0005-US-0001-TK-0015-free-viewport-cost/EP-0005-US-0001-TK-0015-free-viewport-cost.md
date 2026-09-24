---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0015-free-viewport-cost
title: "Проверить стоимость поиска свободной области"
stage: draft
layer: client
depends_on: ["EP-0005-US-0001-TK-0008-layout-before-map"]
files_touched: 3
serves: [AC-0015]
source_finding: R04
evidence_status: risk-to-verify
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0015-free-viewport-cost

## Зачем

Риск: четыре вложенных перебора границ плюс obstacles checks повторяются при layout invalidation. Реальная стоимость не измерена.

## Решения и полномочия

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Сначала воспроизвести риск и записать evidence. Если риск не подтверждается, сохранить regression/probe и обоснованный результат not-reproduced; оптимизацию не внедрять без evidence.

Целевой результат: Проверить стоимость поиска свободной области. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapViewGeometry.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/MapViewGeometryTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0001-TK-0008-layout-before-map

## Публичный API

No public API change.

## Порядок реализации

1. Измерить 0/4/8/16/32 obstacles и frequent resizing; exhaustive oracle сохранить только в tests.
2. Дедуплицировать координаты и применять layout revision. При превышении бюджета заменить поиск sweep/interval алгоритмом с тем же результатом.
3. Нет свободного места -> empty; равные площади выбираются по top,left,bottom,right. Измерить counters и p50/p95/p99.

## Критерии приёмки — AC-0015

- [ ] `Free_viewport_matches_exhaustive_oracle`: Для fixed/random small fixtures результат равен exhaustive oracle.
- [ ] `Fully_occluded_viewport_is_empty`: Полностью перекрытый viewport не выдаётся свободным.
- [ ] `Repeated_layout_reuses_free_viewport`: Одинаковый layout не запускает поиск; результат риска подтверждён evidence.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/MapViewGeometryTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapViewGeometry.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs tests/DeepSpaceSaga.Client.Tests/MapViewGeometryTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
