---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0002-stable-orbital-clusters
ticket: EP-0018-US-0002-TK-0002-cluster-link-graph
title: "Торговые направления на новой географии без коротких исключений"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0002-TK-0001-radial-lane-placement]
files_touched: 3
serves: [AC-01, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Торговые направления на новой географии без коротких исключений

## Why
После увеличения расстояний сохранить несколько торговых районов, устойчивую геометрию и совместимое размещение ИИ.
Поставляемый результат: торговые направления на новой географии без коротких исключений. Тикет покрывает AC-01, AC-03 [истории](../EP-0018-US-0002-stable-orbital-clusters.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs | BuildLinks/ValidateGraph выбирают supply/demand flows | В v2 одинаковые distance eligibility для old/new станций, intercluster return paths. |
| src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs | Планируемый файл predecessor ticket: v2 candidate helpers | Stable graph candidates из реально размещённых stations. |
| tests/DeepSpaceSaga.Engine.Tests/V2ClusterGraphTests.cs | Новый файл | Проверить link semantics, strong reachability, preserved member равноправие. |

## Public API after the change
No API change; ClusterTradeLink по-прежнему потенциальное направление без фиксированного ETA.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Строить внутрикластерные eligible links по расстоянию1–3d без preserved shortcut; coverage по supply/demand проверять отдельно от geometry.
2. Межкластерные направления строить только по допустимым market roles; обеспечить достижимость всех участников в обе стороны.
3. Не добавлять quote/стоимость в map links; seed/order стабильны, география не подстраивается под экран.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- V2ClusterGraphTests.LegacyIdsHaveNoDistanceExemption
- V2ClusterGraphTests.AllStationsHaveOutboundAndReturnPaths
- V2ClusterGraphTests.CargoFlowsMatchProfilesAfterReposition
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0002-TK-0002-cluster-link-graph"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~V2ClusterGraphTests.LegacyIdsHaveNoDistanceExemption|FullyQualifiedName~V2ClusterGraphTests.AllStationsHaveOutboundAndReturnPaths|FullyQualifiedName~V2ClusterGraphTests.CargoFlowsMatchProfilesAfterReposition" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationClusterGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/V2ClusterGraphTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-03 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Фактическое исполнение — 2026-11-07

V2 BuildGraph использует одинаковую distance eligibility1–3d для всех локальных
stationIDs, без preserved исключения. Между соседними по ordinal cluster-index
районами строятся только supply/demand-compatible направления (пустые только от
transit); graph проверяется на strong forward/reverse reachability и>=2cycles.
В multi-cluster мире каждый участник имеет>=2outbound directions и producer consumer.
После final start correction граф заново строится по финальной геометрии.

Техническое уточнение: два исходящих направления проверяются по всей торговой сети,
не обязательно двум локальным покупателям. В изолированной1cluster fixture допускается
1outbound с strong reachability: фиксированный sourceID порядок и min1d могут давать
scientific producer только одного локального consumer. Shipped3–5clusters имеют>=2,
это не ослабление min или cargo eligibility. При несовместимых ролях placement
повторяется в прежнем bounded128budget, без добавления фиктивного cargo/shortcut.

25/25focusedPASS: все3namedtests + radial/bootstrap/v2/v1cluster regressions.
Live Engine3/5clusters seeds1/2/42, deterministic repeat, directed BFS от каждой
станции, current voyage map, independent profile intersection и no old-ID exemption.
Это закрывает runtime dependency, выявленную предыдущим radial тикетом.
Release Engine build0warnings/errors, scopedformat3files, diffcheckPASS.
Self-review C01/04/AC01/03 без findings; независимого review нет; Native NOT RUN.
Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-cluster-graph/
(focused-final.trx/log, build.log, format-engine.log, format-tests.log).
cap300000ms; временныеscripts/slots/worktrees отсутствуют.