---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0005-local-and-intercluster-voyages
ticket: EP-0003-US-0005-TK-0002-voyage-user-path
title: Пользовательский путь поездки между кластерами
stage: approved
layer: client
depends_on: [EP-0003-US-0004-TK-0001-cluster-travel-estimates, EP-0001-US-0006-TK-0001-round-trip-engine-proof, EP-0001-US-0006-TK-0002-trade-visit-context, EP-0001-US-0006-TK-0003-station-trade-navigation, EP-0001-US-0009-TK-0001-voyage-fuel-contract, EP-0001-US-0009-TK-0002-fuel-accounting-foundation, EP-0001-US-0009-TK-0003-engine-efficiency-content, EP-0001-US-0009-TK-0004-voyage-fuel-settlement, EP-0001-US-0011-TK-0001-voyage-finance-contract, EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle, EP-0001-US-0011-TK-0003-voyage-profit-realization, EP-0001-US-0011-TK-0004-voyage-profit-texts, EP-0001-US-0011-TK-0005-voyage-profit-presentation, EP-0001-US-0014-TK-0001-voyage-contract, EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle, EP-0001-US-0014-TK-0003-station-route-departure, EP-0001-US-0014-TK-0004-voyage-status-presentation, EP-0003-US-0005-TK-0001-cluster-voyage-integration]
files_touched: 3
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Пользовательский путь поездки между кластерами

## Why

Местный цикл и дальний рейс с возвращением: пользовательский путь поездки между кластерами даёт проверяемый шаг к результату истории. End state: fake connection authoritative sequences, команды с выбранным station ID.; negative/zero/positive result отображается без нового расчёта.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Демонстрационный прогон завершает местный цикл и полный межкластерный рейс туда и обратно, фиксируя посещения, сделки и смены состояния рейса.
- AC-0002: Сближение со станциями работает на новых расстояниях без изменения скорости Approach; фактическое время отделено от начальной оценки по прямой.
- AC-0003: Расходы и финансовый результат получены из торговли EP-0001; нет второго расчёта цены, топлива или прибыли внутри географии.

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

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 3 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs | src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs:1 — using DeepSpaceSaga.Client.UI.Controls; | Только алгоритм/представление, явно названные в Implementation steps этого тикета; остальные функции не менять. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs | Новый файл; владелец EP-0003-US-0001-TK-0004-local-cluster-map. В исходном дереве отсутствует. | Только алгоритм/представление, явно названные в Implementation steps этого тикета; остальные функции не менять. |
| tests/DeepSpaceSaga.Client.Tests/VoyageUserPathTests.cs | Новый файл; владелец EP-0003-US-0005-TK-0002-voyage-user-path. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **client**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No new public API; переиспользуются VoyageState/VoyageFinanceSnapshot из EP-0001. Если базовый UI не поставлен, тикет ждёт явную dependency, не создаёт дубль.

## Implementation steps

1. Подключить выбор cluster station к существующему route/departure UI EP-0001; показывать текущий destination ID/cluster, observed voyage state и ledger result из snapshot.
2. Проверить полный UI путь selection→departure→arrival→trade→return; не вычислять свою прибыль или расход топлива. Недоступная trade кнопка соответствует authoritative docking state.
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

Test class: `VoyageUserPathTests`. Тестовые сценарии и ожидаемые результаты:

- `VoyageUserPathTests.LocalAndRemoteVoyageUiPath` — fake connection authoritative sequences, команды с выбранным station ID.
- `VoyageUserPathTests.FinanceValuesComeFromSnapshot` — negative/zero/positive result отображается без нового расчёта.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `VoyageUserPathTests.LocalAndRemoteVoyageUiPath`, `VoyageUserPathTests.FinanceValuesComeFromSnapshot` |
| AC-0002 | `VoyageUserPathTests.LocalAndRemoteVoyageUiPath`, `VoyageUserPathTests.FinanceValuesComeFromSnapshot` |
| AC-0003 | `VoyageUserPathTests.LocalAndRemoteVoyageUiPath`, `VoyageUserPathTests.FinanceValuesComeFromSnapshot` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore --filter "FullyQualifiedName~VoyageUserPathTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/VoyageUserPathTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/Station/StationScreen.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/VoyageUserPathTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов.

## Definition of Done

- Все implementation steps выполнены только в 3 разрешённых файлах.
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

### EP-0003-US-0005-TK-0001-cluster-voyage-integration

No new public API. Существующие EP-0001 GetTradeQuote(TradeQuoteRequest), navigation.undock(TargetObjectId), VoyageFinanceSnapshot используются через утверждённые dependency контракты; расширение касается route endpoint resolution в orbital mode.

### EP-0001-US-0006-TK-0001-round-trip-engine-proof

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0001-round-trip-engine-proof/EP-0001-US-0006-TK-0001-round-trip-engine-proof.md; зависимость ещё не объявляется реализованной.

No API change. Required prerequisites (planned consumer contract, не текущая реализация):
- Undock: ReceiveCommand(new PlayerCommand(id,sequence,playerId,navigationModuleId,"navigation.undock",TargetObjectId:destinationId)).
- Snapshot.ActiveVoyage nullable; properties VoyageId/OriginStationObjectId/DestinationStationObjectId/State и nullable ReservedFuelKg. Unique voyage до completion, затем null. Отказ terminal Rejected с непустым reason.
- Quote: SimulationEngine.GetTradeQuote(new TradeQuoteRequest(requestId,playerId,moduleId,commandType,itemId,quantity)); PlayerCommand добавляет QuoteId и MarketRevision. Команды trade.buy/trade.sell неизменны. TradeReceipt предоставляет StationObjectId/ItemTypeId/ExecutedQuantity/TotalCredits; RequestedQuantity и revisions из US-0003.
- Existing `CaptureSnapshotForTests(long gameTimeMs=0, SimulationSpeed? speed=null, long? simulationTimeMs=null)`, ReceiveDialogueCommand(DialogueCommand), LoadScenario(ScenarioFile,bool isSave=false). Использовать LoadScenario только при начальном создании fixture; повторная загрузка не является перелётом.

### EP-0001-US-0006-TK-0002-trade-visit-context

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0002-trade-visit-context/EP-0001-US-0006-TK-0002-trade-visit-context.md; зависимость ещё не объявляется реализованной.

No public API change. В существующих файлах добавить internal consumer seam:
- `static string? TradeModel.ResolveLocalStationId(AuthoritativeSnapshot? snapshot)`;
- `long TradeModel.VisitEpoch { get; }`, `string? TradeModel.LocalStationObjectId { get; }`;
- `string? TradeScreen.OpenedForStationObjectId { get; }` и `bool TradeScreen.HasValidVisit { get; }` для TK-0003.

ResolveLocalStationId возвращает ID только если найден живой player по PlayerShipObjectId, он IsDocked, его DockedStationObjectId непустой и равен DockedStationTrade.StationObjectId, ActiveVoyage отсутствует либо State=Docked. Иначе null. ActiveVoyage — prerequisite US-0014 из story, не правка Contracts этим тикетом.

Quote API из US-0003/0015: GetTradeQuoteAsync(TradeQuoteRequest) → TradeQuoteSnapshot с RequestId,QuoteId,MarketRevision,StationObjectId,ObjectId,ModuleId,CommandType,ItemTypeId,RequestedQuantity. Receipt: CommandResult.TradeReceipt с StationObjectId/ItemTypeId/ExecutedQuantity/TotalCredits. Client не изменяет это API.

### EP-0001-US-0006-TK-0003-station-trade-navigation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0006-repeatable-trading-voyage/EP-0001-US-0006-TK-0003-station-trade-navigation/EP-0001-US-0006-TK-0003-station-trade-navigation.md; зависимость ещё не объявляется реализованной.

No public API change. Internal seam в StationScreen:
`string? OpenedForStationObjectId { get; }`, `long? OpenedAtPortFeeGameTimeMs { get; }`, `bool HasValidVisit { get; }`.

Internal `StationTradeNavigation` в новом файле:
- `static bool CanOpen(AuthoritativeSnapshot? snapshot)` — использовать predicate TradeModel.ResolveLocalStationId != null из TK-0002;
- `Task RemoveInvalidOverlaysAsync(ScreenStack stack, Func<AuthoritativeSnapshot?> latest, Func<Task> popModalAsync)` — на UI path, single in-flight operation, повторные вызовы во время ожидания не запускают второй Pop.

Station context validity: совпадают текущий station ID и FirstPortFeeGameTimeMs из PortFees с captured значениями, player жив/Docked, market принадлежит ему, ActiveVoyage не Undocking/InTransit/Docking. Для обычных legacy snapshots без PortFees сохранить проверку station ID; полноценный US-0014 обязан проецировать timestamp новой стоянки. Trade.HasValidVisit дополнительно проверяет epoch/lifetime TK-0002.

### EP-0001-US-0009-TK-0001-voyage-fuel-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0001-voyage-fuel-contract/EP-0001-US-0009-TK-0001-voyage-fuel-contract.md; зависимость ещё не объявляется реализованной.

Trailing fields dependency record:

```csharp
public sealed record ActiveVoyageSnapshot(
    // lifecycle fields supplied by US-0014,
    long? ReservedFuelKg = null,
    long? ProjectedConsumedFuelKg = null,
    long? ProjectedRouteFuelCostCredits = null);
```

Новые DTO/поле:

```csharp
public sealed record VoyageFuelSettlementSnapshot(
    string VoyageId,
    long ReservedFuelKg,
    long ConsumedFuelKg,
    long ReturnedFuelKg,
    long RouteFuelCostCredits);

// Last trailing AuthoritativeSnapshot parameter:
VoyageFuelSettlementSnapshot? LastVoyageFuelSettlement = null

public const string InsufficientVoyageFuel = "insufficient_voyage_fuel";
public const string FuelEfficiencyUnavailable = "fuel_efficiency_unavailable";
```

Все количества/стоимость неотрицательны по Engine invariant; Contracts не бросает validation exceptions. Для settlement Engine гарантирует `ConsumedFuelKg + ReturnedFuelKg == ReservedFuelKg`.

### EP-0001-US-0009-TK-0002-fuel-accounting-foundation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0002-fuel-accounting-foundation/EP-0001-US-0009-TK-0002-fuel-accounting-foundation.md; зависимость ещё не объявляется реализованной.

No Contracts/public session API change. Engine-internal/data schema:

```csharp
// ModuleImplementation JSON + internal definition
long? FuelEfficiencyKmPerKg

// ShipModuleData JSON + InstalledModuleRuntime
long? FuelCostBasisCredits // DTO; runtime is resolved non-negative long for fuel modules
```

Resolution rules:

```text
engine module + explicit basis >= 0  => use explicit basis
engine module + missing basis        => FuelAmountKg * item.fuel.BasePriceCredits
non-fuel module                       => runtime basis 0; explicit non-zero is invalid
```

### EP-0001-US-0009-TK-0003-engine-efficiency-content

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0003-engine-efficiency-content/EP-0001-US-0009-TK-0003-engine-efficiency-content.md; зависимость ещё не объявляется реализованной.

No API change. Content fragment:

```json
{
  "typeId": "module.engine.basic",
  "fuelCapacityKg": 1000,
  "fuelEfficiencyKmPerKg": 10
}
```

### EP-0001-US-0009-TK-0004-voyage-fuel-settlement

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0009-voyage-fuel-cost/EP-0001-US-0009-TK-0004-voyage-fuel-settlement/EP-0001-US-0009-TK-0004-voyage-fuel-settlement.md; зависимость ещё не объявляется реализованной.

Используется Contracts API TK-0001; других public types нет. Engine-internal persisted shape:

```csharp
public sealed record VoyageFuelReservationPartData(
    string ModuleId,
    long ReservedFuelKg,
    long ReservedFuelCostBasisCredits);

// trailing field on dependency ActiveVoyageData
IReadOnlyList<VoyageFuelReservationPartData>? FuelReservationParts = null;

// trailing GameStateData field
VoyageFuelSettlementSnapshot? LastVoyageFuelSettlement = null;
```

Required partial seam:

```csharp
private string? TryReserveVoyageFuel(long distanceKm, int fuelMultiplierPermille,
    out ImmutableArray<VoyageFuelReservationPartData> parts);
private VoyageFuelSettlementSnapshot? SettleVoyageFuel(
    ActiveVoyageData voyage, bool arrived, int progressPermille);
private (long ConsumedKg, long CostCredits) ProjectVoyageFuel(ActiveVoyageData voyage);
```

`null` reason from reserve means success; failure uses `CommandReasonCodes.InsufficientVoyageFuel`, `FuelEfficiencyUnavailable` or existing `value_overflow` convention. Lifecycle не меняется при failure.

### EP-0001-US-0011-TK-0001-voyage-finance-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0001-voyage-finance-contract/EP-0001-US-0011-TK-0001-voyage-finance-contract.md; зависимость ещё не объявляется реализованной.

```csharp
public static class VoyageFinanceStates
{
    public const string InTransit = "in_transit";
    public const string AwaitingRealization = "awaiting_realization";
    public const string Finalized = "finalized";
    public const string Interrupted = "interrupted";
}

public sealed record VoyageCargoRemainderSnapshot(
    string ItemTypeId,
    long Quantity,
    long? CostBasisCredits);

public sealed record VoyageFinanceSnapshot(
    string VoyageId,
    string OriginStationObjectId,
    string? DestinationStationObjectId,
    long StartedGameTimeMs,
    long? CompletedGameTimeMs,
    string State,
    long GrossSalesCredits,
    long? CostOfGoodsSoldCredits,
    bool HasUnknownCostOfGoodsSold,
    long RouteFuelCostCredits,
    long PortFeesAssessedCredits,
    long PortFeesPaidCredits,
    long OutstandingPortFeeDebtCredits,
    long EventCostsCredits,
    long PassengerPayoutCredits,
    long PassengerPenaltyCredits,
    long? NetProfitCredits,
    ImmutableArray<VoyageCargoRemainderSnapshot> UnsoldCargo = default);

// Last trailing AuthoritativeSnapshot parameter:
[property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<VoyageFinanceSnapshot>))]
ImmutableArray<VoyageFinanceSnapshot> VoyageFinances = default
```

`VoyageFinances` упорядочен oldest-first/newest-last и содержит максимум 50 entries. `NetProfitCredits` и `CostOfGoodsSoldCredits` могут быть отрицательным/null только по указанным semantics: COGS неотрицателен, net может быть отрицательным, unknown COGS делает оба nullable результата unavailable. Contracts не выполняет validation и arithmetic.

### EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle/EP-0001-US-0011-TK-0002-voyage-ledger-lifecycle.md; зависимость ещё не объявляется реализованной.

Нового public API нет; используется contract TK-0001. В `SimulationEngine.VoyageLedger.cs` единственный internal owner предоставляет:

```csharp
private void BeginVoyageLedger(ActiveVoyageData voyage, long gameTimeMs);
private void CompleteVoyageTransport(string voyageId, string? destinationStationObjectId,
    long gameTimeMs, bool interrupted, VoyageFuelSettlementSnapshot? fuelSettlement);
private void RecordVoyagePortFee(string postingId, long assessedCredits,
    long paidCredits, long debtAddedCredits);
private void RecordVoyageAmount(string postingId, VoyageAmountKind kind, long credits);
private void FinalizeAwaitingVoyageBeforeUndock();
```

`VoyageAmountKind` — private/internal enum `EventCost`, `PassengerPayout`, `PassengerPenalty`; текущий тикет не создаёт producers. Posting принимается только при positive amount и уникальном non-empty `postingId`; duplicate — no-op, invalid/overflow — отказ до mutation.

### EP-0001-US-0011-TK-0003-voyage-profit-realization

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0003-voyage-profit-realization/EP-0001-US-0011-TK-0003-voyage-profit-realization.md; зависимость ещё не объявляется реализованной.

Нового public API сверх TK-0001 нет. Internal seam:

```csharp
private void RecordVoyageSale(string postingId, TradeExecutionReceipt receipt);
private ImmutableArray<VoyageFinanceSnapshot> BuildVoyageFinanceProjection();
```

`postingId` использует immutable command/receipt identity и принимается один раз. `RecordVoyageSale` ничего не делает для non-Sell, rejected, zero executed quantity, interrupted/finalized/in-transit ledger, другой станции или item без remaining carried quantity.

### EP-0001-US-0011-TK-0004-voyage-profit-texts

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0004-voyage-profit-texts/EP-0001-US-0011-TK-0004-voyage-profit-texts.md; зависимость ещё не объявляется реализованной.

No CLR API change. Обязательные keys:

```text
Finance.VoyageTitle
Finance.VoyageRoute
Finance.VoyageState.InTransit
Finance.VoyageState.AwaitingRealization
Finance.VoyageState.Finalized
Finance.VoyageState.Interrupted
Finance.GrossSales
Finance.CostOfGoodsSold
Finance.RouteFuelCost
Finance.PortFeesAssessed
Finance.PortFeesPaid
Finance.PortFeeDebt
Finance.EventCosts
Finance.PassengerPayout
Finance.PassengerPenalty
Finance.NetProfit
Finance.Unavailable
Finance.UnsoldCargo
Finance.NoVoyages
Trade.VoyageSummary
Trade.VoyageProfit
Trade.VoyageLoss
Trade.VoyageResultUnavailable
```

`Finance.VoyageRoute`, `Finance.UnsoldCargo` и Trade summary keys принимают только positional placeholders, одинаковые в обеих локалях. Знак/цвет/числовое форматирование не кодируются текстом.

### EP-0001-US-0011-TK-0005-voyage-profit-presentation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0011-net-voyage-profit/EP-0001-US-0011-TK-0005-voyage-profit-presentation/EP-0001-US-0011-TK-0005-voyage-profit-presentation.md; зависимость ещё не объявляется реализованной.

No public API change. Client projection rules:

- Finance rows берутся прямо из выбранного `VoyageFinanceSnapshot`; отображаются route/state, Gross Sales, COGS, route fuel, assessed/paid/debt, optional nonzero event/passenger rows, net и unsold cargo.
- `CostOfGoodsSoldCredits`/`NetProfitCredits == null` показывают localized `Unavailable`; никаких zero или derived totals.
- Net sign выбирает presentation color/label only: positive profit, negative loss, zero neutral. Значение не пересчитывается.
- Trade history summary key — `VoyageId`; repeated snapshots обновляют одну строку при переходе `in_transit → awaiting_realization → finalized`, а не добавляют копии.
- Порядок voyage summaries соответствует `snapshot.VoyageFinances`; journal/history остаётся bounded 50 display entries newest-first. Reopen Trade сохраняет journal на `GameSessionHandle` и не дублирует уже seen IDs.

### EP-0001-US-0014-TK-0001-voyage-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0001-voyage-contract/EP-0001-US-0014-TK-0001-voyage-contract.md; зависимость ещё не объявляется реализованной.

```csharp
public static class VoyagePhases
{
    public const string Docked = "Docked";
    public const string Undocking = "Undocking";
    public const string InTransit = "InTransit";
    public const string Docking = "Docking";
}

public sealed record VoyageRouteOptionSnapshot(
    string DestinationStationObjectId,
    string DestinationDisplayName,
    long TravelEstimateGameTimeMs,
    string DistanceClass,
    bool IsAvailable = true,
    string? BlockReasonCode = null);

public sealed record VoyageSnapshot(
    string Phase,
    string? VoyageId = null,
    string? OriginStationObjectId = null,
    string? DestinationStationObjectId = null,
    string? DestinationDisplayName = null,
    int ProgressPermille = 0,
    string? BlockReasonCode = null,
    ImmutableArray<VoyageRouteOptionSnapshot> RouteOptions = default);
```

Trailing parameter `AuthoritativeSnapshot(..., VoyageSnapshot? Voyage = null)`.

Новые `CommandReasonCodes`: `VoyageDestinationRequired = "voyage_destination_required"`, `VoyageDestinationUnavailable = "voyage_destination_unavailable"`, `VoyageAlreadyActive = "voyage_already_active"`, `VoyageWrongDestination = "voyage_wrong_destination"`, `VoyageOutstandingDebt = "voyage_outstanding_debt"`, `VoyageInsufficientFuel = "voyage_insufficient_fuel"`.

### EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle/EP-0001-US-0014-TK-0002-authoritative-voyage-lifecycle.md; зависимость ещё не объявляется реализованной.

В `GameStateData` последний optional параметр:

```csharp
[property: JsonPropertyName("voyageState")]
VoyageStateData? VoyageState = null
```

В namespace `DeepSpaceSaga.Engine.Scenario`:

```csharp
public sealed record VoyageStateData(
    [property: JsonPropertyName("phase")] string Phase,
    [property: JsonPropertyName("voyageId")] string? VoyageId = null,
    [property: JsonPropertyName("originStationObjectId")] string? OriginStationObjectId = null,
    [property: JsonPropertyName("destinationStationObjectId")] string? DestinationStationObjectId = null,
    [property: JsonPropertyName("startedMotionTimeMs")] long StartedMotionTimeMs = 0,
    [property: JsonPropertyName("initialDistanceWorldUnits")] double InitialDistanceWorldUnits = 0,
    [property: JsonPropertyName("progressPermille")] int ProgressPermille = 0,
    [property: JsonPropertyName("blockReasonCode")] string? BlockReasonCode = null);
```

No new public Engine method. Existing `navigation.undock` requires `TargetObjectId` only when a materialized trading map exists; legacy mapless behavior stays unchanged. Snapshot API is TK-0001.

### EP-0001-US-0014-TK-0003-station-route-departure

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0003-station-route-departure/EP-0001-US-0014-TK-0003-station-route-departure.md; зависимость ещё не объявляется реализованной.

Client-internal surface:

```csharp
// StationScreen
internal string? SelectedVoyageDestinationObjectId { get; }

// GameSessionHandle
public void SendUndockCommand(string? destinationStationObjectId = null)
```

`ScreenEvent.Undock` остаётся прежним. Для map-backed snapshot `SkiaWindow` при этом event читает `SelectedVoyageDestinationObjectId`; при null ничего не отправляет и modal не закрывает. Для legacy snapshot с `Voyage == null` parameterless call сохраняет прежний untargeted Undock. No Contracts API beyond TK-0001.

### EP-0001-US-0014-TK-0004-voyage-status-presentation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0014-voyage-lifecycle/EP-0001-US-0014-TK-0004-voyage-status-presentation/EP-0001-US-0014-TK-0004-voyage-status-presentation.md; зависимость ещё не объявляется реализованной.

No public API change. Разрешён internal test seam в `GameSessionScreen`:

```csharp
internal static string VoyageReasonText(string? reasonCode);
```

`BuildPanelLines(BufferedSnapshot?)` остаётся существующей сигнатурой.
