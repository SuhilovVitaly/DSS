---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0008-system-map-acceptance
ticket: EP-0018-US-0008-TK-0002-rendered-map-evidence
title: "Измеримый отчёт четырёх масштабов и производительности"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: tooling
test_project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj
depends_on: [EP-0018-US-0008-TK-0001-seed-geography-corpus]
files_touched: 4
serves: [AC-01, AC-02, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Измеримый отчёт четырёх масштабов и производительности

## Why
Подтвердить результат числами и реальным клиентом: seed corpus, четыре уровня карты, сценарный путь к станции и сохранение.
Поставляемый результат: измеримый отчёт четырёх масштабов и производительности. Тикет покрывает AC-01, AC-02, AC-04 [истории](../EP-0018-US-0008-system-map-acceptance.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| tools/DeepSpaceSaga.Performance/SolarMapEvidence.cs | Run(root,output,...config/seeds/clusters/all-map-layers) | Добавить version/scale-level/viewport/UI matrix report и camera-based validation. |
| tools/DeepSpaceSaga.Performance/Program.cs | Существующий command dispatcher | Добавить v2 map-acceptance режим и строгие flags/output paths. |
| tools/DeepSpaceSaga.Performance.Tests/SystemPerformanceReportTests.cs | Существующие report tests | Проверить schema/required measurements/unknown settings failure. |
| tools/DeepSpaceSaga.Performance.Tests/V2MapAcceptanceReportTests.cs | Новый файл | Проверить actual report validity, screenshot paths, reproducibility. |

## Public API after the change
Planned tooling flag --map-v2-acceptance; report schema содержит generationVersion, camera, detailLevel, geometry, evidence, machine, status.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
Execution scope clarification (2026-11-08): add `tools/DeepSpaceSaga.Performance/V2MapAcceptance.cs` as a separate implementation file (five files total), keeping the legacy report separate. The historical `--solar-map` explicitly uses generation v1 and its original .08 belt/cluster ranges; the new command uses shipped v2 settings. Unexposed solar/cluster retry counters are reported unavailable, never invented; configured limits and actual AI placement attempts are recorded. CPU raster evidence, native presentation, baseline comparison and human acceptance retain separate statuses.

Acceptance defect discovered: GameSessionScreen silently maps requested UI scale 2 to 1, contrary to C-07. Scope expands to `src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs` and `tests/DeepSpaceSaga.Client.Tests/GameSessionUiScaleTests.cs` to accept and exercise 2.0; no unrelated settings redesign. The report must assert actual scale equals requested scale. The interrupted first matrix is excluded from acceptance.

1. Добавить явный --map-v2-acceptance в существующий solar evidence dispatcher; exact invocation записать в report. Не использовать UI screenshots как единственный oracle.
2. Fixed seeds1/42/100 и сценарииC-07: render Local/player, selected station, cluster, system; viewport/UI matrix и units/scale/levels в manifest.
3. Включить all-human min/nearest, version/reference Vmax, generation retries/time, render allocation/time, machine/runtime/config hash и HEAD.
4. FPS80/p95 target12.5ms показывать отдельным измерением; raster timings не выдавать за native presented FPS. Baseline unavailable=>OPEN, а не invented regression pass.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- V2MapAcceptanceReportTests.ReportBindsCameraGeometryAndImagesToSeed
- V2MapAcceptanceReportTests.MissingEvidenceCannotPass
- SystemPerformanceReportTests.V2ReportKeepsNativeAndRasterMetricsSeparate
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0008-TK-0002-rendered-map-evidence"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" -c Release --filter "FullyQualifiedName~V2MapAcceptanceReportTests.ReportBindsCameraGeometryAndImagesToSeed|FullyQualifiedName~V2MapAcceptanceReportTests.MissingEvidenceCannotPass|FullyQualifiedName~SystemPerformanceReportTests.V2ReportKeepsNativeAndRasterMetricsSeparate" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/SolarMapEvidence.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/Program.cs"`

`dotnet format "tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/SystemPerformanceReportTests.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/V2MapAcceptanceReportTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-02, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Execution — 2026-11-08

Implemented strict `--map-v2-acceptance` and `--validate-map-v2-report`. Real shipped v2 generation, production GameSessionScreen raster rendering, actual UI scale, archived snapshots, camera/detail states and SHA256-bound PNG evidence. Unknown flags/settings fail. UI2 fallback defect reproduced and fixed by extending actual screen acceptance; UI hit test 17/17. Legacy --solar-map remains explicitly v1.

Evidence root `D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/`:
- `20261108-raster-verified/RasterReport.md`, `MatrixSummary.json`, six scenario JSON and 864 PNG/18 world snapshots: all fixed seeds, windows including32:9, UI1/1.5/2, four requested actions. Each case5 warmup/30 measured frames. Six reports revalidated by final validator.
- `20261108-raster-tool/verified.trx`:6/6 report/false-pass tests; `ui-scale.trx`:17/17. Build and scoped format logs there. Client full regression recorded separately after completion, not inferred from focused tests.
- Initial `20261108-raster-matrix` excluded (UI2 fallback), corrected `20261108-raster-pilot` retained as diagnostic. No timeouts accepted.

Minimum observed human spacing1.1829439681468572days; max generation215.7554ms; worst raster p95 row24.8952ms. Raster timings do not assert80 nativeFPS. Baseline OPEN; physical/human acceptance NOT RUN. Actual AI attempt diagnostics/configured solar limit retained; unexposed solar/cluster retry counts unavailable.

OPEN readability finding:24 cluster-fit cases (mostly1280x720 at high UI scale) fit members at System detail because free viewport is small. Exact cases/images in MatrixSummary. This ticket completes measurable evidence tooling, not human/product acceptance; AC-02 evidence present, semantic usability remains OPEN. Self-review only; no independent approval claimed. No production Engine calls added to render loop.

Final full Client regression:2117/2117 (20261108-raster-tool/client-full.trx), no skips/failures. Release build and scoped format PASS; git diff --check PASS.
