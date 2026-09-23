---
epic: EP-0002-procedural-solar-system
story: EP-0002-US-0003-playable-asteroid-belts
ticket: EP-0002-US-0003-TK-0001-seeded-belt-asteroids
title: Детерминированные игровые астероиды в поясах
stage: approved
layer: engine
depends_on: [EP-0002-US-0002-TK-0001-absolute-orbit-math, EP-0002-US-0002-TK-0002-authoritative-orbit-runtime, EP-0002-US-0002-TK-0003-orbital-client-prediction]
files_touched: 3
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Детерминированные игровые астероиды в поясах

## Why

Наполненные астероидные пояса: детерминированные игровые астероиды в поясах даёт проверяемый шаг к результату истории. End state: все игровые астероиды внутри своего annulus и совпадают после повторения.; увеличение samples 2048→8192 не меняет authoritative objects/RNG.; count/IDs/linear motion не заменены постоянными астероидами.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: При обзоре системы видны полные границы 2–5 поясов, при приближении — неоднородность и выбираемые игровые астероиды; запас границы системы 10% учитывает всю протяжённость поясов.
- AC-0002: Одинаковые условия воспроизводят наполнение; положение игровых астероидов согласовано при паузе и ускорении.
- AC-0003: Повышение визуальной плотности не требует соответствующего роста количества авторитетных объектов; временные малые астероиды сохраняют своё назначение.

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
| src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs | Новый файл; владелец EP-0002-US-0001-TK-0003-seeded-world-bootstrap. В исходном дереве отсутствует. | Seeded detached world geometry, IDs, конечные retries и bounded stages генерации. |
| src/DeepSpaceSaga.Engine/SimulationEngine.SolarSystem.cs | Новый файл; владелец EP-0002-US-0001-TK-0003-seeded-world-bootstrap. В исходном дереве отсутствует. | Resolved map state и projection/persistence hooks, перечисленные в шагах. |
| tests/DeepSpaceSaga.Engine.Tests/SeededBeltAsteroidsTests.cs | Новый файл; владелец EP-0002-US-0003-TK-0001-seeded-belt-asteroids. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **engine**. Matching test project: `D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`.

## Public API after the change

No API change. Дополняются SpaceObjects и SolarSystemMap.Orbits; плотность задаётся уже объявленными AsteroidsPerBelt/DecorationSamplesPerBelt.

## Implementation steps

1. Добавить отдельный stage belts после каркаса: ровно asteroidsPerBelt Permanent Asteroid на пояс, ID SYS-AST-{belt}-{ordinal}, известные тип/позиция; использовать действующие compositionType и массовые ограничения, не создавать новый товар.
2. Размещать локальные группы с пустыми секторами, circular orbit внутри annulus, collision clearance и конечные attempts. Отдельный decoration seed не влияет на world RNG; количество decorative samples не меняет список объектов.
3. Проверить повторение и проигрывание орбит; сценарные Temporary астероиды сохраняют IDs, движение и count. Расширение SystemRadius учитывает внешнюю геометрию пояса.
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

Test class: `SeededBeltAsteroidsTests`. Тестовые сценарии и ожидаемые результаты:

- `SeededBeltAsteroidsTests.BeltSeedAndOrbitContainment` — все игровые астероиды внутри своего annulus и совпадают после повторения.
- `SeededBeltAsteroidsTests.DecorationDoesNotCreateEntities` — увеличение samples 2048→8192 не меняет authoritative objects/RNG.
- `SeededBeltAsteroidsTests.TemporaryScenarioAsteroidsPreserved` — count/IDs/linear motion не заменены постоянными астероидами.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `SeededBeltAsteroidsTests.BeltSeedAndOrbitContainment`, `SeededBeltAsteroidsTests.DecorationDoesNotCreateEntities`, `SeededBeltAsteroidsTests.TemporaryScenarioAsteroidsPreserved` |
| AC-0002 | `SeededBeltAsteroidsTests.BeltSeedAndOrbitContainment`, `SeededBeltAsteroidsTests.DecorationDoesNotCreateEntities`, `SeededBeltAsteroidsTests.TemporaryScenarioAsteroidsPreserved` |
| AC-0003 | `SeededBeltAsteroidsTests.BeltSeedAndOrbitContainment`, `SeededBeltAsteroidsTests.DecorationDoesNotCreateEntities`, `SeededBeltAsteroidsTests.TemporaryScenarioAsteroidsPreserved` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore --filter "FullyQualifiedName~SeededBeltAsteroidsTests"
dotnet test "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.SolarSystem.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/SeededBeltAsteroidsTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.SolarSystem.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/SeededBeltAsteroidsTests.cs"
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
