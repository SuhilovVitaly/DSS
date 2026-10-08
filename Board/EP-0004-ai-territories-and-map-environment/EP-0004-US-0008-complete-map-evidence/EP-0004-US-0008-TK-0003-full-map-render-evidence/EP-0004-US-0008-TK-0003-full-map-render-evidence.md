---
epic: EP-0004-ai-territories-and-map-environment
story: EP-0004-US-0008-complete-map-evidence
ticket: EP-0004-US-0008-TK-0003-full-map-render-evidence
title: Проверка взаимодействия и кадров полной карты
stage: in_progress
implementation: complete
evidence: recorded
performance_acceptance: open
layer: client
depends_on: [EP-0004-US-0007-TK-0001-map-environment-save, EP-0004-US-0007-TK-0002-full-map-local-load, EP-0003-US-0008-TK-0001-cluster-correctness-corpus, EP-0003-US-0008-TK-0002-cluster-performance-report, EP-0003-US-0008-TK-0003-cluster-interaction-evidence, EP-0002-US-0008-TK-0001-system-correctness-corpus, EP-0002-US-0008-TK-0002-system-performance-report, EP-0002-US-0008-TK-0003-presented-frame-evidence, EP-0004-US-0008-TK-0002-full-map-performance-report]
files_touched: 7
serves: [AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

## Текущий контракт — 2026-10-08

Исходный план и скопированные dependency inputs ниже сохранены для трассировки. Фактические версии/API и расширенный scope определяются Execution/Resolved sections и [текущим контрактом](../../../../Documentation/04-Engineering/AiMapEnvironment.md). SaveFormat15, AiMap rulesVersion1, шесть сценариев, inline poiTemplates; Field/POI metadata не являются entities или engine command targets. Исторический NOT RUN не заменяет финальное native evidence US8; FPS80 acceptance остаётся OPEN.


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

## Resolved scope before implementation — 2026-10-08

Add src/DeepSpaceSaga.Client/UI/SkiaWindow.cs and tools/DeepSpaceSaga.Performance/SolarNativeEvidence.cs to the two planned files. The real window must supply live counts/flags/epochs to the existing collector; the existing native CLI must force min/max AI+cluster counts, exclude new layers in baseline, and exercise actual mouse/speed/layer handlers with --all-map-layers. No session/gameplay API changes. Snapshot metadata is read locally; GPU execution and physical scanout remain unmeasured, swap intervals are measured. Native scripted acceptance plus PNG inspection is distinct from human playthrough. FullMapInteractionMatrix complements existing all-scale overlap and generated-map matrix; tests are not FPS proof. Native views system/belt/cluster/selected plus base/field/poi allow inspection of each local descriptor panel.

### Confirmed integration repair

FullMapInteractionMatrix reproduced POI selection failure at1280x720/UI1.2: aggregate LOD hit-test consumed the click on a visible POI marker and reframed the map before descriptor selection. Add src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs to scope: an enabled POI marker, like an explicit real target, prevents aggregate expansion at its hit point. Keep field-area priority below real objects/aggregates. Also repair the test-only throwing connection lifetime (an immediately ended snapshot stream correctly fails the session; hold it until cancellation).

The same regression next isolated a second interruption: cycling through an overlapping player marker enabled Follow and moved the camera before reaching POI. Add src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs: automatic Follow on player click applies only to a unique candidate; overlapping candidates preserve the camera for cycling. Explicit Follow toolbar remains available. This supersedes legacy auto-follow only for overlap; regression must pass at all scales before publication.

At1920x1080/UI1.2 the regression also reproduced aggregate expansion over the Sun marker, preventing selection of its radiation area. Preserve explicit Sun/Planet markers over nearby aggregates as well; these unclustered celestial markers are already drawn individually. Fields themselves still do not blanket-block aggregates.

Native PNG review and added assertion reproduced toolbar/Object Info overlap at1280x720/UI1.5 after AI selection. Add src/DeepSpaceSaga.Client/UI/Screens/GameSession/Controls/ObjectInfoPanel.cs: optional maximum bottom reserves space for the full-map toolbar while retaining both scrolling rows. Native script closes the optional diagnostic text panel through its actual X button before acceptance, and explicitly verifies running/paused/resumed states plus selected-base info scrolling. The legacy optional diagnostic overlay can crowd bottom controls at small logical sizes; closing it is an existing user path, not a hidden frame optimization. Earlier native measurements remain failed intermediate evidence; final matrix reruns after repair.

The strengthened native1280/UI1.5 case reproduced an unreachable diagnostic X: toolbar hit-test consumed it. Repair in already allowed GameSessionScreen.cs: paint the diagnostic close control last and hit-test it first among left-click UI controls. This keeps the existing optional overlay dismissible without changing its data. Repeat the affected native case after the fix; other seven successful final interaction cases closed the panel before measured frames and do not execute this changed branch thereafter. All FPS failures remain recorded.

## Execution and self-review — 2026-10-08

Implementation and required measurements complete; ticket acceptance remains OPEN because the 80 FPS goal was measured and failed. Final scripted native interaction 8/8 PASS across min/max, system/belt/cluster/base/field/POI, UI1/1.2/1.5, 1280x720/1920x1080. Each case checks actual layer buttons, real/descriptor selection, pause/resume, diagnostic close and info scrolling. All eight final PNGs inspected. Seven passing cases precede the final close-control repair; the affected eighth case was repeated afterwards. Earlier failures and individual module IDs retained in [native evidence](../../evidence/us8-native-summary.json).

Final Release Client1758/1758 PASS (1m21s), Engine1803/1803, Contracts173/173, Motion141/141, tooling9/9 PASS. Client and tooling builds: zero warnings/errors; scoped format and diff check PASS. Named FullMapRenderEvidenceTests cover render with a throwing session, generated all-scale mouse interaction and live report context. Review confirmed no synchronous Engine call from Render, no decorative entity multiplication, no gameplay effects.

Native collector: 120 warmup +600 measured swap-completion intervals per case, VSync enabled, 100Hz Intel Arc140V, OpenGL3.3 driver32.0.101.8860. Final p99=50.5839–54.4007ms (target<=12.5ms); all eight FPS verdicts FAILED. Fresh no-new-layers baseline also fails (p9953.2056ms); empty OpenGL VSync control p9945.7321ms. Empty no-VSync control reduces swap wait below1ms but is not gameplay evidence; temporary1ms process-timer diagnostic failed and was removed. No exact driver/OS root cause proven. GPU execution, physical scanout and human playthrough NOT RUN/unmeasured; do not substitute raster for them.

Actual reproduction: `dotnet tools/DeepSpaceSaga.Performance/bin/Release/net8.0/DeepSpaceSaga.Performance.dll <absolute-root> <absolute-output.json> --solar-window max base 1.5 1280x720 --clusters --all-map-layers`. Vary final case matrix as recorded in evidence. Separate one-case PlayerShipOnly --solar-map probe with absolute --client-frame-report path verified hashed native reference and retained failed verdict. Temporary native JSON/PNG removed after durable summaries/hashes and visual review, as required.

Remaining work: diagnose presentation pacing on the target environment and obtain native p99<=12.5ms before performance acceptance. This is measured failure, not an unavailable native run. US9 documentation may proceed independently under the user's execution prompt; it must preserve this open gate. Self-review is not independent approval.
