---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0007-map-navigation-and-estimates
ticket: EP-0018-US-0007-TK-0001-cluster-focus-navigation
title: "Навигация по доменным кластерам и локальное приближение цели"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0006-TK-0004-smooth-detail-transitions]
files_touched: 4
serves: [AC-01, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Навигация по доменным кластерам и локальное приближение цели

## Why
Сохранить удобную навигацию между уровнями и показывать честные текущие оценки после увеличения расстояний.
Поставляемый результат: навигация по доменным кластерам и локальное приближение цели. Тикет покрывает AC-01, AC-04 [истории](../EP-0018-US-0007-map-navigation-and-estimates.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | Focus/fit selection helpers и cluster click cycles | Domain cluster focus, explicit station local focus и возврат к player. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs | Bounds по members/current poses | Передать camera target bounds без fake object identity. |
| tests/DeepSpaceSaga.Client.Tests/SystemMapNavigationTests.cs | Существующий fit/resize suite | Проверить System->cluster->station->player path. |
| tests/DeepSpaceSaga.Client.Tests/PoiMapSelectionTests.cs | Существующий POI selection suite | Регрессия выбора POI/ресурсов после domain aggregation. |

## Public API after the change
No public gameplay API change; explicit internal camera focus actions.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Сделать click domain cluster marker явным fit-to-members с padding; station focus использует local scale boundsC-05.
2. Не отправлять gameplay command от одного camera fit и не заменять текущую nav target выбранным marker автоматически.
3. FitSystem включает весь belt/planet extents; FitCluster текущие presented member poses; player focus возвращает attached behavior.
4. Сохранить existing POI/resource selection semantics через LOD transitions и no-aggregate важные цели.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- SystemMapNavigationTests.SystemClusterStationPlayerNavigationPreservesTarget
- SystemMapNavigationTests.ClusterFitUsesPresentedMembersAndWholeSystemBounds
- PoiMapSelectionTests.DetailTransitionsPreservePoiAndResourceSelection
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0007-TK-0001-cluster-focus-navigation"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~SystemMapNavigationTests.SystemClusterStationPlayerNavigationPreservesTarget|FullyQualifiedName~SystemMapNavigationTests.ClusterFitUsesPresentedMembersAndWholeSystemBounds|FullyQualifiedName~PoiMapSelectionTests.DetailTransitionsPreservePoiAndResourceSelection" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/SystemMapNavigationTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/PoiMapSelectionTests.cs"`

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
