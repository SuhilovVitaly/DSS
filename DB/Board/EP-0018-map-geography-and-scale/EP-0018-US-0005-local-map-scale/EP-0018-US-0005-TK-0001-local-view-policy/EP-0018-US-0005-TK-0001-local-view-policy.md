---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0005-local-map-scale
ticket: EP-0018-US-0005-TK-0001-local-view-policy
title: "Чистая политика локального охвата и максимального приближения"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0001-TK-0001-geography-descriptor]
files_touched: 3
serves: [AC-01, AC-04]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Чистая политика локального охвата и максимального приближения

## Why
Придать максимуму приближения физический смысл и открывать New Game около игрока, а не в обзоре всей системы.
Поставляемый результат: чистая политика локального охвата и максимального приближения. Тикет покрывает AC-01, AC-04 [истории](../EP-0018-US-0005-local-map-scale.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/TacticalMapSettings.cs | MaximumPpu статический, Validate требует5 scale targets | Добавить referenceSpeedKmS fallback и локальные bounds settings; сохранять legacy defaults. |
| src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs | Новый pure Client helper | C-05 required PPU из viewport/reference speed, без Engine вызова. |
| tests/DeepSpaceSaga.Client.Tests/LocalMapScalePolicyTests.cs | Новый файл | Проверить huge/zero/nonfinite/aspect/UI/reference cases. |

## Public API after the change
Новый internal pure helper; additive Client settings. Имена planned settings: referenceSpeedKmS, maxLocalHours.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Создать internal MapScalePolicy.MaximumPpu(TacticalMapSettings, int width, int height, double referenceSpeedKmS) -> double; invalid viewport безопасно fallback existing bounds.
2. Вывести ограничения из единицC-05, не из текущей скорости корабля: frozen descriptor or explicit fallback; never mutate world.
3. Добавить settings fallback referenceSpeedKmS4 и maxLocalHours4 с validation; legacy JSON без полей должен сохранить работоспособность.
4. Проверить опорные2/4км/с и windows до32:9, extreme viewport без overflow и перепутывания logical/device pixels.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- LocalMapScalePolicyTests.MaximumZoomBoundsHoursAndDiagonal
- LocalMapScalePolicyTests.LegacyAndInvalidViewportHaveDeterministicFallback
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0005-TK-0001-local-view-policy"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~LocalMapScalePolicyTests.MaximumZoomBoundsHoursAndDiagonal|FullyQualifiedName~LocalMapScalePolicyTests.LegacyAndInvalidViewportHaveDeterministicFallback" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/TacticalMapSettings.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/MapScalePolicy.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/LocalMapScalePolicyTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-04 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
