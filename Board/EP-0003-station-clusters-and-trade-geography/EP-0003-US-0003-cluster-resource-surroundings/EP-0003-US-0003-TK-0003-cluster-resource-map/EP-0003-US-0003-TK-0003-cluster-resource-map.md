---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0003-cluster-resource-surroundings
ticket: EP-0003-US-0003-TK-0003-cluster-resource-map
title: Известные ресурсы рядом с торговыми районами
stage: approved
layer: client
depends_on: [EP-0003-US-0002-TK-0001-multi-cluster-placement, EP-0003-US-0002-TK-0002-full-cluster-content, EP-0003-US-0002-TK-0003-multi-cluster-overview, EP-0001-US-0005-TK-0001-resource-survey-contract, EP-0001-US-0005-TK-0002-seeded-resource-fields, EP-0001-US-0005-TK-0003-resource-field-content, EP-0001-US-0005-TK-0004-structural-resource-survey, EP-0001-US-0005-TK-0005-resource-info-panel, EP-0003-US-0003-TK-0002-cluster-resource-placement]
files_touched: 4
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Известные ресурсы рядом с торговыми районами

## Why

Ресурсное окружение торговых районов: известные ресурсы рядом с торговыми районами даёт проверяемый шаг к результату истории. End state: snapshot открытой карты отображается сразу.; один marker/region на canonical ID, без дублей.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: У кластеров показаны ресурсные области и сведения о составе, согласованные с ролями станций и допустимыми товарами.
- AC-0002: Повторный запуск воспроизводит геометрию и состав; движение сохраняет назначенные привязки, а ссылки на объекты и ресурсы разрешаются.
- AC-0003: Просмотр не требует разведки; обычные правила действующих команд сохраняются, автоматического снабжения склада из поля не возникает.

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
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs | Новый файл; владелец EP-0003-US-0001-TK-0004-local-cluster-map. В исходном дереве отсутствует. | Только алгоритм/представление, явно названные в Implementation steps этого тикета; остальные функции не менять. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs:989 — public void Render(SKCanvas canvas, int width, int height) | Точки wiring renderer/prediction/selection этого тикета; никаких прямых Engine запросов. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs | src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs:161 — public static List<(string Label, string Value)> BuildLines(ObjectInfoPanelData? data) | Только описанные поля panel data и их представление/размещение. |
| tests/DeepSpaceSaga.Client.Tests/ClusterResourceMapTests.cs | Новый файл; владелец EP-0003-US-0003-TK-0003-cluster-resource-map. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **client**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No session API change. UI присоединяет resource data по fieldId, не генерирует его из specialization.

## Implementation steps

1. Отображать canonical resource fields через binding текущей группы и существующие resource knowledge DTO; выбор показывает состав/типы и принадлежность, без визита/разведки для новой карты.
2. Сохранить command eligibility текущих scanner/drilling команд. Визуальная область не обещает автоматическую добычу или поставку на станцию.
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

Test class: `ClusterResourceMapTests`. Тестовые сценарии и ожидаемые результаты:

- `ClusterResourceMapTests.KnownResourcesSelectableWithoutScan` — snapshot открытой карты отображается сразу.
- `ClusterResourceMapTests.ResourceIdsStableAcrossLayers` — один marker/region на canonical ID, без дублей.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `ClusterResourceMapTests.KnownResourcesSelectableWithoutScan`, `ClusterResourceMapTests.ResourceIdsStableAcrossLayers` |
| AC-0002 | `ClusterResourceMapTests.KnownResourcesSelectableWithoutScan`, `ClusterResourceMapTests.ResourceIdsStableAcrossLayers` |
| AC-0003 | `ClusterResourceMapTests.KnownResourcesSelectableWithoutScan`, `ClusterResourceMapTests.ResourceIdsStableAcrossLayers` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore --filter "FullyQualifiedName~ClusterResourceMapTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/ClusterResourceMapTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/ClusterResourceMapTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов.

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

### EP-0003-US-0003-TK-0002-cluster-resource-placement

No new resource API. Единый source StationResourceFieldsState/DTO/generator в src/DeepSpaceSaga.Engine/Scenario/StationResourceFields.cs — плановый файл EP-0001-US-0005-TK-0002. ClusterMap.ResourceBindings хранит только FieldId/ClusterId/AnchorStationId/offset. Runtime data и knowledge projection используют исходный state, не дубликат.

### EP-0001-US-0005-TK-0001-resource-survey-contract

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0001-resource-survey-contract/EP-0001-US-0005-TK-0001-resource-survey-contract.md; зависимость ещё не объявляется реализованной.

Namespace DeepSpaceSaga.Contracts; все типы можно поместить в существующий ObjectMotionSnapshot.cs:
```csharp
// В конце ObjectMotionSnapshot:
AsteroidSurveySnapshot? Survey = null

public sealed record ResourceFractionSnapshot(string ItemTypeId, int Permille);
public sealed record AsteroidSurveySnapshot(long MassKg, bool CompositionKnown,
    bool CanStructuralScan, string? CompositionType = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ResourceFractionSnapshot>))]
    ImmutableArray<ResourceFractionSnapshot> Resources = default);
```
`ResourceSurveyReasonCodes` static constants:
`UnsupportedTarget="resource_survey_unsupported"`, `TargetNotIdentified="target_not_identified"`, `AlreadyKnown="target_already_structurally_identified"`, `OutOfRange="target_out_of_range"`, `TargetLost="target_lost"`, `ScanFailed="scan_failed"`, `InvalidTime="resource_survey_invalid_time"`.
Existing Busy/ModuleUnavailable/MissingTarget/UnknownTarget reason constants переиспользуются.

Unknown: CompositionKnown=false, CompositionType=null, Resources empty. Known: CompositionKnown=true, CompositionType один из3, Resources positive integer fractions, sum1000; CanStructuralScan=false. CanStructuralScan — разрешённость target/range и отсутствие текущей попытки для target; availability выбранного модуля Client проверяет отдельно; Engine валидирует всё заново. MassKg допустима в обоих состояниях: постоянный generated asteroid уже IsKnown=true по требованиям.

### EP-0001-US-0005-TK-0002-seeded-resource-fields

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0002-seeded-resource-fields/EP-0001-US-0005-TK-0002-seeded-resource-fields.md; зависимость ещё не объявляется реализованной.

Settings: optional `typeData.stationResourceFields` path. Отсутствие означает disabled; объявленные null/blank/missing file — ContentException. GameStateData: `[JsonPropertyName("stationResourceFields")] StationResourceFieldsState? StationResourceFields = null`.

В новом файле namespace DeepSpaceSaga.Engine.Scenario, public sealed records с JsonPropertyName camelCase на КАЖДОМ поле и IReadOnlyList коллекциями:
```csharp
StationResourceFieldConfig(int SchemaVersion, int FirstObjectNumber,
 int MaxPlacementAttempts, double InnerRadiusKm, double OuterRadiusKm,
 double StationClearanceKm, double AsteroidSpacingKm, double CorridorHalfWidthKm,
 IReadOnlyList<ResourceFieldRoleData> Roles, IReadOnlyList<ResourceFieldKindData> FieldKinds,
 IReadOnlyList<ResourceMassBandData> MassBands, ResourceSurveyRulesData StructuralScan);
ResourceFieldRoleData(string MarketProfileId, IReadOnlyList<ResourceFieldCountData> Fields);
ResourceFieldCountData(string FieldKindId, int AsteroidCount);
ResourceFieldKindData(string FieldKindId, IReadOnlyList<ResourceVariantData> Variants);
ResourceVariantData(string VariantId, string CompositionType, int Weight,
 IReadOnlyList<ResourceFractionData> Resources);
ResourceFractionData(string ItemTypeId, int Permille);
ResourceMassBandData(string BandId, long MinKg, long MaxKg, int Weight);
ResourceSurveyRulesData(double RangeKm, long DurationGameTimeMs, int SuccessChancePercent);
ResourceFieldRngData(string Name, ulong Seed, ulong Counter);
ResourceFieldAsteroidData(string ObjectId, string StationObjectId, string FieldKindId,
 string VariantId, IReadOnlyList<ResourceFractionData> Resources, bool CompositionKnown = false);
ResourceSurveyJobData(string CommandId, string ObjectId, string ModuleId, string TargetObjectId,
 long StartedGameTimeMs, long DueGameTimeMs, long LastValidatedSimulationTimeMs);
StationResourceFieldsState(int SchemaVersion, StationResourceFieldConfig Rules,
 int NextObjectNumber, IReadOnlyList<ResourceFieldAsteroidData> Asteroids,
 IReadOnlyList<ResourceFieldRngData> RngStreams, IReadOnlyList<ResourceSurveyJobData> Surveys);
```
Config/state schemaVersion=1. New internal Engine entry `void ConfigureStationResourceFields(StationResourceFieldConfig? config)` stores validated immutable copy, invoked before initial LoadScenario.
New helper `StationResourceFields.Generate(GameStateData materialized, ulong masterSeed, StationResourceFieldConfig config, GameDataRegistry registry)` returns GameStateData with appended SpaceObjects and manifest; `ValidateSaved(GameStateData state, GameDataRegistry registry)` returns normalized validated state without RNG draws. Public DTOs coexist with internal static helper in the new file.

Dependency US-0004: GameStateData.TradingMap.Rules.Stations(ObjectId,MarketProfileId), TradingMap.Edges(FromStationObjectId,ToStationObjectId); actual station positions in SpaceObjects. No map geometry/role creation here.

### EP-0001-US-0005-TK-0003-resource-field-content

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0003-resource-field-content/EP-0001-US-0005-TK-0003-resource-field-content.md; зависимость ещё не объявляется реализованной.

No API change. Settings.typeData.stationResourceFields — optional path из TK-0002. JSON использует точные camelCase properties его StationResourceFieldConfig. Копирование нового файла уже обеспечено src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj:264; project-файл не редактировать.

### EP-0001-US-0005-TK-0004-structural-resource-survey

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0004-structural-resource-survey/EP-0001-US-0005-TK-0004-structural-resource-survey.md; зависимость ещё не объявляется реализованной.

No new public API. Использовать additive Survey DTO и ResourceSurveyReasonCodes из TK-0001; PlayerCommand(CommandId,ClientSequence,ObjectId,ModuleId,CommandType,TargetObjectId) из Contracts/PlayerCommand.cs:6. Команда scanner.structuralScan адресуется существующему установленному scanner module.

State из TK-0002: ResourceSurveyJobData(CommandId,ObjectId,ModuleId,TargetObjectId,StartedGameTimeMs,DueGameTimeMs,LastValidatedSimulationTimeMs). Первые два времени — calendar, последнее — physical. Сохранённый job не содержит generic ActiveCycle.

Новые internal/private методы разместить только в новом partial: start/validate/complete survey, validate restored jobs, project Survey/module busy, restore pending command IDs. Точные private names свободны; внешний контракт фиксирован ниже.

### EP-0001-US-0005-TK-0005-resource-info-panel

Read-only плановый контракт из Board/EP-0001-trading-system/EP-0001-US-0005-station-resource-fields/EP-0001-US-0005-TK-0005-resource-info-panel/EP-0001-US-0005-TK-0005-resource-info-panel.md; зависимость ещё не объявляется реализованной.

В существующем ObjectInfoPanel.cs record ObjectInfoPanelData получает последний optional параметр `AsteroidSurveySnapshot? Survey = null`. Остальные параметры/значения по умолчанию сохраняются. Contracts не меняются, используется TK-0001.

ObjectInfoPanel.BuildLines(ObjectInfoPanelData? data) сохраняет тип результата List<(string Label,string Value)>.
