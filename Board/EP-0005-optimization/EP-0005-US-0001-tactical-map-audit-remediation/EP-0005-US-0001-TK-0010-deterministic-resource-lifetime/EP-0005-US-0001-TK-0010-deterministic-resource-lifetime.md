---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0010-deterministic-resource-lifetime
title: "Освобождать ресурсы карты при уничтожении экрана"
stage: implemented
layer: client
depends_on: ["EP-0005-US-0001-TK-0009-nonblocking-render-io"]
files_touched: 9
serves: [AC-0010]
source_finding: F12
evidence_status: audit-finding
priority: P3
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0010-deterministic-resource-lifetime

## Зачем

Нет явного disposal owned paints/paths/filters/bitmaps на окончательном удалении screen. Постоянная утечка не доказана, отсутствует детерминированное освобождение.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Освобождать ресурсы карты при уничтожении экрана. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/GridRenderer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs` — существует
- `src/DeepSpaceSaga.Client/UI/ScreenStack.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapResourceLifetimeTests.cs` — создать в этом тикете / после зависимости

Исключение из ориентира <=5 файлов: 9 файлов образуют один атомарный контракт этой находки. Lifecycle требует согласованного освобождения владельца и всех принадлежащих ему renderer/panel ресурсов. Расширение allowlist за этот список требует обновления draft.

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- EP-0005-US-0001-TK-0009-nonblocking-render-io

## Публичный API

No public API change.

## Порядок реализации

1. Добавить идемпотентный IDisposable всем владельцам карты; перечислить owned/shared resources. Shared fonts/assets не уничтожать вместе с экраном.
2. ScreenStack вызывает Dispose для окончательно удалённых экранов в SetRoot/Pop/Replace/ReplaceAll/DeactivateAll. Push и временный OnDeactivated не уничтожают underlying map.
3. Связать cleanup с async work F11; проверить native handles/memory на 100 циклах создания/закрытия, отдельно от managed GC.

## Критерии приёмки — AC-0010

- [x] `Replacing_root_disposes_owned_resources_once`: Owned resources освобождаются ровно один раз.
- [x] `Pushing_modal_preserves_map_resources`: Возврат из modal сохраняет рабочую карту.
- [x] `Repeated_sessions_release_native_resources`: 100 циклов после завершения workers не дают линейного роста owned handles.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapResourceLifetimeTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/TacticalMapDepthRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs src/DeepSpaceSaga.Client/UI/GridRenderer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/CommandsPanel.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs src/DeepSpaceSaga.Client/UI/ScreenStack.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapResourceLifetimeTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution evidence 2026-10-09
Extended the TK-0009 permanent-removal lifecycle to deterministic, idempotent disposal of screen, grid, label/depth renderers and both panels. Explicit owned-resource lists include paints, paths, filters, dash effects, command bitmaps, trajectory paint sets and lazy reticle textures; decoded image workers retain the cancellation/late-disposal contract. Temporary scale/speed indicator paths and locally created typeface wrappers now have lexical disposal. Shared Xenon fonts, SKTypeface.Default and the process-wide portrait-button paint are not screen-owned and remain alive. SolarSystemLayerRenderer and CombatJournalPanel have no retained native resources; their temporary Skia objects already use using.
Modal OnDeactivated now retains combat graphics resources; permanent Dispose releases them. Screen Render rejects use after disposal. ScreenStack integration from TK-0009 covers SetRoot/Pop/Replace/ReplaceAll/DeactivateAll; production call sites create replacement instances.
Validation: lifecycle/async/stack 23/23; final lifecycle 6/6; full Client 1774/1774; Release 0 warnings/errors; scoped whitespace verification and diff check pass. The 100-cycle raster test retains over 9000 native wrappers, exercises lazy reticle SKImages and verifies every owned handle becomes zero without relying on GC; shared font stays valid. This is native Skia ownership evidence, not real-window/GPU memory evidence. Native window/manual matrix remains OPEN. Self-review found no remaining owned field omitted by the direct-resource traversal.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.
