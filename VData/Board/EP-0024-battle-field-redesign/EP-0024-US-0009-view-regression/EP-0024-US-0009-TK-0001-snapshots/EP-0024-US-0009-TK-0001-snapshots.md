---
epic: EP-0024-battle-field-redesign
story: EP-0024-US-0009-view-regression
ticket: EP-0024-US-0009-TK-0001-snapshots
title: Снимки `tabletop` и проверка Classic
stage: done
layer: client-tests
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0024-US-0007-TK-0002-torpedo-fx, EP-0024-US-0007-TK-0001-salvo, EP-0024-US-0008-TK-0001-camera, EP-0024-US-0004-TK-0002-clouds-cache, EP-0024-US-0005-TK-0001-templates]
files_touched: 4
serves: [US9-AC1, US9-AC2]
created: 2026-11-12T13:00:00Z
revision: 1
---

# Снимки `tabletop` и проверка Classic

## Why

Новый вид поля нужно зафиксировать, не меняя эталонов Classic.

## Decisions

Применяются [D-01–D-11](../../Documentation.md#решения-пользователя). Нормативный вид — [UiSpec](../../UiSpec.md); всё, что там не названо, остаётся как в Classic на базе. Материалы — [Materials](../../Materials/README.md). Утверждение эпика пользователем — в [review log эпика](../../Documentation.md#review-log).

## Assumptions

A-13.

## Code context

Строки — `main` @ `d7895f73`; перед правкой сверить с текущей веткой.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/TabletopSnapshots.cs` | — | Новый файл по образцу `Ep0019Snapshots`: рендер сцен в скине `tabletop`, сравнение, принятие по `DSS_EP0024_ACCEPT_SNAPSHOTS`. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Snapshots/tabletop` | — | 8 PNG. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/View/Ep0022SkinScenesTests.cs` | :38-80 список скинов | Добавить `tabletop`. |
| `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj` | :26 `TacticalBattle\View\Snapshots\*.png` | Добавить правило для `TacticalBattle\View\Snapshots\tabletop\*.png`. |

## Public API after the change

—

## Implementation steps

1. Тест снимков нового скина.
2. Принять снимки после просмотра и сверки с эталонами.
3. Прогнать категории Classic без принятия.

## Out of scope

—

## Invariants

15 снимков Classic не пересоздаются.

## Tests

Проект `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Именованные проверки:

- `TabletopSnapshots`
- `Ep0022SkinScenesTests`
- `Ep0019ViewTests`

Команды из корня DSS. Заменить `<run>` уникальным ID, создать каталог результатов заранее, ограничить дерево каждого тестового процесса 300000 ms.

```powershell
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
$env:DSS_EP0024_ACCEPT_SNAPSHOTS = "1"; dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSnapshots" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us9-accept"; Remove-Item Env:DSS_EP0024_ACCEPT_SNAPSHOTS
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --no-restore --filter "FullyQualifiedName~TabletopSnapshots|FullyQualifiedName~Ep0022SkinScenesTests|Category=Ep0019View|Category=Ep0020View" --results-directory "D:/DeepSpaceSaga/artifacts/validation/ep-0024/<run>/us9-verify" --logger "trx;LogFileName=client.trx"
git diff --check
```

## Definition of Done

- Выполнены все шаги в пределах Code context (расширение scope — по EpicExecutionPrompt с записью причины в карточке).
- Для US9-AC1, US9-AC2 записано, какую часть критерия демонстрирует тикет и каким результатом это подтверждено.
- Именованные проверки, сборка и scoped форматирование выполнены; фактические результаты и пути artifacts записаны в раздел «Исполнение».
- Соблюдены инварианты.

## Self-containment check

Сцены и механизм принятия указаны; эталоны — в материалах.

## Исполнение

Не начато.

## Scope и зависимости перед реализацией

Расширение: Ep0019Scenes.cs принимает необязательный settingsPath для настоящего Tabletop screen через существующий Fixture/SkiaWindow catalog. Диагностический профиль с копией готовых PNG создаётся только в DSS_VALIDATION_ROOT и удаляется после сцены. Поскольку US11–13 уже реализованы, исключения сравнения колонки из TK-0002 выполняются здесь: Classic/Contrast/Xenon сохраняют полное равенство, Tabletop отдельно сравнивает геометрию и тексты правых окон, проверяет ширину карточки150/высоту40. TK-0002 дополняет это точными пробами. US4-cache реализован, cold12ms acceptance открыт; он не блокирует снимки доступного runtime по D-12.

При review до принятия снимков найдено расхождение §2.5: кольцо выбранного дрона использовало Classic Yellow вместо Tabletop Gold. В scope добавлен BattleMapRenderer.cs для замены ровно этого цвета на TTGold; геометрия и Classic не меняются.

Подсказка UiSpec «пробел — пропустить» выявила недоступный Space: общий world-key guard его поглощал. В scope добавлены TacticalBattleScreen.cs и TabletopSalvoTests.cs: только Tabletop, только полёт, без модификаторов/repeat/confirm/busy — Space пропускает презентацию тем же AdvanceSalvo, что Enter. В остальных состояниях мировой shortcut остаётся заблокированным.


### Результат us9-tk1

Приняты и затем проверены 8 полных сцен 1920×1080 через реальный Tabletop screen и Engine seed1. Визуально сверены поле и панели с Materials/Reference, в том числе вырезки панелей предыдущих US13; реальные портреты/шрифт соответствуют A-15/A-16. Classic PNG не изменены. Регрессия сцен/ввода 93/93 PASS; после уточнения opt-in по metrics.slim вместо ID отдельная проверка Space 3/3 PASS (us9-keyboard-final/client.trx). Space пропускает только полёт без world side effects, включая переименованный slim-пакет. Дополнительно исправлен цвет кольца выбранного дрона по UiSpec.

Проверки: 93/93 PASS; сборка Client/tests выполнена тестовой командой. Scoped whitespace и diff check PASS. Evidence: `D:/DeepSpaceSaga/artifacts/validation/ep-0024/20261112-implementation-01/us9-tk1/client.trx`, `us9-tk1-test.log`, `us9-tk1-format.log` в том же run. Самопроверка diff выполнена; это не независимое review. Native/product gates отдельно в ExecutionStatus.
