---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0007-map-navigation-and-estimates
ticket: EP-0018-US-0007-TK-0002-current-travel-estimates
title: "Расстояния и оценки пути отделены от генерационного масштаба"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0007-TK-0001-cluster-focus-navigation, EP-0018-US-0003-TK-0003-voyage-geography]
files_touched: 5
serves: [AC-02, AC-03, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Расстояния и оценки пути отделены от генерационного масштаба

## Why
Сохранить удобную навигацию между уровнями и показывать честные текущие оценки после увеличения расстояний.
Поставляемый результат: расстояния и оценки пути отделены от генерационного масштаба. Тикет покрывает AC-02, AC-03, AC-04 [истории](../EP-0018-US-0007-map-navigation-and-estimates.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs | Station presentation directions и current poses | Форматировать часы/дни, текущий estimate и unavailable state. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | Ruler rendering:411 и camera actions | Разделить reference-time линейку и ETA к moving target. |
| tests/DeepSpaceSaga.Client.Tests/ClusterTravelEstimatesTests.cs | Существующие straight-flight estimates | Проверить current-engine update, unavailable, boundary hour/day, moving target. |
| tests/DeepSpaceSaga.Client.Tests/LocalClusterMapTests.cs | Существующие cluster map tests | Проверить UI выбранной станции и отсутствие remote quote/торговли. |

## Public API after the change
No new Engine query/API; presentation получает данные только из текущего snapshot.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Использовать snapshot current engine Vmax и poses для target estimate; generation Geography.ReferenceSpeedKmS применяется только к scale legend.
2. Выводить расстояние и часы/дни без обещания полного Dock; no engine/unknown pose даёт unavailable.
3. Новая установка двигателя обновляет ETA, но не station positions/cluster membership/reference ruler.
4. Проверить selected target на local/overview и отсутствие stale cached ETA после движения/load.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- ClusterTravelEstimatesTests.V2EtaUsesCurrentEngineWhileScaleUsesFrozenReference
- ClusterTravelEstimatesTests.MissingEngineShowsUnavailableAndDayBoundaryIsHonest
- LocalClusterMapTests.V2StationSelectionDoesNotEnableRemoteTrade
Перечисленные проверки реализованы; фактический запуск и evidence указаны в разделе Execution. Исходные planning-команды ниже не являются отдельным результатом запуска.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0007-TK-0002-current-travel-estimates"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~ClusterTravelEstimatesTests.V2EtaUsesCurrentEngineWhileScaleUsesFrozenReference|FullyQualifiedName~ClusterTravelEstimatesTests.MissingEngineShowsUnavailableAndDayBoundaryIsHonest|FullyQualifiedName~LocalClusterMapTests.V2StationSelectionDoesNotEnableRemoteTrade" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/ClusterTravelEstimatesTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/LocalClusterMapTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-02, AC-03, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Scope refinement before implementation
2026-11-07: the actual displayed estimate is built in Controls/ObjectInfoPanel.cs, outside the initial list. Add it to allowlist so hours/day formatting is user-visible and retains the existing Straight flight estimate caption (no full Dock promise). Current Vmax/current presented pose calculation already exists; test its update independently of frozen-reference ruler. Formatter explicitly handles near-day rounding and unavailable values.

## Execution evidence
2026-11-07: existing current-pose/current operational Vmax estimator retained and independently tested against doubled engine speed, moving epoch and missing engine. User-visible ObjectInfoPanel now formats calendar hours below1d and days above; near-day rounding is explicit<1d, invalid/unavailable remains unavailable. Frozen-reference ruler stays unchanged after Vmax update; local focus/selection does not reveal remote quote/trade. Initial new test used wrong market property name and failed compile; corrected to actual IsAvailable contract. Focused-final20/20 and full Client2115/2115 PASS; Release build, scoped format-verified and diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-current-estimates/{focused.log,focused-final.trx,client-full.trx,build.log,format-verified.log}. No new Engine query or cached ETA; no full Dock promise. Self-review complete.
