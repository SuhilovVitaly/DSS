---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0003-trade-compatible-ai-placement
ticket: EP-0004-US-0003-TK-0001-temporal-placement-validation
title: Проверка обходов и критических эпох
stage: approved
layer: engine
depends_on: [EP-0004-US-0002-TK-0001-territory-radii-contract, EP-0004-US-0002-TK-0002-moving-territory-data, EP-0004-US-0002-TK-0003-territory-rendering, EP-0003-US-0004-TK-0001-cluster-travel-estimates]
files_touched: 4
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Проверка обходов и критических эпох

## Why

Территории ИИ вокруг доступной торговой сети: проверка обходов и критических эпох даёт проверяемый шаг к результату истории. End state: искусственная опасная эпоха между1/7d, interval bound.; overlap circles, blocked chokepoint, tangency и sun boundary.; impossible map отказ без partial world.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: В отчёте размещения проверены старт, 1, 7, 30, 100 и 365 календарных дней и критические сближения; названы горизонт, настройки и нарушения.
- AC-0002: Старт и местные связи вне областей, межкластерные обходные направления образуют связную сеть; сохранена компактность человеческих кластеров.
- AC-0003: Повторные попытки размещения детерминированы и ограничены; невозможная конфигурация даёт понятную ошибку до публикации мира.

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
- Новые области только информационные. Все human фракции враждебны Ai, но отношения между human фракциями не задаются. Counts/radii — configurable стартовые значения из content tickets.
- Public API after the change является точным плановым контрактом этого тикета. Статус approved не означает, что этот API уже существует.

## Code context

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 4 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs | Новый файл; владелец EP-0004-US-0003-TK-0001-temporal-placement-validation. В исходном дереве отсутствует. | Только алгоритм/представление, явно названные в Implementation steps этого тикета; остальные функции не менять. |
| src/DeepSpaceSaga.Engine/Scenario/AiBaseGenerator.cs | Новый файл; владелец EP-0004-US-0001-TK-0002-seeded-ai-bases. В исходном дереве отсутствует. | Seeded base placement/owner/radii, detached validation и named diagnostics этого тикета. |
| src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs | Новый файл; владелец EP-0002-US-0001-TK-0003-seeded-world-bootstrap. В исходном дереве отсутствует. | Seeded detached world geometry, IDs, конечные retries и bounded stages генерации. |
| tests/DeepSpaceSaga.Engine.Tests/TemporalPlacementValidationTests.cs | Новый файл; владелец EP-0004-US-0003-TK-0001-temporal-placement-validation. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **engine**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Public API after the change

internal static PlacementValidationResult AiTradePlacementValidator.Validate(ScenarioFile world,StationClusterMapSnapshot clusters,AiMapEnvironmentSnapshot ai,long horizonGameTimeMs); result содержит IsValid, checks(epochGameTimeMs,baseId,linkId,clearance), violations; raw placement data доступна generator, не новая session command.

## Implementation steps

1. Проверять 0,1,7,30,100,365 game days плюс critical conjunction/opposition пар base/group, рассчитанные из относительной angular velocity. Для локальных route segments дополнить консервативной interval subdivision: bound displacement vmax*dt, уменьшать interval до certified clearance или отмечать uncertain как invalid placement. Horizon конечный, не вечная безопасность.
2. В каждой эпохе построить геометрический visibility graph вне union outer discs внутри system bounds: nodes stations/cluster access и polygonal conservative circumscribed disc vertices (64 угла), edges без пересечений interiors и вне Sun exclusion extent. BFS должен связывать все clusters; это reservation proof, не игровой автопилот.
3. Точность геометрии epsilon1e-6 world; касание считается blocked. При неопределённом interval/недостаточном clearance отклонять candidate с base/link/epoch/reason; finite deterministic retries только Ai stage. Human compactness и initial50–75d не меняются.
4. Если validator diagnostics internal, добавить в уже разрешённый aiGen файл public read-only DTO report и сохранить его как last-generation diagnostic, доступный Performance friend; тикет отчёта читает result из генерации, а не требует новой mutation API.
5. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

## Out of scope

Патрули, урон, перехват, блокировка пролёта, сенсорные/ценовые эффекты, diplomacy/exploration rewards. Нельзя изменять файлы вне Code context, requirements/состав эпика, обходить dependency недостающим вторым API или менять не связанный пользовательский diff.

## Invariants

- `src/DeepSpaceSaga.Contracts/SimulationSpeed.cs:27`: календарный коэффициент300; движение и календарь не взаимозаменяемы.
- `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:4`: world units100m, speed km/s, clockwise0up. Отображение не изменяет мир.
- `Documentation/00-Process/CLAUDE.md:34`: render loop не запрашивает Engine; Contracts без graphics, Motion общий.
- `Documentation/01-Requirements/EngineRequirements.md:5335`: Approach с постоянной скоростью; новый map scope не даёт ускорение кораблю.
- Один layer, максимум5 файлов, новые DTO immutable/JSON-safe. Результат ошибки не публикует partial state.
- Пересечение информационных областей не создаёт gameplay side effects; источник — Documentation/02-FirstRelease/Mechanics/SolarSystemMapConcept.md:228 (территории/поля первой стадии).

## Tests

Test class: `TemporalPlacementValidationTests`. Тестовые сценарии и ожидаемые результаты:

- `TemporalPlacementValidationTests.CriticalApproachBetweenSamplesIsDetected` — искусственная опасная эпоха между1/7d, interval bound.
- `TemporalPlacementValidationTests.DetourGraphConnectsClusters` — overlap circles, blocked chokepoint, tangency и sun boundary.
- `TemporalPlacementValidationTests.RetryIsBoundedAndDoesNotMoveHumans` — impossible map отказ без partial world.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `TemporalPlacementValidationTests.CriticalApproachBetweenSamplesIsDetected`, `TemporalPlacementValidationTests.DetourGraphConnectsClusters`, `TemporalPlacementValidationTests.RetryIsBoundedAndDoesNotMoveHumans` |
| AC-0002 | `TemporalPlacementValidationTests.CriticalApproachBetweenSamplesIsDetected`, `TemporalPlacementValidationTests.DetourGraphConnectsClusters`, `TemporalPlacementValidationTests.RetryIsBoundedAndDoesNotMoveHumans` |
| AC-0003 | `TemporalPlacementValidationTests.CriticalApproachBetweenSamplesIsDetected`, `TemporalPlacementValidationTests.DetourGraphConnectsClusters`, `TemporalPlacementValidationTests.RetryIsBoundedAndDoesNotMoveHumans` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore --filter "FullyQualifiedName~TemporalPlacementValidationTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/AiBaseGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/TemporalPlacementValidationTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/AiBaseGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/TemporalPlacementValidationTests.cs"
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

### EP-0004-US-0001-TK-0001-ai-base-contract

public sealed record AiBaseMapData(string ObjectId,string BaseType,string Owner,string? ParentObjectId,OrbitalElements? Orbit,double OffsetX,double OffsetY);  public sealed record AiMapEnvironmentSnapshot(int RulesVersion,ImmutableArray<AiBaseMapData> Bases); AuthoritativeSnapshot: AiMapEnvironmentSnapshot? AiMap=null. Owner="Ai"; BaseType="Planetary"|"Orbital"; ровно один parent/own orbit. Не вводить новую общую diplomacy system. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0004-US-0002-TK-0001-territory-radii-contract

public sealed record TerritoryMapData(string Id,string BaseObjectId,double DefenceRadiusKm,double PatrolRadiusKm); AiMapEnvironmentSnapshot: ImmutableArray<TerritoryMapData> Territories=default. Правило 0<DefenceRadiusKm<=PatrolRadiusKm finite. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.
