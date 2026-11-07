---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0005-local-map-scale
title: "Максимальное приближение показывает локальную окрестность"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0001-unified-station-geography]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Максимальное приближение показывает локальную окрестность

## Входное техническое задание
Придать максимуму приближения физический смысл и открывать New Game около игрока, а не в обзоре всей системы.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу на максимальном приближении видеть непосредственную окрестность корабля. Станция в игровом дне пути не должна помещаться рядом с ним на таком экране. При изменении окна или интерфейсного масштаба это ощущение сохраняется.

## Grounding
Settings.json:28 задаёт metersPerPixel[0]=100; TacticalMapSettings.MaximumPpu вычисляется из него. GameSessionScreen.MapView.cs:105 ZoomBy clamps static MaximumPpu; CameraState хранит PPU и выполняет WorldToScreen/ScreenToWorld.

## Acceptance criteria
- AC-01: Max zoom удовлетворяет C-05: short-side <=4ч и entire diagonal<1d на матрице viewport/aspect/UI scales.
- AC-02: New Game показывает Local с player focus; явный FitSystem остаётся доступен и не переопределяется очередными snapshots.
- AC-03: Pan/focus/zoom animation/resize сохраняют world coordinate consistency и selection; TacticalBattle camera не меняется.
- AC-04: Линейка показывает км и честный reference-time context; legacy snapshot без Geography использует explicit settings fallback.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0001-unified-station-geography
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Camera state и screenshots fixed seed42 при1280x720/1920x1080/3440x1440, UI1/1.5/2; аналитический расчёт охвата без OCR.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0005-TK-0001-local-view-policy](EP-0018-US-0005-TK-0001-local-view-policy/EP-0018-US-0005-TK-0001-local-view-policy.md) | Чистая политика локального охвата и максимального приближения | client | EP-0018-US-0001-TK-0001-geography-descriptor | AC-01, AC-04 |
| [EP-0018-US-0005-TK-0002-initial-camera-and-zoom](EP-0018-US-0005-TK-0002-initial-camera-and-zoom/EP-0018-US-0005-TK-0002-initial-camera-and-zoom.md) | Локальный старт камеры и согласованные границы zoom/resize | client | EP-0018-US-0005-TK-0001-local-view-policy | AC-01, AC-02, AC-03 |
| [EP-0018-US-0005-TK-0003-local-scale-content](EP-0018-US-0005-TK-0003-local-scale-content/EP-0018-US-0005-TK-0003-local-scale-content.md) | Настройки локального масштаба и понятная линейка расстояний | content-data | EP-0018-US-0005-TK-0002-initial-camera-and-zoom | AC-01, AC-04 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.
