---
epic: EP-0002-procedural-solar-system
story: EP-0002-US-0008-system-generation-evidence
ticket: EP-0002-US-0008-TK-0002-system-performance-report
title: Измерительный отчёт генерации, снимков и raster кадров
stage: approved
layer: tooling
depends_on: [EP-0002-US-0007-TK-0001-generated-world-persistence, EP-0002-US-0007-TK-0002-local-world-save-roundtrip, EP-0002-US-0008-TK-0001-system-correctness-corpus]
files_touched: 4
serves: [AC-0001, AC-0002, AC-0003]
created: 2026-09-22T14:40:41Z
revision: 1
---

# Измерительный отчёт генерации, снимков и raster кадров

## Why

Проверяемая устойчивость генерации системы: измерительный отчёт генерации, снимков и raster кадров даёт проверяемый шаг к результату истории. End state: fixture CLI output содержит все метрики/параметры.; неуспех не выдаётся за пустой успешный отчёт.

Served story criteria (вклад этого тикета в полный результат):
- AC-0001: Доступен отчёт по корпусу seed и сценариев с версиями, настройками, результатами проверок и воспроизводимыми причинами отказов.
- AC-0002: На минимальной и максимальной конфигурации измерены генерация, размер и время snapshot/save, кадры и взаимодействие; указаны оборудование и условия цели 80 FPS.
- AC-0003: Отдельно проверены ошибочные конфигурации и конечность неуспешной генерации; сравнения ускорения сделаны только со свежим сопоставимым baseline.

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

Пути относительно D:/DeepSpaceSaga/DSS. Ровно 4 implementation files, включая tests/project/config. Других разрешённых файлов нет.

| File | Current state | Allowed change |
|---|---|---|
| tools/DeepSpaceSaga.Performance/Program.cs | tools/DeepSpaceSaga.Performance/Program.cs:1 — using System.Diagnostics; | Указанный opt-in CLI/report режим; существующие режимы и их semantics не менять. |
| tools/DeepSpaceSaga.Performance/SolarMapEvidence.cs | Новый файл; владелец EP-0002-US-0008-TK-0002-system-performance-report. В исходном дереве отсутствует. | Runner/схема evidence/метрики и verdict только текущего map режима. |
| tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj | Новый файл; владелец EP-0002-US-0008-TK-0002-system-performance-report. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |
| tools/DeepSpaceSaga.Performance.Tests/SystemPerformanceReportTests.cs | Новый файл; владелец EP-0002-US-0008-TK-0002-system-performance-report. В исходном дереве отсутствует. | Именованные проверки этого тикета; если csproj — только указанный reference/test setup |

Production layer: **tooling**. Matching test project: `D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj`.

## Public API after the change

CLI: dotnet run -c Release --project tools/DeepSpaceSaga.Performance -- <DSS-root> <output.json> --solar-map --seeds 1:100 --scenarios all --config max. Report schemaVersion=1; status passed/failed/not-measured; измерения имеют backend, machine, commit и параметры.

## Implementation steps

1. Добавить --solar-map режим существующего performance harness, не заменять прежние режимы. Принимать --seeds 1:100 --scenarios all --config min|max, писать output.json с hardware/runtime/git revision/seed/config/versions/counts и timings generation/snapshot/save bytes+ms, frame p50/p95/p99 и allocations.
2. Использовать production bootstrap, 120 warmup и 600 measured frames; отдельно CPU/raster и GPU presentation. raster <=12.5ms не объявлять доказательством GPU 80 FPS. Любое сравнение требует отдельного свежего baseline.
3. Создать отдельный tooling test project net8.0/xUnit по центральным пакетам, reference Performance; тестировать JSON schema и CLI failure exit code 1 через process. Производственные timing thresholds не выдумывать.
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

Test class: `SystemPerformanceReportTests`. Тестовые сценарии и ожидаемые результаты:

- `SystemPerformanceReportTests.ReportHasReproductionAndBackend` — fixture CLI output содержит все метрики/параметры.
- `SystemPerformanceReportTests.InvalidConfigReturnsFailure` — неуспех не выдаётся за пустой успешный отчёт.

| Served criterion | Named tests |
|---|---|
| AC-0001 | `SystemPerformanceReportTests.ReportHasReproductionAndBackend`, `SystemPerformanceReportTests.InvalidConfigReturnsFailure` |
| AC-0002 | `SystemPerformanceReportTests.ReportHasReproductionAndBackend`, `SystemPerformanceReportTests.InvalidConfigReturnsFailure` |
| AC-0003 | `SystemPerformanceReportTests.ReportHasReproductionAndBackend`, `SystemPerformanceReportTests.InvalidConfigReturnsFailure` |

Coverage: AC-0001, AC-0002, AC-0003 проверяются этими сценариями в пределах end state тикета; остальные слои закрывают критерий своими named tests по Approved ticket map. Fixtures самостоятельные; static oracle не вычислять тем же helper, который тестируется.

Команды из любого cwd (новый tooling test project сначала restore):

```powershell
dotnet restore D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj
dotnet test "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" --no-restore --filter "FullyQualifiedName~SystemPerformanceReportTests"
dotnet test "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" --no-restore
dotnet build "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj" --no-restore
dotnet format "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/Program.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/SolarMapEvidence.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/SystemPerformanceReportTests.cs"
dotnet format "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/Program.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/SolarMapEvidence.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/SystemPerformanceReportTests.cs"
git -c safe.directory=D:/DeepSpaceSaga/DSS -C D:/DeepSpaceSaga/DSS diff --check
```

Команды приведены для implementer и в planning-задаче не выполнялись. Для baseline failure записать точное имя/сообщение и отдельно результат новых тестов. Производительность измерять лишь в перечисленных сценариях; unit tests не доказывают80FPS.

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

### EP-0002-US-0008-TK-0001-system-correctness-corpus

No API change. Тестовый corpus — фиксированная последовательность ulong 1..100; независимые expected quarter-orbit/scale anchors.
