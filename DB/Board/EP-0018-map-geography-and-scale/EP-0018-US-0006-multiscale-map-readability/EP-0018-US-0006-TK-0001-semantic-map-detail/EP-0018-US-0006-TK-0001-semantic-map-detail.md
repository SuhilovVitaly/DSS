---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0006-multiscale-map-readability
ticket: EP-0018-US-0006-TK-0001-semantic-map-detail
title: "Доменные уровни детализации и агрегация объектов"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0004-TK-0004-activate-v2-content, EP-0018-US-0005-TK-0003-local-scale-content]
files_touched: 5
serves: [AC-01, AC-02, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Доменные уровни детализации и агрегация объектов

## Why
Единая карта раскрывает Local/Region/Cluster/System, ресурсы и орбиты не создают нечитаемые полосы.
Поставляемый результат: доменные уровни детализации и агрегация объектов. Тикет покрывает AC-01, AC-02, AC-04 [истории](../EP-0018-US-0006-multiscale-map-readability.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapDetailPolicy.cs | Новый pure helper | Enum MapDetailLevel и deterministic policyC-06 с10% hysteresis. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs | Draw/Bounds/Resource helpers | System markers из ClusterMap membership, региональные resource summaries. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | UpdateMapClusters и click cycling | Заменить смысл screen cells доменной aggregation policy, сохранить spatial optimization. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | Map render sequence | Применить единый detail level ко всем world-map слоям. |
| tests/DeepSpaceSaga.Client.Tests/MapDetailPolicyTests.cs | Новый файл | Threshold/hysteresis/domain membership/identity tests. |

## Public API after the change
Новые internal MapDetailLevel и MapDetailPolicy; public snapshots не расширяются кроме US-0001 descriptor.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Создать enum и MapDetailPolicy.Resolve из camera world span/reference speed/current level; пороги Local4ч/Region4д/Cluster30д/System дальше, hysteresis10%.
2. На System кластерный маркер отражает реальные domain members, не случайную cell; center/bounds по current presented poses.
3. На Region ресурсы агрегировать по station owner/field, не объединяя рынки или скрытые composition.
4. Selected/player/nav target выводить отдельно; side effects и engine calls отсутствуют; render и hit testing используют одну projection.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- MapDetailPolicyTests.DetailDependsOnWorldSpanWithHysteresis
- MapDetailPolicyTests.SystemMarkersUseDomainMembership
- MapDetailPolicyTests.AggregationNeverChangesWorldIdentity
Перечисленные проверки реализованы; фактический запуск и evidence указаны в разделе Execution. Исходные planning-команды ниже не являются отдельным результатом запуска.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0006-TK-0001-semantic-map-detail"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~MapDetailPolicyTests.DetailDependsOnWorldSpanWithHysteresis|FullyQualifiedName~MapDetailPolicyTests.SystemMarkersUseDomainMembership|FullyQualifiedName~MapDetailPolicyTests.AggregationNeverChangesWorldIdentity" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapDetailPolicy.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ClusterMapPresentation.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/MapDetailPolicyTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-02, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Execution evidence
2026-11-07: Local/Region/Cluster/System resolved from short-side physical span at frozen/fallback reference, thresholds4h/4d/30d with10% hysteresis. System markers derive from exact domain memberships/current pose bounds; Region/Cluster aggregate fields by owner, not markets or screen cells. Selected/player/nav targets stay individual. Legacy cells remain a spatial optimization for unmapped contacts; human station membership is never reassigned. Cluster outlines/directions only on Cluster detail. Focused-final30/30 PASS including three named regressions; Release build, format-verified/format-last and diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-map-detail/{focused-final.trx,build.log,format-verified.log,format-last.log}. Tests confirm original snapshot identity and no survey-content transfer to aggregates. Self-review complete; raster/native readability remains US8.
