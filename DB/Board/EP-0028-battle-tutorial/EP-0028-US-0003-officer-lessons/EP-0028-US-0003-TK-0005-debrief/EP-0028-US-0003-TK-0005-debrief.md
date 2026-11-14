---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0005-debrief
title: Разбор итога боя
stage: approved
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

## Журнал

2026-11-14T15:04:43Z — тикет создан.
