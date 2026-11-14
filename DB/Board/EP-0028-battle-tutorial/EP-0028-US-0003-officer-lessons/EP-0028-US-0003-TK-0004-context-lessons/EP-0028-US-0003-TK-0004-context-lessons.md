---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0003-officer-lessons
ticket: EP-0028-US-0003-TK-0004-context-lessons
title: Контекстные уроки
stage: implemented
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

## Результат

`src/DeepSpaceSaga.Client/Data/Tutorial/battle-tutorial.json`: добавлены 5 шагов — `ctx.under-fire` (Fire, minTurn 2, `under-fire`, `evade-or-decoy`, `pilot:{signal}`, **79**), `ctx.damage` (Results, minTurn 1, `own-damaged`, `acknowledged`, `pilot:{signal}`, 70), `ctx.low-mind` (Fire, minTurn 2, `low-consciousness`, `acknowledged`, `pilot:{signal}`, 75), `ctx.anomaly` (Move, **minTurn 3**, `anomaly-present`, `acknowledged`, `pilot:{signal}`, 65), `ctx.drone-lost` (Results, minTurn 1, `drone-lost`, `acknowledged`, `pilot:{signal}`, 95).

| Key | RU | EN |
|---|---|---|
| `Tutorial.Line.CtxUnderFire` | Нашего взяли на прицел. Выберите его: E — уклониться ценой сознания, D — ловушка. | One of ours is targeted. Select them: E evades at a cost in consciousness, D drops a decoy. |
| `Tutorial.Line.CtxDamage` | Есть попадание. Броня и корпус — на карточке; подбитых держите подальше от огня. | We took a hit. Armor and hull are on the card — keep damaged drones out of the line of fire. |
| `Tutorial.Line.CtxLowMind` | Пилот выдохся: на уклонение не хватит сознания. Оно возвращается с каждым ходом. | That pilot's running on empty — not enough consciousness to evade. It recovers each turn. |
| `Tutorial.Line.CtxAnomaly` | Облака на поле — не декорация: из пыли дрон не стреляет, а жар гасит выстрелы. | Those clouds aren't scenery: a drone in dust can't fire, and heat zones swallow shots. |
| `Tutorial.Line.CtxDroneLost` | Потеряли дрон. Без паники: прикройте подбитых и добивайте ослабленных врагов. | We've lost a drone. Steady now — cover the damaged and finish off weakened enemies. |

Все реплики — 2 строки панели в RU и EN (`snap/lines.txt`). Факты сверены: уклонение тратит сознание и недоступно ниже порога, сознание восстанавливается каждый ход (`04-Engineering/TacticalBattle.md:29,31`, `TacticalBattle.MindRecovery`); дрон в пыли не стреляет, температурная зона поглощает снаряды (`TacticalBattle.Anomaly.DustLines/HeatLines`, `TacticalBattle.md:23,55`); клавиши E/D совпадают с надписями карточки `TacticalBattle.Evade` «Уклониться [E]» / `TacticalBattle.Decoy` «Ловушка [D]». Числа баланса в тексты не попали.

Тесты: `TutorialContentTests.ContextLessonsMatchTheCatalog` (включая `ctx.under-fire` ниже `fire.submit` по приоритету), `HotkeysNamedInLinesAreTheBattleKeys` (RU, EN — вместо `NoLineMentionsUnverifiedHotkeys`, см. «Журнал»); `TutorialLessonFlowTests.ContextLessonsAppearOnceWhenTheirSituationArises`: (а) живой бой обучения seed 1, «терпеливый» игрок только с Enter (перед каждым Enter — паузы `minGapMs`) до Outcome — каждый `ctx.*` услышан не более одного раза и только в своей фазе; (б) синтетические наблюдения через `TutorialSession` с поставляемыми `ctx.*` шагами (снимки как в `TutorialSignalsTests`) для всех пяти сигналов: в чужой фазе — тишина, в своей — показ с верным `SignalUnitId`, после ухода из фазы и повторения ситуации — не повторяется.

Живой прогон (временный дамп, не закоммичен; `snap/live-patient.txt`, `snap/live-enter.txt`): бой обучения seed 1 только по Enter — победа (`A`) на 21-м ходу; «терпеливый» порядок реплик: `intro` (T1) → `move.cycle` (T2) → `ctx.anomaly` (T3) → `move.reachable` (T4) → `fire.target` (первая стрельба T4) → `salvo.watch` → `results.read` → `fire.submit` (T5) → `ctx.damage` → `ctx.under-fire` (T6) → `ctx.drone-lost` (T21). `ctx.low-mind` в этом бою не возникает (без уклонений сознание не падает) — покрыт синтетикой.

Проверки 2026-11-14 (артефакты `D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0003-tk0004-20261114T192910/`, `DSS_VALIDATION_ROOT` = папка прогона):

- Красный старт: `ContextLessonsMatchTheCatalog`, `HotkeysNamedInLinesAreTheBattleKeys(English)`, `ContextLessonsAppearOnceWhenTheirSituationArises` (0 из 5 `ctx.*` шагов) — `../us0003-tk0004-red/red.log`; с приоритетом 85 из каталога — падал `FirstFireAndSalvoAreTaughtThenGetOutOfTheWay` (`ctx.under-fire` перебивал `fire.submit`, первая стрельба seed 1 — на 4-м ходу) — `green.log`; после приоритета 79 — 21/21 (`green2.log`).
- `dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~DeepSpaceSaga.Client.Tests.Tutorial|FullyQualifiedName~TutorialFlowTests"` — **169/169** — `context.trx`, `context.log`.
- Растровые снимки панели RU/EN: `snap/panel-{ru,en}-<step>.png`.
- Правок кода и `UI/Screens/TacticalBattle/` нет.

## Журнал

2026-11-14T15:04:43Z — тикет создан.

2026-11-14 — реализовано (TDD), `stage: implemented`. Узкие решения исполнителя: (1) **Клавиши E/D упоминаются** в `ctx.under-fire` — по указанию пользователя 2026-11-14 (E/D опрашиваются нативно с US-0002-TK-0005); проверка `NoLineMentionsUnverifiedHotkeys` из шага 2 заменена на `HotkeysNamedInLinesAreTheBattleKeys`: E/D допустимы только в `ctx.under-fire` и должны совпадать с надписями карточки «[E]»/«[D]», в остальных репликах однобуквенных клавиш нет (кроме H). Нативное подтверждение E/D по-прежнему — US-0006-TK-0002. (2) **Приоритет `ctx.under-fire` 85 → 79**: в seed 1 первая стрельба на 4-м ходу, и угроза перебивала `fire.submit` (80) — в первой стрельбе сначала основы (цель → огонь), угроза — со следующей. Вариант `after: fire.submit` отвергнут: при никогда не показанном `fire.submit` урок угрозы заблокировался бы навсегда. (3) **`ctx.anomaly` minTurn 2 → 3**: аномалии есть в каждом бою, и во 2-м ходу урок аномалий (65) вставал между `move.cycle` и `move.reachable` (60); 2-й ход оставлен за Tab и зоной досягаемости (одна механика за ход). (4) Тексты переписаны под ёмкость панели и тон; «броня держит первые удары» из предложения не использовано (порядок применения брони/корпуса — деталь движка), вместо этого — где смотреть (карточка) и что делать. Epic `Documentation.md` (G-01) и документ истории вне `files_touched` — синхронизация в US-0006-TK-0003.
