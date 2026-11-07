---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0003-preserved-starts-and-resources
title: "Привычные старты, ресурсы и рейсы работают на новой карте"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0001-unified-station-geography, EP-0018-US-0002-stable-orbital-clusters]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Привычные старты, ресурсы и рейсы работают на новой карте

## Входное техническое задание
Перенести окружение относительно владельцев, сохранить смысл Default/Docked/Undocked и согласовать торговые путешествия с новой географией.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу начать выбранный сценарий с прежним кораблём, грузом и назначением станций. После разнесения станций ресурсы остаются рядом со своими владельцами, а пристыкованный корабль находится у нужного порта. Я могу совершить рейс к соседней станции и вернуться с корректным расходом топлива.

## Grounding
StationResourceFields.Generate/ExtendForClusters и BindResources материализуют реальные астероиды. ScenarioGroupPlacement.Translate нормализует Docked на parent+(1,1). SimulationEngine.ClusterVoyages.cs:BuildClusterVoyageMap пересчитывает distances/current engine speed, сохраняя legacy risk политики.

## Acceptance criteria
- AC-01: Каждый resource manifest остаётся единственным, у своей станции, с прежним составом/ID/RNG; относительные distances сохраняются при орбите.
- AC-02: Шесть enabled scenarios сохраняют explicit ship/station payload, docking state и фактическую стартовую дистанцию50–75d.
- AC-03: Выполнимы local/intercluster рейс и возврат; текущие расстояния/оценки/топливо согласованы с переставленными станциями.
- AC-04: Approach/Dock движущейся станции, отход и продолжение рейса после Save/Load не получают новых телепортов/повторных расходов.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0001-unified-station-geography
- EP-0018-US-0002-stable-orbital-clusters
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Snapshots и named tests по каждому сценарию, resource ownership/offset comparison, voyage ledger до/после рейса и roundtrip.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0003-TK-0001-station-resource-relocation](EP-0018-US-0003-TK-0001-station-resource-relocation/EP-0018-US-0003-TK-0001-station-resource-relocation.md) | Ресурсные поля создаются у окончательных координат станции | engine | EP-0018-US-0002-TK-0003-full-system-integration | AC-01 |
| [EP-0018-US-0003-TK-0002-scenario-start-bindings](EP-0018-US-0003-TK-0002-scenario-start-bindings/EP-0018-US-0003-TK-0002-scenario-start-bindings.md) | Стартовые сценарии сохраняют игрока и локальные привязки | engine | EP-0018-US-0003-TK-0001-station-resource-relocation | AC-02, AC-04 |
| [EP-0018-US-0003-TK-0003-voyage-geography](EP-0018-US-0003-TK-0003-voyage-geography/EP-0018-US-0003-TK-0003-voyage-geography.md) | Торговые рейсы используют новые расстояния и текущий двигатель | engine | EP-0018-US-0003-TK-0002-scenario-start-bindings | AC-03, AC-04 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.
