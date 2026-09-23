---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0007-long-voyage-economy-evidence
ticket: EP-0003-US-0007-TK-0001-long-voyage-runner
title: Экономический прогон полной дальней поездки
stage: approved
layer: tooling
depends_on: [EP-0003-US-0005-TK-0001-cluster-voyage-integration, EP-0003-US-0005-TK-0002-voyage-user-path, EP-0001-US-0002-TK-0001-market-stock-snapshot, EP-0001-US-0002-TK-0002-market-economy-schema, EP-0001-US-0002-TK-0003-hourly-market-simulation, EP-0001-US-0002-TK-0004-market-flow-content, EP-0001-US-0002-TK-0005-market-state-trade-ui, EP-0001-US-0007-TK-0001-market-event-contract, EP-0001-US-0007-TK-0002-market-event-catalog, EP-0001-US-0007-TK-0003-market-event-content, EP-0001-US-0007-TK-0004-market-event-lifecycle, EP-0001-US-0007-TK-0005-trade-event-presentation, EP-0001-US-0008-TK-0001-route-risk-contract, EP-0001-US-0008-TK-0002-effective-route-evaluator, EP-0001-US-0008-TK-0003-route-event-voyage-integration, EP-0001-US-0008-TK-0004-route-choice-presentation, EP-0001-US-0008-TK-0005-route-risk-content, EP-0001-US-0013-TK-0001-balance-diagnostic-seam, EP-0001-US-0013-TK-0002-balance-run-matrix, EP-0001-US-0013-TK-0003-market-health-evaluation, EP-0001-US-0013-TK-0004-strategy-balance-evaluation, EP-0001-US-0013-TK-0005-balance-report-cli]
files_touched: 4
serves: [AC-0001, AC-0002]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Экономический прогон полной дальней поездки

## Why

Проверяемая экономика длительных путешествий: экономический прогон полной дальней поездки даёт проверяемый шаг к результату истории. End state: horizon автоматически растёт, cap диагностируется.; суммы совпадают с receipts, event expiry по часам/дням.; старый режим/пороговый отчёт не меняется.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Для объявленных seed и параметров воспроизводятся несколько местных циклов и полный дальний рейс с возвращением на достаточном горизонте.
- AC-0002: В отчёте есть запасы, снабжение, бюджеты, сроки событий, расходы и результаты; условия и недостатки данных названы явно.

## Decisions

Единственное новое решение пользователя — поручение создать тикеты для всех историй трёх эпиков. Технические детали ниже записаны как assumptions, не как слова пользователя. UTC фиксации 2026-09-22T14:40:41Z. Точное сообщение:

```text
сделай тикеты для историй в эпиках 2 3 и 4&#x20;
D:\DeepSpaceSaga\DSS\Board\EP-0002-procedural-solar-system&#x20;
D:\DeepSpaceSaga\DSS\Board\EP-0003-station-clusters-and-trade-geography
D:\DeepSpaceSaga\DSS\Board\EP-0004-ai-territories-and-map-environment
```

## Assumptions

- Новые технические API, файлы и значения конфигурации ниже — план, а не реализованный код. До dependent тикета обязательны все depends_on, включая EP-0001; статус документа не доказывает поставку.
- Все depends_on в frontmatter должны быть реализованы до этого merge unit. Контрактные входы ниже read-only; они не расширяют allowlist.
- Сценарные расстояния/explicit inventory сохраняются. Близкие исходные станции MarketProfiles — явное исключение начального neighbourMinDays; новые связи соблюдают конфигурацию. Ресурсные данные единственные, из EP-0001.
- Public API after the change является точным плановым контрактом этого тикета. Статус approved не означает, что этот API уже существует.

## Code context

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 4 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| tools/DeepSpaceSaga.EconomyBalance/BalanceRun.cs | Плановый файл зависимости EP-0001-US-0013-TK-0002-balance-run-matrix; пока не подтверждён runtime. Контракт dependency приведён ниже. | Только cluster matrix command-driven run и hourly authoritative evidence. |
| tools/DeepSpaceSaga.EconomyBalance/Program.cs | Плановый файл зависимости EP-0001-US-0013-TK-0005-balance-report-cli; пока не подтверждён runtime. Контракт dependency приведён ниже. | Указанный opt-in CLI/report режим; существующие режимы и их semantics не менять. |
| tools/DeepSpaceSaga.EconomyBalance/cluster-matrix.json | Новый файл; владелец EP-0003-US-0007-TK-0001-long-voyage-runner. В исходном дереве отсутствует. | Только конфигурация/тексты, перечисленные в Implementation steps |
| tests/DeepSpaceSaga.EconomyBalance.Tests/LongVoyageRunnerTests.cs | Новый файл; владелец EP-0003-US-0007-TK-0001-long-voyage-runner. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **tooling**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj`.

## Public API after the change

CLI extension: --cluster-matrix <json>; fields schemaVersion,seeds,shipConfigurations,horizonDays,sampleIntervalHours,maxHorizonDays,localCycles. Экономические timestamps только GameTimeMs; геометрия сохраняет sample SimulationTimeMs.

## Implementation steps

1. Добавить opt-in --cluster-matrix путь к отдельной конфигурации, legacy 5-station matrix оставить. Default seeds [1,2,42], configurations starter/cargo-upgrade, minimum horizonDays=100, sampleIntervalHours=1; продлевать до завершённого дальнего возврата, cap=1000days с failed/incomplete diagnostic.
2. Использовать EconomyBalanceRunner/BalanceCaseEvidence EP-0001: route selection по cluster membership, минимум три local cycles и один intercluster roundtrip. Все действия — действующие команды, расходы/цены/прибыль только authoritative receipts.
3. Ежечасно записывать stock/target/max/budget, production input shortage, active event expiry, fuel/port/event costs, revenue/cost basis и net profit; utc hardware не включать в deterministic economy payload. Missing runtime dependency — failure, не synthetic successful data.
4. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

## Out of scope

Переписывание pricing/quote/fuel/ledger EP-0001; скрытое изменение пятистанционного legacy режима; AI/патрули. Нельзя изменять файлы вне Code context, requirements/состав эпика, обходить dependency недостающим вторым API или менять не связанный пользовательский diff.

## Invariants

- `src/DeepSpaceSaga.Contracts/SimulationSpeed.cs:27`: календарный коэффициент300; движение и календарь не взаимозаменяемы.
- `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:4`: world units100m, speed km/s, clockwise0up. Отображение не изменяет мир.
- `Documentation/00-Process/CLAUDE.md:34`: render loop не запрашивает Engine; Contracts без graphics, Motion общий.
- `Documentation/01-Requirements/EngineRequirements.md:5335`: Approach с постоянной скоростью; новый map scope не даёт ускорение кораблю.
- Один layer, максимум5 файлов, новые DTO immutable/JSON-safe. Результат ошибки не публикует partial state.
- EP-0001 владеет экономикой; существующие market profile IDs находятся в src/DeepSpaceSaga.Client/Data/Markets/station-market-profiles.json:11.

## Tests

Test class: `LongVoyageRunnerTests`. Тестовые сценарии и ожидаемые результаты:

- `LongVoyageRunnerTests.ClusterRunCompletesReturnBeyondHundredDays` — horizon автоматически растёт, cap диагностируется.
- `LongVoyageRunnerTests.HourlyEconomyAndLedgerAreAuthoritative` — суммы совпадают с receipts, event expiry по часам/дням.
- `LongVoyageRunnerTests.LegacyMatrixUnchanged` — старый режим/пороговый отчёт не меняется.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `LongVoyageRunnerTests.ClusterRunCompletesReturnBeyondHundredDays`, `LongVoyageRunnerTests.HourlyEconomyAndLedgerAreAuthoritative`, `LongVoyageRunnerTests.LegacyMatrixUnchanged` |
| AC-0002 | `LongVoyageRunnerTests.ClusterRunCompletesReturnBeyondHundredDays`, `LongVoyageRunnerTests.HourlyEconomyAndLedgerAreAuthoritative`, `LongVoyageRunnerTests.LegacyMatrixUnchanged` |

Coverage: AC-0001, AC-0002 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet restore D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj" --no-restore --filter "FullyQualifiedName~LongVoyageRunnerTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.EconomyBalance/DeepSpaceSaga.EconomyBalance.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.EconomyBalance/DeepSpaceSaga.EconomyBalance.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.EconomyBalance/BalanceRun.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.EconomyBalance/Program.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/LongVoyageRunnerTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/DeepSpaceSaga.EconomyBalance.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.EconomyBalance/BalanceRun.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.EconomyBalance/Program.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.EconomyBalance.Tests/LongVoyageRunnerTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов. Производительность измерять лишь в перечисленных сценариях; unit tests не доказывают80FPS.

## Definition of Done

- Все implementation steps выполнены только в 4 разрешённых файлах.
- Наблюдаемый end state и каждый declared served criterion покрыты named tests в объёме этого тикета; остальные contributions указаны в story map.
- Все перечисленные named tests проходят; actual commands/results приложены. Не пропущены negative/legacy/dependency cases.
- Build и scoped format matching проектов проходят либо точное исходное падение записано отдельно; новое падение не замаскировано baseline.
- Public API, единицы/время, atomic failure, invariants и out-of-scope соблюдены; нет второго источника данных.
- Все dependency gates выполнены; нет незаписанных assumptions, незакрытых блокирующих вопросов и скрытой работы вне Code context.
- Результат проверяем тестами, diff и описанным поведением; где требуется manual/GPU/corpus evidence — оно приложено либо тикет остаётся незавершённым.

## Self-containment check

Тикет содержит allowed paths, текущие evidence anchors, требуемый API, шаги, units/defaults, named tests, exact commands и end state. Ниже скопированы входные контракты зависимостей. Их implementation-файлы read-only вне Code context; не нужно придумывать shape или создавать параллельную подсистему. Если после merge dependency API отличается, сначала согласовать ticket context с фактической поставкой; не реализовывать на догадках и не расширять allowlist.

## Dependency inputs (read-only)

### EP-0002-US-0001-TK-0001-system-map-contract

В src/DeepSpaceSaga.Contracts/SolarSystemMap.cs: public sealed record OrbitalElements(double SemiMajorAxis, double SemiMinorAxis, long OrbitalPeriodMs, int InitialPhase, double PhaseOffsetDegrees, long EpochSimulationTimeMs, string OrbitDirection);  public sealed record BeltMapData(string Id, double InnerRadius, double OuterRadius, ulong DecorationSeed);  public sealed record PlanetMapData(string ObjectId, string Kind, double VisualRadius);  public sealed record OrbitMapData(string ObjectId, OrbitalElements Elements);  public sealed record SolarSystemMapSnapshot(int GeneratorVersion, ulong Seed, double SystemRadius, ImmutableArray<BeltMapData> Belts, ImmutableArray<PlanetMapData> Planets, ImmutableArray<OrbitMapData> Orbits). Optional snapshot properties: SolarSystemMapSnapshot? SolarSystemMap=null; OrbitalElements? Orbit=null; long? OrbitSampleSimulationTimeMs=null. Kind: Rocky/Icy/Gas; OrbitDirection: clockwise/counterclockwise. OrbitalPeriodMs — календарные ms; EpochSimulationTimeMs — физические ms. ObjectMotionSnapshot optional double WorldOffsetX=0,WorldOffsetY=0; OrbitalMotionMath добавляет их к аналитической позиции после вычисления орбиты. Для docked ship offset=(1,1), остальные=0. Все DTO properties имеют явные JsonPropertyName camelCase; это обеспечивает одинаковую схему ScenarioLoader и snapshot JSON. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0003-US-0001-TK-0001-cluster-geography-contract

public sealed record StationClusterData(string Id,string Name,string BeltId,ImmutableArray<string> StationIds,string Specialization);  public sealed record ClusterStationData(string ObjectId,string ClusterId,string MarketProfileId);  public sealed record ClusterTradeLink(string Id,string FromStationId,string ToStationId,ImmutableArray<string> ItemTypeIds);  public sealed record StationClusterMapSnapshot(int RulesVersion,string StartClusterId,ImmutableArray<StationClusterData> Clusters,ImmutableArray<ClusterStationData> Stations,ImmutableArray<ClusterTradeLink> Links); AuthoritativeSnapshot: StationClusterMapSnapshot? ClusterMap=null. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0003-US-0003-TK-0001-resource-orbit-binding

public sealed record ClusterResourceBinding(string FieldId,string ClusterId,string AnchorStationId,double OffsetX,double OffsetY); StationClusterMapSnapshot: ImmutableArray<ClusterResourceBinding> ResourceBindings=default. Offset вращается с группой; source composition остаётся StationResourceFieldsState EP-0001. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0001-US-0002-TK-0001-market-stock-snapshot

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0001-market-stock-snapshot/EP-0001-US-0002-TK-0001-market-stock-snapshot.md; зависимость ещё не объявляется реализованной.

В том же production-файле добавить:

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<StationMarketStockState>))]
public enum StationMarketStockState { Shortage, Normal, Surplus }
```

В конец StationInventoryItemSnapshot после UnitMassKg:

```csharp
long? TargetStock = null,
long? MaxStock = null,
long? FreeStockCapacity = null,
StationMarketStockState? StockState = null
```

Существующие ItemTypeId, StockQuantity, UnitPriceCredits, MaxSellableQuantity, Category, UnitMassKg и StationTradeSnapshot shape сохраняются. JSON использует существующий serializer naming convention, enum сериализуется строкой.

Producer contract для TK-0003: четыре поля либо все null, либо TargetStock>0, MaxStock=2×TargetStock, 0≤StockQuantity≤MaxStock, FreeStockCapacity=MaxStock−StockQuantity и корректный StockState. DTO не вычисляет state/price и не выполняет validation при конструировании. MaxSellableQuantity ограничен min(available market budget/unit price, FreeStockCapacity) для bounded cargo. Fuel не получает bounded cargo fields.

### EP-0001-US-0002-TK-0002-market-economy-schema

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0002-market-economy-schema/EP-0001-US-0002-TK-0002-market-economy-schema.md; зависимость ещё не объявляется реализованной.

Contracts не меняются. Добавить в конец внутреннего StationMarketProfileDefinition:

```csharp
StationMarketEconomyDefinition? Economy = null
```

В том же файле объявить:

```csharp
internal enum StationMarketProductionSource { Profile, Modules }
internal sealed record StationMarketTargetDefinition(string ItemTypeId, long TargetStock);
internal sealed record StationMarketEconomyDefinition(
    StationMarketProductionSource ProductionSource,
    ImmutableArray<StationMarketStockDefinition> HourlyInputs,
    ImmutableArray<StationMarketStockDefinition> HourlyOutputs,
    ImmutableArray<StationMarketStockDefinition> HourlyConsumption,
    ImmutableArray<StationMarketTargetDefinition> StockTargets,
    int ShortageThresholdPermille,
    int SurplusThresholdPermille,
    int BudgetRegenerationDivisorPerDay);
```

Quantity в Hourly* — целое число trade units за один игровой час; StationMarketStockDefinition(string ItemTypeId,long Quantity) уже предусмотрен US-0001.

В конец SpaceObjectData добавить [property: JsonPropertyName("marketBudgetCredits")] long? MarketBudgetCredits = null.
В конец StationProducingModuleData добавить [property: JsonPropertyName("pendingOutput")] IReadOnlyList<StationInventoryItemData>? PendingOutput = null.
SaveFormat.CurrentSaveFormatVersion=9. PendingOutput содержит ItemTypeId/Quantity уже произведённого, не recipe definition. Null/[] означает отсутствие остатка.

Новый optional JSON fragment profile:

```json
"economy": {
  "productionSource": "Profile",
  "hourlyInputs": [{ "itemTypeId": "item.water", "quantity": 4 }],
  "hourlyOutputs": [{ "itemTypeId": "item.ice", "quantity": 18 }],
  "hourlyConsumption": [],
  "stockTargets": [
    { "itemTypeId": "item.ice", "targetStock": 108 },
    { "itemTypeId": "item.water", "targetStock": 72 }
  ],
  "shortageThresholdPermille": 500,
  "surplusThresholdPermille": 1500,
  "budgetRegenerationDivisorPerDay": 24
}
```

Пример — сокращённый fixture-профиль, не полная shipping Mining. Если economy задан, все его поля обязательны. DTO обязаны отличать missing/null numbers/arrays от разрешённых пустых массивов.

### EP-0001-US-0002-TK-0003-hourly-market-simulation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0003-hourly-market-simulation/EP-0001-US-0002-TK-0003-hourly-market-simulation.md; зависимость ещё не объявляется реализованной.

Новых публичных команд нет. Dependency TK-0001 расширяет StationInventoryItemSnapshot optional TargetStock/MaxStock/FreeStockCapacity/StockState. TK-0002 расширяет profile.Economy (Source, Inputs/Outputs/Consumption, StockTargets, thresholds, divisor), SpaceObjectData.MarketBudgetCredits и StationProducingModuleData.PendingOutput; SaveFormat=9.

В runtime record SpaceObjectRuntime (SimulationEngine.cs) добавить long? MarketBudgetCredits = null. В StationProducingModuleRuntime добавить ImmutableArray<StationInventoryItemRuntime> PendingOutput = default. Это только Engine runtime; сохранение использует стабильные item IDs, не индексы.

Сохраняемые поля v9:
- economy-enabled station: marketProfileId, marketProfileFingerprint (US-0001), marketBudgetCredits, explicit credits/stock/size;
- producing module: nextProductionDueGameTimeMs либо pendingOutput, не оба одновременно; pending quantity>0, элементы уникальны и принадлежат его recipe outputs, количество не выше одного recipe output batch.
- no economy → MarketBudgetCredits null; без pending → null/empty.
- общий календарный cursor уже восстанавливается из GameState.GameTimeMs, новый lastTick/системное время не нужны.

### EP-0001-US-0002-TK-0004-market-flow-content

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0004-market-flow-content/EP-0001-US-0002-TK-0004-market-flow-content.md; зависимость ещё не объявляется реализованной.

No API change. JSON shape из TK-0002; все ключи ниже внутри optional economy объекта. Все abbreviated IDs в таблицах означают item.<name>.

Общие поля каждого блока: productionSource="Profile", shortageThresholdPermille=500, surplusThresholdPermille=1500, budgetRegenerationDivisorPerDay=24. Массивы Hourly* в JSON lowerCamelCase; каждый entry={itemTypeId,quantity}. stockTargets entry={itemTypeId,targetStock}. Не записывать maxStock/maxBudget отдельно: они вычисляются Engine как 2×effective target/initialBudget.

| Profile | hourlyOutputs | hourlyInputs | hourlyConsumption |
|---|---|---|---|
| market.mining | ice18, iron-ore24, magnesium-ore10, carbon-ore8 | water4, food-rations6, energy-cells8, steel2 | [] |
| market.industrial | steel16, energy-cells12, electronics4 | iron-ore20, magnesium-ore8, carbon-ore6, silicon8, water2, food-rations3 | [] |
| market.hydroponic | water24, protein-mass10, food-rations8 | ice20, energy-cells8, steel2, electronics1 | [] |
| market.transit | [] | [] | water8, food-rations12, energy-cells6, steel2, electronics2 |
| market.scientific-military | electronics5 | silicon7, energy-cells6, steel3, water2, food-rations4 | [] |

| Profile | stockTargets для Resource | stockTargets для Good |
|---|---|---|
| mining | ice/iron-ore/magnesium-ore/carbon-ore =108 каждый | water/food-rations/energy-cells/steel =72 каждый |
| industrial | iron-ore/magnesium-ore/carbon-ore/silicon =132 каждый | steel/energy-cells/electronics/water/food-rations =88 каждый |
| hydroponic | ice=120 | water/protein-mass/food-rations/energy-cells/steel/electronics =80 каждый |
| transit | нет | water/food-rations/energy-cells/steel/electronics =96 каждый |
| scientific-military | silicon=96 | electronics/energy-cells/steel/water/food-rations =64 каждый |

Targets в таблице — Medium. US-0001 demo Transit Large получает targets144/max288, все остальные сохраняют Medium. Fuel/uranium отсутствуют в rates/targets. RefuelStockKg остаётся как в US-0001.

Новые locale keys, сохранять existing keys/арность:

| Key | English | Russian |
|---|---|---|
| TradeUX.StockShortage | Shortage | Дефицит |
| TradeUX.StockNormal | Normal | Норма |
| TradeUX.StockSurplus | Surplus | Избыток |
| TradeUX.MarketStockSummary | {0} · target {1} · max {2} | {0} · цель {1} · максимум {2} |
| TradeUX.StationStorageLimit | Limited by station storage capacity | Ограничено свободным складом станции |
| TradeUX.PartialWithRemaining | {0}: {1} of {3} completed · {2} tokens · remaining {4} | {0}: выполнено {1} из {3} · {2} токенов · остаток {4} |

MarketStockSummary args = localized state, target quantity with unit, max quantity with unit. PartialWithRemaining сохраняет первые четыре аргумента старого PartialResult, добавляет requested−executed quantity с unit; raw amount не считается по stock delta.

### EP-0001-US-0002-TK-0005-market-state-trade-ui

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0002-market-replenishment/EP-0001-US-0002-TK-0005-market-state-trade-ui/EP-0001-US-0002-TK-0005-market-state-trade-ui.md; зависимость ещё не объявляется реализованной.

No API change. Использовать поля TK-0001: TargetStock/MaxStock/FreeStockCapacity/StockState, все nullable; enum Shortage/Normal/Surplus. MaxSellableQuantity уже включает Budget/Storage bound.

Новые internal presentation helpers допускаются только внутри TradeScreen.Render.cs, например:
- internal static string StockStateLabel(StationMarketStockState state);
- internal static string MarketStockSummary(StationInventoryItemSnapshot item).

TK-0004 locale keys:
TradeUX.StockShortage / StockNormal / StockSurplus;
TradeUX.MarketStockSummary "{0} · target/цель {1} · max/максимум {2}";
TradeUX.StationStorageLimit;
TradeUX.PartialWithRemaining "{0}: {1} of/из {3} ... {2} tokens ... remaining/остаток {4}".

Unit formatting берётся из US-0001 TradeItemPresentation.FormatQuantity(itemId,quantity). Новая rejection string Engine "station_stock_full" отображается через L("StationStorageLimit") в существующем Reason switch.

### EP-0001-US-0007-TK-0001-market-event-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0001-market-event-contract/EP-0001-US-0007-TK-0001-market-event-contract.md; зависимость ещё не объявляется реализованной.

```csharp
public sealed record StationTradeSnapshot(
    string StationObjectId,
    ImmutableArray<StationInventoryItemSnapshot> Items = default,
    ImmutableArray<StationMarketEventSnapshot> ActiveEvents = default);

public sealed record StationMarketEventSnapshot(
    string EventId,
    string DefinitionId,
    string DisplayNameKey,
    string DescriptionKey,
    string EffectSummaryKey,
    long StartedGameTimeMs,
    long EndsGameTimeMs,
    long RemainingGameTimeMs,
    StationMarketRouteEffectSnapshot? RouteEffect = null,
    string? LegacyDisplayName = null,
    string? LegacyDescription = null);

public sealed record StationMarketRouteEffectSnapshot(
    string Availability,
    int MaxAffectedIncidentEdges,
    int TravelTimeMultiplierPermille,
    int FuelMultiplierPermille,
    string? RiskProfileId = null);

public static class StationRouteAvailabilityEffects
{
    public const string None = "None";
    public const string Restricted = "Restricted";
    public const string Unavailable = "Unavailable";
}
```

`EndsGameTimeMs` всегда строго больше `StartedGameTimeMs` для generated shipping event. `RemainingGameTimeMs` — authoritative `max(0, EndsGameTimeMs-snapshot.GameTimeMs)`; permanent legacy event проецируется с обоими значениями `long.MaxValue`. `MaxAffectedIncidentEdges` допустим 0/1 в этой story; `None` требует 0 и multipliers1000, blockade/quarantine используют 1. DTO не содержит probability, stock delta, hidden budget или price formula.

### EP-0001-US-0007-TK-0002-market-event-catalog

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0002-market-event-catalog/EP-0001-US-0007-TK-0002-market-event-catalog.md; зависимость ещё не объявляется реализованной.

Internal content API in `StationMarketEventDefinition.cs`:

```csharp
internal sealed record StationMarketEventDefinition(
    string TypeId,
    string DisplayNameKey,
    string DescriptionKey,
    string EffectSummaryKey,
    int Priority,
    int ChancePermillePerHour,
    int MinDurationHours,
    int MaxDurationHours,
    ImmutableArray<string> EligibleMarketProfileIds,
    ImmutableArray<StationMarketEventItemEffectDefinition> ItemEffects,
    StationMarketEventRouteEffectDefinition? RouteEffect = null);

internal sealed record StationMarketEventItemEffectDefinition(
    string ItemTypeId,
    int ProductionMultiplierPermille,
    int DemandMultiplierPermille,
    int PriceMultiplierPermille,
    long ActivationStockDelta = 0);

internal sealed record StationMarketEventRouteEffectDefinition(
    string Availability,
    int MaxAffectedIncidentEdges,
    int TravelTimeMultiplierPermille,
    int FuelMultiplierPermille,
    string? RiskProfileId = null);

internal static class StationMarketEventIds
{
    // event.reactor-accident, event.decompression, event.hydroponics-failure,
    // event.pirate-blockade, event.cargo-convoy, event.quarantine,
    // event.repair-boom, event.scientific-contract
    public static ImmutableArray<string> All { get; }
}
```

`GameDataRegistry` получает `TypeRegistry<StationMarketEventDefinition> StationMarketEvents` и uppercase SHA-256 `StationMarketEventCatalogFingerprint` от canonical ordinal JSON payload всех semantic fields. Create получает trailing optional `IEnumerable<StationMarketEventDefinition>? stationMarketEvents=null` и internal `bool requireCompleteMarketEventSet=false`; Settings loader всегда передаёт true.

`EngineSettingsFile.TypeData` получает optional string `StationMarketEvents`. Declared path обязателен и nonempty; absent path означает legacy/no generated events.

Scenario/save extensions:

```csharp
GameStateData(...,
    [JsonPropertyName("marketEventCatalogFingerprint")]
    string? MarketEventCatalogFingerprint = null)

StationEventData(...,
    [JsonPropertyName("definitionId")] string? DefinitionId = null,
    [JsonPropertyName("displayNameKey")] string? DisplayNameKey = null,
    [JsonPropertyName("descriptionKey")] string? DescriptionKey = null,
    [JsonPropertyName("effectSummaryKey")] string? EffectSummaryKey = null,
    [JsonPropertyName("itemEffects")]
    IReadOnlyList<StationMarketEventItemEffectData>? ItemEffects = null,
    [JsonPropertyName("routeEffect")]
    StationMarketEventRouteEffectData? RouteEffect = null,
    [JsonPropertyName("activationStockDeltaApplied")]
    bool ActivationStockDeltaApplied = false)
```

`StationMarketEventItemEffectData` mirrors the four item fields; `StationMarketEventRouteEffectData` mirrors route definition. Existing `DisplayName`, `Description`, `PriceFactors`, start and duration remain present for backward compatibility. Generated event requires non-null DefinitionId/keys/finite positive DurationMs/resolved effects and `ActivationStockDeltaApplied`; legacy event has DefinitionId null and follows existing rules.

### EP-0001-US-0007-TK-0003-market-event-content

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0003-market-event-content/EP-0001-US-0007-TK-0003-market-event-content.md; зависимость ещё не объявляется реализованной.

Новых C# API нет. Settings получает:

```json
"stationMarketEvents": "Data/Markets/station-market-events.json"
```

JSON definitions должны точно соответствовать таблице. `P/D/$` — production/demand/price permille; `Δ` — one-time activation stock delta. Неуказанные multipliers равны1000, delta0. Все eligible profiles: `market.mining`, `market.industrial`, `market.hydroponic`, `market.transit`, `market.scientific-military`.

| Definition ID | Eligible profiles | Priority | Chance/h | Duration h | Item effects | Route effect |
|---|---|---:|---:|---:|---|---|
| event.reactor-accident | all five | 90 | 4‰ | 6–18 | energy-cells P500/D1600/$1500 | none |
| event.decompression | all five | 80 | 5‰ | 4–12 | water D1600/$1350; food-rations D1500/$1300; steel D1300/$1200 | none |
| event.hydroponics-failure | hydroponic | 85 | 4‰ | 8–24 | food-rations P300/D1600/$1450; protein-mass P300/$1400 | none |
| event.pirate-blockade | all five | 100 | 3‰ | 6–18 | water, food-rations, steel, electronics: P700/D1300/$1250 | Unavailable, maxEdges1, time1000, fuel1400, risk.pirate-blockade |
| event.cargo-convoy | all five | 40 | 8‰ | 4–8 | water Δ+24/$800; food-rations Δ+24/$800; steel Δ+16/$850 | none |
| event.quarantine | all five | 95 | 3‰ | 8–24 | water D1400/$1250; food-rations D1500/$1300; energy-cells D1200/$1150 | Restricted, maxEdges1, time1500, fuel1200, risk.quarantine |
| event.repair-boom | mining, industrial, transit | 60 | 6‰ | 8–20 | steel P1300/$1150; iron-ore D1600/$1350; magnesium-ore D1500/$1300; electronics D1500/$1350 | none |
| event.scientific-contract | scientific-military | 70 | 5‰ | 6–16 | electronics P500/$1450; silicon D1700/$1500; energy-cells D1500/$1350 | none |

Every definition uses keys `TradeUX.Event.<PascalId>.Name`, `.Description`, `.Effect`; JSON stores suffix after `TradeUX.` or the full key consistently with TK-0002 loader choice. Client uses exactly one convention; test prohibits mixed prefixes.

Common keys: `TradeUX.ActiveEvents`, `TradeUX.EventRemainingHours` with one numeric placeholder, `TradeUX.EventPermanent`.

### EP-0001-US-0007-TK-0004-market-event-lifecycle

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0004-market-event-lifecycle/EP-0001-US-0007-TK-0004-market-event-lifecycle.md; зависимость ещё не объявляется реализованной.

Новых public Engine methods нет. `StationTradeSnapshot.ActiveEvents` — TK-0001 public contract. Internal runtime `StationEventRuntime` расширяется до полного resolved shape: `DefinitionId`, three keys, `ItemEffects`, `RouteEffect`, `ActivationStockDeltaApplied`; legacy display fields/PriceFactors сохраняются.

В новом partial:

```csharp
private bool ApplyMarketEventBoundary(long boundaryGameTimeMs);
private ImmutableArray<StationEventRuntime> ResolveMarketEventsForLoad(
    SpaceObjectData station, bool isSave, string? saveFingerprint);
private static ulong MarketEventRoll(ulong masterSeed, string stationId,
    string definitionId, long hourIndex, string purpose);
private static int ResolveEventMultiplier(
    SpaceObjectRuntime station, int itemTypeIndex, MarketEventFlow flow, long atGameTimeMs);
private bool ApplyActivationStockDeltas(ref SpaceObjectRuntime station, long atGameTimeMs);
private ImmutableArray<StationMarketEventSnapshot> BuildActiveEventProjection(
    SpaceObjectRuntime station, long atGameTimeMs);
```

`MarketEventFlow` internal enum: Production, Demand. Price continues through existing `ResolveStationPriceFactors`; no new price API. US-0002 `ApplyMarketHour` returns/collects station IDs whose stock/budget changed. Event boundary adds event-set/delta changes. For each station with any combined visible change, call prerequisite revision staging/commit exactly once after all fallible candidate calculations succeed.

### EP-0001-US-0007-TK-0005-trade-event-presentation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0007-temporary-market-events/EP-0001-US-0007-TK-0005-trade-event-presentation/EP-0001-US-0007-TK-0005-trade-event-presentation.md; зависимость ещё не объявляется реализованной.

No public API change. Internal model additions:

```csharp
internal StationMarketEventSnapshot[] ActiveEvents { get; private set; } = [];
internal static string EventText(string key, string? legacyFallback, string id);
internal static string EventRemaining(StationMarketEventSnapshot evt);
```

`Refresh` copies `snapshot.DockedStationTrade?.ActiveEvents`, filters impossible already-ended (`RemainingGameTimeMs<=0`, except `long.MaxValue`), sorts by `StartedGameTimeMs` then `EventId` ordinal and caps defensive display at2. Engine remains responsible for semantic max-two validation.

Layout:

```csharp
EventBadge   = (700, 84) .. (984, 126)
EventTooltip = (650, 130) .. (984, 330)
```

Paused/not-docked text is narrowed to `(450,84)..(690,126)`; Market/Fuel tabs, Catalog, Detail and all controls keep exact existing rectangles.

### EP-0001-US-0008-TK-0001-route-risk-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0001-route-risk-contract/EP-0001-US-0008-TK-0001-route-risk-contract.md; зависимость ещё не объявляется реализованной.

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<TradingRouteAvailability>))]
public enum TradingRouteAvailability { Available, Restricted, Unavailable }

[JsonConverter(typeof(JsonStringEnumConverter<TradingRouteRisk>))]
public enum TradingRouteRisk { Safe, Elevated }

public sealed record TradingRouteSnapshot(
    string OriginStationObjectId,
    string DestinationStationObjectId,
    string DistanceClass,
    long BaseTravelEstimateGameTimeMs,
    long EffectiveTravelEstimateGameTimeMs,
    int BaseFuelMultiplierPermille,
    int EffectiveFuelMultiplierPermille,
    string RiskProfileId,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability,
    string? ReasonText = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> ActiveEventIds = default);
```

Trailing field in `AuthoritativeSnapshot`:

```csharp
[property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<TradingRouteSnapshot>))]
ImmutableArray<TradingRouteSnapshot> TradingRoutes = default
```

### EP-0001-US-0008-TK-0002-effective-route-evaluator

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0002-effective-route-evaluator/EP-0001-US-0008-TK-0002-effective-route-evaluator.md; зависимость ещё не объявляется реализованной.

Нет нового public/session API. В `DeepSpaceSaga.Engine.Trading`:

```csharp
internal sealed record TradingRouteModifier(
    string EventId, string EventTypeId, int Priority, long StartedGameTimeMs,
    string FromStationObjectId, string ToStationObjectId,
    TradingRouteAvailability Availability,
    int TravelTimeMultiplierPermille, int FuelMultiplierPermille,
    string ReasonText);

internal sealed record EffectiveTradingRoute(
    TradingMapEdgeData BaseEdge,
    long EffectiveTravelEstimateGameTimeMs,
    int EffectiveFuelMultiplierPermille,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability,
    string? ReasonText,
    ImmutableArray<string> ActiveEventIds);

internal static class TradingRouteEvaluator
{
    internal static ImmutableArray<EffectiveTradingRoute> Evaluate(
        TradingMapStateData map, IReadOnlyList<TradingRouteModifier> activeModifiers);
    internal static bool CanApply(
        TradingMapStateData map,
        IReadOnlyList<TradingRouteModifier> activeModifiers,
        IReadOnlyList<TradingRouteModifier> candidateModifiers);
}
```

Dependency API is fixed by EP-0001-US-0004-TK-0001: `TradingMapStateData.Rules.Stations`, `.Edges`, `.CargoFlows`; edge fields are From/To/DistanceKm/TravelEstimateGameTimeMs/DistanceClass/FuelMultiplierPermille/RiskProfileId. TK-0001 provides Contracts enums. If implemented dependency signatures differ, stop and review; do not change extra files here.

### EP-0001-US-0008-TK-0003-route-event-voyage-integration

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0003-route-event-voyage-integration/EP-0001-US-0008-TK-0003-route-event-voyage-integration.md; зависимость ещё не объявляется реализованной.

No new public API beyond TK-0001. Internal partial seam:

```csharp
private ImmutableArray<TradingRouteSnapshot> BuildTradingRouteProjection(long gameTimeMs);
private ImmutableArray<TradingRouteModifier> BuildActiveRouteModifiers(long gameTimeMs);
```

Required dependency contracts at implementation start:

- US-0004: loaded `TradingMapStateData? _tradingMap` with materialized Edges/CargoFlows.
- US-0007: active event state contains EventId, EventTypeId, Priority, StartedGameTimeMs, DurationMs, player text and zero-or-more route effects with canonical endpoints/availability/travel/fuel permille; expired events are not returned as active.
- US-0006/0014: voyage start receives destination station ID; persisted voyage stores `TravelEstimateGameTimeMs`, `FuelMultiplierPermille`, `RiskProfileId`, event IDs and arrival time; rejection reason `route_unavailable` already belongs to that command contract.

If a dependency contract differs, update this ticket/story through review; do not add Contracts fields or persistence schema in this engine ticket.

### EP-0001-US-0008-TK-0004-route-choice-presentation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0004-route-choice-presentation/EP-0001-US-0008-TK-0004-route-choice-presentation.md; зависимость ещё не объявляется реализованной.

No Contracts/session API change. Internal client model:

```csharp
internal sealed record StationRouteRow(
    string DestinationStationObjectId,
    string PrimaryText,
    string SecondaryText,
    string? ReasonText,
    bool IsEnabled,
    TradingRouteRisk Risk,
    TradingRouteAvailability Availability);

internal static class StationRoutePresentation
{
    internal static ImmutableArray<StationRouteRow> Build(
        ImmutableArray<TradingRouteSnapshot> routes);
    internal static string FormatGameDuration(long gameTimeMs);
    internal static string FormatFuelMultiplier(int permille);
}
```

Dependency UI seam: US-0006/0014 exposes one selected destination and a submit action through the existing `StationScreen`/`GameSessionHandle`; this ticket must use it unchanged. If it uses other files/API, return to review before touching additional files.

### EP-0001-US-0008-TK-0005-route-risk-content

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0008-route-risk-and-alternatives/EP-0001-US-0008-TK-0005-route-risk-content/EP-0001-US-0008-TK-0005-route-risk-content.md; зависимость ещё не объявляется реализованной.

No API change. JSON использует dependency schemas без новых полей этого тикета:

- map risks: `riskProfileId`, `fuelMultiplierPermille`; links reference one profile.
- event route effect: dependency US-0007 fields for availability, travel/fuel permille, ordered candidate endpoint pairs and player-facing description.

Exact baseline:

- `risk.safe`: fuel 1000; минимум одно Short edge от start station.
- `risk.elevated`: fuel 1300; минимум одно Medium/Long edge, не единственный incident edge станции.
- `event.pirate-blockade`: `Restricted`, travel 1500, fuel 1250, текст причины «Пиратская блокада: рейс дольше и дороже».
- `event.quarantine`: `Unavailable`, travel/fuel 1000, текст «Карантин: направление временно закрыто».

### EP-0001-US-0013-TK-0001-balance-diagnostic-seam

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0001-balance-diagnostic-seam/EP-0001-US-0013-TK-0001-balance-diagnostic-seam.md; зависимость ещё не объявляется реализованной.

No public API change. Новый assembly friendship:

```xml
<InternalsVisibleTo Include="DeepSpaceSaga.EconomyBalance" />
```

Tool использует существующие signatures:

```csharp
internal AuthoritativeSnapshot CaptureSnapshotForTests(
    long gameTimeMs = 0,
    SimulationSpeed? speed = null,
    long? simulationTimeMs = null);

internal ScenarioFile CaptureSaveStateForTests(
    long gameTimeMs,
    SimulationSpeed speed);
```

### EP-0001-US-0013-TK-0002-balance-run-matrix

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0002-balance-run-matrix/EP-0001-US-0013-TK-0002-balance-run-matrix.md; зависимость ещё не объявляется реализованной.

Tool assembly internal API (visible to its tests):

```csharp
internal sealed record BalanceShipConfiguration(
    string Id,
    int CargoCapacityMultiplierPermille,
    int FuelEfficiencyMultiplierPermille);

internal sealed record BalanceMatrix(
    ImmutableArray<ulong> Seeds,
    ImmutableArray<BalanceShipConfiguration> ShipConfigurations,
    long HorizonGameTimeMs,
    long SampleIntervalGameTimeMs,
    long SaveLoadCheckpointGameTimeMs);

internal sealed record BalanceLedgerEvidence(
    string VoyageId, string RouteId, string ItemTypeId,
    long GrossSalesCredits, long? CostOfGoodsSoldCredits,
    long RouteFuelCostCredits, long PortFeesAssessedCredits,
    long EventCostsCredits, long PassengerPayoutCredits,
    long PassengerPenaltyCredits, long? NetProfitCredits);

internal sealed record BalanceCaseEvidence(
    ulong Seed, string ShipConfigurationId,
    ImmutableArray<BalanceHourlySample> HourlySamples,
    ImmutableArray<BalanceStrategyEvidence> Strategies,
    string ContinuousStateHash, string SaveLoadStateHash);

internal sealed class EconomyBalanceRunner
{
    internal ImmutableArray<BalanceCaseEvidence> Run(
        string settingsPath, string scenarioPath, BalanceMatrix matrix);
}
```

`BalanceHourlySample` содержит `GameTimeMs`, canonical station stock/target/max/budget, active influencing events, effective routes/cargo flows и market revisions. `BalanceStrategyEvidence` содержит origin/destination/distance class/risk/item/config, requested/executed quantities, quote/result revisions, authoritative receipts и `BalanceLedgerEvidence`; exact DTO definitions находятся в `BalanceRun.cs` и не используются production runtime.

Dependency command path фиксирован `EP-0001-US-0006-repeatable-trading-voyage.md:50–56`: `GetTradeQuote(TradeQuoteRequest)`, quote-bound `trade.buy`/`trade.sell`, `PlayerCommand(...,"navigation.undock",TargetObjectId:destination)`, explicit-time snapshots до Docked, затем latest matching `VoyageFinanceSnapshot`. Tool не вызывает Client.

### EP-0001-US-0013-TK-0003-market-health-evaluation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0003-market-health-evaluation/EP-0001-US-0013-TK-0003-market-health-evaluation.md; зависимость ещё не объявляется реализованной.

Internal tooling API:

```csharp
internal sealed record BalanceViolation(
    string Code,
    ulong Seed,
    string ShipConfigurationId,
    string? StationObjectId,
    string? RouteId,
    string? ItemTypeId,
    long? GameTimeMs,
    string Expected,
    string Observed);

internal static class MarketHealthEvaluator
{
    internal static ImmutableArray<BalanceViolation> Evaluate(
        BalanceCaseEvidence evidence);
}
```

Stable codes: `station_count`, `route_disconnected`, `station_cannot_buy`, `station_cannot_sell`, `stock_below_zero`, `stock_above_max`, `budget_below_zero`, `budget_above_max`, `zero_stock_ratio`, `supply_not_recovered`, `state_hash_mismatch`.

### EP-0001-US-0013-TK-0004-strategy-balance-evaluation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0004-strategy-balance-evaluation/EP-0001-US-0013-TK-0004-strategy-balance-evaluation.md; зависимость ещё не объявляется реализованной.

Internal tooling API:

```csharp
internal static class StrategyBalanceEvaluator
{
    internal static ImmutableArray<BalanceViolation> EvaluateCase(
        BalanceCaseEvidence evidence);

    internal static ImmutableArray<BalanceViolation> EvaluateCorpus(
        ImmutableArray<BalanceCaseEvidence> evidence);
}
```

Stable codes: `missing_comparable_strategy`, `short_margin_band`, `medium_margin_band`, `long_upgrade_crossover`, `route_margin_dominance`, `route_always_best`, `event_did_not_change_leader`, `ledger_formula_mismatch`, `duplicate_ledger_posting`, `stale_quote_or_command_reapplied`, `unchanged_market_created_profit`.

### EP-0001-US-0013-TK-0005-balance-report-cli

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0013-economy-balance-evidence/EP-0001-US-0013-TK-0005-balance-report-cli/EP-0001-US-0013-TK-0005-balance-report-cli.md; зависимость ещё не объявляется реализованной.

CLI:

```text
dotnet run --project tools/DeepSpaceSaga.EconomyBalance/DeepSpaceSaga.EconomyBalance.csproj -- \
  <DSS-root> <output.json> [--matrix <matrix.json>]
```

Default matrix is packaged `balance-matrix.json`:

```json
{
  "schemaVersion": 1,
  "seeds": [1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233],
  "shipConfigurations": [
    { "id": "starter", "cargoCapacityMultiplierPermille": 1000, "fuelEfficiencyMultiplierPermille": 1000 },
    { "id": "cargo-upgrade", "cargoCapacityMultiplierPermille": 2000, "fuelEfficiencyMultiplierPermille": 1000 }
  ],
  "horizonHours": 240,
  "sampleIntervalHours": 1,
  "saveLoadCheckpointHours": 120,
  "zeroStockMaximumPermille": 250,
  "shortMarginMinimumPermille": 50,
  "shortMarginMaximumPermille": 150,
  "mediumMarginMinimumPermille": 150,
  "mediumMarginMaximumPermille": 350,
  "maximumToMedianMultiplierPermille": 2000
}
```

Report root fields: `schemaVersion`, `status`, `matrix`, `summary`, `cases`, `violations`. `cases` sorted seed/config; nested samples/strategies/ledgers use TK-0002 order. No generated-at timestamp, OS, temp path or absolute repository path. `violations` use shared `BalanceViolation` schema.
