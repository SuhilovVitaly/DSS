---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0008-layout-before-map
title: "Вычислять UI layout до геометрии карты"
stage: implemented
layer: client
depends_on: []
files_touched: 5
serves: [AC-0008]
source_finding: F10
evidence_status: audit-finding
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0008-layout-before-map

## Зачем

AvailableMapRect вызывается до обновления panel rects в Render и использует предыдущий layout.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Вычислять UI layout до геометрии карты. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapLayoutOrderTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

No public API change.

## Порядок реализации

1. Выделить Layout control panels: текущий viewport/UI scale/data без Draw.
2. Вычислить все panel rects и AvailableMapRect до map prepare. Draw только использует рассчитанные rects.
3. Input применяет ту же layout revision, first frame не зависит от предыдущего размера.

## Критерии приёмки — AC-0008

- [x] `Resize_uses_current_panel_obstacles`: Первый frame после resize/collapse не размещает labels под UI.
- [x] `Ui_scale_change_updates_hit_rects_before_draw`: UI scale обновляет hit rects и available map одновременно.
- [x] `First_frame_has_complete_layout`: Первый frame имеет полный layout без предыдущего Render.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapLayoutOrderTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapLayoutOrderTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09
Add GameSessionScreen.Layout.cs for overlay layout and Controls/CombatJournalPanel.cs because its rectangle also obstructs the map. Split CommandsPanel/ObjectInfoPanel layout from Draw while retaining Render wrappers for existing consumers. Screen computes current UI rectangles before hit testing and label geometry.

## Execution evidence 2026-10-09
Implemented current-frame layout for all map-obstructing panels, toolbar and time display before hit testing and label placement. Draw consumes prepared panel rectangles; selected/hover data relayout precedes geometry. Existing public Render wrappers remain compatible.
Regression reproduced: all four scale cases failed on the first frame with empty panel rectangles before the fix. After fix: focused 46/46; full Client 1763/1763; Release build 0 warnings/errors; scoped whitespace verification and git diff --check pass. Tests cover first frame, resize, UI scale 0.8/1/1.2/1.5 and panel collapse, asserting identical layout at map preparation and completed frame.
Self-review: Engine/Motion/session/save unchanged; current-frame snapshot also drives time text. Native GPU/manual matrix NOT RUN here, remains OPEN for integrated epic validation.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.

## AC-to-test naming map

Planned names are AC labels; actual tests consolidate related transitions without dropping assertions:

- `Resize_uses_current_panel_obstacles` → `First_frame_resize_and_scale_have_current_layout` (combined behavioral fixture).
- `Ui_scale_change_updates_hit_rects_before_draw` → `First_frame_resize_and_scale_have_current_layout` (combined behavioral fixture).
- `First_frame_has_complete_layout` → `First_frame_resize_and_scale_have_current_layout` (combined behavioral fixture).
