---
epic: EP-0001-trading-system
story: EP-0001-US-0004-seeded-trading-map
ticket: EP-0001-US-0004-TK-0001-trading-map-schema
title: Схема запроса и сохранённой карты
stage: approved
layer: engine
depends_on: [EP-0001-US-0001-TK-0002-profile-market-bootstrap]
files_touched: 4
serves: [AC-05, AC-06]
created: 2026-09-21T09:16:09Z
revision: 1
---

# Схема запроса и сохранённой карты

## Why

Явно различить вход генерации и готовую сеть, чтобы save нельзя было принять за повторный New Game. End state: strict JSON reader принимает валидные map blocks, отклоняет повреждённые, round-trip сохраняет данные и RNG.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj. Все относительные пути от D:/DeepSpaceSaga/DSS.

## Decisions

Единственное сообщение пользователя: «сделай следующую стори»; зафиксировано 2026-09-21T09:16:09Z UTC. Технических решений пользователь не добавлял.

## Assumptions

Схема additive: старые GameStateData без обоих полей валидны. SaveFormat после US-0001 >=8 не понижать; номер этой истории не резервировать, внутренний schemaVersion=1. Global catalog/profile compatibility сохраняется. ID станций задаются конфигом, новые глобальные счётчики объектов не нужны.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioData.cs | :51–73 GameStateData optional fields; dependency US-0001 дополняет SpaceObjectData | Добавить два optional map fields в конец GameStateData; не менять остальные DTO/version |
| src/DeepSpaceSaga.Engine/Scenario/TradingMapData.cs | Новый файл; map DTO отсутствуют в GameStateData :51–73 | Все record DTO ниже и structural validator, без генерации/registry |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs | :27–30 strict parser; :76–99 normalize; :129 Validate | Вызвать structural validator; канонизировать map object references через existing IDs + declared station IDs |
| tests/DeepSpaceSaga.Engine.Tests/TradingMapSchemaTests.cs | Новый файл; образец serialize/load API: ScenarioLoaderTests.cs:9–35 | Самодостаточные JSON fixtures и schema regressions |

## Public API after the change

В конце GameStateData:
```csharp
[property: JsonPropertyName("tradingMapGeneration")] TradingMapGenerationData? TradingMapGeneration = null,
[property: JsonPropertyName("tradingMap")] TradingMapStateData? TradingMap = null
```
Все следующие public sealed record находятся в namespace DeepSpaceSaga.Engine.Scenario. Каждое свойство имеет JsonPropertyName camelCase (включая `schemaVersion`, `riskProfileId`, `counter`). Коллекции IReadOnlyList<T>; значения — обязательные параметры без неявных default:
```csharp
TradingMapGenerationData(int SchemaVersion, string StartStationObjectId,
    double ReferenceSpeedMps, long ShortMaxGameTimeMs, long MediumMaxGameTimeMs,
    long MaxTravelGameTimeMs, double MinStationDistanceKm, double ClearanceKm,
    IReadOnlyList<TradingMapStationData> Stations,
    IReadOnlyList<TradingMapTemplateData> Templates,
    IReadOnlyList<TradingMapRiskData> RiskProfiles);
TradingMapStationData(string ObjectId, string MarketProfileId, string Name, string StationSize);
TradingMapTemplateData(string TemplateId, IReadOnlyList<TradingMapLinkData> Links,
    IReadOnlyList<TradingMapOffsetData> Offsets);
TradingMapLinkData(string FromStationObjectId, string ToStationObjectId, string RiskProfileId);
TradingMapOffsetData(string StationObjectId, double X, double Y);
TradingMapRiskData(string RiskProfileId, int FuelMultiplierPermille);
TradingMapRngData(string Name, ulong Seed, ulong Counter);
TradingMapCargoFlowData(string FromStationObjectId, string ToStationObjectId,
    IReadOnlyList<string> ItemTypeIds);
TradingMapEdgeData(string FromStationObjectId, string ToStationObjectId,
    double DistanceKm, long TravelEstimateGameTimeMs, string DistanceClass,
    int FuelMultiplierPermille, string RiskProfileId);
TradingMapStateData(int SchemaVersion, string TemplateId, int QuarterTurns,
    TradingMapGenerationData Rules, IReadOnlyList<TradingMapEdgeData> Edges,
    IReadOnlyList<TradingMapCargoFlowData> CargoFlows,
    IReadOnlyList<TradingMapRngData> RngStreams);
```

Internal helper в новом файле: `static GameStateData TradingMapDataValidation.ValidateAndNormalize(GameStateData state, int saveFormatVersion)`. Ошибки — ScenarioException с точным JSON field и object/template ID. No Contracts API change.

## Implementation steps

1. Добавить DTO/fields. Отсутствующие required reference fields, null элементы и пустые коллекции, кроме CargoFlows при intermediate DTO construction (но не при чтении готового state), отклонять явно. `tradingMapGeneration` и `tradingMap` вместе запрещены. Request разрешён только при SaveFormatVersion=0 и GameTimeMs=0; `isSave` дополнительно проверит TK-0004. Result разрешён и в контрольном materialized scenario, и в save; отсутствие обоих сохраняет legacy behavior.
2. Rules: schemaVersion=1, ровно пять Stations и по одному profile ID market.transit/mining/industrial/hydroponic/scientific-military; это фиксированный состав MVP, без зашитых rates. StartStationObjectId соответствует transit, уже существующему Station из SpaceObjects, размер Large. Другие четыре station ID не пересекаются с исходными SpaceObjects для request; для result все пять существуют и имеют тип Station. IDs уникальны без учёта регистра; привести ссылки к объявленному написанию. Profiles/items — ordinal type IDs. Имена непустые, size один из Outpost/Medium/Large/Huge.
3. Rules numeric validation: все double finite, referenceSpeed>0, 0<short<medium<max, 0<clearance<minDistance; risks уникальны, непусты, multipliers>0. Templates непусты с уникальными TemplateId; каждый содержит ровно пять offsets с (0,0) для start, finite X/Y. Links только существующие endpoints и risks; без self-loop и дубликатов даже в обратном порядке. Не проверять товарные ссылки без registry.
4. Result: version=1, TemplateId существует в Rules, QuarterTurns 0..3; пять materialized stations соответствуют Rules profile IDs/size (dependency US-0001: SpaceObjectData.MarketProfileId). Рёбра имеют тот же набор endpoints/risk, что выбранный template. Class строго Short/Medium/Long, distance/travel/fuel положительны и finite; cargo flows содержат разные существующие endpoints на одном ребре, непустые уникальные item IDs, без повторов направленной пары. Структурные ограничения не заменяют semantic проверки TK-0002/0003/0004.
5. RngStreams результата: ровно `TradingMap.Topology` и `TradingMap.Geometry`, уникальные; counter>=10000. Округлить некратный 10 counter вверх checked arithmetic и выдать System.Diagnostics.Trace.TraceWarning с именем/старым/новым значением, как требует EngineRequirements.md:348. Переполнение — ScenarioException. Семантический seed match c masterSeed проверит bootstrap. Serialization сохраняет уже нормализованные counters, а не отбрасывает их. Result без masterSeed отклонять: карту нельзя восстановить с иным seed.
6. Канонизировать/сортировать Stations и Offsets по ObjectId ordinal, Templates/Risks по их ID, endpoints каждого неориентированного ребра по ordinal; flows по from/to, items ordinal, streams по Name. Вызвать validator после штатной нормализации SpaceObjects; неизвестные ссылки map не очищать молча.

## Out of scope

Генерация, registry lookup, изменение RNG других подсистем, смена global SaveFormat/catalog version, миграции несовместимых профилей, new Contracts или UI.

## Invariants

- Strict unknown-field detection остаётся: ScenarioLoader.cs:27–30.
- Global catalog/save checks остаются: ScenarioLoader.cs:131–138; SimulationEngine.cs:200–204.
- MasterSeed ulong и stream counter policy: EngineRequirements.md:321–348.
- Production только Engine; тесты только matching project; никакой работы вне четырёх файлов таблицы.

## Tests

Класс TradingMapSchemaTests, тесты:
- `Legacy_scenario_without_map_fields_roundtrips` (AC-06).
- `Generation_and_materialized_map_each_roundtrip_all_fields` (AC-05/06).
- `Request_and_result_are_mutually_exclusive` и `Generation_request_is_rejected_in_versioned_or_nonzero_time_save` (AC-06).
- `Map_schema_rejects_bad_versions_ids_profiles_numbers_and_links` (Theory: missing/null, NaN/Infinity via typed DTO, duplicate/case IDs, unknown endpoint/risk, self-loop, reversed duplicate, bad class).
- `Result_requires_materialized_stations_seed_and_two_rng_streams` (AC-06).
- `Counter_is_rounded_up_without_overflow_and_persisted` (AC-05/06); Trace listener проверяет warning.
- `Input_order_does_not_change_normalized_map_json` (AC-05).

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradingMapSchemaTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Все шаги выполнены только в четырёх разрешённых файлах; matching layer build/format и named tests пройдены либо конкретное исходное падение записано отдельно.
- AC-05/06 покрыты named roundtrip/validation/order tests в объёме схемы; генерация и runtime roundtrip явно принадлежат зависимым тикетам.
- Полный API выше реализован, legacy default и invariants сохранены; ошибки имеют field/ID context.
- Нет скрытых files, незаписанных assumptions или блокирующих вопросов; результат проверяем по сериализации, тесту и diff.

## Self-containment check

Схема, casing, canonical ordering, defaults, ошибки и dependency fields приведены полностью. Ни генератор, ни будущий UI не требуются для выполнения и проверки этого merge unit.
