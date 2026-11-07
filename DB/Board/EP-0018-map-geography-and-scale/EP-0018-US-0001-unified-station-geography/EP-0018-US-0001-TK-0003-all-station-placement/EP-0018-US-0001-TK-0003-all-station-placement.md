---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0001-unified-station-geography
ticket: EP-0018-US-0001-TK-0003-all-station-placement
title: "Единое размещение исходных и новых станций"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0001-TK-0002-v2-generation-input]
files_touched: 3
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Единое размещение исходных и новых станций

## Why
Устранить исключение для старых станций Default и сделать географический минимум реальным расстоянием между каждой парой человеческих станций.
Поставляемый результат: единое размещение исходных и новых станций. Тикет покрывает AC-01, AC-02, AC-03, AC-04 [истории](../EP-0018-US-0001-unified-station-geography.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs | Новый internal helper, владелец этого тикета | Создать deterministic circular placement v2 и role materialization. |
| src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs | Place/Generate/BuildLinks | Маршрутизировать v2 через helper; legacy branch сохранить; убрать preserved exemption только v2. |
| tests/DeepSpaceSaga.Engine.Tests/StationGeographyV2Tests.cs | Новый тестовый файл | Независимо проверить все пары, nearest, role quotas и повторяемость. |

## Public API after the change
Новый internal helper Place с указанной сигнатурой; public API не меняется.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Создать internal static StationGeographyV2.Place(ScenarioFile, ClusterGenerationConfig, GameDataRegistry, ulong, int) -> ClusterGenerationResult; descriptor/reference Vmax из C-01/C-02.
2. Материализовать station IDs/roles из source/trading request, сохранить explicit payload; каждому назначить новую орбиту по C-01. Не копировать legacy coordinates в final v2 stations.
3. Проверить pairwise min и nearest, diameter10–16d, finite orbit/ID, required market profiles. Сначала build candidate, затем validate.
4. Не размещать ресурсы/игрока в этом helper; complete bootstrap следующий тикет. Не выдавать промежуточный candidate за публикуемую сессию.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- StationGeographyV2Tests.LegacyAndGeneratedStationsShareMinimumDays
- StationGeographyV2Tests.EveryMemberHasOneToThreeDayNeighbour
- StationGeographyV2Tests.SeedAndFailureAreDeterministic
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0001-TK-0003-all-station-placement"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~StationGeographyV2Tests.LegacyAndGeneratedStationsShareMinimumDays|FullyQualifiedName~StationGeographyV2Tests.EveryMemberHasOneToThreeDayNeighbour|FullyQualifiedName~StationGeographyV2Tests.SeedAndFailureAreDeterministic" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/StationGeographyV2Tests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-02, AC-03, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
