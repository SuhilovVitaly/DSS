# Tactical button assets

Design standard: [Trade-based tactical buttons](../03-Design/TacticalButtons/STYLE.md).

The 43 button PNGs use the trade window's navy / off-white / cyan palette.
Keep filenames and dimensions stable. `.active.png` means hover, not an executing command.
Masters and the contact sheet: `DSS-Images/temp/tactical-buttons-20260913` (beside DSS).
The manifest and export script are documented in the design standard.


## EP-0007 — актуальное дополнение от 2026-10-04

GameSessionScreen.Countermeasures.cs проецирует CountermeasureSnapshot/DefenseSnapshot; CombatJournalPanel отображает CombatJournalEntry. CombatEffectStore отделяет физическое состояние от monotonic срока эффектов и устанавливает watermark при загрузке. CommandsPanel содержит шесть групп, ObjectInfoPanel показывает frozen breakdown. [Технический контракт и приёмка](../04-Engineering/CountermeasureCombat.md).

## EP-0005 production pipeline и диагностика

`SnapshotBuffer.PredictionAt(timestamp) → TacticalMapStateUpdater.Update → TacticalMapFrameState → Prepare → TacticalMapSceneGeometry → TacticalMapRenderer.Draw → presented frame`.

Update владеет prediction/reconciliation и immutable poses. `GameSessionScreen` подготавливает layout, trajectories, labels, hit candidates и записывает vector display list через `TacticalMapPaintRecorder`. `TacticalMapRenderer` воспроизводит `SKPicture` и явные UI animations; prediction, session calls, layout и file I/O в Draw отсутствуют. Подготовка панелей читает тот же snapshot/speed; input-команды вне Render читают актуальный authoritative snapshot.

`TacticalMapSceneCache` хранит одну сцену с раздельными dependency revisions. `TacticalMapSpatialIndex` ограничен 10000 объектами; при превышении используется корректный full-frame fallback, крупные bounds учитываются отдельно. Неизменные paused кадры не перестраивают geometry/paint. Полного нулевого расхода CPU/allocations это не обещает.

Обычная история следов: 10 секунд, ring 16–512 точек, detailed work для important/visible контактов и резерва 64. `ObjectTrailBuffer.Freeze()` использует страницы по 16 точек с copy-on-write, поэтому capture сохраняет показанную историю при следующем update без полного копирования каждого следа. Боевые подтверждённые следы сохраняют отдельный контракт.

`AsyncImageCache`: один worker decoder, максимум 16 pending и 128 ready entries; Prepare/Layout забирает готовые результаты, Draw не открывает файлы. Dispose отменяет очередь и освобождает поздние bitmap. Snapshot writer ограничен одним owned DTO в работе и пишет JSON вне render thread. Удаление экрана из `ScreenStack` освобождает его ресурсы; временный Push/modal сохраняет экран для возврата. Fonts общего владельца не освобождаются экраном.

Кнопка Snapshot сохраняет schema v3 последнего представленного кадра: FrameId, camera/viewport, authoritative baseline, rendered poses, замороженные trails, фактически показанные trajectories, reconciliation и профиль. До первого кадра `HasPresentedFrame` false. Это diagnostic schema, не версия игрового save.

`PipelineMetricsEnabled` добавляет Update/Prepare/Draw, geometry/paint reuse, spatial updates и represented path points. При выключении дополнительных metrics нет новых timestamp reads/metrics allocations; существующий bounded frame profiler остаётся отдельным механизмом. Четыре timestamp reads на включённый кадр проверены тестом. `representedPathPoints` — число представленных точек, не число обращений к predictor.

Воспроизведение: [Performance tool README](../../tools/DeepSpaceSaga.Performance/README.md). [Методика, бинарный hash, raw JSON/PNG и открытые gates](../../Board/EP-0005-optimization/PerformanceEvidence.md). Реальные GPU execution и scanout отдельно не измерены; native scripts не являются human manual acceptance.
