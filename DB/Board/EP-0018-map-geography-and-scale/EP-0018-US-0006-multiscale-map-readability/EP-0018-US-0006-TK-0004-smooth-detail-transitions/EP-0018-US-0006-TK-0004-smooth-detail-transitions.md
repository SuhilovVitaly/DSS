---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0006-multiscale-map-readability
ticket: EP-0018-US-0006-TK-0004-smooth-detail-transitions
title: "Устойчивые переходы детализации и очистка состояния карты"
stage: approved
approval_kind: workflow
implementation_status: not_started
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0006-TK-0003-map-label-priorities]
files_touched: 4
serves: [AC-01, AC-05]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Устойчивые переходы детализации и очистка состояния карты

## Why
Единая карта раскрывает Local/Region/Cluster/System, ресурсы и орбиты не создают нечитаемые полосы.
Поставляемый результат: устойчивые переходы детализации и очистка состояния карты. Тикет покрывает AC-01, AC-05 [истории](../EP-0018-US-0006-multiscale-map-readability.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapDetailPolicy.cs | Новая policy TK-0001 | Boundary hysteresis lifecycle/reset без зависимостей от FPS. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | CameraZoomTransition и map cluster pools | Reset/resize/cancel stale hit caches and transitions. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | Session lifetime/map render context | Сбрасывать detail state при новом мире; сохранять на очередном snapshot. |
| tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs | Существующий smoothness suite | Добавить boundary zoom/pan/session replacement tests. |

## Public API after the change
No API change.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Проверить переходы detail вверх/вниз около порогов с10% hysteresis, pause/fast snapshots и canceled animation.
2. При pan/resize пересобирать screen projection/hit regions из одного presented frame; не обращаться к будущему snapshot.
3. При new world/load clear pooled aggregate identity, detail previousLevel и stale selection caches по existing lifecycle.
4. Проверить стабильные allocations/нет накопления pattern caches; performance claims только после US-0008 измерений.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- TacticalMapSmoothnessTests.DetailThresholdDoesNotFlicker
- TacticalMapSmoothnessTests.NewSessionClearsOldClusterPresentation
- TacticalMapSmoothnessTests.ResizeKeepsSelectionAndHitsAligned
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0006-TK-0004-smooth-detail-transitions"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~TacticalMapSmoothnessTests.DetailThresholdDoesNotFlicker|FullyQualifiedName~TacticalMapSmoothnessTests.NewSessionClearsOldClusterPresentation|FullyQualifiedName~TacticalMapSmoothnessTests.ResizeKeepsSelectionAndHitsAligned" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/MapDetailPolicy.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/TacticalMapSmoothnessTests.cs"`

## Definition of Done
- Все Implementation steps выполнены только в allowlist; публичный контракт и зависимости соблюдены.
- AC-01, AC-05 родительской истории покрыты named tests и наблюдаемым evidence без подмены старого поведения новым.
- Focused tests и owning build проходят; исходные падения описаны отдельно и не скрывают новые.
- Diff, scoped format для C#, JSON/Markdown проверки по типу изменения проходят.
- Нет скрытых assumptions или нерешённых блокирующих вопросов; фактические evidence paths записаны в Board.
- Временные файлы очищены; retained evidence находится только в разрешённом validation run.
- Native/product acceptance не объявлена по одним unit tests.

## Self-containment check
Разрешённые файлы, текущие точки входа, конечное поведение, шаги, planned API и точные проверки перечислены здесь. Общий продуктовый контракт — ../../ImplementationContract.md; зависимости — frontmatter. Прежде чем менять API вне этих границ, пересмотреть Board, а не угадывать.
