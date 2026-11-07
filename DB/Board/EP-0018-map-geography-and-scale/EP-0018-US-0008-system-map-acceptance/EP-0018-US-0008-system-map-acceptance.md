---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0008-system-map-acceptance
title: "География и читаемость подтверждены воспроизводимой приёмкой"
stage: approved
approval_kind: delegated-planning
implementation_status: not_started
dependencies: [EP-0018-US-0001-unified-station-geography, EP-0018-US-0002-stable-orbital-clusters, EP-0018-US-0003-preserved-starts-and-resources, EP-0018-US-0004-materialized-world-compatibility, EP-0018-US-0005-local-map-scale, EP-0018-US-0006-multiscale-map-readability, EP-0018-US-0007-map-navigation-and-estimates]
created: 2026-11-07T17:21:08Z
source_request: "отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами"
current_review: complete
revision: 1
---
# География и читаемость подтверждены воспроизводимой приёмкой

## Входное техническое задание
Подтвердить результат числами и реальным клиентом: seed corpus, четыре уровня карты, сценарный путь к станции и сохранение.

## Связь с техническим заданием
Часть [эпика](../Documentation.md) и его [контракта переделки](../ImplementationContract.md). Новые численные настройки отделены от текущего поведения. Основание процесса: пользователь прямо поручил создать весь состав без дополнительных подтверждений.

## User story
Как разработчик и игрок, я хочу проверяемое подтверждение, что новая карта даёт задуманные расстояния и читается на разных экранах. Отчёт должен воспроизводиться по seed и настройкам. Успешные автоматические проверки не подменяют оценку удобства человеком.

## Grounding
SystemCorrectnessCorpusTests.HundredSeedScenarioBoundaryCorpus уже проверяет100seed и boundary geometry. tools/DeepSpaceSaga.Performance/SolarMapEvidence.cs:Run и SolarNativeEvidence.Run собирают raster/native evidence; FPS исторически open в BoardEP-0002/4.

## Acceptance criteria
- AC-01: Corpus фиксирует все pair/nearest violations, versions, roles, feasibility, seed/config/scenario и bounded failures; нет timeout-as-pass.
- AC-02: Rendered evidence включает Start/selected station/cluster/system и разрешения/UI scales изC-07, scale вычисляется из camera state.
- AC-03: Native input path проходит zoom/pan/selection/Approach/Dock/Save/Load/return; source slots/временные процессы убраны.
- AC-04: Отдельно записаны automated/native/human acceptance и FPS median/p95/machine; непройденное остаётся OPEN/FAIL.

## Non-goals
Реальный N-body, изменение единиц/часов, новая боевая карта, скрытая перебалансировка экономики и перегенерация старых сохранений не входят.

## Dependencies
- EP-0018-US-0001-unified-station-geography
- EP-0018-US-0002-stable-orbital-clusters
- EP-0018-US-0003-preserved-starts-and-resources
- EP-0018-US-0004-materialized-world-compatibility
- EP-0018-US-0005-local-map-scale
- EP-0018-US-0006-multiscale-map-readability
- EP-0018-US-0007-map-navigation-and-estimates
Точные ticket dependencies в таблице ниже; итоговая публикация новой конфигурации запрещена до compatibility gate US-0004.

## Invariants
Engine владеет географией и RNG; Client только проецирует snapshot. 1 world unit = 100 м; календарь/motion = 300 (EngineRequirements, раздел «Время и единицы»). Seed/version/scenario/config воспроизводят результат. Подробнее: ImplementationContract.md, I-01–I-09.

## Completion evidence
Retained report с реальными абсолютными paths под D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<run>, manifest и screenshots с соответствующим HEAD.
Планируемые проверки не являются выполненной приёмкой. Automated, native и human/product acceptance фиксируются отдельно.

## Approved ticket map
План и тикеты приняты автоматическим workflow по прямому поручению пользователя; индивидуальных человеческих approvals не было.

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [EP-0018-US-0008-TK-0001-seed-geography-corpus](EP-0018-US-0008-TK-0001-seed-geography-corpus/EP-0018-US-0008-TK-0001-seed-geography-corpus.md) | Разбитый корпус генерации с независимым измерением расстояний | engine | EP-0018-US-0007-TK-0002-current-travel-estimates | AC-01, AC-04 |
| [EP-0018-US-0008-TK-0002-rendered-map-evidence](EP-0018-US-0008-TK-0002-rendered-map-evidence/EP-0018-US-0008-TK-0002-rendered-map-evidence.md) | Измеримый отчёт четырёх масштабов и производительности | tooling | EP-0018-US-0008-TK-0001-seed-geography-corpus | AC-01, AC-02, AC-04 |
| [EP-0018-US-0008-TK-0003-native-map-evidence](EP-0018-US-0008-TK-0003-native-map-evidence/EP-0018-US-0008-TK-0003-native-map-evidence.md) | Нативный маршрут по карте и отдельная продуктовая приёмка | tooling | EP-0018-US-0008-TK-0002-rendered-map-evidence | AC-02, AC-03, AC-04 |

## Decisions / Review log
2026-11-07T17:21:08Z — время записи, не время исходного сообщения. Точное поручение: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами». Результат: делегированное планирование всего состава; execution не разрешён этим документом. Общие D/A/G находятся в Documentation.md.

## Gaps and backlog
Блокирующих вопросов планирования нет. Численные assumptions проверяются по критериям истории; изменение пользовательского контракта требует явного пересмотра Board.
