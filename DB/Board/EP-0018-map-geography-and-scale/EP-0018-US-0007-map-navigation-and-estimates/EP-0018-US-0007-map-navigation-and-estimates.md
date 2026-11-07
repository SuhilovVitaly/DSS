---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0007-map-navigation-and-estimates
title: "Переход от обзора системы к станции и понятное время пути"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0003-preserved-starts-and-resources, EP-0018-US-0004-materialized-world-compatibility, EP-0018-US-0005-local-map-scale, EP-0018-US-0006-multiscale-map-readability]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Переход от обзора системы к станции и понятное время пути

## Входное техническое задание
Сохранить удобную навигацию между уровнями и показывать честные текущие оценки после увеличения расстояний.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу выбрать торговый район на обзорной карте и перейти к его станциям. Затем я могу приблизить нужную станцию и понять расстояние и ориентировочное время пути. При возврате к кораблю выбранная навигационная цель сохраняется.

## Grounding
ClusterMapPresentation.Station/Bounds/Draw уже использует ClusterMap и current poses. ClusterTravelEstimatesTests закрепляет presentation ETA; MapView поддерживает cluster/belt focus и system bounds.

## Acceptance criteria
- AC-01: Явные действия System->cluster fit->station local focus->player focus работают на единой карте, не меняя target самопроизвольно.
- AC-02: Отображаются актуальные distance и straight-flight estimate по текущему доступному двигателю; reference scale отделена от ETA.
- AC-03: Без двигателя/при invalid target показано отсутствие оценки, не ложный0; дни/часы не округляются до неверного смысла.
- AC-04: Выбор и подход к moving station, Save/Load и camera actions не сбрасывают реальные команды.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0003-preserved-starts-and-resources
- EP-0018-US-0004-materialized-world-compatibility
- EP-0018-US-0005-local-map-scale
- EP-0018-US-0006-multiscale-map-readability
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Client integration tests и native route selection script; сверка displayed estimate независимой формулой по current pose/Vmax.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0007-TK-0001-cluster-focus-navigation](EP-0018-US-0007-TK-0001-cluster-focus-navigation/EP-0018-US-0007-TK-0001-cluster-focus-navigation.md) | Навигация по доменным кластерам и локальное приближение цели | client | EP-0018-US-0006-TK-0004-smooth-detail-transitions | AC-01, AC-04 |
| [EP-0018-US-0007-TK-0002-current-travel-estimates](EP-0018-US-0007-TK-0002-current-travel-estimates/EP-0018-US-0007-TK-0002-current-travel-estimates.md) | Расстояния и оценки пути отделены от генерационного масштаба | client | EP-0018-US-0007-TK-0001-cluster-focus-navigation, EP-0018-US-0003-TK-0003-voyage-geography | AC-02, AC-03, AC-04 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.
