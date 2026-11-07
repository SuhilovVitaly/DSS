---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0003-preserved-starts-and-resources
ticket: EP-0018-US-0003-TK-0003-voyage-geography
title: "Торговые рейсы используют новые расстояния и текущий двигатель"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0003-TK-0002-scenario-start-bindings, EP-0018-US-0004-TK-0002-legacy-trade-map-validation]
files_touched: 3
serves: [AC-03, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Торговые рейсы используют новые расстояния и текущий двигатель

## Why
Перенести окружение относительно владельцев, сохранить смысл Default/Docked/Undocked и согласовать торговые путешествия с новой географией.
Поставляемый результат: торговые рейсы используют новые расстояния и текущий двигатель. Тикет покрывает AC-03, AC-04 [истории](../EP-0018-US-0003-preserved-starts-and-resources.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.ClusterVoyages.cs | BuildClusterVoyageMap/CurrentVoyageMap | Устранить legacy hour-limit assumptions для v2 route projection без изменения ledger semantics. |
| tests/DeepSpaceSaga.Engine.Tests/ClusterVoyageIntegrationTests.cs | Существующий voyage integration suite | Добавить v2 local/intercluster return и engine-change evaluations. |
| tests/DeepSpaceSaga.Engine.Tests/VoyageSaveLoadContinuityTests.cs | Существующая continuity suite | Проверить active v2 voyage resume и exactly-once fuel/receipts. |

## Public API after the change
No API change; CurrentVoyageMap остаётся projection, не второй сохранённой экономикой.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Пересчитывать геометрию по текущим орбитальным poses и ETA по текущему operational engine; frozen generation reference только для min rule.
2. Сохранить valid risk/fuel multipliers и cargo flows при новых station positions; старые generation-hour caps не запрещают v2 поездку.
3. Проверить один местный рейс и межкластерный возврат, топливные ограничения и неизменность accounting; невозможный рейс даёт штатный отказ, не hidden refill.
4. Доказать save/load continuation без повторного reserve/spend/settlement; рынки не становятся удалённо исполняемыми из map UI.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- ClusterVoyageIntegrationTests.V2LocalAndInterclusterReturnUseCurrentGeometry
- ClusterVoyageIntegrationTests.V2EngineUpgradeChangesEtaNotWorld
- VoyageSaveLoadContinuityTests.V2ActiveVoyageHasExactlyOnceFuelSettlement
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0003-TK-0003-voyage-geography"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~ClusterVoyageIntegrationTests.V2LocalAndInterclusterReturnUseCurrentGeometry|FullyQualifiedName~ClusterVoyageIntegrationTests.V2EngineUpgradeChangesEtaNotWorld|FullyQualifiedName~VoyageSaveLoadContinuityTests.V2ActiveVoyageHasExactlyOnceFuelSettlement" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.ClusterVoyages.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterVoyageIntegrationTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/VoyageSaveLoadContinuityTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-03, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
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
2026-11-07: v2 voyage projection uses only canonical cluster links, excluding legacy-only far local edges; v1 union unchanged. Actual local/intercluster buy-flight-sell-return passes; current engine2x halves ETA without changing stored geography, active mid-flight JSON resume produces identical finance/fuel state and duplicate terminal settlement has no effect. Before: two tests detect two legacy-only edges; after focused36/36 PASS, Release build/format/diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-voyages/{before.trx,focused.trx,build.log,format.log}. Physical/human not run; self-review complete.
