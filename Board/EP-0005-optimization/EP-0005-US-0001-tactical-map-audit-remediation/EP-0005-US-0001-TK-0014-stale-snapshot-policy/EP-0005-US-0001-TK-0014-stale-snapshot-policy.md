---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
ticket: EP-0005-US-0001-TK-0014-stale-snapshot-policy
title: "Ограничить прогноз при зависшем потоке снимков"
stage: draft
layer: client
depends_on: []
files_touched: 4
serves: [AC-0014]
source_finding: R03
evidence_status: risk-to-verify
priority: P2
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005-US-0001-TK-0014-stale-snapshot-policy

## Зачем

Риск: задержка без исключения не ограничивает LatestPrediction delta. Зарегистрированная Failure уже переводит Client в Speed0.

## Решения и полномочия

Пользователь поручил создать story и тикеты. Этот документ — план; он не означает разрешения на реализацию, commit или push. Исправления F01 и F03 выполнены отдельным прямым поручением и не входят в этот тикет. Канонический контракт: `Documentation/01-Requirements/EngineRequirements.md`; архитектурные рекомендации не заменяют его.

## Предположения и проверка основания

Сначала воспроизвести риск и записать evidence. Если риск не подтверждается, сохранить regression/probe и обоснованный результат not-reproduced; оптимизацию не внедрять без evidence.

Целевой результат: Ограничить прогноз при зависшем потоке снимков. Значения новых порогов, явно названные draft assumption, подлежат согласованию при утверждении тикета.

## Контекст и разрешённые файлы

Все пути от корня DSS. Это полный write allowlist; прочие файлы доступны только для чтения. Новые тесты располагаются только в перечисленных файлах.

- `src/DeepSpaceSaga.Client/SnapshotBuffer.cs` — существует
- `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/SnapshotBufferTests.cs` — существует
- `tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs` — существует

Контекст для чтения: `GameSessionScreen.Render`, `GameSessionScreen.UpdateObjectRenderStates`, `GameSessionScreen.MapView.cs`, `SnapshotBuffer`, `CameraState`, `FutureTrajectoryProjector`, `NavigationTrajectoryProjector`, соответствующие существующие tests. Точные сигнатуры перед реализацией сверить с текущим кодом.

## Зависимости

- Нет зависимостей внутри story.

## Публичный API

План: SnapshotPrediction получает client-only IsStale и SnapshotAgeMs; transport DTO неизменны.

## Порядок реализации

1. Ввести receipt age/IsStale. Draft assumption: предел 2000 ms real time после последнего snapshot, независимо от speed.
2. После лимита не увеличивать visual delta; сообщать stale в существующей info panel, не отправлять gameplay pause и не менять authoritative clock.
3. Fresh snapshot восстанавливает prediction через reconciliation. Покрыть speed changes, pause, monotonic time и отсутствие дублирования Failure flow.

## Критерии приёмки — AC-0014

- [ ] `Stalled_snapshot_stream_caps_prediction`: Зависший поток не уводит marker бесконечно далеко.
- [ ] `Stale_age_is_real_time_at_all_speeds`: Speed4 не меняет допустимый real age.
- [ ] `Fresh_snapshot_recovers_from_stale_state`: Свежий снимок восстанавливает карту без rewind authoritative state.

## Инварианты и границы

- Authoritative Engine state, gameplay commands, transport и save format не изменяются, если это прямо не предусмотрено API выше.
- Визуальные исправления не изменяют motion model, RNG, время мира и конечную точку authoritative route.
- Независимые изменения working tree сохраняются. Не решать соседние findings в этом тикете.
- Ошибки ресурсов, пустой viewport/снимок и смена сессии обрабатываются в пределах затронутого контракта.

## Проверки и Definition of Done

1. Добавить/обновить именованные проверки выше в `tests/DeepSpaceSaga.Client.Tests/SnapshotBufferTests.cs`, `tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs`. Проверять наблюдаемое поведение, а не копию реализации.
2. `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore` — все тесты Client; для изменения shared Motion дополнительно Motion/Engine suites (изменение этих проектов здесь не разрешено).
3. `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --no-restore`.
4. `dotnet format whitespace DeepSpaceSaga.sln --verify-no-changes --no-restore --include src/DeepSpaceSaga.Client/SnapshotBuffer.cs src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs tests/DeepSpaceSaga.Client.Tests/SnapshotBufferTests.cs tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs` — запускать из solution directory; при нескольких solutions явно указать актуальную `.sln`/`.slnx`.
5. `git diff --check`; UTF-8 и CRLF в изменённых файлах. Проверить diff только allowlist, посторонние изменения не нормализовать.
6. Для визуального контракта: ручной smoke на реальном GPU, viewport 1920x1080 и 3440x1440, scale 0.8/1/1.2/1.5, pause/resume, resize и modal. Автотесты не подменяют этот пункт.
7. Записать commands/results, ограничения и screenshot/metrics evidence при необходимости. Нет flaky assertions по elapsed wall time в unit tests.

## Проверка самодостаточности

В тикете указаны причина, границы, API, зависимости, allowlist, шаги, наблюдаемые критерии и проверки. Cross-ticket dependencies названы явно. Планирование завершено; реализация и runtime/UI validation не выполнялись в рамках этого draft.
