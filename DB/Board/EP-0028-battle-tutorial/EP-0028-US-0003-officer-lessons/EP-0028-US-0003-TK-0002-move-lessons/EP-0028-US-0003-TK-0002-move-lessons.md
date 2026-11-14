---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0002-move-lessons
title: Уроки движения
stage: implemented
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0003-TK-0001-officer, EP-0028-US-0002-TK-0003-target-highlight, EP-0028-US-0002-TK-0004-quiet-rules]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs
serves: [US-0003/AC-02, US-0003/AC-06]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Уроки движения

[История](../EP-0028-US-0003-officer-lessons.md) · [Эпик](../../Documentation.md)

## Цель

Научить ходу движения в первые два хода: проложить курс и отправить ход (T1), переключать пилотов и понимать зону досягаемости (T2). Каждый шаг закрывается действием игрока.

## Decisions

D-02, D-06 — [эпик](../../Documentation.md#решения-пользователя-обязательные). Шаги — из [каталога уроков](../EP-0028-US-0003-officer-lessons.md#каталог-уроков): `move.steer`, `move.submit`, `move.cycle`, `move.reachable`.

## Assumptions

- Управление по коду: стрелки ←/→ — курс ±5°, ↑/↓ — скорость ±2 (`UI/Screens/TacticalBattle/TacticalBattleScreen.cs:206-214`), перетаскивание конца пути (`:316-320`), Tab/Shift+Tab — переключение (`:200-205`), Enter — отправка (`TacticalBattleFlow.cs:314`). Неотредактированные дроны летят по ИИ (`TutorialFlowTests.cs:175-185`).
- В тексте стрелки называются словами (глифы стрелок зависят от шрифта; `ControlText.SafeSymbols`, `UI/Controls/DropDown.cs:264`).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` | После TK-0001: officer, limits, `intro` | Добавить 4 шага по каталогу |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` / `English.json` | Ключи TK-0001 | `Tutorial.Line.MoveSteer`, `MoveSubmit`, `MoveCycle`, `MoveReachable` |
| `tests/.../Tutorial/TutorialContentTests.cs` | Общие тесты содержимого (TK-0001) | Проверка наличия и параметров 4 шагов |
| `tests/.../Tutorial/TutorialLessonFlowTests.cs` | Flow-тест `intro` (TK-0001) | Сценарий T1→T2 |

Реплики (предложение):

| Key | RU | EN |
|---|---|---|
| `MoveSteer` | Тяните треугольник на конце пути или жмите стрелки: влево-вправо — курс, вверх-вниз — скорость. | Drag the triangle at the path's end, or use the arrow keys: left/right to turn, up/down for speed. |
| `MoveSubmit` | Остальные дроны полетят по тактике крыла. Готово — Enter или кнопка хода. | The other drones follow the wing's tactic. When ready, press Enter or the turn button. |
| `MoveCycle` | Tab переключает пилотов по очереди — так удобно проверить каждого. | Tab cycles through your pilots — an easy way to check each one. |
| `MoveReachable` | Подсвеченная зона — куда дрон успеет за ход. Чем выше скорость, тем шире разворот. | The highlighted zone is where the drone can reach this turn. More speed means wider turns. |

Утверждение о развороте сверить с `TacticalMovementLimits`/`TurnAtMinimum` (`TacticalBattleOrders.cs:8-10`) при реализации; при расхождении — переформулировать, не меняя ключ.

## Public API after the change

Новые шаги данных и ключи локали. Кода нет.

## Implementation steps

1. Добавить шаги по каталогу (when/complete/anchor/priority).
2. Добавить реплики в RU и EN.
3. `TutorialContentTests.MoveLessonsMatchTheCatalog` — шаги присутствуют с заданными полями.
4. `TutorialLessonFlowTests.FirstTwoTurnsTeachMovementByPlayerActions`: живой Tabletop-бой; T1 — Tab (intro закрыт) → `move.steer` показан с подсветкой `endpoint:*` → стрелка вправо (`path-edited`) → `move.submit` с подсветкой `button:primary` → Enter → фаза ушла. В T2 Move — `move.cycle`; Tab дважды → закрыт; затем `move.reachable`. Между шагами двигать `ClockMilliseconds` на ≥ `minGapMs`.
5. `TutorialLessonFlowTests.PureEnterNeverWaitsForTheOfficer` — только Enter до Over: бой завершается, ни одно состояние директора не мешает отправке.

## Acceptance criteria

1. Четыре шага урока движения в данных и в обеих локалях, ≤ 2 строк панели.
2. В живом бою шаги показываются по порядку и закрываются действиями игрока; Enter-only игра не задерживается.

## Verification signal

Первым красным — `TutorialContentTests.MoveLessonsMatchTheCatalog`; затем flow-тесты шагов 4–5. Общие проверки (паритет, вмещаемость, загрузка без предупреждений) уже покрывают новые ключи.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0002-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=move.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Стрельба, залп, контекст, разбор; код.

## Invariants

- Существующие ключи не меняются; паритет зелёный.
- Ввод боя не блокируется (D-02).

## Known fallout

Нет.

## Definition of Done

- Шаги и ключи добавлены; named tests зелёные.
- Отдельный коммит и `git push`.

## Self-containment check

Каталог, тексты, источники управления и сценарий теста заданы.

## Результат

`src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json`: добавлены 4 шага по [каталогу](../EP-0028-US-0003-officer-lessons.md#каталог-уроков) без изменений полей — `move.steer` (Move, turn 1, after `intro`, `path-edited`, `endpoint:{selected}`, 90), `move.submit` (Move, turn 1, after `move.steer`, `phase-left`, `button:primary`, 80), `move.cycle` (Move, turn 2, `pilot-cycled`, `pilot:{own}`, 70), `move.reachable` (Move, minTurn 2, after `move.cycle`, `path-edited`, `reachable`, 60); `limits.maxLinesPerPhase` 2 → **3** (см. «Журнал»).

| Key | RU | EN |
|---|---|---|
| `Tutorial.Line.MoveSteer` | Дрон закончит ход там, где кончается путь. Тяните его конец или жмите стрелки. | A drone ends its turn where its path ends. Drag that end, or steer with the arrow keys. |
| `Tutorial.Line.MoveSubmit` | Остальных поведёт тактика крыла — они своё дело знают. Готовы? Жмите Enter. | The rest will fly the wing's tactic — they know their job. Ready? Press Enter. |
| `Tutorial.Line.MoveCycle` | Tab переключает пилотов по кругу — так быстрее проверить курс каждого. | Tab cycles through your pilots — a quick way to check everyone's course. |
| `Tutorial.Line.MoveReachable` | Пунктирная зона — всё, куда этот дрон долетит за ход. Точку выбирайте внутри. | The dashed zone is everywhere this drone can reach this turn. Pick your point inside it. |

Все реплики — 2 строки панели в RU и EN (`snap/lines.txt`).

Тесты: `TutorialContentTests.MoveLessonsMatchTheCatalog` (поля 4 шагов и `maxLinesPerPhase ≥ 3`), `MoveLessonsNameTheirControls` (Enter, Tab в RU/EN); `TutorialLessonFlowTests.FirstTwoTurnsTeachMovementByPlayerActions` (живой бой обучения: T1 — `intro` → Tab → после `minGapMs` `move.steer` с подсветкой `endpoint:<выбранный>` → стрелка вправо → `move.submit` с подсветкой `button:primary` → Enter → фаза ушла, шаг Completed; T2 Move — `move.cycle` (подсветка `pilot:*`), Tab, Tab → Completed → `move.reachable` с подсветкой `reachable` → стрелка вверх → Completed; порядок в `Heard` intro < steer < submit < cycle < reachable), `PureEnterNeverWaitsForTheOfficer` (только Enter до Over: каждый Enter уводит из фазы/хода, показанная реплика не переживает свою фазу, `intro` прозвучал и истёк). Помощники `LiveTutorial.Press/Enter/EnterUntil/Pause`.

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0002-20261114T192142/`, `DSS_VALIDATION_ROOT` = папка прогона):

- Красный старт: `MoveLessonsMatchTheCatalog`, `MoveLessonsNameTheirControls` (шагов нет) и `FirstTwoTurnsTeachMovementByPlayerActions` (`move.steer` не показан) — `../us0003-tk0002-red/red.log`; с шагами и пределом 2 — `move.submit` не показан (`cap2.log`); с пределом 3 — зелёные (`cap3b.log`).
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests"` — **161/161** — `move.trx`, `move.log`.
- Растровые снимки панели RU/EN: `snap/panel-{ru,en}-<step>.png`; строки — `snap/lines.txt` (временный тест-дамп, не закоммичен).
- Правок кода и `UI/Screens/TacticalBattle/` нет.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие решения исполнителя: (1) **`limits.maxLinesPerPhase` в поставляемом сценарии поднят с 2 до 3.** Первый ход Move учит три действия (выбор пилота → курс → отправка), каждое звучит только после действия игрока; при пределе 2 на пару (фаза, ход) `move.submit` подавлялся (доказано прогоном `us0003-tk0002-red/cap2.log`). Значение — в данных (`limits`), код и умолчание `TutorialLimits` не менялись; интервал 2500 мс и сворачивание 15000 мс прежние. Фазы, где шаг закрывается сменой фазы (`phase-left`), всё равно держат одну реплику до конца фазы, поэтому лишнего шума предел 3 не добавляет. Epic `Documentation.md` (D-*/A-*) вне `files_touched` — не правился; синхронизация — US-0006-TK-0003. (2) Тексты переписаны под ёмкость панели (≤ 2 строк в RU и EN) и тон пользователя: «зачем» + конкретный элемент управления (конец пути/стрелки, Enter, Tab, пунктирная зона). Утверждение о «шире разворот» из предложения не использовано (не проверено по `TacticalMovementLimits`); вместо него — факт отрисовки: зона досягаемости рисуется пунктиром (`BattleMapRenderer.Templates.cs:15-23`). (3) Во flow-тесте отправка хода — через `Enter()` с продвижением часов анимации движения: во время анимации кадр ещё показывает Move T1.
