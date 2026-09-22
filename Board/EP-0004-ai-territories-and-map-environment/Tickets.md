# Тикеты EP-0004-ai-territories-and-map-environment

Созданы 2026-09-22T14:40:41Z по запросу пользователя. 8 историй, 24 тикетов. Planning complete; production/test execution не выполнялись. Story и epic draft сохранены, ticket approval — автоматический StoryBuilder workflow.

Основной порядок EP-0002 → EP-0003 → EP-0004; EP-0003 также ждёт указанные в story файлах поставки EP-0001. Внутри эпика US-0001→…→US-0008 — безопасный порядок, фактические dependency edges — в frontmatter.

## US-0001 — Различимые враждебные базы ИИ

Story: [EP-0004-US-0001-hostile-ai-bases](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-hostile-ai-bases.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0001-ai-base-contract/EP-0004-US-0001-TK-0001-ai-base-contract.md) | Принадлежность баз и привязки объектов ИИ | contracts | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases.md) | Воспроизводимые планетные и свободные базы | engine | TK-0001 + внешние gates | AC-0001, AC-0002 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0003-authoritative-hostile-access/EP-0004-US-0001-TK-0003-authoritative-hostile-access.md) | Авторитетный запрет стыковки и торговли с ИИ | engine | TK-0002 + внешние gates | AC-0003 |
| [TK-0004](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0004-ai-base-content/EP-0004-US-0001-TK-0004-ai-base-content.md) | Начальные настройки баз ИИ | content-data | TK-0003 + внешние gates | AC-0001, AC-0002 |
| [TK-0005](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0005-ai-base-presentation/EP-0004-US-0001-TK-0005-ai-base-presentation.md) | Выбор базы и различимая принадлежность | client | TK-0004 + внешние gates | AC-0001, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0001-TK-0001-ai-base-contract](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0001-ai-base-contract/EP-0004-US-0001-TK-0001-ai-base-contract.md)
- [EP-0004-US-0001-TK-0002-seeded-ai-bases](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases/EP-0004-US-0001-TK-0002-seeded-ai-bases.md)
- [EP-0004-US-0001-TK-0003-authoritative-hostile-access](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0003-authoritative-hostile-access/EP-0004-US-0001-TK-0003-authoritative-hostile-access.md)
- [EP-0004-US-0001-TK-0004-ai-base-content](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0004-ai-base-content/EP-0004-US-0001-TK-0004-ai-base-content.md)
- [EP-0004-US-0001-TK-0005-ai-base-presentation](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0001-hostile-ai-bases/EP-0004-US-0001-TK-0005-ai-base-presentation/EP-0004-US-0001-TK-0005-ai-base-presentation.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0006-known-system-map, EP-0003-US-0002-distinct-orbital-clusters.

## US-0002 — Движущиеся области будущей угрозы

Story: [EP-0004-US-0002-moving-ai-territories](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-moving-ai-territories.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0001-territory-radii-contract/EP-0004-US-0002-TK-0001-territory-radii-contract.md) | Радиусы с независимыми владельцами | contracts | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0002-moving-territory-data/EP-0004-US-0002-TK-0002-moving-territory-data.md) | Движущиеся области и безопасная стартовая сеть | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0003-territory-rendering/EP-0004-US-0002-TK-0003-territory-rendering.md) | Читаемые радиусы будущей угрозы | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0002-TK-0001-territory-radii-contract](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0001-territory-radii-contract/EP-0004-US-0002-TK-0001-territory-radii-contract.md)
- [EP-0004-US-0002-TK-0002-moving-territory-data](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0002-moving-territory-data/EP-0004-US-0002-TK-0002-moving-territory-data.md)
- [EP-0004-US-0002-TK-0003-territory-rendering](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0002-moving-ai-territories/EP-0004-US-0002-TK-0003-territory-rendering/EP-0004-US-0002-TK-0003-territory-rendering.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0001-hostile-ai-bases.

## US-0003 — Территории ИИ вокруг доступной торговой сети

Story: [EP-0004-US-0003-trade-compatible-ai-placement](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-trade-compatible-ai-placement.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-TK-0001-temporal-placement-validation/EP-0004-US-0003-TK-0001-temporal-placement-validation.md) | Проверка обходов и критических эпох | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-TK-0002-placement-evidence-report/EP-0004-US-0003-TK-0002-placement-evidence-report.md) | Воспроизводимый отчёт геометрического размещения | tooling | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0003-TK-0001-temporal-placement-validation](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-TK-0001-temporal-placement-validation/EP-0004-US-0003-TK-0001-temporal-placement-validation.md)
- [EP-0004-US-0003-TK-0002-placement-evidence-report](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0003-trade-compatible-ai-placement/EP-0004-US-0003-TK-0002-placement-evidence-report/EP-0004-US-0003-TK-0002-placement-evidence-report.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0002-moving-ai-territories, EP-0003-US-0004-cluster-map-and-travel-estimates.

## US-0004 — Различимые пространственные поля

Story: [EP-0004-US-0004-informational-environment-fields](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-informational-environment-fields.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0001-environment-field-contract/EP-0004-US-0004-TK-0001-environment-field-contract.md) | Геометрия и привязки информационных полей | contracts | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0002-seeded-environment-fields/EP-0004-US-0004-TK-0002-seeded-environment-fields.md) | Воспроизводимые поля с проверяемыми привязками | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0003-environment-field-content/EP-0004-US-0004-TK-0003-environment-field-content.md) | Настройки трёх типов пространственных полей | content-data | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0004](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0004-environment-field-rendering/EP-0004-US-0004-TK-0004-environment-field-rendering.md) | Различимые поля и сведения об интенсивности | client | TK-0003 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0004-TK-0001-environment-field-contract](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0001-environment-field-contract/EP-0004-US-0004-TK-0001-environment-field-contract.md)
- [EP-0004-US-0004-TK-0002-seeded-environment-fields](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0002-seeded-environment-fields/EP-0004-US-0004-TK-0002-seeded-environment-fields.md)
- [EP-0004-US-0004-TK-0003-environment-field-content](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0003-environment-field-content/EP-0004-US-0004-TK-0003-environment-field-content.md)
- [EP-0004-US-0004-TK-0004-environment-field-rendering](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0004-informational-environment-fields/EP-0004-US-0004-TK-0004-environment-field-rendering/EP-0004-US-0004-TK-0004-environment-field-rendering.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0003-trade-compatible-ai-placement.

## US-0005 — Известные заброшенные объекты среди ресурсов

Story: [EP-0004-US-0005-known-points-of-interest](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-known-points-of-interest.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0001-poi-contract/EP-0004-US-0005-TK-0001-poi-contract.md) | Известные точки интереса без игровой механики | contracts | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0002-seeded-abandoned-objects/EP-0004-US-0005-TK-0002-seeded-abandoned-objects.md) | Генерация заброшенных объектов рядом с ресурсами | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0003-abandoned-object-content/EP-0004-US-0005-TK-0003-abandoned-object-content.md) | Содержимое заброшенных точек интереса | content-data | TK-0002 + внешние gates | AC-0001, AC-0003 |
| [TK-0004](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0004-poi-map-selection/EP-0004-US-0005-TK-0004-poi-map-selection.md) | Выбор точек интереса среди существующих ресурсов | client | TK-0003 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0005-TK-0001-poi-contract](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0001-poi-contract/EP-0004-US-0005-TK-0001-poi-contract.md)
- [EP-0004-US-0005-TK-0002-seeded-abandoned-objects](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0002-seeded-abandoned-objects/EP-0004-US-0005-TK-0002-seeded-abandoned-objects.md)
- [EP-0004-US-0005-TK-0003-abandoned-object-content](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0003-abandoned-object-content/EP-0004-US-0005-TK-0003-abandoned-object-content.md)
- [EP-0004-US-0005-TK-0004-poi-map-selection](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0005-known-points-of-interest/EP-0004-US-0005-TK-0004-poi-map-selection/EP-0004-US-0005-TK-0004-poi-map-selection.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0004-informational-environment-fields, EP-0003-US-0003-cluster-resource-surroundings.

## US-0006 — Управляемые слои полной карты

Story: [EP-0004-US-0006-readable-map-layers](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0006-readable-map-layers/EP-0004-US-0006-readable-map-layers.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0006-readable-map-layers/EP-0004-US-0006-TK-0001-map-layer-controls/EP-0004-US-0006-TK-0001-map-layer-controls.md) | Переключаемые слои и приоритет взаимодействия | client | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0006-TK-0001-map-layer-controls](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0006-readable-map-layers/EP-0004-US-0006-TK-0001-map-layer-controls/EP-0004-US-0006-TK-0001-map-layer-controls.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0005-known-points-of-interest, EP-0003-US-0004-cluster-map-and-travel-estimates.

## US-0007 — Продолжение мира с прежними территориями и полями

Story: [EP-0004-US-0007-resume-territories-and-fields](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-resume-territories-and-fields.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-TK-0001-map-environment-save/EP-0004-US-0007-TK-0001-map-environment-save.md) | Сохранение баз, областей, полей и точек интереса | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-TK-0002-full-map-local-load/EP-0004-US-0007-TK-0002-full-map-local-load.md) | Продолжение полной карты через файловое сохранение | local-client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0007-TK-0001-map-environment-save](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-TK-0001-map-environment-save/EP-0004-US-0007-TK-0001-map-environment-save.md)
- [EP-0004-US-0007-TK-0002-full-map-local-load](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0007-resume-territories-and-fields/EP-0004-US-0007-TK-0002-full-map-local-load/EP-0004-US-0007-TK-0002-full-map-local-load.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0006-readable-map-layers, EP-0003-US-0006-resume-cluster-voyage.

## US-0008 — Проверяемая работа карты со всеми слоями

Story: [EP-0004-US-0008-complete-map-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-complete-map-evidence.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0001-full-map-correctness-corpus/EP-0004-US-0008-TK-0001-full-map-correctness-corpus.md) | Корпус полной карты и отсутствия побочных эффектов | engine | Внешние gates ниже | AC-0001, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0002-full-map-performance-report/EP-0004-US-0008-TK-0002-full-map-performance-report.md) | Сводный отчёт полной карты со всеми слоями | tooling | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0003-full-map-render-evidence/EP-0004-US-0008-TK-0003-full-map-render-evidence.md) | Проверка взаимодействия и кадров полной карты | client | TK-0002 + внешние gates | AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0004-US-0008-TK-0001-full-map-correctness-corpus](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0001-full-map-correctness-corpus/EP-0004-US-0008-TK-0001-full-map-correctness-corpus.md)
- [EP-0004-US-0008-TK-0002-full-map-performance-report](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0002-full-map-performance-report/EP-0004-US-0008-TK-0002-full-map-performance-report.md)
- [EP-0004-US-0008-TK-0003-full-map-render-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0004-ai-territories-and-map-environment/EP-0004-US-0008-complete-map-evidence/EP-0004-US-0008-TK-0003-full-map-render-evidence/EP-0004-US-0008-TK-0003-full-map-render-evidence.md)

Покрытие: AC-0001–0003; зависимости: EP-0004-US-0007-resume-territories-and-fields, EP-0003-US-0008-cluster-scale-evidence, EP-0002-US-0008-system-generation-evidence.

## Gaps, backlog и вопросы

Блокирующих вопросов для нарезки нет. Наличие планового тикета не означает поставленный runtime. Неизвестны hardware results и согласованные численные budgets генерации/snapshot/save/прибыль; они не выдуманы, измерительные тикеты требуют фактический отчёт. Legacy mode сохраняется, старые saves не регенерируются. EP-0003: исходные близкие станции MarketProfiles сохраняют расстояния как явное исключение начальных балансных ориентиров. EP-0004: патрули/бой/реальные эффекты/exploration остаются backlog. Полные assumptions/resolutions находятся в каждой истории.

## Проверка planning artifacts

Проверены канонические ID и пути, наличие всех секций/DoD, зависимости без циклов и отсутствующих ticket IDs, по1–5 тикетов на историю, один layer и максимум5 файлов, покрытие всех24 критериев восьми историй этого эпика. Исходные user story, completion evidence, stage и продуктовые dependencies сохранены. Новые технические зависимости измерительных инструментов указаны в story-файлах и depends_on тикетов.

Общий корпус:24 истории,65 тикетов,72 критерия; структурная проверка без ошибок. Фактические .NET tests/build/performance не запускались: это планирование, команды находятся внутри тикетов.
