---
epic: EP-0002-procedural-solar-system
story: EP-0002-US-0002-coherent-orbital-world
ticket: EP-0002-US-0002-TK-0001-absolute-orbit-math
title: Общая аналитическая поза и скорость орбиты
stage: approved
layer: motion
depends_on: [EP-0002-US-0001-TK-0001-system-map-contract, EP-0002-US-0001-TK-0002-generation-input-schema, EP-0002-US-0001-TK-0003-seeded-world-bootstrap, EP-0002-US-0001-TK-0004-default-system-content, EP-0002-US-0001-TK-0005-initial-system-view]
files_touched: 3
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Общая аналитическая поза и скорость орбиты

## Why

Согласованное движение орбитального мира: общая аналитическая поза и скорость орбиты даёт проверяемый шаг к результату истории. End state: положения/касательные по эталонным четвертям, оба направления.; одно и дробное продвижение; ненулевой sub-degree offset сохраняет компактные расстояния.; null Orbit идёт существующим путём без множителя 300.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: На одном мире сопоставлены авторитетные и отображаемые положения и скорости при Speed0–4, дробном продвижении и календарном переходе; Солнце остаётся в центре.
- AC-0002: Пристыкованный корабль сохраняет относительное положение, свободный корабль не получает множитель 300; одинаковая конечная эпоха даёт одинаковые орбитальные положения.
- AC-0003: Проверка компактных фазовых смещений подтверждает отсутствие округления, разрушающего заданные малые расстояния на большом радиусе.

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
- Точная фаза задаётся integer InitialPhase+double PhaseOffsetDegrees; период остаётся календарным, evaluator использует physicalPeriod=OrbitalPeriodMs/300. Legacy mode сохраняется.
- Public API after the change является точным плановым контрактом этого тикета. Статус approved не означает, что этот API уже существует.

## Code context

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 3 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Motion/OrbitalMotionMath.cs | Новый файл; владелец EP-0002-US-0002-TK-0001-absolute-orbit-math. В исходном дереве отсутствует. | Общая pure функция орбитальной позы/скорости с объявленным временем и допусками. |
| src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs | src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs:35 — public ObjectMotionSnapshot Predict(ObjectMotionSnapshot state, long elapsedMs) | Orbit branch и запрет linear fast path для Orbit; сигнатура IMotionPredictor неизменна. |
| tests/DeepSpaceSaga.Motion.Tests/AbsoluteOrbitMathTests.cs | Новый файл; владелец EP-0002-US-0002-TK-0001-absolute-orbit-math. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **motion**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj`.

## Public API after the change

public static ObjectMotionSnapshot OrbitalMotionMath.At(ObjectMotionSnapshot state,OrbitalElements orbit,long simulationTimeMs); IMotionPredictor.Predict signature unchanged. Position tolerance 1e-6 world, speed 1e-9 km/s; использовать абсолютную/относительную погрешность 1e-12 при больших координатах.

## Implementation steps

1. Реализовать pure At по абсолютному simulationTime: physicalPeriod=orbitalPeriodMs/300. theta=(initialPhase+offset)*pi/180 + sign*2*pi*((t-epoch) mod physicalPeriod)/physicalPeriod; x=a*sin(theta), y=-b*cos(theta). Производная в world/s даёт speedKmS=hypot(vx,vy)/10, heading=atan2(vx,-vy) в [0,360). Не округлять позу/скорость.
2. Сначала проверять Orbit на ObjectMotionSnapshot; Predict использует OrbitSampleSimulationTimeMs+elapsedMs и обновляет sample timestamp. IsLinear обязан вернуть false для Orbit. Approach ship branch и legacy snapshots без Orbit сохраняются.
3. Проверить quarter period, clockwise/counterclockwise, разные абсолютные эпохи, многократный период и маленький offset на 720000 world; reject invalid finite values. Разбиение dt не должно накапливать фазу.
4. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

## Out of scope

Торговые кластеры, AI/боевые эффекты, изменение скорости/оптимальности Approach, новая экономическая формула. Нельзя изменять файлы вне Code context, requirements/состав эпика, обходить dependency недостающим вторым API или менять не связанный пользовательский diff.

## Invariants

- `src/DeepSpaceSaga.Contracts/SimulationSpeed.cs:27`: календарный коэффициент300; движение и календарь не взаимозаменяемы.
- `src/DeepSpaceSaga.Contracts/ObjectMotionSnapshot.cs:4`: world units100m, speed km/s, clockwise0up. Отображение не изменяет мир.
- `Documentation/00-Process/CLAUDE.md:34`: render loop не запрашивает Engine; Contracts без graphics, Motion общий.
- `Documentation/01-Requirements/EngineRequirements.md:5335`: Approach с постоянной скоростью; новый map scope не даёт ускорение кораблю.
- Один layer, максимум5 файлов, новые DTO immutable/JSON-safe. Результат ошибки не публикует partial state.
- Load и New Game различаются: src/DeepSpaceSaga.Engine/Content/EngineContentLoader.cs:29 (CreateEngineFromSaveFile).

## Tests

Test class: `AbsoluteOrbitMathTests`. Тестовые сценарии и ожидаемые результаты:

- `AbsoluteOrbitMathTests.QuarterOrbitMatchesIndependentDerivative` — положения/касательные по эталонным четвертям, оба направления.
- `AbsoluteOrbitMathTests.PartitionAndLargeEpochInvariant` — одно и дробное продвижение; ненулевой sub-degree offset сохраняет компактные расстояния.
- `AbsoluteOrbitMathTests.LinearAndApproachRegression` — null Orbit идёт существующим путём без множителя 300.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `AbsoluteOrbitMathTests.QuarterOrbitMatchesIndependentDerivative`, `AbsoluteOrbitMathTests.PartitionAndLargeEpochInvariant`, `AbsoluteOrbitMathTests.LinearAndApproachRegression` |
| AC-0002 | `AbsoluteOrbitMathTests.QuarterOrbitMatchesIndependentDerivative`, `AbsoluteOrbitMathTests.PartitionAndLargeEpochInvariant`, `AbsoluteOrbitMathTests.LinearAndApproachRegression` |
| AC-0003 | `AbsoluteOrbitMathTests.QuarterOrbitMatchesIndependentDerivative`, `AbsoluteOrbitMathTests.PartitionAndLargeEpochInvariant`, `AbsoluteOrbitMathTests.LinearAndApproachRegression` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj" --no-restore --filter "FullyQualifiedName~AbsoluteOrbitMathTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Motion/DeepSpaceSaga.Motion.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Motion/OrbitalMotionMath.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Motion.Tests/AbsoluteOrbitMathTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Motion.Tests/DeepSpaceSaga.Motion.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Motion/OrbitalMotionMath.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Motion/LinearMotionPredictor.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Motion.Tests/AbsoluteOrbitMathTests.cs"
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
