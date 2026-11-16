---
epic: EP-0030-debug-console
story: EP-0030-US-0002-exit-tutorial-battle
title: Выход из обучающего боя командой exit_battle
stage: implemented
dependencies: [EP-0030-US-0001-global-console]
revision: 2
---

# US-0002 — Выход из обучающего боя

## Связь с техническим заданием

D-03 [эпика](../Documentation.md): `exit_battle` закрывает обучающий бой и показывает солнечную систему. Это дебаг-исключение из обычного запрета раннего выхода [EP-0027](../../EP-0027-tutorial-start/Documentation.md), с сохранением постоянных потерь по [EP-0029](../../EP-0029-unified-carrier-and-torpedoes/Documentation.md).

## User story

Как оператор, я хочу ввести `exit_battle` в консоли во время обучения, чтобы сразу выйти на карту солнечной системы без доигрывания боя. Команда без аргументов доступна на паузе, в любой фазе боя и поверх его внутренних окон. Незавершённый бой прерывается без объявления победы, с сохранением уже понесённых потерь и расхода ресурсов. После подтверждённого завершения операции окна боя и консоль закрываются, а карта той же сессии становится доступной для управления. При ошибке я вижу её причину и могу повторить операцию без двойного применения последствий.

## Зависимости

EP-0030-US-0001-global-console — глобальный вызов, ввод и вывод результата команды.

## Completion evidence

- **AC-01:** во время незавершённого обучения с живым носителем команда без аргументов закрывает бой и открывает карту существующего мира, без промежуточного главного меню, нового мира или экрана победы.
- **AC-02:** тот же результат достигается в фазах движения/огня, на паузе, во время анимации и с открытым журналом/подтверждением выхода; устаревшие события боя больше не влияют на карту.
- **AC-03:** потери и расход торпед сохраняются ровно один раз; уроки не отмечаются фиктивно пройденными. Карта допускает штатное сохранение после завершения выхода.
- **AC-04:** при повторном Enter операция не дублируется; ошибка завершения или checkpoint не даёт ложного успеха/перехода на карту. Повтор после восстановления записи не повторяет settlement.
- **AC-05:** вне обучения, в обычном бою и без сессии команда возвращает понятную ошибку без изменения состояния; лишние аргументы отклоняются. Ответ старой сессии не закрывает бой новой.
- **AC-06:** после естественного окончания обучения команда выполняет штатное закрытие уже завершённого боя без изменения его результата. Обычный Esc и другие кнопки сохраняют прежнее поведение.

## Ограничения и gaps

**G-01 решён** пользователем 2026-11-16: «Штатный экран гибели». При уничтоженном носителе `exit_battle` прерывает бой, а существующий guard `EnsureShipDestroyedScreen` (`src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:1281-1294`) показывает экран гибели вместо карты. Команда не воскрешает корабль. Предложения A-04–A-06 приняты исполнителем как технические допущения автономного исполнения 2026-11-16 (это не решение пользователя). Автоматические и native-проверки перечислены в эпике как будущие; их результаты не заявлены.

## Решения 2026-11-16

- **G-01** (пользователь): «Штатный экран гибели» — исключение из D-03 для погибшего носителя; guard не обходится.
- Пользователь о декомпозиции: «Декомпозировать сам».
- Оператор: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».

## Ticket map

| Тикет | Layer | Depends on | Criteria |
|---|---|---|---|
| [TK-0001 — exit-battle](EP-0030-US-0002-TK-0001-exit-battle/EP-0030-US-0002-TK-0001-exit-battle.md) | client | none; prerequisite US-0001 | AC-01..AC-06 |

## Review log

2026-11-16T06:50:00Z — G-01 решён, история декомпозирована на один тикет, stage → approved (autonomous-execution).

## Story test evidence 2026-11-16

Тикет TK-0001 — единственный тикет истории; тесты запущены после реализации, до коммита тикета (коммит `EP-0030 US-0002 TK-0001: add exit_battle debug command` включает эту запись).

Прогон тестов истории (process-tree cap 300000 ms):

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~ExitBattleDebugCommandTests|FullyQualifiedName~DebugConsole|FullyQualifiedName~Ep0029Us0012Tk0003Tests|FullyQualifiedName~BattleNavigationTests|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~TutorialFlowTests" --results-directory D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us0002-20261116T071521Z/ --logger trx
```

Результат: Total 98, Passed 95, Failed 3, Skipped 0. TRX: `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us0002-20261116T071521Z/sushi_VITA-CASTLE_2026-11-16_09_15_28_net8.0.trx`.

| Класс | Passed | Failed |
|---|---|---|
| `ExitBattleDebugCommandTests` (TK-0001, 11 названных тестов, AC-01..AC-06) | 11 | 0 |
| `DebugConsoleControllerTests` (регрессия US-0001) | 21 | 0 |
| `DebugConsoleWindowTests` (регрессия US-0001) | 8 | 0 |
| `Ep0029Us0012Tk0003Tests` (регрессия: settlement/checkpoint/гибель) | 4 | 0 |
| `KeyboardRoutingTests` (регрессия) | 17 | 0 |
| `BattleNavigationTests` (регрессия) | 14 | 1 |
| `TutorialFlowTests` (регрессия) | 20 | 2 |

Предыдущий прогон той же команды (`us0002-20261116T071327Z`, TRX `sushi_VITA-CASTLE_2026-11-16_09_13_38_net8.0.trx`): Total 98, Passed 94, Failed 4 — дополнительно падал `ExitBattleDebugCommandTests.FinishedTutorialClosesNormally` из-за слишком строгого сравнения журнала обучения (штатное событие `expired`); исправлено в тесте (проверяется отсутствие `abandoned`), код не менялся.

Отказы, существующие до эпика (не связаны с консолью, не исправлялись — вне scope):

- `BattleNavigationTests.ExitOfAStaleBattleExitsTheLiveEngineBattleInstead` (строка 145, Engine отклоняет повторный `StartTacticalBattle`) — известен с US-0001.
- `TutorialFlowTests.TutorialBattleAlwaysUsesTabletopAndKeepsSkinPreference` и `TutorialFlowTests.TutorialBattleCarriesTutorialSessionAndF8BattleDoesNot` — после завершённого обучения обычный бой с карты не стартует (остаётся `TacticalBattleSetupScreen`). Воспроизводятся без кода US-0002 на `688c0ba5` (TRX `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us0002-20261116T071327Z/baseline-688c0ba5/sushi_VITA-CASTLE_2026-11-16_09_14_46_net8.0.trx`, 3 отказа вместе с первым) и на до-эпиковом `43a6fecf` (TRX `.../us0002-20261116T071327Z/baseline-43a6fecf/sushi_VITA-CASTLE_2026-11-16_09_15_07_net8.0.trx`, те же 2 отказа). Передаются как остаточный риск.

Native-проверка (консоль → `exit_battle` → карта той же системы): NOT RUN — меньший монитор для `DSS_VALIDATION_SMALL_MONITOR=1` в этой сессии не подтверждён.
