---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0003-fire-lessons
title: Уроки стрельбы и залпа
stage: approved
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0003-TK-0002-move-lessons]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs
serves: [US-0003/AC-03, US-0003/AC-06]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Уроки стрельбы и залпа

[История](../EP-0028-US-0003-officer-lessons.md) · [Эпик](../../Documentation.md)

## Цель

В первом ходу объяснить выбор цели (включая «Не стрелять»), отправку огня, просмотр залпа (пропуск и «Повтор») и чтение итогов.

## Decisions

D-02, D-06 — [эпик](../../Documentation.md#решения-пользователя-обязательные). Шаги каталога: `fire.target`, `fire.submit`, `salvo.watch`, `results.read`.

## Assumptions

- По коду: цель по умолчанию уже выбрана (`TacticalBattleFireOrders.cs:45-52`); смена — клик по врагу (`TacticalBattleScreen.cs:322-326`) или `target:<id>` на карточке (`TacticalBattleScreen.Overlay.cs:116-121`); «Не стрелять» — `hold` (`:131`). Залп: Enter/Space пропускает (`TacticalBattleScreen.cs:182-196`), «Повтор» — `replay` (`TacticalBattleScreen.Overlay.cs:109`, `TacticalBattleScreen.cs:393-399`). Очередь инициативы видна справа (`PhasePanel.Salvo.cs:44`).
- Цитируемые метки — дословно из Locale: «Не стрелять»/"Hold fire" (`TacticalBattle.Ui.HoldFire`, Russian.json/English.json `:670`), «Повтор»/"Replay" (`TacticalBattle.Ui.Replay`, `:689`, без глифа ↺).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` | После TK-0002 | Добавить 4 шага по каталогу |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` / `English.json` | После TK-0002 | `Tutorial.Line.FireTarget`, `FireSubmit`, `SalvoWatch`, `ResultsRead` |
| `tests/.../Tutorial/TutorialContentTests.cs` | Общие тесты | Шаги по каталогу; цитаты меток совпадают с Locale |
| `tests/.../Tutorial/TutorialLessonFlowTests.cs` | Сценарий движения | Сценарий стрельбы и залпа T1 |

Реплики (предложение):

| Key | RU | EN |
|---|---|---|
| `FireTarget` | Цель уже выбрана. Щёлкните другого врага или строку на карточке; «Не стрелять» — пропустить выстрел. | A target is preselected. Click another enemy or a row on the card; "Hold fire" skips the shot. |
| `FireSubmit` | Стреляем по инициативе — очередь справа. Enter — открыть огонь. | Shots follow initiative — the queue is on the right. Press Enter to open fire. |
| `SalvoWatch` | Залп идёт. Enter или пробел пропускают анимацию, «Повтор» покажет её снова. | Salvo underway. Enter or Space skips the animation; "Replay" shows it again. |
| `ResultsRead` | Итоги — на карточках и в журнале боя. Enter — следующий ход. | Results are on the cards and in the battle log. Press Enter for the next turn. |

## Public API after the change

Шаги данных и ключи локали. Кода нет.

## Implementation steps

1. Шаги и реплики по таблицам.
2. `TutorialContentTests.FireAndSalvoLessonsMatchTheCatalog` и `QuotedUiLabelsMatchCurrentLocale` (RU/EN: текст `FireTarget` содержит значение `TacticalBattle.Ui.HoldFire`; `SalvoWatch` — `TacticalBattle.Ui.Replay` без «↺ »).
3. `TutorialLessonFlowTests.FirstFireAndSalvoAreTaughtThenGetOutOfTheWay`: T1 Fire — `fire.target` с подсветкой `candidate:*`; клик по другой цели → закрыт; `fire.submit` → Enter; `salvo.watch` → Enter (пропуск); `results.read` с подсветкой `button:replay` → Enter → Move T2.

## Acceptance criteria

1. Четыре шага в данных и в обеих локалях, ≤ 2 строк панели; цитаты меток совпадают с UI.
2. В живом бою шаги показываются в фазах Fire/Salvo/Results T1 и закрываются действиями игрока.

## Verification signal

Первым красным — `TutorialContentTests.FireAndSalvoLessonsMatchTheCatalog`; затем `QuotedUiLabelsMatchCurrentLocale` и flow-тест шага 3.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0003-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=fire.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Контекстные уроки, разбор, код.

## Invariants

- Существующие ключи `TacticalBattle.*` не меняются; паритет зелёный.

## Known fallout

Нет.

## Definition of Done

- Шаги и ключи добавлены; named tests зелёные.
- Отдельный коммит и `git push`.

## Self-containment check

Каталог, тексты, источники управления и меток заданы с `path:line`.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
