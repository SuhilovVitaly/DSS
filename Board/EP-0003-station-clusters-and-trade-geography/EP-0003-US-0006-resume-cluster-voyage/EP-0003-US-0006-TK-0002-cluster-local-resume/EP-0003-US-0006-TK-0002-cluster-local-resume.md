---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0006-resume-cluster-voyage
ticket: EP-0003-US-0006-TK-0002-cluster-local-resume
title: Рейс между кластерами после штатного Load
stage: approved
layer: local-client
depends_on: [EP-0003-US-0005-TK-0001-cluster-voyage-integration, EP-0003-US-0005-TK-0002-voyage-user-path, EP-0002-US-0007-TK-0001-generated-world-persistence, EP-0002-US-0007-TK-0002-local-world-save-roundtrip, EP-0001-US-0012-TK-0001-economy-save-schema, EP-0001-US-0012-TK-0002-market-state-continuity, EP-0001-US-0012-TK-0003-voyage-ledger-continuity, EP-0001-US-0012-TK-0004-deterministic-economy-continuation, EP-0001-US-0012-TK-0005-local-save-roundtrip, EP-0003-US-0006-TK-0001-cluster-save-state]
files_touched: 2
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Рейс между кластерами после штатного Load

## Why

Возобновление торговли между теми же кластерами: рейс между кластерами после штатного load даёт проверяемый шаг к результату истории. End state: endpoint/route/cluster/market state сохранены.; один ledger receipt, один расход.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Для сохранения в пути и в стыковке сопоставлены непрерывный и возобновлённый прогоны по группам, ID, орбитальным смещениям, ресурсам и торговым связям.
- AC-0002: Профили, остатки, бюджеты, состояние и результаты рейса продолжаются без повторной инициализации.
- AC-0003: Невалидные ссылки на состав групп и несовместимые данные не приводят к запуску частично восстановленной сети.

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
| tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj | tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj:1 — <Project Sdk="Microsoft.NET.Sdk"> | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |
| tests/DeepSpaceSaga.Engine.Tests/ClusterLocalResumeTests.cs | Новый файл; владелец EP-0003-US-0006-TK-0002-cluster-local-resume. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **local-client**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Production files не меняются; это интеграционный результат локального транспорта, а не Engine feature.

## Public API after the change

No API change. Matching integration project DeepSpaceSaga.Engine.Tests.

## Implementation steps

1. В matching Engine.Tests.csproj добавить ProjectReference на ../../src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj (сейчас есть только Engine reference). После первого тикета reference уже есть: повторно не добавлять. Production layer остаётся local-client, изменение только integration test surface.
2. Через LocalGameSessionConnection сохранить в пути и в доке, закрыть сессию, Load через Settings+save; продолжить теми же command IDs/time.
3. Сопоставить continuous и resumed snapshots/receipts, проверить retry сделки после load не дублирует покупку и склад. Не заменять engine save тестами ручной сериализации DTO.
4. Для детерминированного сравнения создать SimulationEngine в test assembly и обернуть public LocalGameSessionConnection(engine,tempDirectory), держать Speed0; двигать explicit-time Engine test seam между transport Save/Load действиями. Не сравнивать две wall-clock сессии без синхронизации. Проверка production factory ограничена опубликованным paused snapshot и неизменностью save-файла.
5. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

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

Test class: `ClusterLocalResumeTests`. Тестовые сценарии и ожидаемые результаты:

- `ClusterLocalResumeTests.InFlightAndDockedClusterLocalLoad` — endpoint/route/cluster/market state сохранены.
- `ClusterLocalResumeTests.RetriedTradeAfterLocalLoadIsIdempotent` — один ledger receipt, один расход.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `ClusterLocalResumeTests.InFlightAndDockedClusterLocalLoad`, `ClusterLocalResumeTests.RetriedTradeAfterLocalLoadIsIdempotent` |
| AC-0002 | `ClusterLocalResumeTests.InFlightAndDockedClusterLocalLoad`, `ClusterLocalResumeTests.RetriedTradeAfterLocalLoadIsIdempotent` |
| AC-0003 | `ClusterLocalResumeTests.InFlightAndDockedClusterLocalLoad`, `ClusterLocalResumeTests.RetriedTradeAfterLocalLoadIsIdempotent` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet restore "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore --filter "FullyQualifiedName~ClusterLocalResumeTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine.LocalClient/DeepSpaceSaga.Engine.LocalClient.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterLocalResumeTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterLocalResumeTests.cs"
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

### EP-0003-US-0003-TK-0001-resource-orbit-binding

public sealed record ClusterResourceBinding(string FieldId,string ClusterId,string AnchorStationId,double OffsetX,double OffsetY); StationClusterMapSnapshot: ImmutableArray<ClusterResourceBinding> ResourceBindings=default. Offset вращается с группой; source composition остаётся StationResourceFieldsState EP-0001. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0003-US-0006-TK-0001-cluster-save-state

GameStateData: [JsonPropertyName("clusterMap")] StationClusterMapSnapshot? ClusterMap=null; optional для pre-cluster world. SolarSystem без ClusterMap остаётся валидным legacy результата EP-0002.

### EP-0001-US-0012-TK-0001-economy-save-schema

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0001-economy-save-schema/EP-0001-US-0012-TK-0001-economy-save-schema.md; зависимость ещё не объявляется реализованной.

Engine scenario/save schema:

```csharp
public sealed record TradingEconomyContinuationData(
    int SchemaVersion,
    string ConfigurationFingerprint,
    long LastProcessedMarketGameTimeMs,
    long NextMarketRevision,
    long NextMarketEventSequence,
    IReadOnlyList<string>? DurableTerminalReceiptIds = null);

// trailing GameStateData field
[property: JsonPropertyName("tradingEconomyContinuation")]
TradingEconomyContinuationData? TradingEconomyContinuation = null;
```

`SchemaVersion` начинает с 1. Все numeric cursors неотрицательны; receipt ids nonblank, distinct и ordinal-sorted. Manifest обязателен для нового save version, запрещён для `saveFormatVersion=0` scenario и для legacy version, которую migration ещё не нормализовала.

Internal pure seam:

```csharp
internal static class TradingEconomySaveMigration
{
    internal static ScenarioFile Normalize(ScenarioFile source);
}
```

### EP-0001-US-0012-TK-0002-market-state-continuity

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0002-market-state-continuity/EP-0001-US-0012-TK-0002-market-state-continuity.md; зависимость ещё не объявляется реализованной.

No public API change. Internal partial seam:

```csharp
private sealed record StagedMarketContinuation(
    long LastProcessedMarketGameTimeMs,
    long NextMarketRevision,
    long NextMarketEventSequence);

private StagedMarketContinuation StageMarketContinuation(
    ScenarioFile source,
    IReadOnlyList<SpaceObjectRuntime> stagedObjects);

private TradingEconomyContinuationData CaptureMarketContinuation(
    TradingEconomyContinuationData manifest);

private void CommitMarketContinuation(StagedMarketContinuation staged);
```

`Stage` выполняет только validation/conversion, не меняет engine. Map/fields/stocks/events остаются в dependency-owned records/runtime objects; adapter отвечает за cross-cutting cursors, consistency и ephemeral quote invalidation.

### EP-0001-US-0012-TK-0003-voyage-ledger-continuity

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0003-voyage-ledger-continuity/EP-0001-US-0012-TK-0003-voyage-ledger-continuity.md; зависимость ещё не объявляется реализованной.

No public API change. Internal partial seam:

```csharp
private sealed record StagedVoyageContinuation(
    ActiveVoyageData? ActiveVoyage,
    VoyageLedgerData? ActiveLedger,
    ImmutableArray<VoyageLedgerData> ClosedLedgers,
    ImmutableHashSet<string> DurableTerminalReceiptIds);

private StagedVoyageContinuation StageVoyageContinuation(
    ScenarioFile source,
    IReadOnlyList<SpaceObjectRuntime> stagedObjects);

private void CaptureVoyageContinuation(
    ref GameStateData state,
    ref TradingEconomyContinuationData manifest);

private void CommitVoyageContinuation(StagedVoyageContinuation staged);
```

Exact dependency record namespaces/names may differ, но required fields фиксированы: stable voyage/ledger/entry ids; origin/destination; state/progress/arrival; reservation tank/module parts with kg+basis; last settlement; active/closed ledger entries and totals. Несовпадение возвращает ticket в review.

### EP-0001-US-0012-TK-0004-deterministic-economy-continuation

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0004-deterministic-economy-continuation/EP-0001-US-0012-TK-0004-deterministic-economy-continuation.md; зависимость ещё не объявляется реализованной.

No public API change. Internal root seam:

```csharp
private sealed record StagedTradingContinuation(
    StagedMarketContinuation Market,
    StagedVoyageContinuation Voyage);

private StagedTradingContinuation StageTradingContinuation(
    ScenarioFile source,
    IReadOnlyList<SpaceObjectRuntime> stagedObjects);

private string BuildTradingConfigurationFingerprint(TradingMapStateData? map);
private TradingEconomyContinuationData CaptureTradingContinuation();
private void CommitTradingContinuation(StagedTradingContinuation staged);
```

`BuildTradingConfigurationFingerprint` canonicalizes semantic version/fingerprint strings with explicit field labels and ordinal UTF-8, then returns uppercase SHA-256. It never hashes mutable state or raw JSON file order.

### EP-0001-US-0012-TK-0005-local-save-roundtrip

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0012-resume-trading-economy/EP-0001-US-0012-TK-0005-local-save-roundtrip/EP-0001-US-0012-TK-0005-local-save-roundtrip.md; зависимость ещё не объявляется реализованной.

No API change. Existing signatures remain:

```csharp
public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default);
public static LocalGameSessionConnection CreateFromSaveFile(
    string settingsPath, string savePath, string? saveDirectory = null);
```

No LocalClient DTO or economic serializer is introduced.
