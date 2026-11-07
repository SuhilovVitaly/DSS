---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0004-materialized-world-compatibility
title: "Новый и прежний мир продолжаются после сохранения"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0001-unified-station-geography, EP-0018-US-0002-stable-orbital-clusters, EP-0018-US-0003-preserved-starts-and-resources]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# Новый и прежний мир продолжаются после сохранения

## Входное техническое задание
Включить новую географию только после доказанной materialized persistence, сохранив старые миры и строгую проверку ссылок.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как игрок, я хочу загрузить прежнее сохранение без перестановки станций. Новая игра получает новую карту, а после сохранения и загрузки продолжает именно её. Изменение настроек генерации не должно менять уже созданный мир.

## Grounding
SolarSystemGeneration.ValidateWorld допускает только version1; StationClusterSaveValidation.Validate требует rulesVersion1 и legacy source exemption. MaterializeOrRestoreTradingMap вызывает ValidateMaterialized со старой template geometry. LocalWorldSaveRoundtripTests уже проходит реальные local transport/file сценарии.

## Acceptance criteria
- AC-01: v1 сохранения продолжают прежние координаты, epochs/IDs/resources и принимают отсутствие нового descriptor.
- AC-02: v2 Save/Load сохраняет materialized geography/reference speed, все descriptors и активные рейсы; изменение JSON не вызывает generation.
- AC-03: Неподдерживаемые/несогласованные версии и повреждённые v2 distances/links/resources отвергаются до замены session; source save не переписывается.
- AC-04: Shipped solar-system.json включает v2 только после compatibility/regression gate; старты Default и остальные enabled работают через реальную фабрику.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0001-unified-station-geography
- EP-0018-US-0002-stable-orbital-clusters
- EP-0018-US-0003-preserved-starts-and-resources
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Реальные v1/v2 файлы в отдельном validation run, snapshots до/после elapsed time, negative atomic ingress и content-loader suite.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0004-TK-0001-versioned-save-validation](EP-0018-US-0004-TK-0001-versioned-save-validation/EP-0018-US-0004-TK-0001-versioned-save-validation.md) | Строгая версия географии без миграции координат старых миров | engine | EP-0018-US-0003-TK-0003-voyage-geography | AC-01, AC-02, AC-03 |
| [EP-0018-US-0004-TK-0002-legacy-trade-map-validation](EP-0018-US-0004-TK-0002-legacy-trade-map-validation/EP-0018-US-0004-TK-0002-legacy-trade-map-validation.md) | Загрузка v2 не восстанавливает старые торговые offsets | engine | EP-0018-US-0004-TK-0001-versioned-save-validation | AC-01, AC-02, AC-03 |
| [EP-0018-US-0004-TK-0003-local-world-roundtrip](EP-0018-US-0004-TK-0003-local-world-roundtrip/EP-0018-US-0004-TK-0003-local-world-roundtrip.md) | Реальные сохранения продолжают географию обеих версий | engine | EP-0018-US-0004-TK-0002-legacy-trade-map-validation | AC-01, AC-02, AC-03 |
| [EP-0018-US-0004-TK-0004-activate-v2-content](EP-0018-US-0004-TK-0004-activate-v2-content/EP-0018-US-0004-TK-0004-activate-v2-content.md) | Включение новой карты для всех обычных стартов | content-data | EP-0018-US-0004-TK-0003-local-world-roundtrip | AC-04 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.
