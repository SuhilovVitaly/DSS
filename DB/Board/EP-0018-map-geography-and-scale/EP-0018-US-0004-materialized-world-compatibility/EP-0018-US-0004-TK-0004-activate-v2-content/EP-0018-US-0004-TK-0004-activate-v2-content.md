---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0004-materialized-world-compatibility
ticket: EP-0018-US-0004-TK-0004-activate-v2-content
title: "Включение новой карты для всех обычных стартов"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0004-TK-0003-local-world-roundtrip]
files_touched: 3
serves: [AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Включение новой карты для всех обычных стартов

## Why
Включить новую географию только после доказанной materialized persistence, сохранив старые миры и строгую проверку ссылок.
Поставляемый результат: включение новой карты для всех обычных стартов. Тикет покрывает AC-04 [истории](../EP-0018-US-0004-materialized-world-compatibility.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/Data/Maps/solar-system.json | gen1, width.08, neighbor.5–2, diameter3–7, intercluster15–35 | gen2, width.20, minimum1, neighbor1–3, diameter10–16, intercluster30–50; counts/scenarios сохранить. |
| tests/DeepSpaceSaga.Client.Tests/AllScenarioSystemContentTests.cs | Production content loading всех enabled scenarios | Проверить v2 factory startup, min/nearest по published snapshot. |
| tests/DeepSpaceSaga.Client.Tests/DefaultSystemContentTests.cs | Default content suite | Закрепить old station identities на новой географии. |

## Public API after the change
Runtime JSON generatorVersion2, clusters.minimumStationDays=1; additive schema описана C-02.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. После TK-0003 активировать exact contentC-01/02; schemaVersion1, generatorVersion2; явно добавить minimumStationDays.
2. Не менять scenario market payload/engine speeds/calendar. Прежние templates остаются legacy reference, не v2 coordinates.
3. Через production EngineContentLoader получить все6 worlds, проверить default stationIDs/min/nearest/versions и roles.
4. Если boundary counts не укладываются — не ослаблять min1d; исправить owning generation ticket и повторить gate.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- AllScenarioSystemContentTests.AllEnabledStartsPublishV2Geography
- DefaultSystemContentTests.LegacyDefaultStationsRespectDayMinimum
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0004-TK-0004-activate-v2-content"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~AllScenarioSystemContentTests.AllEnabledStartsPublishV2Geography|FullyQualifiedName~DefaultSystemContentTests.LegacyDefaultStationsRespectDayMinimum" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/AllScenarioSystemContentTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/DefaultSystemContentTests.cs"`

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
