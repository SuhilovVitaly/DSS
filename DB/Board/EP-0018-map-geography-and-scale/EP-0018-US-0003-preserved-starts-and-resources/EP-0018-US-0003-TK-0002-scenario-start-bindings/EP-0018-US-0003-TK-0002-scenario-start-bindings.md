---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0003-preserved-starts-and-resources
ticket: EP-0018-US-0003-TK-0002-scenario-start-bindings
title: "Стартовые сценарии сохраняют игрока и локальные привязки"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0003-TK-0001-station-resource-relocation]
files_touched: 5
serves: [AC-02, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Стартовые сценарии сохраняют игрока и локальные привязки

## Why
Перенести окружение относительно владельцев, сохранить смысл Default/Docked/Undocked и согласовать торговые путешествия с новой географией.
Поставляемый результат: стартовые сценарии сохраняют игрока и локальные привязки. Тикет покрывает AC-02, AC-04 [истории](../EP-0018-US-0003-preserved-starts-and-resources.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/ScenarioGroupPlacement.cs | Translate/InitialGeometry | Реализовать anchor правила C-03 для v2 без rigid переноса всех станций. |
| src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs | Планируемый файл predecessor ticket: v2 helpers из US-0001/2 | Принимать start bindings и проверять итоговую player distance. |
| src/DeepSpaceSaga.Engine/SimulationEngine.cs | LoadScenarioCore и SynchronizeOrbitalBindings вызов | Интегрировать final start normalization перед publish. |
| tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs | Существующие scenario translation tests | Покрыть все6 scenarios, engine2/4км/с и Docked/free cases. |
| tests/DeepSpaceSaga.Engine.Tests/OrbitalDockingDepartureTests.cs | Существующие docking/departure regressions | Проверить continuity на v2 station orbit. |

## Public API after the change
No API change; internal anchor representation не выходит в wire.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Определить start anchor до перестановки по C-03; сохранить player/local nonstation vectors и explicit references.
2. Docked нормализовать parent+(1,1); свободный игрок не становится orbital/docked только из-за генерации.
3. Проверить final radius50–75d по фактическому ship pose, включая docking offset и boundary50/75; исправлять candidate generation, не меняя общую скорость/единицы.
4. Оставить v1 сценарные переносы прежними; реальные Approach/Dock/departure проверить existing public commands.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- ScenarioGroupTranslationTests.V2AllStartsKeepMeaningAndFiftyToSeventyFiveDays
- ScenarioGroupTranslationTests.V2ExplicitPayloadAndReferencesArePreserved
- OrbitalDockingDepartureTests.V2DockAndDepartureAreContinuous
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0003-TK-0002-scenario-start-bindings"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~ScenarioGroupTranslationTests.V2AllStartsKeepMeaningAndFiftyToSeventyFiveDays|FullyQualifiedName~ScenarioGroupTranslationTests.V2ExplicitPayloadAndReferencesArePreserved|FullyQualifiedName~OrbitalDockingDepartureTests.V2DockAndDepartureAreContinuous" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/ScenarioGroupPlacement.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/StationGeographyV2.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/SimulationEngine.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/ScenarioGroupTranslationTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/OrbitalDockingDepartureTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-02, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
