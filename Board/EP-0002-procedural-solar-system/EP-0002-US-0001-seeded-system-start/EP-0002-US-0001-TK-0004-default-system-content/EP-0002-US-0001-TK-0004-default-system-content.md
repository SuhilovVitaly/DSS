---
epic: EP-0002-procedural-solar-system
story: EP-0002-US-0001-seeded-system-start
ticket: EP-0002-US-0001-TK-0004-default-system-content
title: Настройки новой системы для Default
stage: approved
layer: content-data
depends_on: [EP-0002-US-0001-TK-0003-seeded-world-bootstrap]
files_touched: 3
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Настройки новой системы для Default

## Why

Новая игра в воспроизводимой Солнечной системе: настройки новой системы для default даёт проверяемый шаг к результату истории. End state: путь и все параметры проходят реальную загрузку.; двигатель/склады/модули совпадают со сценарием; изменения generator content не затрагивают цены.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Повторный запуск с одинаковыми исходными условиями совпадает по ID, типам и геометрии; другой seed даёт другую валидную систему, видимую на карте.
- AC-0002: Независимый расчёт по фактической позиции игрока и Vmax подтверждает 50–75 дней; проверены 2 и 5 поясов, допустимые типы планет, отсутствие человеческих планетных поселений и недопустимых пересечений коридоров.
- AC-0003: Невалидные параметры и исчерпание конечного лимита попыток дают воспроизводимую диагностику до публикации сессии.

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
| src/DeepSpaceSaga.Client/Data/Maps/solar-system.json | Новый файл; владелец EP-0002-US-0001-TK-0004-default-system-content. В исходном дереве отсутствует. | Только конфигурация/тексты, перечисленные в Implementation steps |
| src/DeepSpaceSaga.Client/Settings.json | src/DeepSpaceSaga.Client/Settings.json:2 — "typeData": { | Только конфигурация/тексты, перечисленные в Implementation steps |
| tests/DeepSpaceSaga.Client.Tests/DefaultSystemContentTests.cs | Новый файл; владелец EP-0002-US-0001-TK-0004-default-system-content. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **content-data**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`.

## Public API after the change

No API change. JSON строго соответствует SolarSystemGenerationConfig предыдущих тикетов.

## Implementation steps

1. Подключить typeData.solarSystem="Data/Maps/solar-system.json"; enabledScenarios=["Default"]. schemaVersion/generatorVersion=1; maxPlacementAttempts=64; min/maxPlanets=3/7; min/maxBelts=2/5; startMin/MaxDays=50/75; orbitSpeedFraction=0.001; beltWidthFraction=0.08; orbitClearanceWorld=100; asteroidsPerBelt=24; decorationSamplesPerBelt=2048.
2. Числа периода/плотности — безопасные стартовые настройки, не новые требования баланса. Загружать штатным EngineContentLoader через доступный LocalClient factory в Client tests, проверять существование скопированного content; не редактировать сценарный inventory или maxSpeed.
3. Добавить именованные тесты ниже, привязать результаты к served criteria и записать фактические команды/результат в review evidence этого тикета. Нельзя помечать passed ещё не выполненный прогон.

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

Test class: `DefaultSystemContentTests`. Тестовые сценарии и ожидаемые результаты:

- `DefaultSystemContentTests.DefaultContentGeneratesSystem` — путь и все параметры проходят реальную загрузку.
- `DefaultSystemContentTests.ContentKeepsShipAndInventory` — двигатель/склады/модули совпадают со сценарием; изменения generator content не затрагивают цены.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `DefaultSystemContentTests.DefaultContentGeneratesSystem`, `DefaultSystemContentTests.ContentKeepsShipAndInventory` |
| AC-0002 | `DefaultSystemContentTests.DefaultContentGeneratesSystem`, `DefaultSystemContentTests.ContentKeepsShipAndInventory` |
| AC-0003 | `DefaultSystemContentTests.DefaultContentGeneratesSystem`, `DefaultSystemContentTests.ContentKeepsShipAndInventory` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore --filter "FullyQualifiedName~DefaultSystemContentTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DefaultSystemContentTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DefaultSystemContentTests.cs"
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

### EP-0002-US-0001-TK-0002-generation-input-schema

SolarSystemGenerationConfig(int SchemaVersion,int GeneratorVersion,int MaxPlacementAttempts,int MinPlanets,int MaxPlanets,int MinBelts,int MaxBelts,double StartMinDays,double StartMaxDays,double OrbitSpeedFraction,double BeltWidthFraction,double OrbitClearanceWorld,int AsteroidsPerBelt,int DecorationSamplesPerBelt,IReadOnlyList<string> EnabledScenarios). GameStateData: SolarSystemMapSnapshot? SolarSystem=null; SpaceObjectData: OrbitalElements? Orbit=null. EngineContentLoader читает typeData.solarSystem; JSON schemaVersion=1, generatorVersion=1; неизвестная версия/поле запрещены. Min/MaxPlanets 3..7, Min/MaxBelts 2..5 в поставляемом режиме; числа объектов конечны и неотрицательны, MaxPlacementAttempts>0, StartMinDays=50, StartMaxDays=75, 0<OrbitSpeedFraction<1, 0<BeltWidthFraction<1. SpaceObjectData также получает optional double WorldOffsetX=0, WorldOffsetY=0 с JsonPropertyName worldOffsetX/worldOffsetY; значения finite. solarSystem/orbit JSON использует те же explicit camelCase DTO. Генерационная конфигурация передаётся отдельным reader LoadSolarSystemGenerationConfig(string settingsPath), без изменения session factory API.

### EP-0002-US-0001-TK-0003-seeded-world-bootstrap

internal static ScenarioFile SolarSystemGenerator.Generate(ScenarioFile source,SolarSystemGenerationConfig config,GameDataRegistry registry,ulong masterSeed);  public void SimulationEngine.LoadScenario(ScenarioFile scenario,bool isSave=false,SolarSystemGenerationConfig? generation=null). EngineContentLoader New Game entrypoints читают config и передают generation; Save entrypoint никогда его не передаёт. После seed resolution LoadScenario вызывает detached Generate до ResolveMarketProfiles/runtime construction и commit. worldPart хранит _solarSystem и строит immutable projection.
