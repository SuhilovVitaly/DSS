---
epic: EP-0030-debug-console
story: EP-0030-US-0001-global-console
title: Глобальная дебаг-консоль
stage: implemented
dependencies: []
revision: 2
---

# US-0001 — Глобальная дебаг-консоль

## Связь с техническим заданием

D-01 и D-02 [эпика](../Documentation.md): управление из любого места программы, вызов Ctrl+Alt+P. Детали A-01–A-03 и A-06 предложены для согласования.

## User story

Как оператор, я хочу открыть консоль сочетанием Ctrl+Alt+P на любом экране и поверх внутренних модальных окон, чтобы вводить команды без перехода на специальный экран. В обычном запуске клиента консоль доступна и без активной игровой сессии. Она показывает строку ввода и результат либо понятную ошибку, а ввод не активирует действия нижележащего экрана. Enter отправляет одну команду; повторное Ctrl+Alt+P или Esc закрывает консоль и возвращает фокус. Открытие и закрытие не меняют паузу и скорость игры.

## Зависимости

none. Демонстрация оболочки возможна до появления первой игровой команды: неизвестная команда возвращает диагностическое сообщение.

## Completion evidence

- **AC-01:** Ctrl+Alt+P открывает один экземпляр консоли в главном меню, на карте, в бою и поверх внутренних диалогов; применимость охватывает все экраны по контракту эпика.
- **AC-02:** удержание сочетания не переключает окно многократно; обе стороны модификаторов и RU/EN раскладки работают.
- **AC-03:** набор текста, Enter, Esc и клики по консоли не запускают бой, сохранение, движение или действия под ней; Esc закрывает только консоль.
- **AC-04:** пустой ввод ничего не делает; неизвестная команда отображает ошибку без изменения игры. После закрытия управление возвращается экрану без застрявших клавиш.
- **AC-05:** пауза/скорость до и после открытия совпадают; на экране без сессии консоль не создаёт мир. Во время перехода остаётся доступной, а небезопасное выполнение получает понятное сообщение о занятости.

## Ограничения и gaps

## Контракт подключения команд для зависимых тикетов

Проектируемая граница (ещё не существующий API): US-0001 предоставляет общий реестр команд, передачу разобранной строки обработчику и отображение строк результата с прокруткой. Реестр хранит имя, синтаксис, описание, область применимости и обработчик; справка читает тот же реестр, что диспетчер. Пути: `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs`, `DebugCommandRegistry.cs`, `DebugConsoleController.cs` (TK-0002); точка регистрации команд — `DebugCommandCatalog.cs` (TK-0003), каждая следующая история добавляет одну строку `Register`. Контекст вызова предоставляет текущую сессию и проверку её актуальности после await; закрытие/смена сессии отменяет запрос или отбрасывает его результат. Эти требования нужны тикетам [US-0004](../EP-0030-US-0004-console-discovery/EP-0030-US-0004-console-discovery.md). При декомпозиции US-0001 закрепить этот контракт; не реализовывать оболочку скрыто в тикетах справки.

Справочные команды `help` и `list_units` вынесены в US-0004. Контракт закреплён в TK-0002 (раздел Public API).

Не требуется история между запусками, автодополнение или исполнение shell-кода. Конкретные игровые команды — US-0002 (`exit_battle`) и US-0003 (`kill_drone name`). Предложения A-01–A-03/A-06 приняты исполнителем как технические допущения автономного исполнения 2026-11-16 (это не решение пользователя). Матрица экранов для native-проверки (G-02): главное меню без сессии, выбор сценария, карта, игровое меню, настройки, корабль, станция, сохранение/загрузка, бой и его модальные окна.

## Решения 2026-11-16

- Пользователь о декомпозиции историй: «Декомпозировать сам».
- Оператор: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно». Тесты тикетов пишутся в каждом тикете, запускаются после последнего тикета истории.

## Ticket map

| Тикет | Layer | Depends on | Criteria |
|---|---|---|---|
| [TK-0001 — hotkey-edge](EP-0030-US-0001-TK-0001-hotkey-edge/EP-0030-US-0001-TK-0001-hotkey-edge.md) | client | none | AC-02 |
| [TK-0002 — console-core](EP-0030-US-0001-TK-0002-console-core/EP-0030-US-0001-TK-0002-console-core.md) | client | EP-0030-US-0001-TK-0001-hotkey-edge | AC-03, AC-04, AC-05 |
| [TK-0003 — overlay-integration](EP-0030-US-0001-TK-0003-overlay-integration/EP-0030-US-0001-TK-0003-overlay-integration.md) | client | EP-0030-US-0001-TK-0002-console-core | AC-01..AC-05 |

## Review log

2026-11-16T06:50:00Z — история декомпозирована на три тикета, stage → approved (autonomous-execution). Тикеты US-0004 синхронизированы с этим контрактом.

## Story test evidence 2026-11-16

Коммиты тикетов: TK-0001 `dcd0b565`, TK-0002 `ecee1df5`, TK-0003 `583f75a0` (ветка `feature/ep-0030-debug-console`, push выполнен).

Прогон тестов истории (один раз после последнего тикета, по инструкции оператора), process-tree cap 300000 ms:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter "FullyQualifiedName~KeyboardEdgeTrackerTests|FullyQualifiedName~DebugConsole|FullyQualifiedName~KeyboardRoutingTests|FullyQualifiedName~BattleNavigationTests" --results-directory D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us0001-20261116T070632Z/ --logger trx
```

Результат: Total 95, Passed 94, Failed 1, Skipped 0. TRX: `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us0001-20261116T070632Z/sushi_VITA-CASTLE_2026-11-16_09_06_38_net8.0.trx`.

| Класс | Passed | Failed |
|---|---|---|
| `KeyboardEdgeTrackerTests` (TK-0001: 4 варианта `Ctrl_Alt_P_reports_single_P_edge_with_modifiers`, `Held_P_reports_no_second_edge`, `Bare_P_reports_edge_without_modifiers`) | 34 | 0 |
| `DebugConsoleControllerTests` (TK-0002, 12 названных тестов + `ParsedArgumentsReachTheHandler`, с вариантами Theory) | 21 | 0 |
| `DebugConsoleWindowTests` (TK-0003, 8 названных тестов) | 8 | 0 |
| `KeyboardRoutingTests` (регрессия) | 17 | 0 |
| `BattleNavigationTests` (регрессия) | 14 | 1 |

Единственный отказ — `BattleNavigationTests.ExitOfAStaleBattleExitsTheLiveEngineBattleInstead` (строка 145: `Assert.True(live.Accepted)` — Engine отклоняет повторный `StartTacticalBattle`). Отказ существовал до эпика: на базовом коммите `43a6fecf` (до кода EP-0030) тот же тест падает так же — TRX `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/us0001-20261116T070632Z/baseline-43a6fecf/sushi_VITA-CASTLE_2026-11-16_09_07_13_net8.0.trx`. Тест не касается консоли; в рамках US-0001 не исправлялся (вне scope), передан как остаточный риск.

Native-проверка (матрица экранов G-02, RU/EN раскладки): NOT RUN — меньший монитор для `DSS_VALIDATION_SMALL_MONITOR=1` в этой сессии не подтверждён.
