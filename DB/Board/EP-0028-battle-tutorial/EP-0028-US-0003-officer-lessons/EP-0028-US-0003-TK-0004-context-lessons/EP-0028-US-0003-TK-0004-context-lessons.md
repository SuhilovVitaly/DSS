---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0004-context-lessons
title: Контекстные уроки
stage: approved
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0003-TK-0003-fire-lessons]
implementation_files: 3
files_touched:
  - src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json
  - src/DeepSpaceSaga.Client/Data/Locale/Russian.json
  - src/DeepSpaceSaga.Client/Data/Locale/English.json
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialContentTests.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialLessonFlowTests.cs
serves: [US-0003/AC-04, US-0003/AC-06]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Контекстные уроки

[История](../EP-0028-US-0003-officer-lessons.md) · [Эпик](../../Documentation.md)

## Цель

Одноразовые подсказки «вовремя»: когда по нашему дрону целятся, когда он получил урон, когда пилот вымотан, когда на поле аномалия и когда дрон потерян. Учить «зачем», а не только кнопку.

## Decisions

D-02, D-06 — [эпик](../../Documentation.md#решения-пользователя-обязательные). Шаги каталога: `ctx.under-fire`, `ctx.damage`, `ctx.low-mind`, `ctx.anomaly`, `ctx.drone-lost`. Сигналы — US-0001-TK-0003.

## Assumptions

- Факты для текстов: уклонение ограничено сознанием (`TacticalBattleFireOrders.cs:49`, `rules.json:47` `minConsciousness`), сознание восстанавливается каждый ход (`TacticalBattle.MindRecovery`, English.json `:59`); в пыли дрон не получает целей (`TacticalBattleFireOrders.cs:47`); ловушка доступна при наличии зарядов (`TacticalBattleFireOrders.cs:14`). Числа баланса в текст не попадают.
- Клавиши E/D не упоминаются (G-01); названия кнопок — «Уклониться»/"Evade", «Ловушка»/"Decoy" (`TacticalBattle.Evade`/`TacticalBattle.Decoy` без суффикса клавиши, Russian.json/English.json `:35,747`).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json` | После TK-0003 | Добавить 5 шагов по каталогу |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` / `English.json` | После TK-0003 | `Tutorial.Line.CtxUnderFire`, `CtxDamage`, `CtxLowMind`, `CtxAnomaly`, `CtxDroneLost` |
| `tests/.../Tutorial/TutorialContentTests.cs` | Общие тесты | Шаги по каталогу; запрет упоминания E/D |
| `tests/.../Tutorial/TutorialLessonFlowTests.cs` | Сценарии T1–T2 | Сценарии контекстных уроков |

Реплики (предложение):

| Key | RU | EN |
|---|---|---|
| `CtxUnderFire` | По нашему дрону целятся. На его карточке: «Уклониться» тратит сознание, «Ловушка» сбивает наведение. | One of ours is targeted. On its card, "Evade" costs consciousness and a "Decoy" throws off the aim. |
| `CtxDamage` | Есть попадание. Сначала удар держит броня, потом страдает корпус — берегите подбитых. | We took a hit. Armor soaks the first blows, then the hull suffers — protect damaged drones. |
| `CtxLowMind` | Пилот вымотан: сознания не хватает на уклонение. Оно восстанавливается каждый ход. | That pilot is drained: not enough consciousness to evade. It recovers every turn. |
| `CtxAnomaly` | На поле аномалии. Из пылевого облака дрон не стреляет — прокладывайте курс в обход. | Anomalies ahead. A drone inside a dust cloud can't fire — plot your course around them. |
| `CtxDroneLost` | Потеряли дрон. Держите остальных вместе — поодиночке их выбивают быстрее. | We lost a drone. Keep the rest together — alone they go down faster. |

Утверждения о броне/корпусе сверить с `DB/Documentation/04-Engineering/TacticalBattle.md` при реализации; при расхождении — переформулировать, не меняя ключ.

## Public API after the change

Шаги данных и ключи локали. Кода нет.

## Implementation steps

1. Шаги и реплики по таблицам.
2. `TutorialContentTests.ContextLessonsMatchTheCatalog`; `NoLineMentionsUnverifiedHotkeys` — ни одна реплика `Tutorial.Line.*` RU/EN не содержит отдельных токенов `E`, `D`, `[E]`, `[D]`.
3. `TutorialLessonFlowTests.ContextLessonsAppearOnceWhenTheirSituationArises`: живой бой обучения (seed 1, Enter до Over): каждый показанный `ctx.*` показан ровно один раз, никогда одновременно с другой репликой, только в своей фазе. Для сигналов, не возникших в seed 1, — синтетические наблюдения через `TutorialSession` с поставляемым сценарием (снимки строятся так же, как в `TutorialSignalsTests`).

## Acceptance criteria

1. Пять шагов в данных и в обеих локалях, ≤ 2 строк панели; без клавиш E/D.
2. Каждый контекстный урок появляется не более одного раза и только при своём сигнале.

## Verification signal

Первым красным — `TutorialContentTests.ContextLessonsMatchTheCatalog`; затем `NoLineMentionsUnverifiedHotkeys` и flow-тест шага 3.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0004-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=context.trx"
```

Process-tree cap 300000 ms.

## Out of scope

Разбор итога; код; исправление E (G-01).

## Invariants

- Паритет зелёный; существующие ключи не меняются.

## Known fallout

Нет.

## Definition of Done

- Шаги и ключи добавлены; named tests зелёные.
- Отдельный коммит и `git push`.

## Self-containment check

Каталог, тексты, источники фактов и способ проверки редких сигналов заданы.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
