---
epic: EP-0018-map-geography-and-scale
story: EP-0018-US-0005-local-map-scale
ticket: EP-0018-US-0005-TK-0002-initial-camera-and-zoom
title: "Локальный старт камеры и согласованные границы zoom/resize"
stage: approved
approval_kind: workflow
implementation_status: completed
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0018-US-0005-TK-0001-local-view-policy]
files_touched: 9
serves: [AC-01, AC-02, AC-03]
created: 2026-11-07T17:21:08Z
revision: 1
---
# Локальный старт камеры и согласованные границы zoom/resize

## Why
Придать максимуму приближения физический смысл и открывать New Game около игрока, а не в обзоре всей системы.
Поставляемый результат: локальный старт камеры и согласованные границы zoom/resize. Тикет покрывает AC-01, AC-02, AC-03 [истории](../EP-0018-US-0005-local-map-scale.md).

## Decisions
Пользователь выбрал вариант 3: станции разделены игровыми днями, включая старые Default; максимальное приближение показывает локальную окрестность. Создание всех историй и тикетов без отдельных подтверждений поручено сообщением: «отлично. построй новый эпик на переделку карты и создай все сторис и тикеты без моего подтверждения. я хочу видеть уже готовый эпик со сторисами и тикетами».
Это согласование направления и процесса; технические значения A-01–A-09 — assumptions, не отдельные цитаты пользователя.

## Assumptions
Действует [контракт эпика](../../ImplementationContract.md): min 1 день, nearest 1–3 дня, frozen generation Vmax, единицы/календарь неизменны, v1 Load без новой генерации. Использовать точные ограничения контракта; численные допуски применяются к расчёту, а не ослабляют продуктовые границы.

## Code context
Все пути от корня D:/DeepSpaceSaga/DSS. Allowlist включает тестовые файлы; максимум пять файлов. Новые файлы явно помечены, их API ниже — планируемый контракт.

| File | Current state | Allowed change |
|---|---|---|
| src/DeepSpaceSaga.Client/UI/CameraState.cs | Camera PPU, clamp/world-screen transforms | Поддержать effective maximum при resize без потери focus/inverse transforms. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs | ZoomBy:105, FitSystem/bounds и camera focus | Использовать policy max; New Game local focus; явный system fit без snapshot reset. |
| src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs | Session snapshot/render integration | Применить one-time startup camera init к world map, не tactical battle. |
| tests/DeepSpaceSaga.Client.Tests/GameSessionZoomTests.cs | Существующая zoom suite | Проверить initial/fit/manual zoom/resize/input anchor. |
| tests/DeepSpaceSaga.Client.Tests/CameraStateZoomTests.cs | Существующие camera tests | Закрепить invertibility, max clamp и transition interruption. |

## Public API after the change
No public gameplay API change; internal camera overloads разрешены только listed callers/files.
Никаких новых public Engine вызовов из Client/render loop. Зависимости создают только явно перечисленные predecessor tickets.

## Implementation steps
1. Подключить effective max из policy во всех world-map zoom/clamp путях, включая transition target и resize.
2. Первый snapshot New Game фокусирует игрока на Local; subsequent snapshot не сбрасывает pan/zoom. Load использует существующий lifecycle и fallback без модификации сохранённого мира.
3. Сохранить cursor zoom anchor при свободной камере и player-center anchor при attached focus; FitSystem — явная операция.
4. Проверить непрерывность и world-screen inverse; на неподдержанном aspect diagonal limit всё равно действует. TacticalBattle классы вне scope.
5. В MapView линейке показывать километры и reference-time контекст «при опорной скорости»; не использовать текущую скорость или выдавать шкалу за ETA Dock. Покрыть проверкой GameSessionZoomTests.RulerUsesExplicitReferenceTime.

## Out of scope
Production-файлы вне Code context, изменения чужого слоя и поведения старых миров, не оговорённого compatibility gate. Не обходить ограничения файлов поиском и скрытым расширением allowlist; при фактическом конфликте обновить этот Board до изменения кода. Не коммитить/пушить по одному только planning approval.

## Invariants
- География не зависит от viewport, LOD, FPS или текущей установленной скорости корабля: EngineRequirements, «Архитектура и авторитет», «Мир и наблюдаемость».
- Шкалы времени и единицы сохранены: EngineRequirements, «Время и единицы»; OrbitalMotionMath.At(ObjectMotionSnapshot, OrbitalElements, long).
- Ошибка входа/генерации не заменяет живую сессию: SimulationEngine.LoadScenarioCore и EngineRequirements, «Архитектура и авторитет».
- v1 и v2 разделены; Load не вызывает генератор. I-01–I-09 родительского контракта обязательны.

## Tests
- GameSessionZoomTests.NewGameStartsLocalAndFitSystemRemainsExplicit
- GameSessionZoomTests.ResizePreservesFocusAndLocalLimit
- CameraStateZoomTests.EffectiveMaximumKeepsCoordinateTransformsInvertible
- GameSessionZoomTests.RulerUsesExplicitReferenceTime
Имена новых тестов выше — обязательные будущие проверки, сейчас они не существуют и не выполнялись.

Matching test project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj.
Будущая focused-команда (запуск из корня репозитория; сначала создать каталог):
```powershell
$runDir = "D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/<unique-run>/EP-0018-US-0005-TK-0002-initial-camera-and-zoom"
New-Item -ItemType Directory -Path $runDir -Force | Out-Null
dotnet test "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" -c Release --filter "FullyQualifiedName~GameSessionZoomTests.NewGameStartsLocalAndFitSystemRemainsExplicit|FullyQualifiedName~GameSessionZoomTests.ResizePreservesFocusAndLocalLimit|FullyQualifiedName~CameraStateZoomTests.EffectiveMaximumKeepsCoordinateTransformsInvertible|FullyQualifiedName~GameSessionZoomTests.RulerUsesExplicitReferenceTime" --results-directory "$runDir" --logger "trx;LogFileName=focused.trx"
dotnet build "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" -c Release --no-restore
git diff --check
```
Test process-tree cap: 300000 ms; обеспечить внешний watchdog с остановкой дерева процесса, timeout считать FAIL, а не PASS. Большой corpus делить по точным FullyQualifiedName тестов/случаев, каждый процесс не дольше cap. Build/format stdout и stderr сохранять в абсолютном runDir, не в корне.

Scoped format (будущие команды, из корня репозитория):
`dotnet format "src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/CameraState.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.MapView.cs" "D:/DeepSpaceSaga/DSS/src/DeepSpaceSaga.Client/UI/Screens/GameSession/GameSessionScreen.cs"`

`dotnet format "tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj" --verify-no-changes --no-restore --include "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/GameSessionZoomTests.cs" "D:/DeepSpaceSaga/DSS/tests/DeepSpaceSaga.Client.Tests/CameraStateZoomTests.cs"`

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

## Scope refinement before implementation
2026-11-07: CameraZoomTransition.cs and Screens/GameSession/MapViewGeometry.cs also clamp against static MaximumPpu, and Fit uses CameraState.SetZoom with default hard cap10. Add these two production helpers to allowlist; pass optional effective maximum explicitly, preserving other callers and tactical battle defaults. All world-map zoom/Fit paths must share the viewport/reference policy. Ruler uses an extra compact reference-time row, with toolbar height updated; raw device pixels enter policy, UI scaling only affects chrome.

Ruler reference-time text also requires Data/Locale/{English.json,Russian.json}; add both locale dictionaries before editing. This keeps the explicit reference context visible in either supported UI language.

## Execution evidence
2026-11-07: first player snapshot initializes one-time Local, later snapshots retain user zoom/Fit. Wheel/presets/animation/Fit/resize share effective maximum from frozen geography or explicit legacy fallback; at-maximum resize follows new bound, manual focus/scale remain controlled. Camera SetZoom accepts optional effective bounds without changing tactical callers. Ruler includes localized reference-time row, never current-engine ETA. Focused52/52 PASS; final full Client2093/2093 PASS includes canonical named tests after renaming; Release build, scoped format-verified exit0, diff PASS. Evidence D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale/20261107-camera/{focused.trx,client-full.trx,build.log,format-verified.log}. Pure/raster tests do not claim native physical-input acceptance. Self-review complete.
