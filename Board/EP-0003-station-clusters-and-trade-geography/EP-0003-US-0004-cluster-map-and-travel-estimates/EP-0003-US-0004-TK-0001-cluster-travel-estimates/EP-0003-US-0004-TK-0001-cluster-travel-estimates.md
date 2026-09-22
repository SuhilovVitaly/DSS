---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0004-cluster-map-and-travel-estimates
ticket: EP-0003-US-0004-TK-0001-cluster-travel-estimates
title: Актуальные расстояния и честная оценка прямого полёта
stage: approved
layer: client
depends_on: [EP-0003-US-0002-TK-0001-multi-cluster-placement, EP-0003-US-0002-TK-0002-full-cluster-content, EP-0003-US-0002-TK-0003-multi-cluster-overview, EP-0003-US-0003-TK-0001-resource-orbit-binding, EP-0003-US-0003-TK-0002-cluster-resource-placement, EP-0003-US-0003-TK-0003-cluster-resource-map, EP-0002-US-0006-TK-0001-known-map-projection, EP-0002-US-0006-TK-0002-system-map-navigation, EP-0001-US-0016-TK-0001-market-knowledge-contract, EP-0001-US-0016-TK-0002-authoritative-market-observations, EP-0001-US-0016-TK-0003-market-knowledge-presentation]
files_touched: 5
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Актуальные расстояния и честная оценка прямого полёта

## Why

Выбор станции и направления по карте: актуальные расстояния и честная оценка прямого полёта даёт проверяемый шаг к результату истории. End state: moving groups, 4km/s, unknown Vmax и pausing.; zoom/resize distance invariant.; разные marker styles, stale market knowledge не становится current.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: С общего обзора можно перейти к любому кластеру и выбрать каждую из его станций; видны принадлежность кластеру, профиль, расстояние и оценка.
- AC-0002: Изменение эпохи изменяет межкластерные оценки согласно положению объектов; масштаб камеры не меняет расчёт.
- AC-0003: Подтверждённый маршрут и предполагаемое направление визуально различаются; состояние рыночных знаний и возможность торговли соответствуют правилам EP-0001.

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

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 5 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs | Новый файл; владелец EP-0003-US-0001-TK-0004-local-cluster-map. В исходном дереве отсутствует. | Только алгоритм/представление, явно названные в Implementation steps этого тикета; остальные функции не менять. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:989 — public void Render(SKCanvas canvas, int width, int height) | Точки wiring renderer/prediction/selection этого тикета; никаких прямых Engine запросов. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs:147 — internal bool FitMapView(MapFitMode mode) | Указанные fit/toolbar/LOD actions в существующей карте; мировые данные не менять. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs | src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:161 — public static List<(string Label, string Value)> BuildLines(ObjectInfoPanelData? data) | Только описанные поля panel data и их представление/размещение. |
| tests/DeepSpaceSaga.Client.Tests/ClusterTravelEstimatesTests.cs | Новый файл; владелец EP-0003-US-0004-TK-0001-cluster-travel-estimates. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **client**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

internal static double? EstimateStraightDays(double distanceWorld,double maxSpeedKmS); UI-only projection из immutable снимка, расчёт не является командой или обещанием arrival.

## Implementation steps

1. Показать station cluster/profile, расстояние по текущим predicted poses и straightDays=distanceWorld/10/Vmax*300/86400; у estimate есть simulation epoch и подпись «оценка прямого полёта». При Vmax<=0 — unavailable, не бесконечное ETA.
2. Обновлять межкластерные estimates при смене эпохи; camera zoom/window никак не участвуют. Fit любого кластера и отдельный выбор всех 10–12 members.
3. Предполагаемое торговое направление рисовать пунктиром без rendezvous marker; confirmed ApproachRoute прежним renderer. Market knowledge stale/unknown отображать из EP-0001/US-0016, никаких remote quote refresh.
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

Test class: `ClusterTravelEstimatesTests`. Тестовые сценарии и ожидаемые результаты:

- `ClusterTravelEstimatesTests.EstimatesUseEpochAndMaxSpeed` — moving groups, 4km/s, unknown Vmax и pausing.
- `ClusterTravelEstimatesTests.ResizeDoesNotChangeEstimate` — zoom/resize distance invariant.
- `ClusterTravelEstimatesTests.PlannedLineDiffersFromApproachAndQuote` — разные marker styles, stale market knowledge не становится current.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `ClusterTravelEstimatesTests.EstimatesUseEpochAndMaxSpeed`, `ClusterTravelEstimatesTests.ResizeDoesNotChangeEstimate`, `ClusterTravelEstimatesTests.PlannedLineDiffersFromApproachAndQuote` |
| AC-0002 | `ClusterTravelEstimatesTests.EstimatesUseEpochAndMaxSpeed`, `ClusterTravelEstimatesTests.ResizeDoesNotChangeEstimate`, `ClusterTravelEstimatesTests.PlannedLineDiffersFromApproachAndQuote` |
| AC-0003 | `ClusterTravelEstimatesTests.EstimatesUseEpochAndMaxSpeed`, `ClusterTravelEstimatesTests.ResizeDoesNotChangeEstimate`, `ClusterTravelEstimatesTests.PlannedLineDiffersFromApproachAndQuote` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore --filter "FullyQualifiedName~ClusterTravelEstimatesTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/ClusterTravelEstimatesTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/ClusterTravelEstimatesTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов.

## Definition of Done

- Все implementation steps выполнены только в 5 разрешённых файлах.
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

### EP-0001-US-0016-TK-0001-market-knowledge-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0016-market-knowledge/EP-0001-US-0016-TK-0001-market-knowledge-contract/EP-0001-US-0016-TK-0001-market-knowledge-contract.md; зависимость ещё не объявляется реализованной.

```csharp
public sealed record StationMarketKnowledgeSnapshot(
    string StationObjectId,
    string StationRole,
    bool IsAvailable,
    long ObservedAtGameTimeMs,
    ulong ObservedMarketRevision,
    bool IsStale,
    ImmutableArray<StationMarketStockBandSnapshot> StockBands = default);

public sealed record StationMarketStockBandSnapshot(
    string ItemTypeId,
    StationMarketStockState StockState);
```

`AuthoritativeSnapshot` получает последний trailing/defaulted параметр:

```csharp
[property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<StationMarketKnowledgeSnapshot>))]
ImmutableArray<StationMarketKnowledgeSnapshot> StationMarketKnowledge = default
```

DTO намеренно не содержит `StockQuantity`, `TargetStock`, `MaxStock`, `FreeStockCapacity`, `UnitPriceCredits`, budget/credits, `QuoteId` или quote curve.

### EP-0001-US-0016-TK-0002-authoritative-market-observations

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0016-market-knowledge/EP-0001-US-0016-TK-0002-authoritative-market-observations/EP-0001-US-0016-TK-0002-authoritative-market-observations.md; зависимость ещё не объявляется реализованной.

Нового публичного Engine API нет. `BuildSnapshot` заполняет Contracts field из TK-0001.

Additive save schema в `GameStateData`:

```csharp
[property: JsonPropertyName("marketKnowledge")]
IReadOnlyList<StationMarketKnowledgeData>? MarketKnowledge = null

public sealed record StationMarketKnowledgeData(
    [property: JsonPropertyName("stationObjectId")] string StationObjectId,
    [property: JsonPropertyName("stationRole")] string StationRole,
    [property: JsonPropertyName("isAvailable")] bool IsAvailable,
    [property: JsonPropertyName("observedAtGameTimeMs")] long ObservedAtGameTimeMs,
    [property: JsonPropertyName("observedMarketRevision")] ulong ObservedMarketRevision,
    [property: JsonPropertyName("stockBands")] IReadOnlyList<StationMarketStockBandData> StockBands);

public sealed record StationMarketStockBandData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("stockState")] StationMarketStockState StockState);
```

`IsStale` не входит в save DTO: это производная current-vs-observed величина.

### EP-0001-US-0016-TK-0003-market-knowledge-presentation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0016-market-knowledge/EP-0001-US-0016-TK-0003-market-knowledge-presentation/EP-0001-US-0016-TK-0003-market-knowledge-presentation.md; зависимость ещё не объявляется реализованной.

Публичный cross-project API не меняется. Внутренний Client DTO расширяется trailing optional полем:

```csharp
public readonly record struct ObjectInfoPanelData(
    string ObjectId,
    string? DisplayName,
    double SpeedKmS,
    double Direction,
    string? RenderObjectType,
    string? Image = null,
    StationMarketKnowledgeSnapshot? MarketKnowledge = null);
```

`ObjectInfoPanel.BuildLines(ObjectInfoPanelData?)` остаётся pure formatting seam.
