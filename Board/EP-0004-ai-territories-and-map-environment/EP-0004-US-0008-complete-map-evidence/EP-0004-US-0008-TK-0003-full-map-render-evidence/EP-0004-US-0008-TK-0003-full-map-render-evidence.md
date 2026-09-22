---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0008-complete-map-evidence
ticket: EP-0004-US-0008-TK-0003-full-map-render-evidence
title: Проверка взаимодействия и кадров полной карты
stage: approved
layer: client
depends_on: [EP-0004-US-0007-TK-0001-map-environment-save, EP-0004-US-0007-TK-0002-full-map-local-load, EP-0003-US-0008-TK-0001-cluster-correctness-corpus, EP-0003-US-0008-TK-0002-cluster-performance-report, EP-0003-US-0008-TK-0003-cluster-interaction-evidence, EP-0002-US-0008-TK-0001-system-correctness-corpus, EP-0002-US-0008-TK-0002-system-performance-report, EP-0002-US-0008-TK-0003-presented-frame-evidence, EP-0004-US-0008-TK-0002-full-map-performance-report]
files_touched: 2
serves: [AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Проверка взаимодействия и кадров полной карты

## Why

Проверяемая работа карты со всеми слоями: проверка взаимодействия и кадров полной карты даёт проверяемый шаг к результату истории. End state: throwing fake connection при render100 frames.; all layers/selection/UI paths доступны.; counts/flags/config + manual hardware evidence.

Served story criteria (вклад этого тикета в полный результат):
- AC-0002: Измерены генерация, snapshot/save, кадры и взаимодействие с включёнными слоями на указанном оборудовании с целью 80 FPS; улучшения требуют свежего baseline.
- AC-0003: Проверки подтверждают отсутствие синхронных обращений к Engine из render loop, необоснованного роста авторитетных сущностей и побочных игровых эффектов информационных областей.

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

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 2 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/MapFrameEvidence.cs | Новый файл; владелец EP-0002-US-0008-TK-0003-presented-frame-evidence. В исходном дереве отсутствует. | Opt-in frame evidence/aggregation/context; zero IO при выключенном режиме. |
| tests/DeepSpaceSaga.Client.Tests/FullMapRenderEvidenceTests.cs | Новый файл; владелец EP-0004-US-0008-TK-0003-full-map-render-evidence. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **client**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No session API change. Optional diagnostic output расширяется mapCounts/layers/epoch, unit tests не объявляются FPS проверкой.

## Implementation steps

1. Проверить реальный Render с fake IGameSessionConnection, который падает при любом вызове Engine из Render: все map layers читаются из буфера. Сравнить counts при изменении decorative density.
2. Пройти max map UI: System→cluster→base→field→POI, toggles, overlap cycling, pause/resume, resize/UI scale; записать actual presented frame report на заявленном оборудовании с enabled all layers.
3. Добавить в frame report map counts/layer flags/current epoch, чтобы 80FPS нельзя было приписать пустому/выключенному миру. Сохранить итоговые текстовые результаты, временные PNG/JSON удалить после review.
4. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

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

Test class: `FullMapRenderEvidenceTests`. Тестовые сценарии и ожидаемые результаты:

- `FullMapRenderEvidenceTests.RenderNeverCallsSession` — throwing fake connection при render100 frames.
- `FullMapRenderEvidenceTests.FullMapInteractionMatrix` — all layers/selection/UI paths доступны.
- `FullMapRenderEvidenceTests.FrameReportIdentifiesActualMap` — counts/flags/config + manual hardware evidence.

| Served criterion | Named tests |
|---|---|
| AC-0002 | `FullMapRenderEvidenceTests.RenderNeverCallsSession`, `FullMapRenderEvidenceTests.FullMapInteractionMatrix`, `FullMapRenderEvidenceTests.FrameReportIdentifiesActualMap` |
| AC-0003 | `FullMapRenderEvidenceTests.RenderNeverCallsSession`, `FullMapRenderEvidenceTests.FullMapInteractionMatrix`, `FullMapRenderEvidenceTests.FrameReportIdentifiesActualMap` |

Coverage: AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore --filter "FullyQualifiedName~FullMapRenderEvidenceTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/MapFrameEvidence.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/FullMapRenderEvidenceTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/MapFrameEvidence.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/FullMapRenderEvidenceTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов. Производительность измерять лишь в перечисленных сценариях; unit tests не доказывают80FPS.

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

### EP-0004-US-0001-TK-0001-ai-base-contract

public sealed record AiBaseMapData(string ObjectId,string BaseType,string Owner,string? ParentObjectId,OrbitalElements? Orbit,double OffsetX,double OffsetY);  public sealed record AiMapEnvironmentSnapshot(int RulesVersion,ImmutableArray<AiBaseMapData> Bases); AuthoritativeSnapshot: AiMapEnvironmentSnapshot? AiMap=null. Owner="Ai"; BaseType="Planetary"|"Orbital"; ровно один parent/own orbit. Не вводить новую общую diplomacy system. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0004-US-0002-TK-0001-territory-radii-contract

public sealed record TerritoryMapData(string Id,string BaseObjectId,double DefenceRadiusKm,double PatrolRadiusKm); AiMapEnvironmentSnapshot: ImmutableArray<TerritoryMapData> Territories=default. Правило 0<DefenceRadiusKm<=PatrolRadiusKm finite. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0004-US-0004-TK-0001-environment-field-contract

public sealed record EnvironmentFieldData(string Id,string Kind,double Intensity,string AnchorKind,string? ParentObjectId,OrbitalElements? Orbit,double OffsetX,double OffsetY,double InnerRadius,double OuterRadius,double StartAngleDegrees,double SweepDegrees,ulong DecorationSeed); AiMapEnvironmentSnapshot: ImmutableArray<EnvironmentFieldData> Fields=default. AnchorKind=Parent|Orbit|Sun; Kind=Radiation|Dust|Debris; intensity[0,1], 0<=inner<outer, sweep(0,360]. World dimensions, angles clockwise0up. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0004-US-0005-TK-0001-poi-contract

public sealed record PointOfInterestData(string ObjectId,string Name,string Description,string? ParentObjectId,OrbitalElements? Orbit,double OffsetX,double OffsetY); AiMapEnvironmentSnapshot: ImmutableArray<PointOfInterestData> PointsOfInterest=default. Только map metadata, без rewards/quest triggers. Все новые properties используют явные JsonPropertyName camelCase; ImmutableArray optional/default использует существующий ImmutableArrayDefaultJsonConverter<T>, как AuthoritativeSnapshot. Отсутствующие optional поля совместимы со старым JSON.

### EP-0004-US-0008-TK-0001-full-map-correctness-corpus

No API change. Корпус фиксирует versions/seed/settings; независимые oracle проверки, не только повтор собственного hash.

### EP-0004-US-0008-TK-0002-full-map-performance-report

CLI: --solar-map --clusters --all-map-layers --seeds 1:100 --scenarios all --config max. Схема evidence общая с EP-0002, layer flags и counts присутствуют явно.
