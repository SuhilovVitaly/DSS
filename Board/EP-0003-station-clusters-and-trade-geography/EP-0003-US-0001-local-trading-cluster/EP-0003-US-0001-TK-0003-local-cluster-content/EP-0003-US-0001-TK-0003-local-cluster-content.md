---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0001-local-trading-cluster
ticket: EP-0003-US-0001-TK-0003-local-cluster-content
title: Настройки первой торговой группы
stage: approved
layer: content-data
depends_on: [EP-0002-US-0005-TK-0001-orbital-synchronization, EP-0002-US-0005-TK-0002-orbital-docking-departure, EP-0001-US-0001-TK-0001-market-profile-schema, EP-0001-US-0001-TK-0002-profile-market-bootstrap, EP-0001-US-0001-TK-0003-market-catalog-content, EP-0001-US-0001-TK-0004-five-market-demo, EP-0001-US-0001-TK-0005-profile-trade-presentation, EP-0001-US-0003-TK-0001-trade-execution-contract, EP-0001-US-0003-TK-0002-atomic-quote-execution, EP-0001-US-0003-TK-0003-trade-result-texts, EP-0001-US-0003-TK-0004-quoted-trade-controls, EP-0001-US-0003-TK-0005-confirmed-trade-history, EP-0001-US-0004-TK-0001-trading-map-schema, EP-0001-US-0004-TK-0002-economic-graph, EP-0003-US-0001-TK-0002-local-cluster-generation]
files_touched: 2
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Настройки первой торговой группы

## Why

Рабочая торговля в стартовом кластере: настройки первой торговой группы даёт проверяемый шаг к результату истории. End state: actual startup, пять профилей, поставляемые диапазоны.; исходные запасы MarketProfiles/Default сохранены.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Во всех текущих сценариях стартовый кластер содержит 10–12 станций и пять профилей; сохранены сценарные ссылки, явные склады и назначение старта, а радиус до Солнца остаётся в пределах 50–75 дней.
- AC-0002: На карте и при посещении доступны стартовый рынок и соседние рынки; показаны потенциальные потоки продукции и обратная загрузка без требования производства у транзита.
- AC-0003: Связность, два независимых цикла и направления проверены по ролям; компактность и исходные локальные расстояния соответствуют объявленной конфигурации и сохраняются при движении.

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

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 2 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Maps/solar-system.json | Новый файл; владелец EP-0002-US-0001-TK-0004-default-system-content. В исходном дереве отсутствует. | Только конфигурация/тексты, перечисленные в Implementation steps |
| tests/DeepSpaceSaga.Client.Tests/LocalClusterContentTests.cs | Новый файл; владелец EP-0003-US-0001-TK-0003-local-cluster-content. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **content-data**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No API change. clusters соответствует ClusterGenerationConfig; 1/1 — промежуточная конфигурация US-0001, final 3/5 поставляется US-0002.

## Implementation steps

1. Добавить clusters: min/maxClusters=1/1 для вертикального первого результата; min/maxStations=10/12; neighbourMin/MaxDays=0.5/2; diameterMin/MaxDays=3/7; interclusterMin/MaxDays=15/35. Следующая история меняет только число кластеров до 3–5.
2. Проверить JSON и реальный старт всех пяти сценариев: существующие рыночные профили используются по точному typeId. Не менять каталог, формулы цен или параметры старой five-station карты.
3. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

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

Test class: `LocalClusterContentTests`. Тестовые сценарии и ожидаемые результаты:

- `LocalClusterContentTests.ClusterContentUsesExistingProfiles` — actual startup, пять профилей, поставляемые диапазоны.
- `LocalClusterContentTests.ScenarioMarketsRemainExplicit` — исходные запасы MarketProfiles/Default сохранены.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `LocalClusterContentTests.ClusterContentUsesExistingProfiles`, `LocalClusterContentTests.ScenarioMarketsRemainExplicit` |
| AC-0002 | `LocalClusterContentTests.ClusterContentUsesExistingProfiles`, `LocalClusterContentTests.ScenarioMarketsRemainExplicit` |
| AC-0003 | `LocalClusterContentTests.ClusterContentUsesExistingProfiles`, `LocalClusterContentTests.ScenarioMarketsRemainExplicit` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore --filter "FullyQualifiedName~LocalClusterContentTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/LocalClusterContentTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/LocalClusterContentTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов.

## Definition of Done

- Все implementation steps выполнены только в 2 разрешённых файлах.
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

### EP-0003-US-0001-TK-0002-local-cluster-generation

public sealed record ClusterGenerationConfig(int MinClusters,int MaxClusters,int MinStations,int MaxStations,double NeighbourMinDays,double NeighbourMaxDays,double DiameterMinDays,double DiameterMaxDays,double InterclusterMinDays,double InterclusterMaxDays); SolarSystemGenerationConfig.Clusters=null optional. internal sealed record ClusterGenerationResult(ScenarioFile World,StationClusterMapSnapshot Map);  internal static ClusterGenerationResult StationClusterGenerator.Generate(ScenarioFile source,ClusterGenerationConfig config,GameDataRegistry registry,ulong seed). SimulationEngine сохраняет Map в _clusterMap и публикует ClusterMap. Генератор системы даёт базовый ScenarioFile, кластерный stage вызывается LoadScenario до runtime construction; persistent GameStateData.ClusterMap добавляет US-0006. До этой истории проверяется runtime, не объявляется готовое сохранение кластеров.

### EP-0001-US-0001-TK-0001-market-profile-schema

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0001-market-profile-schema/EP-0001-US-0001-TK-0001-market-profile-schema.md; зависимость ещё не объявляется реализованной.

Изменений Contracts/IGameSessionConnection нет. Engine-internal API:

- В конец TypeDataPaths добавить optional string? StationMarketProfiles = null с JSON stationMarketProfiles.
- GameDataRegistry.StationMarketProfiles: TypeRegistry<StationMarketProfileDefinition>.
- В конец GameDataRegistry.Create добавить optional IEnumerable<StationMarketProfileDefinition>? stationMarketProfiles = null; старые calls и Empty сохраняются.
- internal sealed record StationMarketStockDefinition(string ItemTypeId, long Quantity).
- internal sealed record StationMarketProfileDefinition(string TypeId, string DisplayName, ImmutableArray<string> SupplyItemTypeIds, ImmutableArray<string> DemandItemTypeIds, ImmutableArray<StationMarketStockDefinition> InitialInventory, long InitialCredits, long RefuelStockKg, ImmutableDictionary<StationSize, int> SizeFactors) : ITypeDefinition.
- Read-only string Fingerprint на profile: SHA-256 uppercase hex от canonical JSON экономических полей. Сортировать supply/demand по ordinal ID, inventory по ItemTypeId, size factors по имени размера. Включить TypeId, все списки, quantities, InitialCredits, RefuelStockKg, SizeFactors; DisplayName исключить. Не менять CatalogCompatibility.
- internal static IReadOnlyList<StationMarketProfileDefinition> LoadStationMarketProfiles(string path) в EngineContentLoader; ссылки на товары проверяет registry после загрузки items.

JSON schema v1:

```json
{
  "schemaVersion": 1,
  "sizeFactors": { "Outpost": 500, "Medium": 1000, "Large": 1500, "Huge": 2000 },
  "profiles": [{
    "typeId": "market.mining",
    "displayName": "Mining",
    "supplyItemTypeIds": ["item.ice"],
    "demandItemTypeIds": ["item.water"],
    "initialInventory": [
      { "itemTypeId": "item.ice", "quantity": 162 },
      { "itemTypeId": "item.water", "quantity": 36 }
    ],
    "initialCredits": 9600,
    "refuelStockKg": 200
  }]
}
```

Пример сокращён только по числу товарных позиций; это fixture схемы, не финальный Mining. Все поля обязательны, включая пустой supply Transit; null не равен пустому массиву. Числа целые. DTO nullable numeric или required-presence check не должны превращать отсутствующее обязательное поле в 0.

### EP-0001-US-0001-TK-0002-profile-market-bootstrap

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0002-profile-market-bootstrap/EP-0001-US-0001-TK-0002-profile-market-bootstrap.md; зависимость ещё не объявляется реализованной.

Contracts API не меняется. Новые optional поля в конце публичного Engine SpaceObjectData:

```csharp
[property: JsonPropertyName("marketProfileId")] string? MarketProfileId = null,
[property: JsonPropertyName("marketProfileFingerprint")] string? MarketProfileFingerprint = null
```

SpaceObjectRuntime (в SimulationEngine.cs) получает те же optional string-поля. SaveFormat.CurrentSaveFormatVersion = 8.

Dependency contract TK-0001: registry.StationMarketProfiles — TypeRegistry<StationMarketProfileDefinition>; GetIndex/GetDefinition; definition предоставляет InitialInventory (ItemTypeId/Quantity), InitialCredits, RefuelStockKg, SizeFactors (StationSize → int, 1000=1), Fingerprint (economic SHA-256). Registry validates references до bootstrap. Менять эти dependency-файлы здесь нельзя.

Для новых scenario.json допустим только marketProfileId без fingerprint. Для profile save v8 обязательны оба поля, explicit credits, stationSize и полный materialized inventory. Неизвестный профиль/неподходящий stamp — ScenarioException, контекст включает station ObjectId, profile ID и field.

### EP-0001-US-0001-TK-0003-market-catalog-content

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0003-market-catalog-content/EP-0001-US-0001-TK-0003-market-catalog-content.md; зависимость ещё не объявляется реализованной.

No API change. Новый item объект:

```json
{
  "typeId": "item.electronics",
  "displayName": "Control Electronics",
  "unitMassKg": 1,
  "basePriceCredits": 150,
  "tradeCategory": "Good",
  "catalogCode": "ITM-3006",
  "tradeUnit": "Piece",
  "storageKind": "Cargo",
  "buyQuantityStep": 1,
  "sellQuantityStep": 1
}
```

Точные названия: displayName каталога английский, locale — соответствующий столбец. Для существующих ID имена ключей уже заданы в TradeItemPresentation.cs:4–18; новый — Trade.ItemElectronics.

| TypeId | English | Russian |
|---|---|---|
| item.ice | Cryo-Ice | Криолёд |
| item.iron-ore | Asteroid Iron Ore | Астероидная железная руда |
| item.silicon | Electronic Silicon | Электронный кремний |
| item.magnesium-ore | Magnesium Concentrate | Магниевый концентрат |
| item.carbon-ore | Carbon Regolith | Углеродный реголит |
| item.water | Purified Water | Очищенная вода |
| item.steel | Hull Alloy | Корпусной сплав |
| item.energy-cells | Energy Cells | Энергетические ячейки |
| item.fuel | Cryogenic Fuel | Криогенное топливо |
| item.protein-mass | Synthetic Protein Mass | Синтетическая белковая масса |
| item.food-rations | Sealed Rations | Герметичные пайки |
| item.electronics | Control Electronics | Управляющая электроника |

Новые ключи (не менять арность существующих format keys):

| Key | English | Russian |
|---|---|---|
| Trade.DescriptionElectronics | Control blocks for station systems and ship equipment. | Блоки управления станционными системами и корабельным оборудованием. |
| Trade.UnitKg | kg | кг |
| Trade.UnitRation | rations | рацион. |
| Trade.UnitCell | cells | ячеек |
| Trade.UnitBlock | blocks | блоков |
| Trade.UnitGeneric | units | ед. |
| Trade.QuantityWithUnit | Quantity, {0} | Количество, {0} |
| Trade.AmountWithUnit | {0} {1} | {0} {1} |
| Trade.QuantityMass | 1 {0} = {1} kg | 1 {0} = {1} кг |
| Trade.ConfirmBuyWithUnit | Buy {0} {1} for {2} | Купить {0} {1} за {2} |
| Trade.ConfirmSellWithUnit | Sell {0} {1} for {2} | Продать {0} {1} за {2} |

Для QuantityMass использовать отдельные singular ключи Trade.UnitRationSingle = ration/рацион; Trade.UnitCellSingle = cell/ячейка; Trade.UnitBlockSingle = block/блок; Trade.UnitGenericSingle = unit/ед. Trade.UnitKg подходит для обеих форм. В многострочных описаниях не заявлять новые механики.

### EP-0001-US-0001-TK-0004-five-market-demo

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0004-five-market-demo/EP-0001-US-0001-TK-0004-five-market-demo.md; зависимость ещё не объявляется реализованной.

No API change. Settings: "stationMarketProfiles": "Data/Markets/station-market-profiles.json".

Dependency schema из TK-0001: root schemaVersion=1, sizeFactors={Outpost:500,Medium:1000,Large:1500,Huge:2000}, profiles[]; каждый profile содержит typeId, displayName, supplyItemTypeIds[], demandItemTypeIds[], initialInventory[{itemTypeId,quantity}], initialCredits, refuelStockKg. Все поля обязательны; initialInventory ровно union supply/demand, без Fuel. Item IDs ниже имеют обязательный префикс item.; profile IDs market.*.

| Profile ID / displayName | Supply (quantity каждого) | Demand (quantity каждого) | InitialCredits | RefuelStockKg |
|---|---|---|---:|---:|
| market.mining / Mining | ice=162, iron-ore=162, magnesium-ore=162, carbon-ore=162 | water=36, food-rations=36, energy-cells=36, steel=36 | 9600 | 200 |
| market.industrial / Industrial | steel=132, energy-cells=132, electronics=132 | iron-ore=66, magnesium-ore=66, carbon-ore=66, silicon=66, water=44, food-rations=44 | 14400 | 200 |
| market.hydroponic / Hydroponic | water=120, protein-mass=120, food-rations=120 | ice=60, energy-cells=40, steel=40, electronics=40 | 12000 | 200 |
| market.transit / Transit | пустой список | water=96, food-rations=96, energy-cells=96, steel=96, electronics=96 | 19200 | 400 |
| market.scientific-military / Scientific/Military | electronics=96 | silicon=48, energy-cells=32, steel=32, water=32, food-rations=32 | 16800 | 200 |

При Large Transit результат: каждый товар 144, Fuel 600, Credits 28800 до docking fee. Uranium ни в один профиль не включать. Числа будущих rate/target/maxStock не добавлять.

Scenario blueprint (все поля/ID фиксированы; дополнительные asset paths не нужны):

- scenarioMetadata: scenarioId="market-profiles", name="Five Station Markets", description="Choose a station and dock to compare its market. Start this scenario again to inspect another station; continuous voyages are outside this demo."
- gameState: gameTimeMs=0, currentSpeed="Speed0", masterSeed=20260921, playerShipObjectId="SPC-MARKET-PLAYER", playerTokens=100000; focus={mode:"Attached",objectId:"SPC-MARKET-PLAYER"}.
- Игрок: ObjectType PlayerShip, persistenceType Permanent, name="Market Surveyor", position (10000,10000), speedMps=0, directionDegrees=0, movementType Stationary, isKnown=true, isDocked=false; dockedStationObjectId отсутствует.
- hullLayout: width=9,height=9,cells=[{x:4,y:0},{x:4,y:1},{x:4,y:2}].
- modules: MOD-MARKET-NAV / module.bridge.navigation.computer.basic / occupiedCells[(4,0)] / structurePoints80; MOD-MARKET-CARGO / module.container.basic / [(4,1)] / structurePoints400; MOD-MARKET-ENGINE / module.engine.basic / [(4,2)] / structurePoints100 / fuelAmountKg100. Все powerState On, operationalState Ready, activeCycle null. Cargo модуля CARGO: water10, food-rations10, energy-cells10, electronics10, все с item. префиксом; у остальных cargo=[].
- Три module types и пределы подтверждены Data/Modules/NavigationComputer/modules-navigationcomputer.json:4–10, Container/modules-container.json:4–10, Engine/modules-engine.json:4–16. Вместимость cargo100000кг/tank1000кг позволяет все проверки.
- Пять станций: objectType Station, persistenceType Permanent, movementType Stationary, speedMps0, directionDegrees0, isKnown=true, portFeeCreditsPerDay100, modules=[]; без explicit credits/inventory, producingModules, events и security zone. Поля необязательной картинки/crew не нужны.
- SPC-MARKET-MINING: name="Mining — Local Market", position(9900,10000), Medium, market.mining.
- SPC-MARKET-INDUSTRIAL: name="Industrial — Local Market", position(10100,10000), Medium, market.industrial.
- SPC-MARKET-HYDROPONIC: name="Hydroponic — Local Market", position(10000,9900), Medium, market.hydroponic.
- SPC-MARKET-TRANSIT: name="Transit — Local Market", position(10000,10100), Large, market.transit.
- SPC-MARKET-SCIENTIFIC: name="Scientific/Military — Local Market", position(10100,10100), Medium, market.scientific-military.
- В JSON использовать camelCase поля positionX/positionY, stationSize и marketProfileId (TK-0002). Не записывать saveFormatVersion, fingerprint или runtime receipts для new-game.

### EP-0001-US-0001-TK-0005-profile-trade-presentation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0001-station-market-profiles/EP-0001-US-0001-TK-0005-profile-trade-presentation/EP-0001-US-0001-TK-0005-profile-trade-presentation.md; зависимость ещё не объявляется реализованной.

No API change. Engine/session DTO и команды не меняются. Добавить только internal helpers в TradeItemPresentation:

```csharp
internal static string ItemUnitLabel(string itemTypeId, bool singular = false);
internal static string FormatQuantity(string itemTypeId, long quantity);
internal static string FormatUnitMass(string itemTypeId, long unitMassKg);
```

Mapping:
- item.food-rations → Trade.UnitRation / Trade.UnitRationSingle.
- item.energy-cells → Trade.UnitCell / Trade.UnitCellSingle.
- item.electronics → Trade.UnitBlock / Trade.UnitBlockSingle.
- item.ice, item.iron-ore, item.silicon, item.magnesium-ore, item.carbon-ore, item.water, item.steel, item.protein-mass, item.fuel и legacy item.uranium-ore → Trade.UnitKg для обеих форм.
- unknown → Trade.UnitGeneric / Trade.UnitGenericSingle.
- ItemDisplayName electronics → Trade.ItemElectronics; ItemDescription electronics → Trade.DescriptionElectronics; ItemImagePath electronics остаётся null.

Dependency locale contract TK-0003: ключи выше, плюс Trade.QuantityWithUnit "{0}", Trade.AmountWithUnit "{0} {1}", Trade.QuantityMass "1 {0} = {1} kg/кг", Trade.ConfirmBuyWithUnit "Buy/Купить {0} {1} for/за {2}", Trade.ConfirmSellWithUnit аналогично. Форматирование CultureInfo.CurrentCulture, параметры quantity/unit/credits передаются в этом порядке. Единицы — подписи, не вычислительные коэффициенты.

### EP-0001-US-0003-TK-0001-trade-execution-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0001-trade-execution-contract/EP-0001-US-0003-TK-0001-trade-execution-contract.md; зависимость ещё не объявляется реализованной.

Namespace DeepSpaceSaga.Contracts:
```csharp
// В конце PlayerCommand:
string? QuoteId = null,
long? MarketRevision = null
// В конце CommandResult:
TradeExecutionReceipt? TradeReceipt = null

public sealed record TradeExecutionReceipt(
    string? StationObjectId, string? ItemTypeId, string? QuoteId,
    long? QuotedMarketRevision, long? ResultMarketRevision,
    long? RequestedQuantity, long ExecutedQuantity, long TotalCredits,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> LimitReasons = default);
```
CommandReasonCodes additions: QuoteRequired="quote_required", StaleQuote="stale_quote", InvalidQuote="invalid_quote", StationBudgetExceeded="station_budget_exceeded", StationCapacityExceeded="station_capacity_exceeded", FuelTradeForbidden="fuel_trade_forbidden". Existing value_overflow string не переименовывать; существующие reasons остаются.

Receipt nullable поля позволяют сохранить отказ до разрешения станции/невалидный raw quantity. На success все IDs/revisions/requested присутствуют, requested>0, 0<executed<=requested, total>=0; result revision > quoted revision. На rejection executed=total=0; known result revision не меняется. Item/quote/requested — исходные данные команды, station/revision — известный authoritative context, не выдуманные IDs. TotalCredits — абсолютная сумма; направление задано CommandResult.CommandType. PartialReason не смешивается с CommandResult.ReasonCode: success имеет ReasonCode=null, ограничения — в LimitReasons.

### EP-0001-US-0003-TK-0002-atomic-quote-execution

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0002-atomic-quote-execution/EP-0001-US-0003-TK-0002-atomic-quote-execution.md; зависимость ещё не объявляется реализованной.

Нового session API нет. Partial internal/private methods:
`private CommandStartOutcome PrepareAndCommitQuotedTrade(PlayerCommand command, long gameTimeMs)`.
`RecordCommandResult(..., long? executedQuantity = null, TradeExecutionReceipt? tradeReceipt = null)` получает optional argument в конце. CommandStartOutcome получает optional TradeExecutionReceipt, чтобы rejection записывался существующим caller ровно один раз. Success уже записывается обработчиком, как в действующем flow.

Dependency US-0015 (точная часть integration seam):
- `public TradeQuoteSnapshot GetTradeQuote(TradeQuoteRequest request)` для тестов/transport;
- `private bool TryValidateTradeQuote(PlayerCommand command, out TradeQuoteSnapshot quote, out string reasonCode)` — проверка выданного token, current state и binding, без мутации;
- `private long NextMarketRevision(string stationObjectId)` — checked подготовка;
- `private void CommitMarketRevision(string stationObjectId, long nextRevision)` — non-fallible присвоение/инвалидация под тем же lock.
- Quote содержит QuoteId, MarketRevision, StationObjectId, ObjectId, ModuleId, CommandType, ItemTypeId, RequestedQuantity, ExecutableQuantity, MaximumQuantity, TotalCredits, Curve(Quantity,UnitPriceCredits), DisabledReason, LimitReasons. Curve covers executed prefix, all fields authoritative.
- US-0002: station.MarketBudgetCredits, bounded cargo stock cap из profile Economy; maxBudget=2*scaledInitialBudget. Неограниченные legacy markets используют Credits и существующее отсутствие cargo cap. Не получать лимит из client snapshot.

### EP-0001-US-0003-TK-0003-trade-result-texts

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0003-trade-result-texts/EP-0001-US-0003-TK-0003-trade-result-texts.md; зависимость ещё не объявляется реализованной.

No API change. Все строки имеют префикс `TradeUX.`:

| Key | English | Russian |
|---|---|---|
| QuoteLoading | Updating trade quote… | Обновляем котировку… |
| QuoteStale | Market conditions changed. Review the new quote and confirm again. | Условия торговли изменились. Проверьте новую котировку и подтвердите снова. |
| QuoteRequired | A current quote is required to confirm this trade. | Для подтверждения сделки нужна актуальная котировка. |
| InvalidQuote | This quote does not match the selected trade. Request a new quote. | Котировка не соответствует выбранной сделке. Запросите новую. |
| QuoteUnavailable | Quote unavailable. No trade was sent. | Котировка недоступна. Сделка не отправлена. |
| StationCapacityLimit | The station has no more storage space for this item. | На складе станции нет места для этого товара. |
| PartialPreview | Will sell {0} of {1} for {2} tokens. The remainder stays in cargo. | Будет продано {0} из {1} за {2} токенов. Остаток останется в трюме. |
| ReceiptUnavailable | The confirmed trade amount is unavailable. | Подтверждённая сумма сделки недоступна. |
| FuelServiceOnly | Fuel is available through refuelling only. | Топливо доступно только через заправку. |

### EP-0001-US-0003-TK-0004-quoted-trade-controls

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0004-quoted-trade-controls/EP-0001-US-0003-TK-0004-quoted-trade-controls.md; зависимость ещё не объявляется реализованной.

GameSessionHandle:
```csharp
public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request,
    CancellationToken cancellationToken = default);
public string SendTradeCommand(string objectId, string moduleId, string commandType,
    string itemTypeId, long quantity, string quoteId, long marketRevision,
    CancellationToken cancellationToken = default);
```
Existing overload сохраняется для старых callers/tests, TradeScreen использует только новый. PlayerCommand fields QuoteId/MarketRevision — TK-0001.

Internal TradeModel методы `ApplyQuote(TradeQuoteSnapshot quote)`, `InvalidateQuote(string reasonKey)`, property `TradeQuoteSnapshot? AuthoritativeQuote`. Local TradeQuote сохраняет existing display fields Maximum/Total/CargoQuantity/AmountBefore/After/BalanceAfter/DisabledReason/LimitReason, добавляет `long ExecutableQuantity = 0` в конец. Maximum/Total/ExecutableQuantity берутся из server quote. Journal.Entry дополняется optional QuoteId/MarketRevision/QuotedTotalCredits, существующие constructor args остаются совместимыми.

Dependency DTO полный набор:
`TradeQuoteRequest(RequestId,ObjectId,ModuleId,CommandType,ItemTypeId,Quantity)`;
`TradeQuoteSnapshot(RequestId,QuoteId,MarketRevision,StationObjectId,ObjectId,ModuleId,CommandType,ItemTypeId,RequestedQuantity,ExecutableQuantity,MaximumQuantity,TotalCredits,Curve,DisabledReason,LimitReasons)`;
`IGameSessionConnection.GetTradeQuoteAsync(request,cancellationToken)`.
US-0015 также предоставляет optional `long? StationTradeSnapshot.MarketRevision` (null legacy snapshot) для обнаружения обновлённого рынка; это prerequisite seam, не изменение Contracts в этом тикете.

### EP-0001-US-0003-TK-0005-confirmed-trade-history

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0003-dynamic-market-trading/EP-0001-US-0003-TK-0005-confirmed-trade-history/EP-0001-US-0003-TK-0005-confirmed-trade-history.md; зависимость ещё не объявляется реализованной.

No public API change. Internal formatting seam: `internal string EntryMessage(TradeJournal.Entry entry)` вместо private, либо эквивалентный internal чистый formatter в том же Render file, не новый production file.
Entry сохраняет CommandId, ItemId, ModuleId, Mode, RequestedQuantity, ModuleLabel и optional QuoteId/MarketRevision/QuotedTotalCredits от TK-0004. Result.TradeReceipt содержит StationObjectId, ItemTypeId, QuoteId, QuotedMarketRevision, ResultMarketRevision, RequestedQuantity, ExecutedQuantity, TotalCredits, LimitReasons. `CommandResult.ExecutedQuantity` остаётся legacy partial field, но при наличии receipt отображение использует только TradeReceipt.

### EP-0001-US-0004-TK-0001-trading-map-schema

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0001-trading-map-schema/EP-0001-US-0004-TK-0001-trading-map-schema.md; зависимость ещё не объявляется реализованной.

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

### EP-0001-US-0004-TK-0002-economic-graph

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0004-seeded-trading-map/EP-0001-US-0004-TK-0002-economic-graph/EP-0001-US-0004-TK-0002-economic-graph.md; зависимость ещё не объявляется реализованной.

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
