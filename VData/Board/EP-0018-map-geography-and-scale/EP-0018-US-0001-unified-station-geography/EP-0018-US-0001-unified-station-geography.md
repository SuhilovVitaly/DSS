---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0001-unified-station-geography
title: "Все станции стартового кластера разделены игровыми днями"
stage: approved
approval_kind: delegated-planning
implementation_status: implemented_pending_acceptance
dependencies: []
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Все станции стартового кластера разделены игровыми днями

## Входное техническое задание
Устранить исключение для старых станций Default и сделать географический минимум реальным расстоянием между каждой парой человеческих станций.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу получать единый стартовый кластер, где путешествия между станциями занимают игровые дни. Исходные станции сохраняют свои имена и назначение, но получают новые места по тем же правилам, что остальные. Смена масштаба карты не меняет эти расстояния.

## Grounding
StationClusterGenerator.Place(ScenarioFile, ClusterGenerationConfig, GameDataRegistry, ulong, int) оставляет original координаты и строит новые станции по дуге; BuildLinks допускает preserved пары без neighbor bounds. StationClusterMapSnapshot пока не хранит reference Vmax.

## Acceptance criteria
- AC-01: Во v2 каждая human pair внутри первого кластера находится >=1d, nearest каждого участника 1–3d по frozen reference speed.
- AC-02: Старые SPC-0002/0005/0006/0007/0008 участвуют в тех же проверках; ID, роли и explicit данные сохранены.
- AC-03: Одинаковые seed/config/scenario/version воспроизводят placement; невозможный вход даёт bounded atomic failure.
- AC-04: Descriptor версии/опорной скорости и min/neighbor bounds проходит сериализацию, v1 отсутствие полей остаётся допустимым.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
none
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Фиксированный Default seed42: таблица всех pair distances/nearest и новый snapshot; unit tests независимой формулой проверяют legacy и generated members.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0001-TK-0001-geography-descriptor](EP-0018-US-0001-TK-0001-geography-descriptor/EP-0018-US-0001-TK-0001-geography-descriptor.md) | Сериализуемые правила географии нового кластера | contracts | none | AC-04 |
| [EP-0018-US-0001-TK-0002-v2-generation-input](EP-0018-US-0001-TK-0002-v2-generation-input/EP-0018-US-0001-TK-0002-v2-generation-input.md) | Версионированные входы генерации и проверки диапазонов | engine | EP-0018-US-0001-TK-0001-geography-descriptor | AC-03, AC-04 |
| [EP-0018-US-0001-TK-0003-all-station-placement](EP-0018-US-0001-TK-0003-all-station-placement/EP-0018-US-0001-TK-0003-all-station-placement.md) | Единое размещение исходных и новых станций | engine | EP-0018-US-0001-TK-0002-v2-generation-input | AC-01, AC-02, AC-03, AC-04 |
| [EP-0018-US-0001-TK-0004-atomic-world-bootstrap](EP-0018-US-0001-TK-0004-atomic-world-bootstrap/EP-0018-US-0001-TK-0004-atomic-world-bootstrap.md) | Атомарный запуск нового мира без старой компактной геометрии | engine | EP-0018-US-0001-TK-0003-all-station-placement | AC-01, AC-02, AC-03 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.

## Review реализации истории — 2026-11-07

Все4тикета реализованы. AC01/02: реальныеDefaultlegacy+generated станции проверены
одинаково; immutable descriptor/atomic failure/seed repeat покрываютAC03/04.
Story integration: реальный Engine bootstrap, resources ownership/docking context,
явный station payload;27focused и2076non-corpus Engine checks PASS. Default42
snapshot/66pairs/12nearest — focused-final.trx run20261107-bootstrap.
Self-review findings нет; независимый review не проведён. Общий Save/Load gate
не подменяется DTO roundtrip: v2Load реализуетсяUS0004 до activation. Поэтому
статус истории implemented_pending_compatibility_gate; окончательная приёмка
и full corpus/native входят последующие обязательные gates эпика.