# Тикеты EP-0002-procedural-solar-system

Созданы 2026-09-22T14:40:41Z по запросу пользователя. 8 историй, 21 тикетов. Planning complete; production/test execution не выполнялись. Story и epic draft сохранены, ticket approval — автоматический StoryBuilder workflow.

Основной порядок EP-0002 → EP-0003 → EP-0004; EP-0003 также ждёт указанные в story файлах поставки EP-0001. Внутри эпика US-0001→…→US-0008 — безопасный порядок, фактические dependency edges — в frontmatter.

## US-0001 — Новая игра в воспроизводимой Солнечной системе

Story: [EP-0002-US-0001-seeded-system-start](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-seeded-system-start.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0001-system-map-contract/EP-0002-US-0001-TK-0001-system-map-contract.md) | Сериализуемая структура системы и точные орбитальные данные | contracts | Внешние gates ниже | AC-0001, AC-0002 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0002-generation-input-schema/EP-0002-US-0001-TK-0002-generation-input-schema.md) | Проверяемая конфигурация генерации и вход загрузчика | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0003-seeded-world-bootstrap/EP-0002-US-0001-TK-0003-seeded-world-bootstrap.md) | Атомарная генерация Default с масштабом 50–75 дней | engine | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0004](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0004-default-system-content/EP-0002-US-0001-TK-0004-default-system-content.md) | Настройки новой системы для Default | content-data | TK-0003 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0005](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0005-initial-system-view/EP-0002-US-0001-TK-0005-initial-system-view.md) | Первый обзор Солнца, планет и границ поясов | client | TK-0004 + внешние gates | AC-0001, AC-0002 |

Полные пути тикетов:

- [EP-0002-US-0001-TK-0001-system-map-contract](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0001-system-map-contract/EP-0002-US-0001-TK-0001-system-map-contract.md)
- [EP-0002-US-0001-TK-0002-generation-input-schema](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0002-generation-input-schema/EP-0002-US-0001-TK-0002-generation-input-schema.md)
- [EP-0002-US-0001-TK-0003-seeded-world-bootstrap](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0003-seeded-world-bootstrap/EP-0002-US-0001-TK-0003-seeded-world-bootstrap.md)
- [EP-0002-US-0001-TK-0004-default-system-content](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0004-default-system-content/EP-0002-US-0001-TK-0004-default-system-content.md)
- [EP-0002-US-0001-TK-0005-initial-system-view](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0001-seeded-system-start/EP-0002-US-0001-TK-0005-initial-system-view/EP-0002-US-0001-TK-0005-initial-system-view.md)

Покрытие: AC-0001–0003; зависимости: none.

## US-0002 — Согласованное движение орбитального мира

Story: [EP-0002-US-0002-coherent-orbital-world](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-coherent-orbital-world.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-TK-0001-absolute-orbit-math/EP-0002-US-0002-TK-0001-absolute-orbit-math.md) | Общая аналитическая поза и скорость орбиты | motion | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-TK-0002-authoritative-orbit-runtime/EP-0002-US-0002-TK-0002-authoritative-orbit-runtime.md) | Орбиты и пристыкованные корабли в авторитетном мире | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-TK-0003-orbital-client-prediction/EP-0002-US-0002-TK-0003-orbital-client-prediction.md) | Плавный прогноз орбит из клиентского буфера | client | TK-0002 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0002-TK-0001-absolute-orbit-math](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-TK-0001-absolute-orbit-math/EP-0002-US-0002-TK-0001-absolute-orbit-math.md)
- [EP-0002-US-0002-TK-0002-authoritative-orbit-runtime](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-TK-0002-authoritative-orbit-runtime/EP-0002-US-0002-TK-0002-authoritative-orbit-runtime.md)
- [EP-0002-US-0002-TK-0003-orbital-client-prediction](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0002-coherent-orbital-world/EP-0002-US-0002-TK-0003-orbital-client-prediction/EP-0002-US-0002-TK-0003-orbital-client-prediction.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0001-seeded-system-start.

## US-0003 — Наполненные астероидные пояса

Story: [EP-0002-US-0003-playable-asteroid-belts](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0003-playable-asteroid-belts/EP-0002-US-0003-playable-asteroid-belts.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0003-playable-asteroid-belts/EP-0002-US-0003-TK-0001-seeded-belt-asteroids/EP-0002-US-0003-TK-0001-seeded-belt-asteroids.md) | Детерминированные игровые астероиды в поясах | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0003-playable-asteroid-belts/EP-0002-US-0003-TK-0002-belt-detail-rendering/EP-0002-US-0003-TK-0002-belt-detail-rendering.md) | Неоднородные пояса и выбор игровых астероидов | client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0003-TK-0001-seeded-belt-asteroids](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0003-playable-asteroid-belts/EP-0002-US-0003-TK-0001-seeded-belt-asteroids/EP-0002-US-0003-TK-0001-seeded-belt-asteroids.md)
- [EP-0002-US-0003-TK-0002-belt-detail-rendering](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0003-playable-asteroid-belts/EP-0002-US-0003-TK-0002-belt-detail-rendering/EP-0002-US-0003-TK-0002-belt-detail-rendering.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0002-coherent-orbital-world.

## US-0004 — Привычные сценарии в новом масштабе

Story: [EP-0002-US-0004-preserved-scenario-starts](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0004-preserved-scenario-starts/EP-0002-US-0004-preserved-scenario-starts.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0004-preserved-scenario-starts/EP-0002-US-0004-TK-0001-scenario-group-translation/EP-0002-US-0004-TK-0001-scenario-group-translation.md) | Перенос сценарных групп с сохранением связей | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0004-preserved-scenario-starts/EP-0002-US-0004-TK-0002-all-scenario-system-content/EP-0002-US-0004-TK-0002-all-scenario-system-content.md) | Включение процедурной системы для всех игровых стартов | content-data | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0004-TK-0001-scenario-group-translation](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0004-preserved-scenario-starts/EP-0002-US-0004-TK-0001-scenario-group-translation/EP-0002-US-0004-TK-0001-scenario-group-translation.md)
- [EP-0002-US-0004-TK-0002-all-scenario-system-content](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0004-preserved-scenario-starts/EP-0002-US-0004-TK-0002-all-scenario-system-content/EP-0002-US-0004-TK-0002-all-scenario-system-content.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0003-playable-asteroid-belts.

## US-0005 — Посещение движущейся станции

Story: [EP-0002-US-0005-orbital-station-visit](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0005-orbital-station-visit/EP-0002-US-0005-orbital-station-visit.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0005-orbital-station-visit/EP-0002-US-0005-TK-0001-orbital-synchronization/EP-0002-US-0005-TK-0001-orbital-synchronization.md) | Синхронизация корабля с движущейся станцией | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0005-orbital-station-visit/EP-0002-US-0005-TK-0002-orbital-docking-departure/EP-0002-US-0005-TK-0002-orbital-docking-departure.md) | Стыковка через диалог и непрерывный уход | engine | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0005-TK-0001-orbital-synchronization](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0005-orbital-station-visit/EP-0002-US-0005-TK-0001-orbital-synchronization/EP-0002-US-0005-TK-0001-orbital-synchronization.md)
- [EP-0002-US-0005-TK-0002-orbital-docking-departure](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0005-orbital-station-visit/EP-0002-US-0005-TK-0002-orbital-docking-departure/EP-0002-US-0005-TK-0002-orbital-docking-departure.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0004-preserved-scenario-starts.

## US-0006 — Читаемая полностью известная карта

Story: [EP-0002-US-0006-known-system-map](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0006-known-system-map/EP-0002-US-0006-known-system-map.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0006-known-system-map/EP-0002-US-0006-TK-0001-known-map-projection/EP-0002-US-0006-TK-0001-known-map-projection.md) | Авторитетно известная география новой системы | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0006-known-system-map/EP-0002-US-0006-TK-0002-system-map-navigation/EP-0002-US-0006-TK-0002-system-map-navigation.md) | Слой орбит, кадрирование и приоритет выбора | client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0006-TK-0001-known-map-projection](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0006-known-system-map/EP-0002-US-0006-TK-0001-known-map-projection/EP-0002-US-0006-TK-0001-known-map-projection.md)
- [EP-0002-US-0006-TK-0002-system-map-navigation](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0006-known-system-map/EP-0002-US-0006-TK-0002-system-map-navigation/EP-0002-US-0006-TK-0002-system-map-navigation.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0003-playable-asteroid-belts, EP-0002-US-0004-preserved-scenario-starts.

## US-0007 — Продолжение той же системы после загрузки

Story: [EP-0002-US-0007-resume-generated-system](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0007-resume-generated-system/EP-0002-US-0007-resume-generated-system.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0007-resume-generated-system/EP-0002-US-0007-TK-0001-generated-world-persistence/EP-0002-US-0007-TK-0001-generated-world-persistence.md) | Сохранение структуры системы и старого мира | engine | Внешние gates ниже | AC-0001, AC-0002, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0007-resume-generated-system/EP-0002-US-0007-TK-0002-local-world-save-roundtrip/EP-0002-US-0007-TK-0002-local-world-save-roundtrip.md) | Продолжение системы через локальный транспорт сохранений | local-client | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0007-TK-0001-generated-world-persistence](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0007-resume-generated-system/EP-0002-US-0007-TK-0001-generated-world-persistence/EP-0002-US-0007-TK-0001-generated-world-persistence.md)
- [EP-0002-US-0007-TK-0002-local-world-save-roundtrip](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0007-resume-generated-system/EP-0002-US-0007-TK-0002-local-world-save-roundtrip/EP-0002-US-0007-TK-0002-local-world-save-roundtrip.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0005-orbital-station-visit, EP-0002-US-0006-known-system-map.

## US-0008 — Проверяемая устойчивость генерации системы

Story: [EP-0002-US-0008-system-generation-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-system-generation-evidence.md).

| ID | Title | Layer | Dependencies | Served criteria |
|---|---|---|---|---|
| [TK-0001](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-TK-0001-system-correctness-corpus/EP-0002-US-0008-TK-0001-system-correctness-corpus.md) | Воспроизводимый корпус генерации по 100 seed | engine | Внешние gates ниже | AC-0001, AC-0003 |
| [TK-0002](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-TK-0002-system-performance-report/EP-0002-US-0008-TK-0002-system-performance-report.md) | Измерительный отчёт генерации, снимков и raster кадров | tooling | TK-0001 + внешние gates | AC-0001, AC-0002, AC-0003 |
| [TK-0003](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-TK-0003-presented-frame-evidence/EP-0002-US-0008-TK-0003-presented-frame-evidence.md) | Проверка отображения и реальных кадров клиента | client | TK-0002 + внешние gates | AC-0002, AC-0003 |

Полные пути тикетов:

- [EP-0002-US-0008-TK-0001-system-correctness-corpus](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-TK-0001-system-correctness-corpus/EP-0002-US-0008-TK-0001-system-correctness-corpus.md)
- [EP-0002-US-0008-TK-0002-system-performance-report](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-TK-0002-system-performance-report/EP-0002-US-0008-TK-0002-system-performance-report.md)
- [EP-0002-US-0008-TK-0003-presented-frame-evidence](D:/DeepSpaceSaga/DSS/Board/EP-0002-procedural-solar-system/EP-0002-US-0008-system-generation-evidence/EP-0002-US-0008-TK-0003-presented-frame-evidence/EP-0002-US-0008-TK-0003-presented-frame-evidence.md)

Покрытие: AC-0001–0003; зависимости: EP-0002-US-0007-resume-generated-system.

## Gaps, backlog и вопросы

Блокирующих вопросов для нарезки нет. Наличие планового тикета не означает поставленный runtime. Неизвестны hardware results и согласованные численные budgets генерации/snapshot/save/прибыль; они не выдуманы, измерительные тикеты требуют фактический отчёт. Legacy mode сохраняется, старые saves не регенерируются. EP-0003: исходные близкие станции MarketProfiles сохраняют расстояния как явное исключение начальных балансных ориентиров. EP-0004: патрули/бой/реальные эффекты/exploration остаются backlog. Полные assumptions/resolutions находятся в каждой истории.

## Проверка planning artifacts

Проверены канонические ID и пути, наличие всех секций/DoD, зависимости без циклов и отсутствующих ticket IDs, по1–5 тикетов на историю, один layer и максимум5 файлов, покрытие всех24 критериев восьми историй этого эпика. Исходные user story, completion evidence, stage и продуктовые dependencies сохранены. Новые технические зависимости измерительных инструментов указаны в story-файлах и depends_on тикетов.

Общий корпус:24 истории,65 тикетов,72 критерия; структурная проверка без ошибок. Фактические .NET tests/build/performance не запускались: это планирование, команды находятся внутри тикетов.
