---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0004-materialized-world-compatibility
ticket: EP-0018-US-0004-TK-0002-legacy-trade-map-validation
title: "Загрузка v2 не восстанавливает старые торговые offsets"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0004-TK-0001-versioned-save-validation]
files_touched: 5
serves: [AC-01, AC-02, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Загрузка v2 не восстанавливает старые торговые offsets

## Why
Включить новую географию только после доказанной materialized persistence, сохранив старые миры и строгую проверку ссылок.
Поставляемый результат: загрузка v2 не восстанавливает старые торговые offsets. Тикет покрывает AC-01, AC-02, AC-03 [истории](../EP-0018-US-0004-materialized-world-compatibility.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/TradingMapGeometryGenerator.cs | ValidateMaterialized(map, objects, seed, registry) сверяет template geometry | Добавить overload с SolarSystem/ClusterMap context и узкий v2 validation branch. |
| src/DeepSpaceSaga.Engine/Scenario/TradingMapData.cs | TradingMapDataValidation.ValidateAndNormalize(GameStateData, int), TradingMapData.cs:72 | Развести template-hour constraints и материализованный v2 с сохранением catalog/RNG checks. |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | MaterializeOrRestoreTradingMap передаёт InitialGeometry | Передавать materialized version context; Load не recompute/reposition. |
| tests/DeepSpaceSaga.Engine.Tests/TradingMapGeometryTests.cs | Существующая legacy geometry suite | Добавить v2 moved geometry valid/semantic tamper invalid. |
| tests/DeepSpaceSaga.Engine.Tests/TradingMapSchemaTests.cs | Существующая schema suite | Сохранить strict unknown-field/catalog/risk/cargo checks. |

## Public API after the change
Новый internal overload ValidateMaterialized с version context, существующая сигнатура совместима.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Добавить internal overload ValidateMaterialized(TradingMapStateData, IReadOnlyList<SpaceObjectData>, ulong, GameDataRegistry, SolarSystemMapSnapshot?, StationClusterMapSnapshot?). Старый overload делегирует legacy пути.
2. Только v2 заменяет сравнение old template offsets на canonical cluster membership/orbits; seed/catalog/template identities/RNG/risk/cargo остаются проверяемыми.
3. Согласовать v2 projected day routes с сохранёнными policies; старые hour-bound generation rules не являются v2 географическим cap.
4. Негативно проверить подмену market profile/risk/cargo/seed; запрещён общий bypass validation при наличии ClusterMap.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- TradingMapGeometryTests.V2LoadUsesMaterializedClusterGeometry
- TradingMapGeometryTests.V2DoesNotBypassTradingSemanticValidation
- TradingMapSchemaTests.V2TradingIngressRemainsStrict
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0004-TK-0002-legacy-trade-map-validation"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~TradingMapGeometryTests.V2LoadUsesMaterializedClusterGeometry|FullyQualifiedName~TradingMapGeometryTests.V2DoesNotBypassTradingSemanticValidation|FullyQualifiedName~TradingMapSchemaTests.V2TradingIngressRemainsStrict" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/TradingMapGeometryGenerator.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/TradingMapData.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/TradingMapGeometryTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/TradingMapSchemaTests.cs"`

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
