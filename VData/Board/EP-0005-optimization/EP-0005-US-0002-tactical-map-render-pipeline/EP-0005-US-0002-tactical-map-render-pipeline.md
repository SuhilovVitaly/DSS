---
epic: EP-0005-optimization
story: EP-0005-US-0002-tactical-map-render-pipeline
title: "Разделение состояния, геометрии и рисования"
stage: draft
depends_on: ["EP-0005-US-0001-tactical-map-audit-remediation"]
ticket_count: 5
created: 2026-09-23T20:29:35Z
revision: 1
current_review: complete
---

# Разделение состояния, геометрии и рисования

## User story

Как разработчик, я хочу разделить обновление presentation state, подготовку геометрии и рисование тактической карты. Это позволит повторно рисовать неизменную сцену, адресно инвалидировать геометрию и получать согласованные данные для input и diagnostics. Authoritative simulation и shared motion formulas остаются источниками физического состояния. Сначала исправляются находки первой story, затем поэтапно выделяются стадии с integration checks на каждом переходе.

## Критерии приёмки

- **AC-0001 / A01:** Выделить обновление состояния тактического кадра. Все объекты кадра относятся к одному baseline и timestamp. Следующий update не меняет опубликованный frame. Pause/time advance/resume и corrections эквивалентны исправленной реализации.
- **AC-0002 / A02:** Выделить подготовку геометрии кадра. Одинаковые входы дают одинаковую геометрию и hit candidates. Сохранены слои, viewport clipping, пути и label priorities. При больших world coordinates близкие объекты сохраняют различимое положение.
- **AC-0003 / A03:** Сделать рисование потребителем готовой сцены. Повтор Draw оставляет frame/scene/revisions неизменными. Счётчики запрещённых операций равны нулю. Golden/recording fixtures покрывают пересечения marker/path/label/UI.
- **AC-0004 / A04:** Добавить адресную инвалидацию и ограниченные кэши. Пауза и неизменный view дают ноль повторных geometry builds. Табличный тест всех revisions проверяет правильные invalidation sets. Удалённые объекты очищены; индекс не пропускает пересекающие viewport элементы.
- **AC-0005 / A05:** Согласовать input, capture и измерения с показанным кадром. ID и позиции input/capture совпадают с показанным кадром. Integration fixtures покрывают переходы без stale geometry и ресурсов. Операционные counters проверены автоматически; время и FPS подтверждаются benchmark evidence, не flaky unit assertions.

## Карта тикетов

| Тикет | Основание | Приоритет | Критерий | Статус |
|---|---|---|---|---|
| [EP-0005-US-0002-TK-0001-frame-state-update](EP-0005-US-0002-TK-0001-frame-state-update/EP-0005-US-0002-TK-0001-frame-state-update.md) | A01 | P2 | AC-0001 | draft |
| [EP-0005-US-0002-TK-0002-scene-geometry-prepare](EP-0005-US-0002-TK-0002-scene-geometry-prepare/EP-0005-US-0002-TK-0002-scene-geometry-prepare.md) | A02 | P2 | AC-0002 | draft |
| [EP-0005-US-0002-TK-0003-read-only-map-painter](EP-0005-US-0002-TK-0003-read-only-map-painter/EP-0005-US-0002-TK-0003-read-only-map-painter.md) | A03 | P2 | AC-0003 | draft |
| [EP-0005-US-0002-TK-0004-revision-driven-scene-cache](EP-0005-US-0002-TK-0004-revision-driven-scene-cache/EP-0005-US-0002-TK-0004-revision-driven-scene-cache.md) | A04 | P2 | AC-0004 | draft |
| [EP-0005-US-0002-TK-0005-frame-consumers-and-performance](EP-0005-US-0002-TK-0005-frame-consumers-and-performance/EP-0005-US-0002-TK-0005-frame-consumers-and-performance.md) | A05 | P2 | AC-0005 | draft |

## Зависимости и порядок

Зависимость: EP-0005-US-0001-tactical-map-audit-remediation. Внутри этой story последовательность TK-0001 -> TK-0002 -> TK-0003 -> TK-0004 -> TK-0005. До A03 старый draw path сохраняется через адаптер; промежуточные коммиты должны собираться.

## Границы и риски

Engine simulation, RNG, save format и gameplay navigation не перепроектируются. Переход на другой graphics backend не входит в story. Цель производительности из EngineRequirements — 80 FPS (12.5 ms/frame); соответствие требует отдельного GPU измерения на целевом железе. CPU microbenchmark не доказывает FPS.

Prepare/Draw separation само по себе не гарантирует ускорения. Принимать изменения по counters и benchmark evidence; добавление GPU caches, multithreading или другой библиотеки требует отдельного обоснования.

## Статус проверки

Структура, покрытие findings, зависимости и ссылки проверяются на этапе планирования. `current_review: complete` относится только к полноте draft. Никакой тикет этой story не реализован и не объявлен runtime-validated. Approval story/tickets и разрешение на реализацию — отдельные действия.
