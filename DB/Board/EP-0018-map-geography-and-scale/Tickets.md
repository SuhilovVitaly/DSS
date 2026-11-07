# Тикеты EP-0018 — переделка географии и масштаба карты

9 историй / 29 тикетов.28 опубликованы; TK9.3 текущий.
Статусы/SHA/evidence и остаточные gates — [ImplementationStatus](ImplementationStatus.md).
Первоначальные allowlists расширялись owning карточками до реализации по полномочиям
EpicExecutionPrompt; максимум5 файлов planning не сокращает фактический scope. Полный контракт: [ImplementationContract.md](ImplementationContract.md); grounding/решения: [Documentation.md](Documentation.md).

## Порядок исполнения
US-0001 -> US-0002 -> US-0003 TK-0001/0002 -> US-0004 TK-0001/0002 -> US-0003 TK-0003 -> US-0004 TK-0003/0004 -> US-0005 -> US-0006 -> US-0007 -> US-0008 -> US-0009.
US-0005 можно готовить после US-0001 TK-0001, но итоговая карта/навигация ждут US-0004. Пересекающиеся allowlists исполняются последовательно по ticket dependencies. Никакой production activation до US-0004 TK-0004. Documentation последняя.

## Общие gates
- Каждый тикет имеет exact allowlist, planned API, steps, named tests и DoD.
- Existing source reference — signature либо path:line; новые API помечены planned.
- Future test names не означают наличие/выполнение tests.
- Каждый test process cap300000ms; corpus дробится по точным IDs; timeout=FAIL.
- Diagnostic/evidence root D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/.
- Ошибки/новые факты не дают разрешения на скрытое расширение allowlist; сначала поправить Board в пределах согласованного scope.
- Planning не разрешает реализацию, commit, push или запуск игры.

## US-0001 — Все станции стартового кластера разделены игровыми днями

[История](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-unified-station-geography.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0001-TK-0001-geography-descriptor](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0001-geography-descriptor/EP-0018-US-0001-TK-0001-geography-descriptor.md) | Сериализуемые правила географии нового кластера | contracts | none | AC-04 |
| [EP-0018-US-0001-TK-0002-v2-generation-input](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0002-v2-generation-input/EP-0018-US-0001-TK-0002-v2-generation-input.md) | Версионированные входы генерации и проверки диапазонов | engine | EP-0018-US-0001-TK-0001-geography-descriptor | AC-03, AC-04 |
| [EP-0018-US-0001-TK-0003-all-station-placement](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0003-all-station-placement/EP-0018-US-0001-TK-0003-all-station-placement.md) | Единое размещение исходных и новых станций | engine | EP-0018-US-0001-TK-0002-v2-generation-input | AC-01, AC-02, AC-03, AC-04 |
| [EP-0018-US-0001-TK-0004-atomic-world-bootstrap](EP-0018-US-0001-unified-station-geography/EP-0018-US-0001-TK-0004-atomic-world-bootstrap/EP-0018-US-0001-TK-0004-atomic-world-bootstrap.md) | Атомарный запуск нового мира без старой компактной геометрии | engine | EP-0018-US-0001-TK-0003-all-station-placement | AC-01, AC-02, AC-03 |

## US-0002 — Кластеры остаются разнесёнными при орбитальном движении

[История](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-stable-orbital-clusters.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0002-TK-0001-radial-lane-placement](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-TK-0001-radial-lane-placement/EP-0018-US-0002-TK-0001-radial-lane-placement.md) | Радиальные полосы и формации с доказуемым минимальным расстоянием | engine | EP-0018-US-0001-TK-0004-atomic-world-bootstrap | AC-01, AC-02, AC-03 |
| [EP-0018-US-0002-TK-0002-cluster-link-graph](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-TK-0002-cluster-link-graph/EP-0018-US-0002-TK-0002-cluster-link-graph.md) | Торговые направления на новой географии без коротких исключений | engine | EP-0018-US-0002-TK-0001-radial-lane-placement | AC-01, AC-03 |
| [EP-0018-US-0002-TK-0003-full-system-integration](EP-0018-US-0002-stable-orbital-clusters/EP-0018-US-0002-TK-0003-full-system-integration/EP-0018-US-0002-TK-0003-full-system-integration.md) | Размещение ИИ и слоёв среды на расширенной географии | engine | EP-0018-US-0002-TK-0002-cluster-link-graph | AC-04 |

## US-0003 — Привычные старты, ресурсы и рейсы работают на новой карте

[История](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-preserved-starts-and-resources.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0003-TK-0001-station-resource-relocation](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-TK-0001-station-resource-relocation/EP-0018-US-0003-TK-0001-station-resource-relocation.md) | Ресурсные поля создаются у окончательных координат станции | engine | EP-0018-US-0002-TK-0003-full-system-integration | AC-01 |
| [EP-0018-US-0003-TK-0002-scenario-start-bindings](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-TK-0002-scenario-start-bindings/EP-0018-US-0003-TK-0002-scenario-start-bindings.md) | Стартовые сценарии сохраняют игрока и локальные привязки | engine | EP-0018-US-0003-TK-0001-station-resource-relocation | AC-02, AC-04 |
| [EP-0018-US-0003-TK-0003-voyage-geography](EP-0018-US-0003-preserved-starts-and-resources/EP-0018-US-0003-TK-0003-voyage-geography/EP-0018-US-0003-TK-0003-voyage-geography.md) | Торговые рейсы используют новые расстояния и текущий двигатель | engine | EP-0018-US-0003-TK-0002-scenario-start-bindings, EP-0018-US-0004-TK-0002-legacy-trade-map-validation | AC-03, AC-04 |

## US-0004 — Новый и прежний мир продолжаются после сохранения

[История](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-materialized-world-compatibility.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0004-TK-0001-versioned-save-validation](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0001-versioned-save-validation/EP-0018-US-0004-TK-0001-versioned-save-validation.md) | Строгая версия географии без миграции координат старых миров | engine | EP-0018-US-0003-TK-0002-scenario-start-bindings | AC-01, AC-02, AC-03 |
| [EP-0018-US-0004-TK-0002-legacy-trade-map-validation](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0002-legacy-trade-map-validation/EP-0018-US-0004-TK-0002-legacy-trade-map-validation.md) | Загрузка v2 не восстанавливает старые торговые offsets | engine | EP-0018-US-0004-TK-0001-versioned-save-validation | AC-01, AC-02, AC-03 |
| [EP-0018-US-0004-TK-0003-local-world-roundtrip](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0003-local-world-roundtrip/EP-0018-US-0004-TK-0003-local-world-roundtrip.md) | Реальные сохранения продолжают географию обеих версий | engine | EP-0018-US-0004-TK-0002-legacy-trade-map-validation, EP-0018-US-0003-TK-0003-voyage-geography | AC-01, AC-02, AC-03 |
| [EP-0018-US-0004-TK-0004-activate-v2-content](EP-0018-US-0004-materialized-world-compatibility/EP-0018-US-0004-TK-0004-activate-v2-content/EP-0018-US-0004-TK-0004-activate-v2-content.md) | Включение новой карты для всех обычных стартов | content-data | EP-0018-US-0004-TK-0003-local-world-roundtrip | AC-04 |

## US-0005 — Максимальное приближение показывает локальную окрестность

[История](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-local-map-scale.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0005-TK-0001-local-view-policy](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-TK-0001-local-view-policy/EP-0018-US-0005-TK-0001-local-view-policy.md) | Чистая политика локального охвата и максимального приближения | client | EP-0018-US-0001-TK-0001-geography-descriptor | AC-01, AC-04 |
| [EP-0018-US-0005-TK-0002-initial-camera-and-zoom](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-TK-0002-initial-camera-and-zoom/EP-0018-US-0005-TK-0002-initial-camera-and-zoom.md) | Локальный старт камеры и согласованные границы zoom/resize | client | EP-0018-US-0005-TK-0001-local-view-policy | AC-01, AC-02, AC-03 |
| [EP-0018-US-0005-TK-0003-local-scale-content](EP-0018-US-0005-local-map-scale/EP-0018-US-0005-TK-0003-local-scale-content/EP-0018-US-0005-TK-0003-local-scale-content.md) | Настройки локального масштаба и понятная линейка расстояний | content-data | EP-0018-US-0005-TK-0002-initial-camera-and-zoom | AC-01, AC-04 |

## US-0006 — Район, кластер и система читаются при отдалении карты

[История](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-multiscale-map-readability.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0006-TK-0001-semantic-map-detail](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0001-semantic-map-detail/EP-0018-US-0006-TK-0001-semantic-map-detail.md) | Доменные уровни детализации и агрегация объектов | client | EP-0018-US-0004-TK-0004-activate-v2-content, EP-0018-US-0005-TK-0003-local-scale-content | AC-01, AC-02, AC-04 |
| [EP-0018-US-0006-TK-0002-orbital-layer-policy](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0002-orbital-layer-policy/EP-0018-US-0006-TK-0002-orbital-layer-policy.md) | Орбиты читаемы и следуют выбору объекта | client | EP-0018-US-0006-TK-0001-semantic-map-detail | AC-03, AC-04 |
| [EP-0018-US-0006-TK-0003-map-label-priorities](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0003-map-label-priorities/EP-0018-US-0006-TK-0003-map-label-priorities.md) | Приоритеты подписей и выбора на всех масштабах | client | EP-0018-US-0006-TK-0002-orbital-layer-policy | AC-02, AC-04, AC-05 |
| [EP-0018-US-0006-TK-0004-smooth-detail-transitions](EP-0018-US-0006-multiscale-map-readability/EP-0018-US-0006-TK-0004-smooth-detail-transitions/EP-0018-US-0006-TK-0004-smooth-detail-transitions.md) | Устойчивые переходы детализации и очистка состояния карты | client | EP-0018-US-0006-TK-0003-map-label-priorities | AC-01, AC-05 |

## US-0007 — Переход от обзора системы к станции и понятное время пути

[История](EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-map-navigation-and-estimates.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0007-TK-0001-cluster-focus-navigation](EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-TK-0001-cluster-focus-navigation/EP-0018-US-0007-TK-0001-cluster-focus-navigation.md) | Навигация по доменным кластерам и локальное приближение цели | client | EP-0018-US-0006-TK-0004-smooth-detail-transitions | AC-01, AC-04 |
| [EP-0018-US-0007-TK-0002-current-travel-estimates](EP-0018-US-0007-map-navigation-and-estimates/EP-0018-US-0007-TK-0002-current-travel-estimates/EP-0018-US-0007-TK-0002-current-travel-estimates.md) | Расстояния и оценки пути отделены от генерационного масштаба | client | EP-0018-US-0007-TK-0001-cluster-focus-navigation, EP-0018-US-0003-TK-0003-voyage-geography | AC-02, AC-03, AC-04 |

## US-0008 — География и читаемость подтверждены воспроизводимой приёмкой

[История](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-system-map-acceptance.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0008-TK-0001-seed-geography-corpus](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-TK-0001-seed-geography-corpus/EP-0018-US-0008-TK-0001-seed-geography-corpus.md) | Разбитый корпус генерации с независимым измерением расстояний | engine | EP-0018-US-0007-TK-0002-current-travel-estimates | AC-01, AC-04 |
| [EP-0018-US-0008-TK-0002-rendered-map-evidence](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-TK-0002-rendered-map-evidence/EP-0018-US-0008-TK-0002-rendered-map-evidence.md) | Измеримый отчёт четырёх масштабов и производительности | tooling | EP-0018-US-0008-TK-0001-seed-geography-corpus | AC-01, AC-02, AC-04 |
| [EP-0018-US-0008-TK-0003-native-map-evidence](EP-0018-US-0008-system-map-acceptance/EP-0018-US-0008-TK-0003-native-map-evidence/EP-0018-US-0008-TK-0003-native-map-evidence.md) | Нативный маршрут по карте и отдельная продуктовая приёмка | tooling | EP-0018-US-0008-TK-0002-rendered-map-evidence | AC-02, AC-03, AC-04 |

## US-0009 — Контракты карты и передача результата отражают фактическую поставку

[История](EP-0018-US-0009-project-documentation/EP-0018-US-0009-project-documentation.md)

| Тикет | Название | Layer | Dependencies | AC |
|---|---|---|---|---|
| [EP-0018-US-0009-TK-0001-current-map-contracts](EP-0018-US-0009-project-documentation/EP-0018-US-0009-TK-0001-current-map-contracts/EP-0018-US-0009-TK-0001-current-map-contracts.md) | Действующие правила географии, карты и сохранений | tooling | EP-0018-US-0008-TK-0003-native-map-evidence | AC-01 |
| [EP-0018-US-0009-TK-0002-board-handoff](EP-0018-US-0009-project-documentation/EP-0018-US-0009-TK-0002-board-handoff/EP-0018-US-0009-TK-0002-board-handoff.md) | Реестр исполнения и передача эпика без ложной приёмки | tooling | EP-0018-US-0009-TK-0001-current-map-contracts | AC-02 |
| [EP-0018-US-0009-TK-0003-documentation-navigation](EP-0018-US-0009-project-documentation/EP-0018-US-0009-TK-0003-documentation-navigation/EP-0018-US-0009-TK-0003-documentation-navigation.md) | Навигация и связь прежних эпиков с новой картой | tooling | EP-0018-US-0009-TK-0002-board-handoff | AC-03 |

## Остаток

Блокирующих planning вопросов нет. A-01–A-09 — явные технические assumptions, не выполненный баланс. Все functional/native/human/FPS gates до реализации NOT RUN. Отдельный запрос нужен для execution, а не для завершения данного planning.

## Исправление реального dependency order — 2026-11-07

Сохранение активногоv2рейса изUS0003TK0003 требует strictfileingress и materialized
trading validation US0004TK0001/0002. Поэтому эти2тикета выполняются послеUS0003TK0002,
затем voyageTK0003, затем local-file-roundtrip/activation. Story4 больше не требует
завершения всейStory3 перед началом; точные частичные gates перечислены в карточках.
Это устраняет логическую зависимость тестов от ещё не реализованногоloader без
переноса FAIL вPASS или объединения тикетов. 9историй/29тикетов остаются прежними.
