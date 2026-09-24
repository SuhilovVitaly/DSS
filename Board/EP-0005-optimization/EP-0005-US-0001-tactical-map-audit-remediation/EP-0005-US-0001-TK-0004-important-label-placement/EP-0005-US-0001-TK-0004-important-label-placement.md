---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0004-important-label-placement
title: "Разместить важные подписи без пересечений"
stage: draft
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

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Разместить важные подписи без пересечений. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

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

- [ ] `Important_labels_do_not_overlap_when_docked`: Тексты станции и корабля различимы при совпадающих позициях.
- [ ] `Crowded_labels_use_bounded_fallback`: На маленьком экране fallback не падает и не выходит за viewport.
- [ ] `Label_placement_is_snapshot_order_independent`: Перестановка snapshot.Objects не меняет расположение.

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

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
