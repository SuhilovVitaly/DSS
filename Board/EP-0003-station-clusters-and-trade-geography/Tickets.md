# Тикеты EP-0003-station-clusters-and-trade-geography

Созданы 2026-09-22T14:40:41Z по запросу пользователя. 8 историй, 20 тикетов. Planning complete; production/test execution не выполнялись. Story и epic draft сохранены, ticket approval — автоматический StoryBuilder workflow.

Основной порядок EP-0002 → EP-0003 → EP-0004; EP-0003 также ждёт указанные в story файлах поставки EP-0001. Внутри эпика US-0001→…→US-0008 — безопасный порядок, фактические dependency edges — в frontmatter.

## US-0001 — Рабочая торговля в стартовом кластере

Story: [EP-0003-US-0001-local-trading-cluster](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-local-trading-cluster.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0001-cluster-geography-contract/EP-0003-US-0001-TK-0001-cluster-geography-contract.md) | Доменные кластеры и экономические связи карты | contracts | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0002-local-cluster-generation/EP-0003-US-0001-TK-0002-local-cluster-generation.md) | Стартовый кластер из экономических ролей | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0003-local-cluster-content/EP-0003-US-0001-TK-0003-local-cluster-content.md) | Настройки первой торговой группы | content-data | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0004](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0004-local-cluster-map/EP-0003-US-0001-TK-0004-local-cluster-map.md) | Стартовый район и доступные соседние станции | client | TK-0003 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0001-TK-0001-cluster-geography-contract](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0001-cluster-geography-contract/EP-0003-US-0001-TK-0001-cluster-geography-contract.md)
- [EP-0003-US-0001-TK-0002-local-cluster-generation](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0002-local-cluster-generation/EP-0003-US-0001-TK-0002-local-cluster-generation.md)
- [EP-0003-US-0001-TK-0003-local-cluster-content](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0003-local-cluster-content/EP-0003-US-0001-TK-0003-local-cluster-content.md)
- [EP-0003-US-0001-TK-0004-local-cluster-map](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0001-local-trading-cluster/EP-0003-US-0001-TK-0004-local-cluster-map/EP-0003-US-0001-TK-0004-local-cluster-map.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0005-orbital-station-visit, EP-0001-US-0001-station-market-profiles, EP-0001-US-0003-dynamic-market-trading, EP-0001-US-0004-seeded-trading-map.

## US-0002 — Несколько разных торговых районов

Story: [EP-0003-US-0002-distinct-orbital-clusters](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-distinct-orbital-clusters.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-TK-0001-multi-cluster-placement/EP-0003-US-0002-TK-0001-multi-cluster-placement.md) | Разные орбитальные торговые районы без распада | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-TK-0002-full-cluster-content/EP-0003-US-0002-TK-0002-full-cluster-content.md) | Полная конфигурация 3–5 кластеров | content-data | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-TK-0003-multi-cluster-overview/EP-0003-US-0002-TK-0003-multi-cluster-overview.md) | Обзор нескольких районов одной карты | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0002-TK-0001-multi-cluster-placement](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-TK-0001-multi-cluster-placement/EP-0003-US-0002-TK-0001-multi-cluster-placement.md)
- [EP-0003-US-0002-TK-0002-full-cluster-content](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-TK-0002-full-cluster-content/EP-0003-US-0002-TK-0002-full-cluster-content.md)
- [EP-0003-US-0002-TK-0003-multi-cluster-overview](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0002-distinct-orbital-clusters/EP-0003-US-0002-TK-0003-multi-cluster-overview/EP-0003-US-0002-TK-0003-multi-cluster-overview.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0001-local-trading-cluster.

## US-0003 — Ресурсное окружение торговых районов

Story: [EP-0003-US-0003-cluster-resource-surroundings](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-cluster-resource-surroundings.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-TK-0001-resource-orbit-binding/EP-0003-US-0003-TK-0001-resource-orbit-binding.md) | Орбитальная привязка существующих ресурсных полей | contracts | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-TK-0002-cluster-resource-placement/EP-0003-US-0003-TK-0002-cluster-resource-placement.md) | Ресурсы по ролям без дублирования полей | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-TK-0003-cluster-resource-map/EP-0003-US-0003-TK-0003-cluster-resource-map.md) | Известные ресурсы рядом с торговыми районами | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0003-TK-0001-resource-orbit-binding](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-TK-0001-resource-orbit-binding/EP-0003-US-0003-TK-0001-resource-orbit-binding.md)
- [EP-0003-US-0003-TK-0002-cluster-resource-placement](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-TK-0002-cluster-resource-placement/EP-0003-US-0003-TK-0002-cluster-resource-placement.md)
- [EP-0003-US-0003-TK-0003-cluster-resource-map](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0003-cluster-resource-surroundings/EP-0003-US-0003-TK-0003-cluster-resource-map/EP-0003-US-0003-TK-0003-cluster-resource-map.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0002-distinct-orbital-clusters, EP-0001-US-0005-station-resource-fields.

## US-0004 — Выбор станции и направления по карте

Story: [EP-0003-US-0004-cluster-map-and-travel-estimates](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0004-cluster-map-and-travel-estimates/EP-0003-US-0004-cluster-map-and-travel-estimates.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0004-cluster-map-and-travel-estimates/EP-0003-US-0004-TK-0001-cluster-travel-estimates/EP-0003-US-0004-TK-0001-cluster-travel-estimates.md) | Актуальные расстояния и честная оценка прямого полёта | client | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0004-TK-0001-cluster-travel-estimates](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0004-cluster-map-and-travel-estimates/EP-0003-US-0004-TK-0001-cluster-travel-estimates/EP-0003-US-0004-TK-0001-cluster-travel-estimates.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0002-distinct-orbital-clusters, EP-0003-US-0003-cluster-resource-surroundings, EP-0002-US-0006-known-system-map, EP-0001-US-0016-market-knowledge.

## US-0005 — Местный цикл и дальний рейс с возвращением

Story: [EP-0003-US-0005-local-and-intercluster-voyages](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0005-local-and-intercluster-voyages/EP-0003-US-0005-local-and-intercluster-voyages.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0005-local-and-intercluster-voyages/EP-0003-US-0005-TK-0001-cluster-voyage-integration/EP-0003-US-0005-TK-0001-cluster-voyage-integration.md) | Действующий местный и межкластерный рейс | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0005-local-and-intercluster-voyages/EP-0003-US-0005-TK-0002-voyage-user-path/EP-0003-US-0005-TK-0002-voyage-user-path.md) | Пользовательский путь поездки между кластерами | client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0005-TK-0001-cluster-voyage-integration](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0005-local-and-intercluster-voyages/EP-0003-US-0005-TK-0001-cluster-voyage-integration/EP-0003-US-0005-TK-0001-cluster-voyage-integration.md)
- [EP-0003-US-0005-TK-0002-voyage-user-path](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0005-local-and-intercluster-voyages/EP-0003-US-0005-TK-0002-voyage-user-path/EP-0003-US-0005-TK-0002-voyage-user-path.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0004-cluster-map-and-travel-estimates, EP-0001-US-0006-repeatable-trading-voyage, EP-0001-US-0009-voyage-fuel-cost, EP-0001-US-0011-net-voyage-profit, EP-0001-US-0014-voyage-lifecycle.

## US-0006 — Возобновление торговли между теми же кластерами

Story: [EP-0003-US-0006-resume-cluster-voyage](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0006-resume-cluster-voyage/EP-0003-US-0006-resume-cluster-voyage.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0006-resume-cluster-voyage/EP-0003-US-0006-TK-0001-cluster-save-state/EP-0003-US-0006-TK-0001-cluster-save-state.md) | Состав кластеров и ресурсы в общем сохранении | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0006-resume-cluster-voyage/EP-0003-US-0006-TK-0002-cluster-local-resume/EP-0003-US-0006-TK-0002-cluster-local-resume.md) | Рейс между кластерами после штатного Load | local-client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0006-TK-0001-cluster-save-state](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0006-resume-cluster-voyage/EP-0003-US-0006-TK-0001-cluster-save-state/EP-0003-US-0006-TK-0001-cluster-save-state.md)
- [EP-0003-US-0006-TK-0002-cluster-local-resume](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0006-resume-cluster-voyage/EP-0003-US-0006-TK-0002-cluster-local-resume/EP-0003-US-0006-TK-0002-cluster-local-resume.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0005-local-and-intercluster-voyages, EP-0002-US-0007-resume-generated-system, EP-0001-US-0012-resume-trading-economy.

## US-0007 — Проверяемая экономика длительных путешествий

Story: [EP-0003-US-0007-long-voyage-economy-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0007-long-voyage-economy-evidence/EP-0003-US-0007-long-voyage-economy-evidence.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0007-long-voyage-economy-evidence/EP-0003-US-0007-TK-0001-long-voyage-runner/EP-0003-US-0007-TK-0001-long-voyage-runner.md) | Экономический прогон полной дальней поездки | tooling | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0007-long-voyage-economy-evidence/EP-0003-US-0007-TK-0002-long-voyage-diagnostics/EP-0003-US-0007-TK-0002-long-voyage-diagnostics.md) | Отчёт снабжения и ограничений длительных маршрутов | tooling | TK-0001 + внешние gates | AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0003-US-0007-TK-0001-long-voyage-runner](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0007-long-voyage-economy-evidence/EP-0003-US-0007-TK-0001-long-voyage-runner/EP-0003-US-0007-TK-0001-long-voyage-runner.md)
- [EP-0003-US-0007-TK-0002-long-voyage-diagnostics](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0007-long-voyage-economy-evidence/EP-0003-US-0007-TK-0002-long-voyage-diagnostics/EP-0003-US-0007-TK-0002-long-voyage-diagnostics.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0005-local-and-intercluster-voyages, EP-0001-US-0002-market-replenishment, EP-0001-US-0007-temporary-market-events, EP-0001-US-0008-route-risk-and-alternatives, EP-0001-US-0013-economy-balance-evidence.

## US-0008 — Проверяемая работа полной торговой сети

Story: [EP-0003-US-0008-cluster-scale-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-cluster-scale-evidence.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-TK-0001-cluster-correctness-corpus/EP-0003-US-0008-TK-0001-cluster-correctness-corpus.md) | Корпус геометрии и сохранения полной торговой сети | engine | Внешние gates ниже | AC-0001, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-TK-0002-cluster-performance-report/EP-0003-US-0008-TK-0002-cluster-performance-report.md) | Стоимость максимальной торговой сети | tooling | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-TK-0003-cluster-interaction-evidence/EP-0003-US-0008-TK-0003-cluster-interaction-evidence.md) | Выбор станций на максимальной сети | client | TK-0002 + внешние gates | AC-0002 |

Полные пути тикетов:

- [EP-0003-US-0008-TK-0001-cluster-correctness-corpus](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-TK-0001-cluster-correctness-corpus/EP-0003-US-0008-TK-0001-cluster-correctness-corpus.md)
- [EP-0003-US-0008-TK-0002-cluster-performance-report](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-TK-0002-cluster-performance-report/EP-0003-US-0008-TK-0002-cluster-performance-report.md)
- [EP-0003-US-0008-TK-0003-cluster-interaction-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0003-station-clusters-and-trade-geography/EP-0003-US-0008-cluster-scale-evidence/EP-0003-US-0008-TK-0003-cluster-interaction-evidence/EP-0003-US-0008-TK-0003-cluster-interaction-evidence.md)

Покрытие: AC-0001–0003; зависимости: EP-0003-US-0006-resume-cluster-voyage, EP-0003-US-0007-long-voyage-economy-evidence.

## Gaps, backlog и вопросы

Блокирующих вопросов для нарезки нет. Наличие планового тикета не означает поставленный runtime. Неизвестны hardware results и согласованные численные budgets генерации/snapshot/save/прибыль; они не выдуманы, измерительные тикеты требуют фактический отчёт. Legacy mode сохраняется, старые saves не регенерируются. EP-0003: исходные близкие станции MarketProfiles сохраняют расстояния как явное исключение начальных балансных ориентиров. EP-0004: патрули/бой/реальные эффекты/exploration остаются backlog. Полные assumptions/resolutions находятся в каждой истории.

## Проверка planning artifacts

Проверены канонические ID и пути, наличие всех секций/DoD, зависимости без циклов и отсутствующих ticket IDs, по1–5 тикетов на историю, один layer и максимум5 файлов, покрытие всех24 критериев восьми историй этого эпика. Исходные user story, completion evidence, stage и продуктовые dependencies сохранены. Новые технические зависимости измерительных инструментов указаны в story-файлах и depends_on тикетов.

Общий корпус:24 истории,65 тикетов,72 критерия; структурная проверка без ошибок. Фактические .NET tests/build/performance не запускались: это планирование, команды находятся внутри тикетов.
