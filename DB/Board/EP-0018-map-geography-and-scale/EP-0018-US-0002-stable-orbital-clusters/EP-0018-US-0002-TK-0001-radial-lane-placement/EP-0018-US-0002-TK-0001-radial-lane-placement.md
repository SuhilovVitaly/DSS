---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0002-stable-orbital-clusters
ticket: EP-0018-US-0002-TK-0001-radial-lane-placement
title: "Радиальные полосы и формации с доказуемым минимальным расстоянием"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0001-TK-0004-atomic-world-bootstrap]
files_touched: 3
serves: [AC-01, AC-02, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Радиальные полосы и формации с доказуемым минимальным расстоянием

## Why
После увеличения расстояний сохранить несколько торговых районов, устойчивую геометрию и совместимое размещение ИИ.
Поставляемый результат: радиальные полосы и формации с доказуемым минимальным расстоянием. Тикет покрывает AC-01, AC-02, AC-03 [истории](../EP-0018-US-0002-stable-orbital-clusters.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs | Expand выбирает lanes из belt; существующие группы клонируются | Новая v2 lane reservation с min1d и diameter10–16d; веткаv1 неизменна. |
| src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs | Новый helper US-0001 TK-0003 | Формировать дополнительные группы на различных radii; детерминированный retry. |
| tests/DeepSpaceSaga.Engine.Tests/MultiClusterPlacementTests.cs | Существующие multi-cluster tests | Добавить analytical swept-envelope и nearest checks. |

## Public API after the change
No API change; internal v2 placement helper расширяется только внутри перечисленных файлов.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Использовать C-01: station radius constant в каждой группе; lanes delta >=unitsPerDay плюс численный запас. Не полагаться на будущие фазовые samples как доказательство.
2. Распределить count3–5 внутри human belt, проверяя center distance30–50d на старте; все station orbits и resource envelopes должны быть в bounds.
3. Сохранить deterministic ordering/group seed derivation и explicit map version2. При невозможной ширине finite failure без fallback тесных групп.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- MultiClusterPlacementTests.V2SweptLanesProveAllPhaseMinimum
- MultiClusterPlacementTests.V2BoundaryClusterCountsKeepLocalNeighbours
- MultiClusterPlacementTests.V2NarrowBeltFailsWithinAttemptBudget
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0002-TK-0001-radial-lane-placement"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~MultiClusterPlacementTests.V2SweptLanesProveAllPhaseMinimum|FullyQualifiedName~MultiClusterPlacementTests.V2BoundaryClusterCountsKeepLocalNeighbours|FullyQualifiedName~MultiClusterPlacementTests.V2NarrowBeltFailsWithinAttemptBudget" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/MultiClusterPlacementTests.cs"`

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

## Уточнение dependency gate — 2026-11-07
Реальный Engine отверг multi-cluster candidate как disconnected до TK0002: существующий runtime routing требует межкластерные links. Поэтому этот геометрический тикет проверяет настоящий production SolarSystemGenerator→StationClusterGenerator→CompleteBootstrap до публикации, без mock/snapshot-подмены. Невозможный narrowbelt проверяется через live Engine atomic rejection. Полный runtime путь3–5кластеров и strong connectivity обязателен TK0002, затем story integration. Первый failed run сохраняется, не засчитываетсяPASS. Shippedv2 activation по-прежнему закрыта доUS4.

## Фактическое исполнение — 2026-11-07

Расширение v2 резервирует отдельный радиус на кластер, min gap>=minimumStationDays
с запасом для ресурсов; source home сохраняется, дополнительные группы используют
отдельные derived seeds. Count3–5, station10–12; initial center nearest30–50d.
Одинаковый radius/period/epoch/direction сохраняет расстояния внутри формации;
reverse triangle inequality |p-q|>=||p|-|q|| доказывает intercluster minimum при
любых фазах. После start correction заново проверяются lanes и belt envelopes.
При узком поясе конечная ошибка <=128attempts без тесного fallback.

Final22/22focused (MultiClusterPlacementTests + Bootstrap/V2/LocalCluster) PASS;
все3namedtests, boundary3/5×10/12, independent nearest/centers/radii, repeat seed,
epoch0/1/30/365 и live atomic narrow rejection. Geometry candidate идёт через
настоящий production pipeline; runtime graph integration остаётсяTK0002 как
объяснено в dependency gate выше. Первые2FAIL сохраняются и не выдаются заPASS.
Release Engine build0warnings/errors, scoped format3files, git diffcheck PASS.
Self-review поC01/02/AC01–03 findings нет, независимого review нет. Native NOT RUN.
Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-radial-lanes/
(focused-verified.trx/log, build.log, format-engine.log, format-tests.log; исходный
focused.trx/log/err содержит2integrationFAIL). Process-tree cap300000ms.