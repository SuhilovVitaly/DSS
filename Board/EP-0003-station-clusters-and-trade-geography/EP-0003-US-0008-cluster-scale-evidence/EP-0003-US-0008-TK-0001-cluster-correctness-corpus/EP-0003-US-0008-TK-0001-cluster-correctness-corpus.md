---
epic: EP-0003-station-clusters-and-trade-geography
story: EP-0003-US-0008-cluster-scale-evidence
ticket: EP-0003-US-0008-TK-0001-cluster-correctness-corpus
title: Корпус геометрии и сохранения полной торговой сети
stage: approved
layer: engine
depends_on: [EP-0003-US-0006-TK-0001-cluster-save-state, EP-0003-US-0006-TK-0002-cluster-local-resume, EP-0003-US-0007-TK-0001-long-voyage-runner, EP-0003-US-0007-TK-0002-long-voyage-diagnostics]
files_touched: 1
serves: [AC-0001, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Корпус геометрии и сохранения полной торговой сети

## Why

Проверяемая работа полной торговой сети: корпус геометрии и сохранения полной торговой сети даёт проверяемый шаг к результату истории. End state: полный корпус с воспроизводимой failing case.; все endpoints/IDs/roles определены для AI placement.; отказ без partial snapshot.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Отчёт охватывает границы 3/5 кластеров и 10/12 станций, 2/5 поясов, сценарные старты и временные проверки компактности и раздельности.
- AC-0003: Проверены диагностика невозможного размещения и отсутствие частичной публикации; для EP-0004 доступны состав районов, стартовая сеть и межкластерные направления.

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

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 1 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs | Новый файл; владелец EP-0003-US-0008-TK-0001-cluster-correctness-corpus. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **engine**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Public API after the change

No API change. Engine test corpus, не performance assertions.

## Implementation steps

1. Запустить seeds 1..100 × все пять сценариев × boundary {3,5} clusters/{10,12} stations/{2,5} belts. Проверить scale/IDs/profiles/graph/return opportunity и continuation.
2. Независимо проверить rigid internal distances и swept envelope separation на 0,1,7,30,100,365d плюс critical relative phases; невозможный placement не публикует partial state.
3. Проверить интерфейс передачи EP-0004: membership, start cluster, local graph и intercluster candidates — resolved endpoints, не screen aggregates и не cached eternal ETA.
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

Test class: `ClusterCorrectnessCorpusTests`. Тестовые сценарии и ожидаемые результаты:

- `ClusterCorrectnessCorpusTests.HundredSeedClusterBoundaryMatrix` — полный корпус с воспроизводимой failing case.
- `ClusterCorrectnessCorpusTests.TradeGeographyHandoffIsComplete` — все endpoints/IDs/roles определены для AI placement.
- `ClusterCorrectnessCorpusTests.InvalidClusterPlacementIsAtomic` — отказ без partial snapshot.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `ClusterCorrectnessCorpusTests.HundredSeedClusterBoundaryMatrix`, `ClusterCorrectnessCorpusTests.TradeGeographyHandoffIsComplete`, `ClusterCorrectnessCorpusTests.InvalidClusterPlacementIsAtomic` |
| AC-0003 | `ClusterCorrectnessCorpusTests.HundredSeedClusterBoundaryMatrix`, `ClusterCorrectnessCorpusTests.TradeGeographyHandoffIsComplete`, `ClusterCorrectnessCorpusTests.InvalidClusterPlacementIsAtomic` |

Coverage: AC-0001, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore --filter "FullyQualifiedName~ClusterCorrectnessCorpusTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов.

## Definition of Done

- Все implementation steps выполнены только в 1 разрешённых файлах.
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
