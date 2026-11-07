---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0006-multiscale-map-readability
title: "Район, кластер и система читаются при отдалении карты"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0004-materialized-world-compatibility, EP-0018-US-0005-local-map-scale]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Район, кластер и система читаются при отдалении карты

## Входное техническое задание
Единая карта раскрывает Local/Region/Cluster/System, ресурсы и орбиты не создают нечитаемые полосы.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу отдалять карту от корабля и постепенно видеть торговый район, весь кластер и систему. На каждом уровне мне понятны основные объекты и направления. Выбранная цель остаётся доступной, а мелкие ресурсы не закрывают станции и планеты.

## Grounding
GameSessionScreen.MapView.cs:119 группирует неважные объекты по screen cells. ClusterMapPresentation.Draw рисует domain bounds/labels. SolarSystemLayerRenderer.Draw перечисляет map.Orbits без type-specific LOD; ObjectLabelRenderer.DrawPlaques рисует подписи.

## Acceptance criteria
- AC-01: Уровни C-06 зависят от world span, имеют hysteresis и не меняют state/identity/membership объектов.
- AC-02: Region/Cluster отображают реальные stations/membership; System отображает доменные cluster markers, планеты и whole belts.
- AC-03: Орбиты малых объектов не образуют сплошную полосу; ShowOrbits, selected orbit и planet overview работают согласованно.
- AC-04: Player/selected/nav target сохраняют индивидуальные marker/label/hit target; нет раскрытия неизвестного состава.
- AC-05: Переходы, pan/resize/selection не дают flicker/залипания агрегации; данные прошлого мира очищаются при смене сессии.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0004-materialized-world-compatibility
- EP-0018-US-0005-local-map-scale
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Render сравнения одного snapshot на4 scale levels + transitions по обе стороны thresholds, screenshot матрица labels/orbits/selected targets.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0006-TK-0001-semantic-map-detail](EP-0018-US-0006-TK-0001-semantic-map-detail/EP-0018-US-0006-TK-0001-semantic-map-detail.md) | Доменные уровни детализации и агрегация объектов | client | EP-0018-US-0004-TK-0004-activate-v2-content, EP-0018-US-0005-TK-0003-local-scale-content | AC-01, AC-02, AC-04 |
| [EP-0018-US-0006-TK-0002-orbital-layer-policy](EP-0018-US-0006-TK-0002-orbital-layer-policy/EP-0018-US-0006-TK-0002-orbital-layer-policy.md) | Орбиты читаемы и следуют выбору объекта | client | EP-0018-US-0006-TK-0001-semantic-map-detail | AC-03, AC-04 |
| [EP-0018-US-0006-TK-0003-map-label-priorities](EP-0018-US-0006-TK-0003-map-label-priorities/EP-0018-US-0006-TK-0003-map-label-priorities.md) | Приоритеты подписей и выбора на всех масштабах | client | EP-0018-US-0006-TK-0002-orbital-layer-policy | AC-02, AC-04, AC-05 |
| [EP-0018-US-0006-TK-0004-smooth-detail-transitions](EP-0018-US-0006-TK-0004-smooth-detail-transitions/EP-0018-US-0006-TK-0004-smooth-detail-transitions.md) | Устойчивые переходы детализации и очистка состояния карты | client | EP-0018-US-0006-TK-0003-map-label-priorities | AC-01, AC-05 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.
