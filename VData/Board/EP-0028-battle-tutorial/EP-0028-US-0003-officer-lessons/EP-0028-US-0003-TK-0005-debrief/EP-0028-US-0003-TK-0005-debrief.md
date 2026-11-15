---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0005-debrief
title: Разбор итога боя
stage: implemented
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0003-TK-0004-context-lessons]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs
serves: [US-0003/AC-05, US-0003/AC-06]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Разбор итога боя

[История](../EP-0028-US-0003-officer-lessons.md) · [Эпик](../../Documentation.md)

## Цель

После боя офицер коротко подводит итог (победа, поражение, ничья/тайм-аут) и направляет на карту — кнопка «На карту» подсвечена.

## Decisions

D-01, D-06 — [эпик](../../Documentation.md#решения-пользователя-обязательные). Шаги каталога: `over.victory`, `over.defeat`, `over.other`. Поведение Over из EP-0027 сохраняется: только «На карту»/Esc (`TacticalBattleScreen.cs:96,190`; `TutorialFlowTests.cs:175-222`).

## Assumptions

- Метка кнопки: «На карту»/"Back to map" (`TacticalBattle.ToMap`, Russian.json/English.json `:234`).
- Противник в тексте — «вражеский носитель»/"the enemy carrier" (ID «Логово» не локализуется).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` | После TK-0004 | Добавить 3 шага по каталогу |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` / `English.json` | После TK-0004 | `Tutorial.Line.OverVictory`, `OverDefeat`, `OverOther` |
| `tests/.../Tutorial/TutorialContentTests.cs` | Общие тесты | Шаги по каталогу; цитата «На карту» |
| `tests/.../Tutorial/TutorialLessonFlowTests.cs` | Сценарии | Разбор в Outcome |

Реплики (предложение):

| Key | RU | EN |
|---|---|---|
| `OverVictory` | Чистая работа, командир! Вражеский носитель больше не угроза. Жмите «На карту». | Clean work, Commander! The enemy carrier is no longer a threat. Press "Back to map". |
| `OverDefeat` | Тяжёлый бой, но «Скиталец» цел. Разберём ошибки позже — жмите «На карту». | A hard fight, but Skitalets made it. We'll review it later — press "Back to map". |
| `OverOther` | Бой окончен без победителя. Отходим — жмите «На карту». | The battle ended without a winner. We're pulling back — press "Back to map". |

`OverDefeat` сверить с причинами поражения (`TacticalOutcomeReasonCode`, `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:10`): если носитель уничтожен, «Скиталец цел» неверно — тогда нейтральная формулировка без этого утверждения.

## Public API after the change

Шаги данных и ключи локали. Кода нет.

## Implementation steps

1. Шаги и реплики по таблицам (с проверкой `OverDefeat`).
2. `TutorialContentTests.DebriefLessonsMatchTheCatalogAndQuoteToMap` (RU/EN содержат значение `TacticalBattle.ToMap`).
3. `TutorialLessonFlowTests.OutcomeDebriefMatchesTheResultAndKeepsOnlyToMap`: живой бой обучения до Over — показан `over.*`, соответствующий `Outcome.Kind`; подсветка `button:primary`; `Layout.Texts` по-прежнему содержит «На карту» и не содержит «Новый бой»; клик по `button:primary` → `CloseTacticalBattle`. Остальные исходы — синтетическим наблюдением.

## Acceptance criteria

1. Три шага разбора в данных и в обеих локалях, ≤ 2 строк панели, с дословной меткой «На карту».
2. В Outcome звучит ровно один разбор по исходу; поведение кнопок Over не меняется.

## Verification signal

Первым красным — `TutorialContentTests.DebriefLessonsMatchTheCatalogAndQuoteToMap`; затем flow-тест шага 3 и регрессия `TutorialFlowTests.FinishedTutorialOffersOnlyTheMapThenTheMapIsNormal`.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0005-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=debrief.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Изменение кнопок Over (EP-0027), «Повторить обучение» (D-05), код.

## Invariants

- Паритет зелёный; существующие ключи не меняются.

## Known fallout

`TutorialFlowTests.FinishedTutorialOffersOnlyTheMapThenTheMapIsNormal` проверяет точное совпадение текста «На карту» (`:196`): реплика разбора — другая строка, тест не меняется.

## Definition of Done

- Шаги и ключи добавлены; named tests зелёные.
- Отдельный коммит и `git push`.

## Self-containment check

Каталог, тексты, метка кнопки и сценарий теста заданы.

## Результат

`src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json`: добавлены 3 шага по каталогу — `over.victory` / `over.defeat` / `over.other` (Outcome, сигнал `outcome-victory` / `outcome-defeat` / `outcome-other`, `acknowledged`, `button:primary`, 100). Итого в сценарии 17 шагов.

| Key | RU | EN |
|---|---|---|
| `Tutorial.Line.OverVictory` | Чистая работа, командир! Поле за нами. Жмите «На карту» — курс ждёт. | Clean work, Commander! The field is ours. Press “Back to map” — our course awaits. |
| `Tutorial.Line.OverDefeat` | Этот бой за ними, командир. Опыт остаётся с нами — жмите «На карту», продолжим. | This one's theirs, Commander. We keep the lessons. Press “Back to map” and carry on. |
| `Tutorial.Line.OverOther` | Бой окончен без победителя. Отходим, пока целы: жмите «На карту». | No winner this time. We'll pull back while we can — press “Back to map”. |

Все три — 2 строки панели в RU и EN, содержат дословную метку `TacticalBattle.ToMap` («На карту» / "Back to map"). `OverDefeat` не утверждает, что «Скиталец» цел (поражение возможно и по `CarrierDestroyed`), и не предлагает переиграть.

Тесты: `TutorialContentTests.DebriefLessonsMatchTheCatalogAndQuoteToMap` (RU, EN; поля шагов, цитата «На карту», в поражении нет «заново/снова/повтор/again/restart/retry/replay»); `TutorialLessonFlowTests.OutcomeDebriefMatchesTheResultAndKeepsOnlyToMap` (живой бой обучения RU до Outcome: показан `over.*` по `Outcome.Kind` (seed 1 — победа), ровно один разбор, подсветка `button:primary`, в `Layout.Texts` есть «На карту» и нет «Новый бой», нет `button:new`, клик `button:primary` → `CloseTacticalBattle`; синтетически A/B/Draw/Timeout → `over.victory`/`over.defeat`/`over.other`/`over.other`, в Results до Outcome — тишина); **финальный живой тест истории** `FollowingTheOfficerPlaysTheWholeTutorialInOrder` — настоящий бой обучения (сценарий `initial`, seed 1, Tabletop), игрок выполняет действие каждой показанной реплики (Tab, стрелки, клик по другой цели / «Не стрелять», Tab до пилота под прицелом + E/D, «Понял», Enter), без реплики — Enter: реплики идут в порядке intro → move.steer → move.submit → move.cycle → move.reachable → fire.target → fire.submit → salvo.watch → results.read, все Completed, ни одна не остаётся показанной после своего действия, каждая услышана один раз, последней — `over.*`; `PureEnterNeverWaitsForTheOfficer` дополнен: при игре только Enter бой доходит до Over, разбор показан, «зависших» шагов нет.

Живые прогоны seed 1 (временный дамп, `snap/live-follow.txt`, `../us0003-tk0004-20261114T192910/snap/live-*.txt`): по подсказкам — победа на 19-м ходу, услышано: intro, move.steer, move.submit, move.cycle, move.reachable, ctx.anomaly, fire.target, fire.submit, salvo.watch, results.read, ctx.damage, ctx.under-fire, ctx.drone-lost, over.victory; только Enter — победа на 21-м ходу.

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0005-20261114T193247/`, `DSS_VALIDATION_ROOT` = папка прогона):

- Красный старт: `DebriefLessonsMatchTheCatalogAndQuoteToMap` ×2 (шагов нет), `OutcomeDebriefMatchesTheResultAndKeepsOnlyToMap`, `PureEnterNeverWaitsForTheOfficer` (разбора нет), `FollowingTheOfficerPlaysTheWholeTutorialInOrder` (последней звучала `ctx.drone-lost`, не `over.*`) — 5 failed / 20 passed — `../us0003-tk0005-red/red.log`; после данных — 25/25 (`green.log`).
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests"` — **173/173** — `debrief.trx`, `debrief.log`, включая регрессию `TutorialFlowTests.FinishedTutorialOffersOnlyTheMapThenTheMapIsNormal` (без изменений).
- Растровые снимки панели RU/EN всех 17 реплик: `snap/panel-{ru,en}-<step>.png`, строки — `snap/lines.txt`.
- Правок кода и `UI/Screens/TacticalBattle/` нет.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие решения исполнителя: (1) Тексты переписаны под ёмкость панели и тон пользователя: победа — с восклицанием и теплом; поражение — ободряющее, без «Скиталец цел» (неверно при `CarrierDestroyed`) и без намёка на переигровку, направляет «На карту» и «продолжим»; ничья/тайм-аут — нейтрально. Победа описана как «поле за нами», а не «вражеский носитель», т. к. победа возможна и по `NoDrones`. (2) В этот тикет по указанию пользователя добавлен финальный живой тест истории `FollowingTheOfficerPlaysTheWholeTutorialInOrder` (игра по подсказкам в настоящем бою обучения) и проверка разбора/отсутствия зависших шагов в `PureEnterNeverWaitsForTheOfficer`. Файлы в `files_touched`.
