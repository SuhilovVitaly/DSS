---
epic: EP-0028-battle-tutorial
story: EP-0028-US-0005-reliable-first-battle
ticket: EP-0028-US-0005-TK-0002-winnable-battle
title: Выигрываемый первый бой
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0028-US-0005-TK-0001-forced-tabletop]
implementation_files: 1
files_touched:
  - src/DeepSpaceSaga.Client/UI/SkiaWindow.cs
  - tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs
  - tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs
serves: [US-0005/AC-02]
created: 2026-11-14T15:04:43Z
revision: 1
---

# Выигрываемый первый бой

[История](../EP-0028-US-0005-reliable-first-battle.md) · [Эпик](../../Documentation.md)

## Цель

Доказать детерминированным тестом, что бой обучения выигрывает сторона игрока и при «чистом Enter», и при игре «по подсказкам»; при неудаче — выбрать seed, на котором выигрывают обе политики, и зафиксировать его.

## Decisions

D-07 — без изменения Engine и данных боя; меняется только seed клиентского запроса. Из дизайна: «прощающий» первый бой; при подборе seed — навык `gameplay-balance-check`.

## Assumptions

- Победа = `Outcome.Kind == TacticalBattleOutcomeCode.A` при `PlayerSide == "A"` (`src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:9,115`). Ничья/тайм-аут — не победа.
- Политики строятся теми же клиентскими классами, что и экран, без Engine-специфичных обходов:
  - **Enter:** Move — `TacticalBattleOrders.EditedOrders()` без правок (пусто, ИИ-тактика); Fire — `TacticalBattleFireOrders.BuildTargets()` без правок.
  - **По подсказкам** (действия, которыми закрываются уроки US-0003): T1 Move — выбрать первого своего пилота, `Adjust(0, 5)` один раз; T2 Move — `Cycle(false)` дважды; Fire T1 — для первой строки выбрать первую цель списка, отличную от цели по умолчанию (если есть); в Fire при `Incoming` на своего дрона — `ToggleEvade` при `CanEvade`, иначе `ToggleDecoy` при `CanDecoy`; остальное — как Enter.
- Поиск seed (если seed 1 не проходит): 1…200 по возрастанию, первый, где выигрывают обе политики. Если такого нет — СТОП и эскалация пользователю (изменение баланса вне объёма).

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs` | Запрос `new StartTacticalBattleRequest(TacticalBattleFlow.NewIdentity("start"), 1)` (`:1107`) | Вынести в `internal const long TutorialBattleSeed = <seed>;` (значение 1 или найденное) |
| `tests/DeepSpaceSaga.Client.Tests/Tutorial/TutorialBattleWinnabilityTests.cs` | Нет файла. Движок из сценария — `SimulationEngine.CreateFromScenarioFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"), <Scenarios/Tutorial/scenario.json>)` (как `TutorialFlowTests.cs:329`); команды — `StartTacticalBattle/MoveTacticalBattle/FireTacticalBattle` (`src/DeepSpaceSaga.Engine/SimulationEngine.TacticalBattle.cs:48-80`); приказы — `TacticalBattleOrders` (`UI/Screens/TacticalBattle/TacticalBattleOrders.cs:43-170`), `TacticalBattleFireOrders` (`TacticalBattleFireOrders.cs:33-110`) | Новые тесты двух политик |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/TutorialFlowTests.cs` | `Assert.Equal(1, battle.Flow.State.Seed)` (`:36`) | Заменить литерал на `SkiaWindow.TutorialBattleSeed` |

Бой `initial` и его цель баланса — `src/DeepSpaceSaga.Client/Data/TacticalBattle/scenarios.json:2-14` (`winMaxOneLoss`, `seeds`); данные не меняются.

## Public API after the change

`internal const long TutorialBattleSeed` в `SkiaWindow`.

## Implementation steps

1. Тест с обеими политиками на seed 1 (до 60 ходов, затем — провал «нет исхода»).
2. Если оба выигрывают — вынести константу со значением 1. Иначе — поиск seed по Assumptions, константа = найденный seed, отчёт поиска (seed → исходы) в `$RUN/seed-search.txt`.
3. Обновить `TutorialFlowTests.cs:36`.
4. Записать выбранный seed и обоснование в «Журнал»; изменение seed отражается в документации в US-0006-TK-0003 (`DB/Documentation/04-Engineering/TacticalBattle.md:89`, `DB/Documentation/02-FirstRelease/Screens/TacticalBattle.md:33`).

## Acceptance criteria

1. `EnterOnlyPlayerWinsTutorialBattle` и `HintFollowingPlayerWinsTutorialBattle` зелёные на `TutorialBattleSeed`.
2. Бой обучения стартует с `TutorialBattleSeed`; `TutorialFlowTests` зелёные.
3. Engine, `scenarios.json`, правила — без изменений (`git diff --stat` только по файлам allowlist).

## Verification signal

Первым — `TutorialBattleWinnabilityTests.EnterOnlyPlayerWinsTutorialBattle` и `HintFollowingPlayerWinsTutorialBattle` на seed 1: красные — запускают поиск seed; зелёные — фиксируют seed 1.

## Tests

```bash
RUN=D:/DeepSpaceSaga/artifacts/validation/ep0028-battle-tutorial/us0005-tk0002-<yyyyMMddTHHmmss>
mkdir -p "$RUN"
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~TutorialBattleWinnabilityTests|FullyQualifiedName~TutorialFlowTests" --results-directory "$RUN" --logger "trx;LogFileName=winnability.trx"
```

Поиск seed — отдельный временный тест/скрипт внутри `$RUN`, удаляется после работы. Process-tree cap 300000 ms.

## Out of scope

Баланс, Engine, `scenarios.json`, сложность.

## Invariants

- Детерминизм: одинаковый seed и приказы → одинаковый исход (повторный прогон теста).
- Клиент не потребляет RNG (D-07).

## Known fallout

`TutorialFlowTests.cs:36` — в allowlist.

## Definition of Done

- Шаги выполнены; named tests зелёные; seed записан в «Журнал».
- Отдельный коммит и `git push`.

## Self-containment check

Политики, критерий победы, точка seed, API движка и порядок поиска заданы.

## Журнал

2026-11-14T15:04:43Z — тикет создан.
