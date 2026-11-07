---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0002-stable-orbital-clusters
ticket: EP-0018-US-0002-TK-0003-full-system-integration
title: "Размещение ИИ и слоёв среды на расширенной географии"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0002-TK-0002-cluster-link-graph]
files_touched: 5
serves: [AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Размещение ИИ и слоёв среды на расширенной географии

## Why
После увеличения расстояний сохранить несколько торговых районов, устойчивую геометрию и совместимое размещение ИИ.
Поставляемый результат: размещение ии и слоёв среды на расширенной географии. Тикет покрывает AC-04 [истории](../EP-0018-US-0002-stable-orbital-clusters.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/AiBaseGenerator.cs | Generate/Place/StartNetworkOverlap | Адаптировать кандидаты и guard bounds к v2 без weakening AI exclusions. |
| src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs | Validate с DefaultHorizon365d и маршрутами из ClusterMap | Сохранить проверки detours/возврата для больших human clusters. |
| tests/DeepSpaceSaga.Engine.Tests/V2FullSystemPlacementTests.cs | Новый файл | Интеграция bases/territories/fields/POI/minpair/bounds. |

## Public API after the change
No API change; AI rulesVersion остаётся1 и не смешивается с cluster rulesVersion2.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Сформировать candidate AI после окончательной human geography и ресурсов; проверять те же допустимые каталоги/anchors.
2. Проверить bounded365d trade viability и стартовые пересечения для v2; увеличить разнообразие candidate sampling в пределах заданного attempt cap, если требуется.
3. Проверить environment/POI anchors и SystemRadius без переписывания их unrelated алгоритмов; если требуется их изменение, явный follow-up Board split, а не выход за allowlist.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- V2FullSystemPlacementTests.AiPlacementRespectsV2TradingNetwork
- V2FullSystemPlacementTests.EnvironmentAndPoiAnchorsRemainValid
- V2FullSystemPlacementTests.PlacementFailureIsBoundedAndDiagnostic
Перечисленные проверки реализованы; фактический запуск и evidence указаны в разделе Execution. Исходные planning-команды ниже не являются отдельным результатом запуска.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0002-TK-0003-full-system-integration"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~V2FullSystemPlacementTests.AiPlacementRespectsV2TradingNetwork|FullyQualifiedName~V2FullSystemPlacementTests.EnvironmentAndPoiAnchorsRemainValid|FullyQualifiedName~V2FullSystemPlacementTests.PlacementFailureIsBoundedAndDiagnostic" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/AiBaseGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/AiTradePlacementValidator.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/V2FullSystemPlacementTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Уточнение scope/dependencies до исправления — 2026-11-07

Реальное воспроизведение before.trx:2/3FAIL в StationClusterSaveValidation.Validate,
который вызывается и для NewGame сAI/средой и принимает толькоRulesVersion1.
Добавлен в Code context src/DeepSpaceSaga.Engine/Scenario/StationClusterSaveValidation.cs
(существующий Engine validator): перенести сюда минимально необходимый строгий v2
version/descriptor/rigid/min/neighbor/link/radial ingress изUS0004TK0001. Не применять
временный bypass. Полный file-save ingress/strict JSON/mixed version regressions,
loader и no-regeneration остаютсяUS0004 до shippedactivation. Allowlist теперь
AiBaseGenerator.cs, AiTradePlacementValidator.cs, StationClusterSaveValidation.cs,
V2FullSystemPlacementTests.cs; если AI алгоритмы уже совместимы, они не меняются.
Scope дополнен src/DeepSpaceSaga.Engine/SimulationEngine.cs: перед final v2 candidate preflight присоединить локально построенный ClusterMap к GameStateData и выполнить тот же строгий validator также безAI. Ранее безAI map хранился только в local variable до commit, из-за чего этот общий validator не видел его. V1 pipeline не меняется.

## Фактическое исполнение — 2026-11-07

AI placement/365d validator, environment и POI оказались совместимы без изменения
алгоритмов. Воспроизведён и исправлен общийv1-only cluster validator: version pair,
required finite descriptor, same-radius rigidity, всеlocal pairs/nearest, local
link eligibility и analytic radial separation; v1 legacy проверки сохраняются.
Final v2 candidate всегда присоединяет ClusterMap перед общим preflight, также
безAI; никаких validation bypass. Исходные2/3FAIL before.trx сохранены.

Final36/36focusedPASS:3namedfullsystem + v2graph/bootstrap и v1 save/environment/POI
regressions. Live seeds1/42/100:AI не входят вhuman membership, start exclusions,
365d proof valid/clearance>0/connectivity1, validfield/POIanchors/systembounds,
boundedAI failure attempts2 сохраняет running world. Negative descriptors/mixed
versions отвергаются. Engine non-corpus2085/2085PASS; full corpus ещёUS8.
Release Engine build0warnings/errors, scopedformat3changedfiles, diffcheckPASS.
Self-review tick/story/epic findings нет, независимого review не было.
Native/human/FPS NOT RUN; full v2file-load/no-regeneration остаётсяUS4 gate.

Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-full-system/
(before.trx/log/err; focused-final.trx/log; engine-non-corpus.trx/log; build.log;
format-engine.log; format-tests.log). Test process-tree cap300000ms.
Actual implementation3files: validator, SimulationEngine и namedtests; остальные
разрешённыеAIproductionfiles не изменены. Temporaryscripts/slots/worktrees не созданы.
