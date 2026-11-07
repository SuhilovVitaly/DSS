---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0008-system-map-acceptance
ticket: EP-0018-US-0008-TK-0001-seed-geography-corpus
title: "Разбитый корпус генерации с независимым измерением расстояний"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0007-TK-0002-current-travel-estimates]
files_touched: 3
serves: [AC-01, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Разбитый корпус генерации с независимым измерением расстояний

## Why
Подтвердить результат числами и реальным клиентом: seed corpus, четыре уровня карты, сценарный путь к станции и сохранение.
Поставляемый результат: разбитый корпус генерации с независимым измерением расстояний. Тикет покрывает AC-01, AC-04 [истории](../EP-0018-US-0008-system-map-acceptance.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| tests/DeepSpaceSaga.Engine.Tests/SystemCorrectnessCorpusTests.cs | Cases +100seed loop в одном scenario boundary test | Разбить v2 cases по deterministic seed ranges и сохранять legacy coverage. |
| tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs | Существующий cluster corpus | Добавить independent min/nearest/radial inequality/reference Vmax checks. |
| tests/DeepSpaceSaga.Engine.Tests/FullMapCorrectnessCorpusTests.cs | Существующий full-map corpus | Включить v2 AI/environment/POI/persistence и explicit failure repro. |

## Public API after the change
No production API change; тестовая декомпозиция corpus, существующие fixtures не меняют runtime.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Создать public test cases со scenario/seed-range/bounds в display IDs;100seeds на scenario и boundary combinations C-07, процессы<=300000ms.
2. Вычислять distances independently через hypot, unitsPerDay из frozen descriptor; проверить каждую пару и nearest каждого station.
3. Проверить analytic lane separation и epoch1/30/365д, движки2/4км/с, start50/75; не утверждать универсальную гарантию на основании samples.
4. При FAIL вывести точный seed/scenario/config/version/stage/attempt и сохранить воспроизводимый вход в runDir; timeout=FAIL.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- SystemCorrectnessCorpusTests.V2ScenarioSeedRangeBoundaries
- ClusterCorrectnessCorpusTests.V2EveryPairAndNeighbourIndependentOracle
- FullMapCorrectnessCorpusTests.V2MaterializedWorldContinuationCorpus
Перечисленные проверки реализованы; фактический запуск и evidence указаны в разделе Execution. Исходные planning-команды ниже не являются отдельным результатом запуска.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0008-TK-0001-seed-geography-corpus"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~SystemCorrectnessCorpusTests.V2ScenarioSeedRangeBoundaries|FullyQualifiedName~ClusterCorrectnessCorpusTests.V2EveryPairAndNeighbourIndependentOracle|FullyQualifiedName~FullMapCorrectnessCorpusTests.V2MaterializedWorldContinuationCorpus" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/SystemCorrectnessCorpusTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterCorrectnessCorpusTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/FullMapCorrectnessCorpusTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Corpus design before implementation
2026-11-07: retain historical long methods as explicit gen1/width.08/old cluster ranges; production defaults now v2 must not silently retarget legacy expectations. New public v2 theories expose six scenarios and five named20-seed ranges (001–020…081–100),100 unique seeds/scenario. Seed bits0–5 cover the full64-way human boundary Cartesian product: planets3/7, belts2/5, clusters3/5, stations10/12, start50/75 and operational Vmax2/4. AI counts2/4 follow min/max cluster mode as in historical full-map matrix; this is not a claim of100seeds for every configuration or an exhaustive independent AI-count Cartesian product. Separate cluster oracle and full materialized continuation corpus reuse these cases. All-phase human proof is analytic radial separation plus rigid local circles, independently sampled at epoch/1/30/365d. Failure export includes exact scenario/seed/config/module-speed override/stage and optional materialized world under DSS_VALIDATION_RUN (absolute workspace artifacts), referenced by TRX. No generated output in repository.

2026-11-08: also split the historical100-seed methods into five20-seed display ranges per existing boundary case, retaining the complete old Cartesian matrices and100seeds/config. This makes legacy regression processes obey the same300000ms watchdog. No legacy oracle or production rule is weakened. Shared failure export records the actual generation version and module-speed context for both versions.

## Execution evidence
2026-11-08: final v2 corpus90/90 PASS: six scenarios×100unique seeds,64human boundary combinations/scenario, independent all-pair/nearest checks at epoch/1/30/365day and analytic rigid-circle/radial-lane proof; full materialized repeat/Load after current Vmax change and invalid generation config passes. Sampled human min1.1793579412496d, nearest1.1793579412496–1.45528054304949d; analytic lane lower bound minimum1.07405626966427d. Historical v1 full matrices540/540 PASS (100seeds/old configuration),30 capped processes. Engine main including short corpus facts2158/2158 PASS. Aggregate36TRX contains2788 unique passed IDs, no duplicate counting of pilot/initial runs. Maximum legacy process47.517s; final v2 TRX duration42.456s; no timeout, cap300000ms. One initial orchestration command had a placeholder collision before test launch, corrected; no production failure or weakened oracle. Release Engine build, scoped format-verified and diff PASS. Final evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261108-corpus-validation/{CorpusReport.md,Summary.json,engine-main.trx,build.log,format-verified.log}; all per-case evidence indexed by D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/{20261108-v2-corpus-verified,20261108-legacy-corpus}/Summary.json. Initial/pilot evidence retained separately; final source/DLL SHA256 captured. v1 coverage retained, not silently upgraded; AI-count coverage limitation stated above. Self-review complete; rendered/native/human gates follow.
