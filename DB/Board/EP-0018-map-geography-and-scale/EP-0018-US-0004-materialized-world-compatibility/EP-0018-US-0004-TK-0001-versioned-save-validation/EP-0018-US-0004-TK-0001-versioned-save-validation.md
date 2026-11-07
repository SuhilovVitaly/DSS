---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0004-materialized-world-compatibility
ticket: EP-0018-US-0004-TK-0001-versioned-save-validation
title: "Строгая версия географии без миграции координат старых миров"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0003-TK-0002-scenario-start-bindings]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Строгая версия географии без миграции координат старых миров

## Why
Включить новую географию только после доказанной materialized persistence, сохранив старые миры и строгую проверку ссылок.
Поставляемый результат: строгая версия географии без миграции координат старых миров. Тикет покрывает AC-01, AC-02, AC-03 [истории](../EP-0018-US-0004-materialized-world-compatibility.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/StationClusterSaveValidation.cs | Validate требует map.RulesVersion==1 и совпадение rigid periods | Version dispatch1/2, descriptor validation, analytic min/nearest дляv2. |
| src/DeepSpaceSaga.Engine/Scenario/SolarSystemGeneration.cs | ValidateWorld world version gate из US-0001 | Проверить coherent solar/cluster version pair и materialized invariants. |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs | ValidateAndNormalize вызывает SolarSystemGeneration.ValidateWorld | Preserve optional Geography и strict ingress без generation. |
| tests/DeepSpaceSaga.Engine.Tests/ClusterSaveStateTests.cs | Существующие save state tests | Добавить v1/v2/version mismatch/tamper fixtures. |

## Public API after the change
Wire descriptor C-02; Save20 writer baseline сохраняется, новые public loading entrypoints не нужны.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Оставить v1 семантику загрузки прежней, v2 требует nonnull finite Geography и geometry checksC-02.
2. Проверять соответствие generator2/rules2, radius/period/epoch, all human membership и radial envelope; min не считать по текущему улучшенному двигателю.
3. Отклонять tampered reference/min/crosslinks/missing member/unknown version; не допускать downgrade v2 доv1 обходом поля.
4. Сохранить source file bytes и running session при ошибке.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- ClusterSaveStateTests.V1WorldKeepsHistoricalGeometry
- ClusterSaveStateTests.V2MaterializedRulesAreValidated
- ClusterSaveStateTests.UnknownOrMixedVersionDoesNotReplaceSession
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0004-TK-0001-versioned-save-validation"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~ClusterSaveStateTests.V1WorldKeepsHistoricalGeometry|FullyQualifiedName~ClusterSaveStateTests.V2MaterializedRulesAreValidated|FullyQualifiedName~ClusterSaveStateTests.UnknownOrMixedVersionDoesNotReplaceSession" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationClusterSaveValidation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGeneration.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/ScenarioLoader.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterSaveStateTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-02, AC-03 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Исправление реального dependency order — 2026-11-07

Сохранение активногоv2рейса изUS0003TK0003 требует strictfileingress и materialized
trading validation US0004TK0001/0002. Поэтому эти2тикета выполняются послеUS0003TK0002,
затем voyageTK0003, затем local-file-roundtrip/activation. Story4 больше не требует
завершения всейStory3 перед началом; точные частичные gates перечислены в карточках.
Это устраняет логическую зависимость тестов от ещё не реализованногоloader без
переноса FAIL вPASS или объединения тикетов. 9историй/29тикетов остаются прежними.
## Execution evidence
2026-11-07: strict serialized ingress and runtime completeness guard; coherent versions without changing v1 geometry. Before: missing-cluster accepted (1 FAIL/10 PASS). After: focused 37/37 PASS, Release build and format verify exit0; git diff --check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-save-versions/{before.trx,focused.trx,build.log,format.log}. Roundtrip uses saved geometry despite invalid generation config. Full Default legacy trading map gate remains TK0002; physical input/manual acceptance not run.
