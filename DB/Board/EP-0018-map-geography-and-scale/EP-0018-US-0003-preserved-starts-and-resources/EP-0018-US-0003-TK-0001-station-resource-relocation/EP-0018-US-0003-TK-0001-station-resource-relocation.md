---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0003-preserved-starts-and-resources
ticket: EP-0018-US-0003-TK-0001-station-resource-relocation
title: "Ресурсные поля создаются у окончательных координат станции"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0002-TK-0003-full-system-integration]
files_touched: 4
serves: [AC-01]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Ресурсные поля создаются у окончательных координат станции

## Why
Перенести окружение относительно владельцев, сохранить смысл Default/Docked/Undocked и согласовать торговые путешествия с новой географией.
Поставляемый результат: ресурсные поля создаются у окончательных координат станции. Тикет покрывает AC-01 [истории](../EP-0018-US-0003-preserved-starts-and-resources.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/StationResourceFields.cs | Generate/ExtendForClusters/ValidateSaved | Развести generate-after-placement и перенос явно объявленных полей без повторного draw. |
| src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs | BindResources вычисляет offset и orbit владельца | Создать привязки после relocation; preserve manifest identity. |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | LoadScenarioCore v2 bootstrap из US-0001 | Согласовать порядок ресурсы->binding->AI, исключить double generation. |
| tests/DeepSpaceSaga.Engine.Tests/ClusterResourcePlacementTests.cs | Существующие resource tests | Расширить v2 ownership, repeated bootstrap, orbital local offsets. |

## Public API after the change
No public API change; existing station-resource config и ownership DTO сохраняются.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Для новых fields использовать окончательные station positions и прежний config2.5–4.5км; manifest/streams не дублировать.
2. Если source уже содержит fields, перенести каждый asteroid по delta owner, сохранить ID/composition/mass/RNG; пересоздать только orbital binding.
3. Проверить повторный вызов/полный manifest/отсутствие orphan resources; неизвестный состав не раскрывать в клиентской проекции.
4. Наложить ValidateSaved на начальную геометрию, не на неверно реконструированные legacy offsets.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- ClusterResourcePlacementTests.V2ResourcesFollowFinalStationCoordinates
- ClusterResourcePlacementTests.V2ResourceManifestIsNotGeneratedTwice
- ClusterResourcePlacementTests.V2RigidResourceOffsetsSurviveOrbit
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0003-TK-0001-station-resource-relocation"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~ClusterResourcePlacementTests.V2ResourcesFollowFinalStationCoordinates|FullyQualifiedName~ClusterResourcePlacementTests.V2ResourceManifestIsNotGeneratedTwice|FullyQualifiedName~ClusterResourcePlacementTests.V2RigidResourceOffsetsSurviveOrbit" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationResourceFields.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ClusterResourcePlacementTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
