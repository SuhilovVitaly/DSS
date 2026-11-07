---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0001-unified-station-geography
ticket: EP-0018-US-0001-TK-0004-atomic-world-bootstrap
title: "Атомарный запуск нового мира без старой компактной геометрии"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0001-TK-0003-all-station-placement]
files_touched: 4
serves: [AC-01, AC-02, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Атомарный запуск нового мира без старой компактной геометрии

## Why
Устранить исключение для старых станций Default и сделать географический минимум реальным расстоянием между каждой парой человеческих станций.
Поставляемый результат: атомарный запуск нового мира без старой компактной геометрии. Тикет покрывает AC-01, AC-02, AC-03 [истории](../EP-0018-US-0001-unified-station-geography.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | LoadScenarioCore:290–315; MaterializeOrRestoreTradingMap:535 | Отделить v2 materialization от старых trading offsets; ресурсы после station placement. |
| src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs | Планируемый helper из TK-0003 | Добавить internal bootstrap adapters для station payload/trading descriptor. |
| src/DeepSpaceSaga.Engine/Scenario/ScenarioGroupPlacement.cs | Translate переносит всю группу целиком | Сохранить start anchor/delta contract C-03; не возвращать станции к legacy coordinates. |
| tests/DeepSpaceSaga.Engine.Tests/StationGeographyBootstrapTests.cs | Новый файл | Проверить live engine v2 seed/failure/default identities. |

## Public API after the change
Публичный SimulationEngine.LoadScenario(ScenarioFile, bool=false, SolarSystemGenerationConfig?=null) сохраняется.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Ветку v2 выбирать до MaterializeOrRestoreTradingMap; сохранить topology/risk/flows, не генерировать старые offsets как authoritative geography.
2. Создать base solar и новые clusters в detached state; ресурсы генерировать только после окончательных station coordinates, детали переносов покрывает US-0003.
3. Включить start anchor C-03 и итоговую фактическую дистанцию player->Sun; failure не меняет прежнюю сессию.
4. Сохранить v1 pipeline и новый explicit config-only путь; активация shipped content только US-0004 TK-0004.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- StationGeographyBootstrapTests.DefaultPublishesOnlyCompleteV2World
- StationGeographyBootstrapTests.InvalidV2WorldLeavesRunningSessionUntouched
- StationGeographyBootstrapTests.PreservedStationPayloadSurvivesReposition
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0001-TK-0004-atomic-world-bootstrap"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~StationGeographyBootstrapTests.DefaultPublishesOnlyCompleteV2World|FullyQualifiedName~StationGeographyBootstrapTests.InvalidV2WorldLeavesRunningSessionUntouched|FullyQualifiedName~StationGeographyBootstrapTests.PreservedStationPayloadSurvivesReposition" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/ScenarioGroupPlacement.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/StationGeographyBootstrapTests.cs"`

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

## Фактическое исполнение — 2026-11-07

V2 выбирается до legacy geometry/resources. Station identities/roles/topology
из request материализуются без template offsets; frozen Geography и финальная
формация предшествуют resource generation/BindResources. CompleteBootstrap сохраняет
canonical trading rules/template/risk/cargo/RNG, но строит edges по новым координатам.
BindV2Start сохраняет local source vectors и docking parent+(1,1); малое изменение
радиуса домашней формации решает квадратное уравнение фактической player distance,
сохраняя50–75d даже у границ. Проверки делаются на detached world до runtime publish.
Legacy pipeline остаётся прежним. Shipped activation и строгий v2Load — US0004.

27/27focused placement/bootstrap/schema/v1cluster/seed PASS; все3namedtests PASS.
Engine regression безLongRunning/Corpus2076/2076PASS (не full corpus).
Release Engine build0warnings/errors, scoped formatter4files и diffcheck PASS.
Self-review тикета/US1/эпика без findings; независимого review нет.
Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-bootstrap/
(focused-final.trx/log, engine-non-corpus.trx/log, build.log, format-engine.log,
format-tests-final.log). В passed test output retained66pair distances и12nearest
для Defaultseed42, плюс полный AuthoritativeSnapshot. Test watchdog300000ms.
Native/product/FPS NOT RUN. Save/Load v2 ещё не проверен: явный compatibility gate
US0004 должен завершиться до shipped activation, промежуточный код не выдаётся за
полный production rollout всего эпика. Temporary scripts/slots/worktrees не созданы.