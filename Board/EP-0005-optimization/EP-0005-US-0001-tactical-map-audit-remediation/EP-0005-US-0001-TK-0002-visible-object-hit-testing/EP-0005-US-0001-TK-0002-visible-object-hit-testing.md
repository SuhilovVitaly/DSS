---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0002-visible-object-hit-testing
title: "Исключить UI и невидимые маркеры из наведения"
stage: implemented
layer: client
depends_on: []
files_touched: 2
serves: [AC-0002]
source_finding: F04
evidence_status: audit-finding
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0002-visible-object-hit-testing

## Зачем

RecomputeActiveObjectId не исключает панели, FindNearestObjectId допускает невидимые marker около края. Воспроизведены UNDER_PANEL и OFFSCREEN.

## Решения и полномочия

Историческая карточка планирования; исполнение, расширение scope, commit и push разрешены запросом 2026-10-09 по EpicExecutionPrompt.md. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Исключить UI и невидимые маркеры из наведения. Исходные draft assumptions рассмотрены при исполнении; принятые технические решения записаны в execution evidence и не выдаются за отдельное пользовательское одобрение.

## Контекст и разрешённые файлы

Все пути от корня DSS. Ниже исходный scope; расширения, необходимые для исполнения и разрешённые EpicExecutionPrompt.md, записаны в execution sections.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapInteractionTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

No public API change.

## Порядок реализации

1. Перед hit-test исключать те же панели, которые поглощают click, с одним преобразованием raw/UI scale.
2. Общий предикат допускает только marker core, пересекающий viewport; clustered objects исключены. Декоративный halo не расширяет доступность.
3. Сохранить радиус 30 raw px и priority Station > Player > NpcShip > остальные; затем distance и Ordinal ID.

## Критерии приёмки — AC-0002

- [x] `Hover_over_panel_does_not_activate_map_object`: UI scale 0.8/1/1.2/1.5: объект под панелью не активируется.
- [x] `Offscreen_marker_is_not_interactive`: Полностью невидимый marker недоступен, частично видимый доступен.
- [x] `Visible_hit_priority_is_stable`: Hover и click совпадают при одинаковых входах.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapInteractionTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapInteractionTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Историческая проверка планирования завершена. Фактическая реализация и проверки записаны ниже; native/manual acceptance отделена от автоматических AC.

## Execution scope 2026-10-09
Additional test file: tests/DeepSpaceSaga.Client.Tests/GameSessionObjectInteractionTests.cs. Its hover-preference fixture uses a point under the mechanics panel; move the second contact to open map space while preserving all assertions. Authorized by EpicExecutionPrompt.md to reconcile obsolete fixtures without weakening the intended contract.

## Execution evidence

Five regressions reproduced before the fix: panel hover at UI scales .8/1/1.2/1.5 and a fully offscreen marker. The shared object hit test now excludes UI and requires the actual marker core (including compact/combat radii) to intersect the viewport. Partial cores remain selectable; 30 raw pixels, type priority and ordinal ties are preserved.

Full Client run: 1743 passed, one outdated hover-preference fixture failed (ep5-us1-tk2-client-final.trx). After moving that fixture into open map space, focused regression passed 9/9 including all eight new cases and the repaired existing test. No production changes followed that full run. Release build: zero warnings/errors; scoped whitespace verification and git diff --check passed. Review covered raw/UI coordinate conversion, compact marker sizes, hull-bar selection and existing click priority. Native/manual acceptance OPEN until integrated evidence.

Current status: implementation and automated checks recorded in execution evidence; [publication registry](../../ImplementationStatus.md). [Final native/performance evidence](../../PerformanceEvidence.md) supersedes earlier NOT RUN notes only for the executed scripted cases. Human manual acceptance remains OPEN.
