---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0002-stable-orbital-clusters
title: "Кластеры остаются разнесёнными при орбитальном движении"
stage: approved
approval_kind: delegated-planning
implementation_status: implemented_pending_compatibility_gate
dependencies: [EP-0018-US-0001-unified-station-geography]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Кластеры остаются разнесёнными при орбитальном движении

## Входное техническое задание
После увеличения расстояний сохранить несколько торговых районов, устойчивую геометрию и совместимое размещение ИИ.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу видеть несколько различимых торговых районов с устойчивыми расстояниями между станциями. По мере движения по орбитам станции своего района не сбиваются в тесную группу. Дальние районы остаются отдельными направлениями путешествий.

## Grounding
StationClusterGenerator.Expand размещает дополнительные кластеры в радиальных lanes стартового пояса; текущие lanes не задают min1d. AiBaseGenerator.Generate проверяет starting network overlap и AiTradePlacementValidator.Validate с горизонтом365d.

## Acceptance criteria
- AC-01: 3–5 кластеров по10–12 участников укладываются в пояс без ослабления min1d; nearest внутри каждого1–3d.
- AC-02: Радиусы любых станций разных кластеров разнесены >=1d; аналитически доказано сохранение min при любых фазах.
- AC-03: Внутри каждого кластера одинаковые radius/period/epoch/direction обеспечивают постоянные pair distances; связи позволяют туда/обратно.
- AC-04: Базы ИИ, поля/POI и границы системы валидны; стартовые trade exclusions и bounded placement сохраняются.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0001-unified-station-geography
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Таблица radial lanes/минимумов, orbital snapshots epoch/1/30/365d и результаты placement на boundary count. Отдельно analytic inequality proof.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0002-TK-0001-radial-lane-placement](EP-0018-US-0002-TK-0001-radial-lane-placement/EP-0018-US-0002-TK-0001-radial-lane-placement.md) | Радиальные полосы и формации с доказуемым минимальным расстоянием | engine | EP-0018-US-0001-TK-0004-atomic-world-bootstrap | AC-01, AC-02, AC-03 |
| [EP-0018-US-0002-TK-0002-cluster-link-graph](EP-0018-US-0002-TK-0002-cluster-link-graph/EP-0018-US-0002-TK-0002-cluster-link-graph.md) | Торговые направления на новой географии без коротких исключений | engine | EP-0018-US-0002-TK-0001-radial-lane-placement | AC-01, AC-03 |
| [EP-0018-US-0002-TK-0003-full-system-integration](EP-0018-US-0002-TK-0003-full-system-integration/EP-0018-US-0002-TK-0003-full-system-integration.md) | Размещение ИИ и слоёв среды на расширенной географии | engine | EP-0018-US-0002-TK-0002-cluster-link-graph | AC-04 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.

## Review реализации истории — 2026-11-07

3/3тикета реализованы. AC01/02: boundary3/5×10/12, independent nearest/centers и
all-phase radial inequality; AC03: одинаковыеradius/period/epoch/direction,
strong forward/return paths и cargoсемантика черезliveEngine. AC04: realAI/fields/POI
seeds1/42/100 и horizon365dvalid, boundedfailure/atomic ingress. Последние36focused
и2085Engine non-corpus PASS; native/fullcorpus ещёUS8. Строгийdescriptor/runtime
preflight перенесён сюда изUS4 из-за реальногоversion1-only runtime дефекта;
файловаяsavecompatibility/no-regeneration ещёUS4, до activation. История остаётся
implemented_pending_compatibility_gate. Self-review findings нет, независимогоreview нет.