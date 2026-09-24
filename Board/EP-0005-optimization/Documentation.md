---
epic: EP-0005-optimization
title: Оптимизация тактической карты
stage: draft
created: 2026-09-23T20:29:35Z
revision: 1
---

# EP-0005 — Оптимизация тактической карты

## Основание и объём

Поручение пользователя: «реши 1 и 3 остальные в папке D:\DeepSpaceSaga\DSS\Board\EP-0005-optimization сделай юзерстори с тикетами на каждую найденную проблему и вторую юзерстори с твоими рекомендациями разделить обновление состояния, подготовку геометрии и рисование.».

Аудит охватывал tactical map pipeline в текущем рабочем дереве DSS: snapshot prediction/reconciliation, trails/forecast/routes, clusters/labels, camera/layout, hit-test, diagnostics и lifecycle. Подтверждённые находки обозначены F01–F13; дополнительные риски — R01–R04. Это перечень обнаруженного в проверенном объёме, а не утверждение об отсутствии иных ошибок.

## Выполнено по прямому поручению

- **F01 / P1:** длинные подписи и узкий viewport могли вызывать исключение Math.Clamp. Ширина plaque ограничена viewport, текст сокращается с учётом grapheme boundaries, диапазоны clamp безопасны для малых размеров, рисование текста clip-ится plaque.
- **F03 / P2:** generic turning forecast повторно проходил физику от начала для каждого sample. Теперь shared LinearMotionPredictor вычисляет samples одним проходом в reusable buffer; одинаковое состояние использует кэш, terminal state переиспользуется. Approach/Orbit и custom predictor сохраняют прежний fallback.

Production files этих исправлений: `LinearMotionPredictor.cs`, `FutureTrajectoryProjector.cs`, `ObjectLabelLayout.cs`, `ObjectLabelRenderer.cs`. Добавлены `TurnPositionSamplingTests.cs`, `TurnTrajectorySamplingTests.cs`, `TacticalMapLabelBoundsTests.cs`. Оставшиеся findings не исправлялись в рамках этого поручения.

## Проверки выполненных исправлений

- Motion suite: 118 passed.
- Client suite: 1346 passed.
- Engine suite: 826 passed.
- Client Release build: 0 warnings, 0 errors.
- Release synthetic CPU probe: generic turning forecast с меняющейся позицией около 0.032 ms (p95 0.042 ms), прежний вариант около 3.53 ms (p95 5.74 ms). Cached state около 0.007 ms, 0 B/call. Это локальный microbenchmark, не обещание FPS игры; временный harness не является постоянным performance gate.
- Scoped dotnet format --verify-no-changes для семи изменённых/новых source/test файлов: passed. git diff --check: passed. CRLF и структура Board (24 Markdown-файла, 20 ticket IDs, ссылки, DAG, allowlists, coverage): passed. Реальный GPU smoke не проводился.

## Планирование

1. [Устранение находок аудита тактической карты](EP-0005-US-0001-tactical-map-audit-remediation/EP-0005-US-0001-tactical-map-audit-remediation.md): 11 оставшихся подтверждённых находок и 4 риска, отдельный тикет на каждую позицию.
2. [Разделение состояния, геометрии и рисования](EP-0005-US-0002-tactical-map-render-pipeline/EP-0005-US-0002-tactical-map-render-pipeline.md): 5 последовательных тикетов архитектурного перехода.

Полная карта: [Tickets.md](Tickets.md). Все story и tickets имеют stage draft. Планирование не означает разрешения на реализацию, commit или push.

## Предлагаемая архитектура

`SnapshotPrediction + monotonic clock -> Update -> TacticalMapFrameState -> Prepare(view/layout/settings) -> TacticalMapSceneGeometry -> Draw(canvas, uiTime)`.

Update единожды формирует presentation state кадра. Prepare рассчитывает видимость, clusters, trajectories, labels и hit candidates по явным revision keys. Draw читает готовую геометрию. Input и diagnostic capture потребляют последний представленный кадр. Кэши имеют ограниченный размер и явное владение ресурсами.

Практический первый выбор — сохранить текущий SkiaSharp/OpenGL backend и устранить лишние CPU вычисления. Offscreen GPU layers, instancing и перенос подготовки на worker thread рассматривать только после измерений: они добавляют lifetime/synchronization стоимость и пока не обоснованы данными аудита. Целевой бюджет из EngineRequirements — 12.5 ms/frame при 80 FPS; окончательная оценка требует GPU измерений на целевых конфигурациях.

## Канонические ссылки

- `Documentation/01-Requirements/EngineRequirements.md` — требования движка и карты.
- `Documentation/00-Process/DocumentationSystem.md` — структура документации.
- Текущий код и tests — фактическое состояние реализации; draft API в тикетах обозначает предлагаемое изменение.
