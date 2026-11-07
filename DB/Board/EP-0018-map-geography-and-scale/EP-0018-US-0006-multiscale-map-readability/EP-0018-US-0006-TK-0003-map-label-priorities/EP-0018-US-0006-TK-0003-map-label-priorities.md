---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0006-multiscale-map-readability
ticket: EP-0018-US-0006-TK-0003-map-label-priorities
title: "Приоритеты подписей и выбора на всех масштабах"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0006-TK-0002-orbital-layer-policy]
files_touched: 5
serves: [AC-02, AC-04, AC-05]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Приоритеты подписей и выбора на всех масштабах

## Why
Единая карта раскрывает Local/Region/Cluster/System, ресурсы и орбиты не создают нечитаемые полосы.
Поставляемый результат: приоритеты подписей и выбора на всех масштабах. Тикет покрывает AC-02, AC-04, AC-05 [истории](../EP-0018-US-0006-multiscale-map-readability.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs | DrawPlaques/placement | Учитывать domain summaries/detail priority и selected override. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs | Существующий label layout helper | Bounds/overlap/limit policy для cluster marker, station, selected. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | Hit testing/selection cycling | Согласовать hit regions с visible detail projection. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | Label rendering call и object markers | Передавать единый presentation context и сохранить selection reticle. |
| tests/DeepSpaceSaga.Client.Tests/MapDetailLabelTests.cs | Новый файл | Rendered label/hit tests, selected over limit, hidden resource knowledge. |

## Public API after the change
No gameplay API change; internal presentation context из TK-0001 расширяется только listed render/hit paths.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Зафиксировать порядок player/selected/nav target, затем station/cluster/planet, затем local resources; no selected suppression из-за MaximumLabels.
2. Не назначать aggregated marker чужой object ID; hit marker возвращает cluster focus action, station hit — её canonical ID.
3. Ограничить пересечения подписей и края viewport на UI1/1.5/2; displayed details только из разрешённой snapshot projection.
4. Проверить actual draw/layout/hit result на dense seed, а не mirror implementation branch tests.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- MapDetailLabelTests.SelectedStationRemainsVisibleAndSelectableAtEveryLevel
- MapDetailLabelTests.DenseLabelsStayInsideViewport
- MapDetailLabelTests.SummariesDoNotRevealUnknownResourceComposition
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0006-TK-0003-map-label-priorities"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~MapDetailLabelTests.SelectedStationRemainsVisibleAndSelectableAtEveryLevel|FullyQualifiedName~MapDetailLabelTests.DenseLabelsStayInsideViewport|FullyQualifiedName~MapDetailLabelTests.SummariesDoNotRevealUnknownResourceComposition" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelRenderer.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/ObjectLabelLayout.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/MapDetailLabelTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-02, AC-04, AC-05 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Execution evidence
2026-11-07: explicit targets first; semantic station/planet labels next; mandatory known/hostile ship labels retain existing budget override; local secondary labels follow. Deterministic bounded placement avoids overlaps/viewport edges and station/resource plaques avoid foreign marker hit regions. System cluster names have visible bounded plaques and cluster focus hit regions, no fake object ID. Hover/click use same visible label geometry, canonical markers win over nearby owner-resource aggregates. Reproduced/fixed station-label-to-aggregate interception and resource-summary-to-station interception; initial broad ship/pirate suppression regression corrected without modifying old tests. Focused84/84 and compatibility27/27 PASS; final full Client2106/2106 PASS includes selected every level, dense seed42 UI1/1.5/2, hidden resource composition and60-station selection. Build-last, scoped format-final/last/hover and diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-map-labels/{focused.trx,focused-final.trx,focused-verified.trx,focused-passed.trx,compatibility-verified.trx,client-full.trx,client-verified.trx,build-last.log,format-last.log,format-hover.log}. One interim validation build failed because the still-running full testhost locked its DLL; final checks run sequentially and pass. Self-review complete; native readability remains US8.
