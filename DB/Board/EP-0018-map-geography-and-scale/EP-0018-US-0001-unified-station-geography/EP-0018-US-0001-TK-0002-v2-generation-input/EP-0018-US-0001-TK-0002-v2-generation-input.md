---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0001-unified-station-geography
ticket: EP-0018-US-0001-TK-0002-v2-generation-input
title: "Версионированные входы генерации и проверки диапазонов"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0018-US-0001-TK-0001-geography-descriptor]
files_touched: 3
serves: [AC-03, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Версионированные входы генерации и проверки диапазонов

## Why
Устранить исключение для старых станций Default и сделать географический минимум реальным расстоянием между каждой парой человеческих станций.
Поставляемый результат: версионированные входы генерации и проверки диапазонов. Тикет покрывает AC-03, AC-04 [истории](../EP-0018-US-0001-unified-station-geography.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Engine/Scenario/SolarSystemGeneration.cs | ValidateConfig и ValidateWorld принимают generatorVersion=1 | Добавить v2 config/world ingress и optional MinimumStationDays; v1 semantics сохранить. |
| src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs | Generate, Place, GeneratorRng; map создаётся с literal1 | Выбирать map version из валидированного config; frozen Vmax, отдельный v2 geography stream без изменения v1 golden. |
| tests/DeepSpaceSaga.Engine.Tests/GenerationInputSchemaTests.cs | InvalidConfigDoesNotPublish; Generation_work_is_bounded_before_replacing_the_world | Добавить version/range/NaN/Infinity tests. |

## Public API after the change
ClusterGenerationConfig(..., double MinimumStationDays = 1); существующие LoadScenario/CreateEngine signatures неизменны.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Реализовать C-02; gen1/2 допустимы, unknown отвергать; min1/neighbor consistency валидировать до публикации.
2. Не применять v2 minimum к v1 saved worlds. Оставить прежние planet/belt ограничения и retry/work caps.
3. Добавить v2 geometry stream interface с неизменным v1 GeneratorRng golden; пока не включать shipped JSON.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- GenerationInputSchemaTests.V2RangesAndVersionsAreValidatedAtomically
- GenerationInputSchemaTests.LegacyGeneratorGoldenVectorIsUnchanged
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0001-TK-0002-v2-generation-input"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" -c Release --filter "FullyQualifiedName~GenerationInputSchemaTests.V2RangesAndVersionsAreValidatedAtomically|FullyQualifiedName~GenerationInputSchemaTests.LegacyGeneratorGoldenVectorIsUnchanged" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGeneration.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Engine/Scenario/SolarSystemGenerator.cs"`

`dotnet format "tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Engine.Tests/GenerationInputSchemaTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-03, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.

## Фактическое исполнение — 2026-11-07

Добавлены gen1/2 ingress, optional minimumStationDays=1 и v2 consistency validation;
NaN/Infinity/нижняя граница/neighbor ordering отклоняются до замены мира. v1 minimum
не получает новых ограничений. Solar candidate носит config.GeneratorVersion,
география имеет отдельный named stage geography/v2; golden v1 сохранён.
Полная solar/cluster/save coherence будет в US-0004; shipped config пока v1.

Tests GenerationInputSchemaTests + SeededWorldBootstrapTests: 12/12 PASS, включая
оба named tests, прямую проверку gen2 solar candidate и legacy golden vector.
Engine regression Category!=LongRunning&FullyQualifiedName!~Corpus: 2070/2070 PASS;
это не полный corpus. Первый широкий прогон остановлен для шардирования untagged
100-seed Corpus и не считается PASS. Эти corpus будут выполнены отдельно точными
ID в итоговом gate. Release Engine build PASS0warnings/errors, scoped format
двух production + одного test file PASS, git diff --check PASS. Изменены3 owning
файла, self-review по AC03/04/C02 findings отсутствуют, независимого review нет.
Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-generation-input/
(focused-final.trx/log, engine-non-corpus.trx/log, build-unsandboxed.log,
format-engine.log, format-tests-final.log). Все test процессы cap300000ms.
Sandbox build.log — исходный неуспешный запуск; outside sandbox build — PASS.
Native/human/FPS NOT RUN. Сохранение v2 не объявляется проверенным этим тикетом.