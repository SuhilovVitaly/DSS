---
epic: EP-0005-optimization
story: EP-0005-US-0001-tactical-map-audit-remediation
title: "Устранение находок аудита тактической карты"
stage: draft
depends_on: []
ticket_count: 15
created: 2026-09-23T20:29:35Z
revision: 1
current_review: complete
---

# Устранение находок аудита тактической карты

## User story

Как игрок, я хочу, чтобы тактическая карта корректно отражала состояние мира, выбирала видимые цели и сохраняла стабильную работу при паузе, изменении масштаба и большом числе контактов. Как разработчик, я хочу отдельный проверяемый тикет на каждую оставшуюся находку аудита. Подтверждённые дефекты F02 и F04–F13 отделены от четырёх рисков R01–R04, которые сначала нужно воспроизвести. F01 и F03 уже исправлены по прямому поручению пользователя и повторно не планируются.

## Критерии приёмки

- **AC-0001 / F02:** Обновлять карту после продвижения мира на паузе. При более позднем MotionTimeMs новая поза и camera focus видны за один кадр; trail не изображает телепортацию. Снимки при неизменном времени не создают дрожание. После resume не возвращается старая поза.
- **AC-0002 / F04:** Исключить UI и невидимые маркеры из наведения. UI scale 0.8/1/1.2/1.5: объект под панелью не активируется. Полностью невидимый marker недоступен, частично видимый доступен. Hover и click совпадают при одинаковых входах.
- **AC-0003 / F05:** Согласовать приоритет цели и кластера. В кольце 15..30 px цели соседний cluster не перехватывает click. При равных расстояниях порядок snapshot не меняет победителя. Кластер раскрывается только при отсутствии object hit.
- **AC-0004 / F06:** Разместить важные подписи без пересечений. Тексты станции и корабля различимы при совпадающих позициях. На маленьком экране fallback не падает и не выходит за viewport. Перестановка snapshot.Objects не меняет расположение.
- **AC-0005 / F07:** Сократить обработку объектов вне экрана. Невидимый неважный контакт не получает 201 bootstrap point. Память ограничена независимо от длительности сессии; важные цели сохраняются. Возврат в кадр не меняет identity и не фабрикует историю.
- **AC-0006 / F08:** Повторно использовать геометрию на паузе. На одинаковом paused frame geometry build counters не растут. Изменение камеры обновляет screen geometry в следующий кадр. Анимация UI не запускает физическое прогнозирование.
- **AC-0007 / F09:** Сохранять диагностику одного завершённого кадра. Snapshot arrival между Render и click не смешивает baseline/poses. Saved endpoints равны показанным. Отключённый forecast не появляется в capture.
- **AC-0008 / F10:** Вычислять UI layout до геометрии карты. Первый frame после resize/collapse не размещает labels под UI. UI scale обновляет hit rects и available map одновременно. Первый frame имеет полный layout без предыдущего Render.
- **AC-0009 / F11:** Убрать файловые операции из интерактивного рисования. Заблокированный fake writer не блокирует следующий Render. Заблокированный decoder не блокирует hover. Cancel не оставляет испорченный final JSON или опубликованный disposed image.
- **AC-0010 / F12:** Освобождать ресурсы карты при уничтожении экрана. Owned resources освобождаются ровно один раз. Возврат из modal сохраняет рабочую карту. 100 циклов после завершения workers не дают линейного роста owned handles.
- **AC-0011 / F13:** Локализовать подписи и сообщения карты. RU/EN подписи соответствуют выбранному языку. Оба варианта сообщения локализованы. После смены языка текст помещается и unknown identity не раскрывается.
- **AC-0012 / R01:** Проверить устойчивость кластеров при плавном zoom. Микроколебания zoom внутри уровня не меняют состав. Pan не меняет identity clusters. Есть reproduce/fix или обоснованный not-reproduced outcome.
- **AC-0013 / R02:** Проверить стык сглаженного корабля и Approach. В ходе correction marker связан с маршрутом. Route endpoint остаётся точным. Evidence показывает reproduce/fix либо not-reproduced.
- **AC-0014 / R03:** Ограничить прогноз при зависшем потоке снимков. Зависший поток не уводит marker бесконечно далеко. Speed4 не меняет допустимый real age. Свежий снимок восстанавливает карту без rewind authoritative state.
- **AC-0015 / R04:** Проверить стоимость поиска свободной области. Для fixed/random small fixtures результат равен exhaustive oracle. Полностью перекрытый viewport не выдаётся свободным. Одинаковый layout не запускает поиск; результат риска подтверждён evidence.

## Карта тикетов

| Тикет | Основание | Приоритет | Критерий | Статус |
|---|---|---|---|---|
| [EP-0005-US-0001-TK-0001-paused-authoritative-rebase](EP-0005-US-0001-TK-0001-paused-authoritative-rebase/EP-0005-US-0001-TK-0001-paused-authoritative-rebase.md) | F02 | P1 | AC-0001 | draft |
| [EP-0005-US-0001-TK-0002-visible-object-hit-testing](EP-0005-US-0001-TK-0002-visible-object-hit-testing/EP-0005-US-0001-TK-0002-visible-object-hit-testing.md) | F04 | P2 | AC-0002 | draft |
| [EP-0005-US-0001-TK-0003-cluster-click-priority](EP-0005-US-0001-TK-0003-cluster-click-priority/EP-0005-US-0001-TK-0003-cluster-click-priority.md) | F05 | P2 | AC-0003 | draft |
| [EP-0005-US-0001-TK-0004-important-label-placement](EP-0005-US-0001-TK-0004-important-label-placement/EP-0005-US-0001-TK-0004-important-label-placement.md) | F06 | P2 | AC-0004 | draft |
| [EP-0005-US-0001-TK-0005-bounded-offscreen-work](EP-0005-US-0001-TK-0005-bounded-offscreen-work/EP-0005-US-0001-TK-0005-bounded-offscreen-work.md) | F07 | P2 | AC-0005 | draft |
| [EP-0005-US-0001-TK-0006-paused-geometry-invalidation](EP-0005-US-0001-TK-0006-paused-geometry-invalidation/EP-0005-US-0001-TK-0006-paused-geometry-invalidation.md) | F08 | P2 | AC-0006 | draft |
| [EP-0005-US-0001-TK-0007-coherent-frame-diagnostics](EP-0005-US-0001-TK-0007-coherent-frame-diagnostics/EP-0005-US-0001-TK-0007-coherent-frame-diagnostics.md) | F09 | P2 | AC-0007 | draft |
| [EP-0005-US-0001-TK-0008-layout-before-map](EP-0005-US-0001-TK-0008-layout-before-map/EP-0005-US-0001-TK-0008-layout-before-map.md) | F10 | P2 | AC-0008 | draft |
| [EP-0005-US-0001-TK-0009-nonblocking-render-io](EP-0005-US-0001-TK-0009-nonblocking-render-io/EP-0005-US-0001-TK-0009-nonblocking-render-io.md) | F11 | P3 | AC-0009 | draft |
| [EP-0005-US-0001-TK-0010-deterministic-resource-lifetime](EP-0005-US-0001-TK-0010-deterministic-resource-lifetime/EP-0005-US-0001-TK-0010-deterministic-resource-lifetime.md) | F12 | P3 | AC-0010 | draft |
| [EP-0005-US-0001-TK-0011-map-localization](EP-0005-US-0001-TK-0011-map-localization/EP-0005-US-0001-TK-0011-map-localization.md) | F13 | P3 | AC-0011 | draft |
| [EP-0005-US-0001-TK-0012-stable-cluster-membership](EP-0005-US-0001-TK-0012-stable-cluster-membership/EP-0005-US-0001-TK-0012-stable-cluster-membership.md) | R01 | P2 | AC-0012 | draft |
| [EP-0005-US-0001-TK-0013-reconciled-route-join](EP-0005-US-0001-TK-0013-reconciled-route-join/EP-0005-US-0001-TK-0013-reconciled-route-join.md) | R02 | P2 | AC-0013 | draft |
| [EP-0005-US-0001-TK-0014-stale-snapshot-policy](EP-0005-US-0001-TK-0014-stale-snapshot-policy/EP-0005-US-0001-TK-0014-stale-snapshot-policy.md) | R03 | P2 | AC-0014 | draft |
| [EP-0005-US-0001-TK-0015-free-viewport-cost](EP-0005-US-0001-TK-0015-free-viewport-cost/EP-0005-US-0001-TK-0015-free-viewport-cost.md) | R04 | P2 | AC-0015 | draft |

## Зависимости и порядок

Нет внешних story dependencies. Внутренние зависимости заданы в frontmatter каждого тикета. Независимые тикеты могут выполняться отдельно; одновременные изменения общих файлов нужно согласовать.

## Границы и риски

Engine simulation, RNG, save format и gameplay navigation не перепроектируются. Переход на другой graphics backend не входит в story. Цель производительности из EngineRequirements — 80 FPS (12.5 ms/frame); соответствие требует отдельного GPU измерения на целевом железе. CPU microbenchmark не доказывает FPS.

15 тикетов в одной story — осознанное исключение из обычного ориентира 2–5: пользователь прямо попросил одну story для всех остальных находок. R01–R04 остаются рисками до воспроизведения. Все новые policy choices помечены draft.

## Статус проверки

Структура, покрытие findings, зависимости и ссылки проверяются на этапе планирования. `current_review: complete` относится только к полноте draft. Никакой тикет этой story не реализован и не объявлен runtime-validated. Approval story/tickets и разрешение на реализацию — отдельные действия.
