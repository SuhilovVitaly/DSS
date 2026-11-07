---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0006-multiscale-map-readability
ticket: EP-0018-US-0006-TK-0002-orbital-layer-policy
title: "Орбиты читаемы и следуют выбору объекта"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0006-TK-0001-semantic-map-detail]
files_touched: 4
serves: [AC-03, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Орбиты читаемы и следуют выбору объекта

## Why
Единая карта раскрывает Local/Region/Cluster/System, ресурсы и орбиты не создают нечитаемые полосы.
Поставляемый результат: орбиты читаемы и следуют выбору объекта. Тикет покрывает AC-03, AC-04 [истории](../EP-0018-US-0006-multiscale-map-readability.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/SolarSystemLayerRenderer.cs | Draw рисует все map.Orbits при showOrbits | Передавать detail/selection allowlist, дедупликация и geometry clip сохранить. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | _solarSystemLayer.Draw:1172 | Передавать единый detail/selected/nav context без запроса Engine. |
| tests/DeepSpaceSaga.Client.Tests/BeltDetailRenderingTests.cs | Существующие belt LOD tests | Проверить independent decoration budget и отсутствие asteroid orbit band. |
| tests/DeepSpaceSaga.Client.Tests/MapOrbitDetailTests.cs | Новый файл | ShowOrbits/type/detail/selected/docked-parent/dedup tests. |

## Public API after the change
Internal SolarSystemLayerRenderer.Draw получает detail/selection context; все owning callers в allowlist.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Реализовать C-06 orbital visibility: planet overview, selected station/asteroid orbit при ShowOrbits; player docking parent учитывать.
2. Не рисовать thousands of identical station/resource curves; геометрически identical elements дедуплицировать без удаления объектов.
3. Сохранить bounded visible ellipse arcs и decoration cache; новую карту не смешивать со старым cache.
4. Проверить реальный selected ring projection, не только число вызовов helper.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- MapOrbitDetailTests.SmallObjectOrbitsDoNotFormOverviewBands
- MapOrbitDetailTests.SelectionAndShowOrbitsControlVisibleCurves
- BeltDetailRenderingTests.V2DecorationRemainsIndependentOfWorldObjects
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0006-TK-0002-orbital-layer-policy"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~MapOrbitDetailTests.SmallObjectOrbitsDoNotFormOverviewBands|FullyQualifiedName~MapOrbitDetailTests.SelectionAndShowOrbitsControlVisibleCurves|FullyQualifiedName~BeltDetailRenderingTests.V2DecorationRemainsIndependentOfWorldObjects" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/SolarSystemLayerRenderer.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/BeltDetailRenderingTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/MapOrbitDetailTests.cs"`

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

## Execution evidence
2026-11-07: overview planet curves only on Cluster/System; selected/nav station or asteroid and docked target/player parent add explicit curves when ShowOrbits. Geometrically identical axis pairs deduplicated independent of phase/period, without deleting objects. Existing bounded ellipse clipping and belt decoration/cache retained. Focused7/7 PASS includes actual pixel checks for selected100wu ring and absence of1000 asteroid overview band; v2 decoration cache independent of object/orbit list. Release build, scoped format-verified exit0 and diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-orbit-detail/{focused.trx,build.log,format-verified.log}. Self-review complete; native quality gate remains US8.
