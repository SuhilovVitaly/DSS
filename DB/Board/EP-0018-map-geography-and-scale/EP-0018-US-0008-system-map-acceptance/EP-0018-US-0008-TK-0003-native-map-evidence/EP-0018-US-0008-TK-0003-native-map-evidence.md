---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0008-system-map-acceptance
ticket: EP-0018-US-0008-TK-0003-native-map-evidence
title: "Нативный маршрут по карте и отдельная продуктовая приёмка"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: tooling
test_project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj
depends_on: [EP-0018-US-0008-TK-0002-rendered-map-evidence]
files_touched: 3
serves: [AC-02, AC-03, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Нативный маршрут по карте и отдельная продуктовая приёмка

## Why
Подтвердить результат числами и реальным клиентом: seed corpus, четыре уровня карты, сценарный путь к станции и сохранение.
Поставляемый результат: нативный маршрут по карте и отдельная продуктовая приёмка. Тикет покрывает AC-02, AC-03, AC-04 [истории](../EP-0018-US-0008-system-map-acceptance.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| tools/DeepSpaceSaga.Performance/SolarNativeEvidence.cs | Run(args) создаёт native scripted evidence | Добавить v2 route/input/viewport capture и контролируемое cleanup. |
| tools/DeepSpaceSaga.Performance/Program.cs | Command dispatcher из TK-0002 | Нативный режим --map-v2-native-acceptance и параметры seed/viewport/output. |
| tools/DeepSpaceSaga.Performance.Tests/V2NativeMapEvidenceTests.cs | Новый файл | Проверить scenario manifest, outcomes, false-pass guards и cleanup ownership. |

## Public API after the change
Tooling flag --map-v2-native-acceptance; humanAcceptance явно separate/notRun до фактической оценки.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. В existing native host реализовать путьC-07 Default start->zoom->cluster->station local focus->Approach/Dock->Save/Load->return; журналировать фактические inputs/outcomes.
2. Снять реальную camera/selected/nav target/scale и screenshots на размерах C-07; добавить pause/fastspeed/resize/LOD threshold input cases.
3. Собрать presented frame timing с warmup и sample count; определить80FPS status отдельно от interaction success; physical-input и human readability остаются отдельными полями.
4. Создавать только task-owned slots/processes/output; завершить процессы и убрать disposable slots/cache, сохранить referenced screenshots/report. Указывать NOT RUN там, где native host недоступен.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- V2NativeMapEvidenceTests.RouteRequiresObservedDockLoadAndReturn
- V2NativeMapEvidenceTests.ScriptedPassDoesNotImplyHumanAcceptance
- V2NativeMapEvidenceTests.CleanupNeverRemovesForeignSlots
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0008-TK-0003-native-map-evidence"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" -c Release --filter "FullyQualifiedName~V2NativeMapEvidenceTests.RouteRequiresObservedDockLoadAndReturn|FullyQualifiedName~V2NativeMapEvidenceTests.ScriptedPassDoesNotImplyHumanAcceptance|FullyQualifiedName~V2NativeMapEvidenceTests.CleanupNeverRemovesForeignSlots" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "tools/DeepSpaceSaga.Performance/DeepSpaceSaga.Performance.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/SolarNativeEvidence.cs" "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance/Program.cs"`

`dotnet format "tools/DeepSpaceSaga.Performance.Tests/DeepSpaceSaga.Performance.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tools/DeepSpaceSaga.Performance.Tests/V2NativeMapEvidenceTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-02, AC-03, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
