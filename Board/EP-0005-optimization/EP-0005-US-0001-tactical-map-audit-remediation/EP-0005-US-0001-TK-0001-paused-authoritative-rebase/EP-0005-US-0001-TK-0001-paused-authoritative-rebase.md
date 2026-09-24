---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0001-paused-authoritative-rebase
title: "Обновлять карту после продвижения мира на паузе"
stage: draft
layer: client
depends_on: []
files_touched: 4
serves: [AC-0001]
source_finding: F02
evidence_status: audit-finding
priority: P1
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0001-paused-authoritative-rebase

## Зачем

UpdateObjectRenderStates удерживает pausedVisualAnchors после нового снимка с изменившимися координатами. В аудите authoritative X=100000 оставался rendered X=0. SimulationEngine.Time.TravelStation продвигает MotionTimeMs, сохраняя Speed0.

## Решения и полномочия

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Основание — аудит текущего working tree; номера строк могут сдвинуться. Перед реализацией сверить актуальный код и сохранить независимые изменения.

Целевой результат: Обновлять карту после продвижения мира на паузе. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailStore.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/ObjectTrailTests.cs` — создать в этом тикете / после зависимости

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

No public API change.

## Порядок реализации

1. На Speed0 различать обычный новый снимок при прежнем MotionTimeMs и явное продвижение MotionTimeMs. В первом случае сохранять сглаживание, во втором принимать новые позы и сбрасывать несовместимые corrections.
2. Обновить camera follow, membership и trails в тот же кадр. После скачка времени не соединять старую и новую позицию фиктивной линией полёта; начинать новый участок истории.
3. Удаление/появление контакта работает на паузе; pause/resume не восстанавливает прежний якорь. Формат transport и Engine не менять.

## Критерии приёмки — AC-0001

- [ ] `Paused_time_advance_rebases_pose_camera_and_trail`: При более позднем MotionTimeMs новая поза и camera focus видны за один кадр; trail не изображает телепортацию.
- [ ] `Repeated_paused_snapshot_preserves_visual_anchor`: Снимки при неизменном времени не создают дрожание.
- [ ] `Resume_after_time_advance_does_not_restore_old_pose`: После resume не возвращается старая поза.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs`, `tests/DeepSpaceSaga.Client.Tests/ObjectTrailTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectTrailStore.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs tests/DeepSpaceSaga.Client.Tests/ObjectTrailTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
