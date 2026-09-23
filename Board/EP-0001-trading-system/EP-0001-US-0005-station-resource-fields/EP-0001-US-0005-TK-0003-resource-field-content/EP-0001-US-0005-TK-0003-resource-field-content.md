---
epic: EP-0001-trading-system
story: EP-0001-US-0005-station-resource-fields
ticket: EP-0001-US-0005-TK-0003-resource-field-content
title: Профили ресурсного окружения
stage: approved
layer: content-data
depends_on: [EP-0001-US-0005-TK-0002-seeded-resource-fields]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-07]
created: 2026-09-21T09:56:58Z
revision: 1
---

# Профили ресурсного окружения

## Why

Связать экономические роли пяти станций с проверяемым стартовым ресурсным окружением. Профили, вероятности и геометрия должны загружаться из runtime JSON, чтобы изменение баланса не требовало менять генератор.

## Decisions

Отдельных решений пользователя нет. Исходный запрос создания тикетов записан в story; баланс ниже — явный planning assumption.

## Assumptions

- Используется схема TK-0002 без дополнительных runtime сущностей. Значения — стартовый tuning baseline, не оценка доходности добычи.
- Settings объявляет путь, но генерация требует materialized TradingMap и режима New Game. Не нужно добавлять opt-in во все старые сценарии.
- Fractions показывают минералогию; не создают груз и не обещают yield.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Settings.json | Settings.json:2 — typeData paths | Добавить только stationResourceFields: Data/World/station-resource-fields.json |
| src/DeepSpaceSaga.Client/Data/World/station-resource-fields.json | Новый файл; схема полностью задана в TK-0002, ресурсные IDs подтверждены Data/Items/Resource/items-resource.json:4,16,28,40,64 | Добавить один тематический JSON schemaVersion1 с таблицами ниже |
| tests/DeepSpaceSaga.Client.Tests/StationResourceFieldContentTests.cs | Новый файл; существующее разрешение Engine internals для Client tests: src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj:17 | Проверки реального контента/Settings и всех MVP bootstrap variants без graphics |

Matching test project: `D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No API change. Settings.typeData.stationResourceFields — optional path из TK-0002. JSON использует точные camelCase properties его StationResourceFieldConfig. Копирование нового файла уже обеспечено src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:264; project-файл не редактировать.

## Implementation steps

1. Создать JSON: schemaVersion1, firstObjectNumber1000, maxPlacementAttempts64, innerRadiusKm2.5, outerRadiusKm4.5, stationClearanceKm2, asteroidSpacingKm0.1, corridorHalfWidthKm0.25. structuralScan: rangeKm120, durationGameTimeMs60000, successChancePercent85.
2. Добавить roles с exact MarketProfileId и fields(FieldKindId,AsteroidCount):

| marketProfileId | Fields | Total |
|---|---|---|
| market.mining | metal12, ice6, carbon3 | 21 |
| market.industrial | mixed9, carbon3 | 12 |
| market.hydroponic | ice12 | 12 |
| market.transit | mixed6 | 6 |
| market.scientific-military | mixed6 | 6 |

3. Добавить fieldKinds с variants ниже. В fractions использовать полные ItemTypeId: iron=item.iron-ore; magnesium=item.magnesium-ore; silicon=item.silicon; carbon=item.carbon-ore; ice=item.ice. Числа — Permille, каждый variant sum1000. Только fieldKind carbon считается carbon-bearing; присутствие следовой доли carbon в остальных не меняет класс поля.

| fieldKindId | variantId | compositionType | weight | resources |
|---|---|---|---|---|
| metal | iron-rich | Iron | 80 | iron800, magnesium150, carbon50 |
| metal | magnesium-rich | Iron | 20 | iron650, magnesium300, carbon50 |
| ice | ice-rich | Ice | 80 | ice950, carbon50 |
| ice | ice-mixed | Ice | 20 | ice850, iron100, carbon50 |
| mixed | mixed-common | Silicate | 80 | iron450, magnesium400, silicon100, carbon50 |
| mixed | mixed-low-silicon | Silicate | 20 | iron500, magnesium400, silicon50, carbon50 |
| carbon | carbon-rich | Silicate | 80 | carbon800, iron100, silicon100 |
| carbon | carbon-mixed | Silicate | 20 | carbon750, iron200, silicon50 |

4. massBands: small [1000000,10000000] weight60; medium [10000001,100000000] weight30; large [100000001,500000000] weight8; very-large [500000001,1000000000] weight2. Диапазоны inclusive; не менять действующие требования и item catalog.
5. Добавить Settings path и headless tests, загружающие реальные Settings/catalog/config. Через завершённый US-0004 bootstrap проверить Default, Default_Docked, Default_Undocked с одинаковым seed. Сравнивать manifest/массы/позиции/fractions; отличия исходного положения/статуса корабля не относятся к resource fields.
6. Проверить geometry corpus seeds0..255 плюс ulong.MaxValue для каждого доступного layout US-0004. Если реальные layouts не вмещают поля, тикет не готов: записать конкретный seed/layout и вернуться к согласованию geometry assumptions в story; запрещено пропускать seed, уменьшать counts или добавлять hidden fallback. Отдельно проверить Default_500 и legacy scenario без TradingMap: ни одного нового поля.

## Out of scope

Редактирование scenario JSON, market profiles, project-файлов, US-0004 layouts, item types, UI, добычи, production rates, performance measurements. Не менять Default_500.

## Invariants

- Coarse composition остаётся Ice/Silicate/Iron: Documentation/01-Requirements/EngineRequirements.md:1227–1267.
- Масса/веса: EngineRequirements.md:1461–1504; StructuralScan range/time/chance:1404–1432.
- Item IDs существуют: Data/Items/Resource/items-resource.json:4,16,28,40,64.
- Content assets recursive copy: DeepSpaceSaga.Client.csproj:264.
- 57 generated asteroids; carbon-bearing6 у двух станций, metal12/ice18/mixed21. Редкость оценивается числом объектов, не их случайной суммарной массой.

## Tests

StationResourceFieldContentTests:
- `Real_settings_load_resource_rules_and_registered_items` — AC-01; schema, references, fractions и weights.
- `Profiles_produce_57_objects_with_carbon_at_two_stations` — AC-01; точные counts/class proportions.
- `Reordered_real_json_keeps_same_materialized_fields` — AC-02; shuffle всех списков с фиксированным test seed.
- `Every_trading_layout_fits_real_fields_for_seed_corpus` — AC-03; все constraints TK-0002, без skipped seeds.
- `Mvp_scenarios_share_resource_fields_for_same_master_seed` — AC-07; три реальных сценария.
- `Default_500_and_legacy_without_map_remain_unchanged` — AC-07; 502 исходных объекта, no manifest; файл Default_500 не меняется. Grounding SHA256: 8E31E0462D2B893A54A0778234C3044DC4230D0BC9F4D278ADB8EB32BBE6A526; проверять относительно актуального baseline, если другой автор явно изменил fixture.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Client.Tests\DeepSpaceSaga.Client.Tests.csproj --no-restore --filter FullyQualifiedName~StationResourceFieldContentTests
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Client\DeepSpaceSaga.Client.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Steps выполнены только в трёх разрешённых файлах; реальный JSON загружается через Settings.
- Все served criteria покрыты named tests; counts57 и полная geometry corpus проходят.
- Tests/build/format проходят либо конкретные исходные failures записаны отдельно; failure размещения не считается внешним baseline.
- API/invariants/non-goals соблюдены; скрытых tuning changes и незаписанных assumptions нет.
- Проверка не создаёт постоянных QA/performance artifacts; результата достаточно для выполнения TK-0004.

## Self-containment check

Все JSON keys заданы контрактом TK-0002, значения/профили/items/варианты перечислены здесь. Tests используют уже разрешённые Engine internals; дополнительных production-файлов не требуется. Входной prerequisite US-0004 явно обязателен, не подменяется догадкой о карте.
