---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0004-important-label-placement
title: "Разместить важные подписи без пересечений"
stage: implemented
layer: client
depends_on: []
files_touched: 3
serves: [AC-0004]
source_finding: F06
evidence_status: audit-finding
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0004-important-label-placement

## Зачем

OverlapsExistingPlaque не применяется к important labels; после clamp player, selected и target могут иметь одинаковые прямоугольники.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Разместить важные подписи без пересечений. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs` — существует

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

No public API change.

## Порядок реализации

1. Приоритет размещения: selected, navigation, player с дедупликацией ID; второстепенные сортируются Ordinal.
2. Перебирать фиксированный ограниченный набор позиций вокруг marker; проверять viewport, UI и уже занятые rects.
3. Draft fallback при отсутствии места: одна компактная групповая плашка важных контактов с явными связями; не скрывать цель молча. Сохранить F01 truncation.

## Критерии приёмки — AC-0004

- [x] `Important_labels_do_not_overlap_when_docked`: Тексты станции и корабля различимы при совпадающих позициях.
- [x] `Crowded_labels_use_bounded_fallback`: На маленьком экране fallback не падает и не выходит за viewport.
- [x] `Label_placement_is_snapshot_order_independent`: Перестановка snapshot.Objects не меняет расположение.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs tests/DeepSpaceSaga.Client.Tests/ObjectLabelTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09
Add GameSessionScreen.cs to pass explicit selected/navigation identities into label placement; the existing importance predicate cannot express required ordering. Group fallback keeps the highest-priority name and an explicit count, with leader links for every grouped contact. All text remains knowledge-safe and viewport-clipped.

## Execution evidence

Docked labels overlapped in the pre-fix regression. Placement now sorts selected/navigation/player before other important contacts and ordinal secondary IDs, checks at most 25 candidates after smoothing/clamping, and constrains important text to the free region. If those candidates cannot fit, a single highest-priority group plaque with +N count has explicit leaders from every grouped contact. No hidden object identity enters text. Existing Unicode-safe truncation and hull rendering remain.

Focused label/bounds tests: 56/56. Release build: zero warnings/errors. Full scoped formatting and git diff --check passed. Review checked snapshot-order independence, viewport bounds, group draw-once behavior, leader completeness and layer order. Native visual matrix remains OPEN for integrated validation.

Full Client regression: 1751/1751 passed (ep5-us1-tk4-client.trx). Implementation complete; native acceptance OPEN.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.
