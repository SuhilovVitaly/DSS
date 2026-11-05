---
epic: EP-0001-trading-system
story: EP-0001-US-0004-seeded-trading-map
ticket: EP-0001-US-0004-TK-0002-economic-graph
title: Seeded граф и грузовые связи
stage: approved
layer: engine
depends_on: [EP-0001-US-0004-TK-0001-trading-map-schema, EP-0001-US-0001-TK-0001-market-profile-schema]
files_touched: 2
serves: [AC-02, AC-05]
created: 2026-09-21T09:16:09Z
revision: 1
---

# Seeded граф и грузовые связи

## Why

Случайность выбирает только проверенные экономические сети. End state: pure generator возвращает простой связный граф с несколькими циклами и проверяемыми cargo flows, независимо от геометрии и runtime.

Matching test project: D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj. Пути относительно D:/DeepSpaceSaga/DSS.

## Decisions

«сделай следующую стори» — единственное сообщение, фиксация 2026-09-21T09:16:09Z UTC. Отдельных технических решений пользователя нет.

## Assumptions

Topological cycles не означают гарантированную прибыль. Supply/Demand — потенциальная совместимость, не резервирование груза или проверка текущего stock. Transit может только потреблять. Требование «у каждого производителя потребитель» не означает «каждый supply item востребован».

## Code context

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/TradingGraphGenerator.cs | Новый файл; seed primitive: RngStreamSeedDerivation.DeriveStreamSeed(ulong, string); профиль: StationMarketProfileDefinition.cs:9–18 | Pure graph planner, semantic graph validation, два internal result records и helper для управляемого map RNG |
| tests/DeepSpaceSaga.Engine.Tests/TradingGraphGeneratorTests.cs | Новый файл; registry lookup contract: TypeRegistry.cs:50–71 | In-memory profiles/config, graph invariants/determinism/negative tests без runtime content |

## Public API after the change

Нет нового Contracts/public session API. В namespace DeepSpaceSaga.Engine.Scenario:
```csharp
internal sealed record TradingGraphPlan(TradingMapGenerationData Rules,
    TradingMapTemplateData Template,
    IReadOnlyList<TradingMapCargoFlowData> CargoFlows,
    TradingMapRngData TopologyStream);
internal static class TradingGraphGenerator
{
    internal static TradingGraphPlan Generate(TradingMapGenerationData rules,
        ulong masterSeed, GameDataRegistry registry);
    internal static IReadOnlyList<TradingMapCargoFlowData> ValidateTemplate(
        TradingMapGenerationData rules, TradingMapTemplateData template,
        GameDataRegistry registry);
}
internal static class TradingMapRandom
{
    internal static (int Value, TradingMapRngData State) DrawIndex(
        ulong masterSeed, string streamName, int exclusiveMax);
}
```
Dependency schema: Rules.Stations содержит ObjectId/MarketProfileId/Name/StationSize; Templates содержит TemplateId, Links(FromStationObjectId,ToStationObjectId,RiskProfileId), Offsets(StationObjectId,X,Y). Rules.StartStationObjectId — transit. CargoFlow хранит From/To и отсортированные ItemTypeIds. Registry.StationMarketProfiles.GetDefinition(GetIndex(profileId)) предоставляет SupplyItemTypeIds/DemandItemTypeIds, Fingerprint и InitialInventory (US-0001). Registry.ItemTypes содержит каталог. Не менять dependency files.

## Implementation steps

1. ValidateTemplate работает на canonical копиях, не изменяет config. Проверить все profile/item ссылки через registry и все пять ролей. Unknown reference — ScenarioException c template/profile/item ID (ContentException registry обернуть). Не использовать DisplayName/индекс загрузки для назначения роли.
2. Для каждого шаблона построить adjacency по пяти station ID; запрещены петли, повторные ребра и неизвестные endpoints. BFS/DFS проверяет C=1; degree>=2 и E−V+C>=2. Отдельно обязательные Mining–Industrial, Industrial–Hydroponic, Industrial–Scientific/Military. Проверить отсутствие bridges (удаление любого одиночного ребра сохраняет связность): это достаточная базовая альтернатива, не гарантия любой будущей блокады.
3. Для обеих сторон каждого ребра вычислить supply(from) ∩ demand(to), исключая item.fuel. Непустое пересечение даёт один directed CargoFlow; отсортировать items ordinal. Каждая станция с непустым supply должна иметь >=1 outgoing flow; минимум одна пара endpoints имеет flows в обе стороны; минимум один consumer не transit. Empty transit supply допустим. Не достраивать тайно товарные ссылки при невалидном конфиге.
4. Generate проверяет ВСЕ templates до выбора, поэтому плохой config не становится seed-dependent ошибкой. Отсортировать templates по TemplateId ordinal и выбрать один индекс через отдельный `TradingMap.Topology`. Вернуть его canonical Links и CargoFlows; Stations сортируются по ObjectId. Никакой зависимости от scenario name, ID, времени, Guid, environment или RNG соседних подсистем.
5. DrawIndex использует seed=RngStreamSeedDerivation.DeriveStreamSeed(masterSeed,streamName), RngStreamNames.CreateDeterministicRandom(seed). Пропустить ровно 1000 NextDouble, затем получить одно NextDouble; index=floor(value*exclusiveMax), counter=10010. Возвращается record(name,seed,10010). exclusiveMax<=0 — ArgumentOutOfRangeException. Допустимые callers этой истории: TradingMap.Topology и TradingMap.Geometry; имена стабильны, другие streams не потребляются. Не использовать Random.Shared или несвязанный new Random(). Это новый сохраняемый поток, а не изменение старых генераторов.

## Out of scope

Координаты, UI/Contracts, bootstrap, актуальная цена/stock, гарантии маржи каждого item, события, изменение профилей/каталога, генерация новых ID.

## Invariants

- Seed stream независим и сохраняет шаг counter10: EngineRequirements.md:316–346; существующий deterministic primitive — RngStreamSeedDerivation.DeriveStreamSeed(ulong,string).
- Структура графа — требования эпика Documentation.md:99 и MVP:302–309. Только логические связи; свободное движение не ограничивается.
- Профили/каталог — входы US-0001, не hardcoded рыночные quantities. Stock не читается из текущего runtime.

## Tests

Класс TradingGraphGeneratorTests:
- `Every_template_has_five_roles_degree_two_two_cycles_and_no_bridges` (AC-02).
- `Every_producer_has_consumer_and_a_return_cargo_pair` (AC-02); сравнить каждый Flow со множественным пересечением, не только Count.
- `Simple_ring_without_chord_is_rejected` (E−V+1=1) и `Disconnected_duplicate_self_loop_and_missing_required_role_links_are_rejected` (AC-02).
- `Missing_consumer_and_missing_return_cargo_are_rejected` (AC-02).
- `Unknown_profile_or_item_is_contextual_error` (AC-02).
- `Same_seed_reordered_input_and_unrelated_rng_have_identical_plan` (AC-05): перемешать Stations/Templates/Links/profile registration/supply arrays; отдельно потребить StationInventory RNG.
- `Seed_corpus_selects_multiple_templates` (0..255, ulong.MaxValue; не требовать различия каждой пары seeds).
- `Topology_stream_seed_and_counter_are_replayable` (AC-05): вручную воспроизвести 1001 draw, проверить counter10010 и named seed.

```text
dotnet test D:\DeepSpaceSaga\DSS\tests\DeepSpaceSaga.Engine.Tests\DeepSpaceSaga.Engine.Tests.csproj --no-restore --filter "FullyQualifiedName~TradingGraphGeneratorTests"
dotnet build D:\DeepSpaceSaga\DSS\src\DeepSpaceSaga.Engine\DeepSpaceSaga.Engine.csproj --no-restore
dotnet format D:\DeepSpaceSaga\DSS\DeepSpaceSaga.sln --verify-no-changes --no-restore
```

## Definition of Done

- Реализованы все шаги только в двух разрешённых файлах.
- AC-02 и AC-05 покрыты named tests, включая rejection одного цикла и независимость input order/RNG.
- Named tests, matching layer build и format проходят; исходные failures при наличии записаны отдельно.
- Нет обращения к UI, hidden changes или незаписанных assumptions; invariant/Out of scope соблюдены.
- По plan JSON или тестам можно проверить выбранный template, все directed flows и stream counter без запуска Client.

## Self-containment check

API dependency и проверки графа перечислены; формула cargo flows, алгоритм выбора и RNG replay заданы точно. Fixtures строятся в тестовом файле. Реальные balance values/сценарии не нужны этому merge unit.
