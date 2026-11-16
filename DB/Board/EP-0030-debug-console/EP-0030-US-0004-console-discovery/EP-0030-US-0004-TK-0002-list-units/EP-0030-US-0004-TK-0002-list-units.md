---
epic: EP-0030-debug-console
story: EP-0030-US-0004-console-discovery
ticket: EP-0030-US-0004-TK-0002-list-units
title: Команда list_units — участники текущего боя
stage: approved
approval_kind: autonomous-execution
implementation_status: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0030-US-0004-TK-0001-help]
prerequisite_stories: [EP-0030-US-0001-global-console]
files_touched: 3
serves: [AC-03, AC-04, AC-05]
created: 2026-11-16T06:21:45Z
revision: 2
---

# TK-0002 — Команда list_units

## Why

Дать оператору полный список участников текущего боя для диагностики и различения целей. Покрывает AC-03/04/05 [истории](../EP-0030-US-0004-console-discovery.md); после регистрации команда автоматически появляется в help.

## Decisions

Пользователь: «`list_units`  help - показывает все команды дебага консоли - добавь тикеты на оба». Записано 2026-11-16T06:21:45Z. Команда принята из предложения «имена, стороны и состояние всех участников боя».

Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно» — разрешение на реализацию. Пользователь, 2026-11-16, G-04: «Id или подпись на карте» — `kill_drone` принимает Unit Id или подпись `Build?.PilotName ?? Id`; `list_units` показывает оба значения раздельно.

## Assumptions

Без аргументов, только чтение одного актуального immutable snapshot. Все Units обеих сторон, включая Drone, Carrier и неактивных; ничего не фильтровать по предполагаемому термину «тэг». Поля: Unit ID, PilotName отдельно или «—», Side, Kind, State, Hull/MaxHull. При отсутствии отдельного имени юнита Id остаётся его идентификатором; PilotName выводится отдельной колонкой; по G-04 он также является подписью цели для `kill_drone`. Сортировка Side/Id Ordinal, null Build допустим. Заголовок содержит BattleId/Revision/Turn/Phase. Вывод — текст с прокруткой, без скрытого лимита строк.

## Code context

Revision 2 (2026-11-16): синхронизировано с контрактом [US-0001](../../EP-0030-US-0001-global-console/EP-0030-US-0001-global-console.md). Точка регистрации — `DebugCommandCatalog.cs`; реестр и контроллер — read-only.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` | Создаётся US-0001 TK-0003; help добавлен в TK-0001 | Одна строка `Register(new ListUnitsDebugCommand())` |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/ListUnitsDebugCommand.cs` | Новый | Асинхронное чтение снимка и форматирование списка |
| `tests/DeepSpaceSaga.Client.Tests/DebugConsole/ListUnitsDebugCommandTests.cs` | Новый | Тесты вывода, отказов и гонок контекста |

Read-only sources: `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:51,151` (поля юнитов/снимка), `src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs:21` (`GetTacticalBattleStateAsync`), `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs` (`IDebugCommand`, `DebugCommandContext`, `DebugCommandResult`), `DebugCommandRegistry.cs`, `DebugConsoleController.cs` (US-0001 TK-0002). Эти файлы не менять. Зависимость — [TK-0001](../EP-0030-US-0004-TK-0001-help/EP-0030-US-0004-TK-0001-help.md).

## Public API after the change

Игровые/transport API без изменений. `ListUnitsDebugCommand : IDebugCommand`:

```csharp
DebugCommandDescriptor Descriptor => new("list_units", "list_units", "Показать всех участников текущего боя", "текущий бой");
Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken);
internal static IReadOnlyList<string> Format(TacticalBattleSnapshot snapshot); // чистое форматирование
```

Параметр `isCurrentBattle` revision 1 удалён: актуальность после await проверяется через `context.IsCurrent` (захваченная при создании контекста сессия совпадает с текущей). Прямой доступ к Engine запрещён.

## Implementation steps

1. Зарегистрировать `list_units` в `DebugCommandCatalog.CreateDefault()`; метаданные автоматически попадают в help.
2. Лишние аргументы отклонить с `Синтаксис: list_units`, не запрашивая сессию. При `context.Session == null` вернуть «Нет активной сессии».
3. Запросить `context.Session.GetTacticalBattleStateAsync(cancellationToken)` один раз. Null: «Нет активного боя или данные недоступны»; не выдавать null за достоверное отсутствие боя, поскольку текущая сессия преобразует ошибки чтения в null.
4. После await проверить `context.IsCurrent`; при смене сессии/отмене не выводить старый список как текущий (контроллер дополнительно отбрасывает устаревший результат). Ошибку отобразить без публикации прежнего снимка.
5. Для актуального snapshot вывести заголовок и отсортированные строки всех Units; пустой массив — «Участников нет». Читать Units, не BeforeSalvoUnits/FireSteps и не кадр анимации.
6. Добавить тесты и проверку появления list_units в help. Не менять игровое состояние.

## Out of scope

Новая команда Engine, изменение DTO, изменение имён или поиска kill_drone, фильтры, экспорт, будущие приказы/скрытые планы AI, оболочка и прокрутка (US-0001).

## Invariants

Engine остаётся авторитетным (`DB/Documentation/01-Requirements/EngineRequirements.md:11`); используются только публичные snapshots (`TacticalBattleSnapshot.cs:151`). Один результат относится к одной revision. Нет mutating вызовов, хода, RNG, изменения паузы/скорости или экранного перехода. Поздние данные другой сессии не попадают в текущий вывод.

## Tests

`ListUnitsDebugCommandTests`: `ListsBothSidesAndAllKinds`, `IncludesDestroyedAndDisabled`, `ShowsIdAndPilotNameSeparately`, `HandlesNullBuildAndEmptyUnits`, `SortsSideThenIdOrdinal`, `IncludesSnapshotIdentity`, `RejectsArgumentsBeforeQuery`, `HandlesNoSessionAndNullSnapshot`, `DiscardsOldSessionOrBattleResponse`, `CancellationDoesNotPublishStaleList`, `ReadsOnceWithoutMutation`, `AppearsInHelp`. В фикстуре использовать перемешанные стороны, Carrier с null Build и два Drone с одинаковым PilotName, но разными Id; ожидать все отдельные строки. Гонку проверять управляемым отложенным ответом, без wall-clock sleep.

Из корня репозитория; `$run` — заранее созданный уникальный абсолютный каталог `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/list-units-<UTC>/`. По инструкции оператора тесты пишутся в тикете и запускаются после последнего тикета истории (этот тикет); в тикете — build и format, затем тесты истории:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter 'FullyQualifiedName~ListUnitsDebugCommandTests|FullyQualifiedName~HelpDebugCommandTests' --results-directory $run --logger trx
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs src/DeepSpaceSaga.Client/UI/DebugConsole/ListUnitsDebugCommand.cs tests/DeepSpaceSaga.Client.Tests/DebugConsole/ListUnitsDebugCommandTests.cs
```

Test-процесс ограничить process-tree cap 300000 ms; stdout/stderr направлять в `$run`. Native: help показывает list_units; в учебном и обычном бою вывод согласован с подтверждённым состоянием, при паузе не двигает бой, длинный список доступен целиком. После выхода из боя не показывается старый список. Соблюдать меньший монитор по AGENTS.md.

## Definition of Done

- Реализованы только три разрешённых файла и все AC-03/04/05, без изменения Engine/Contracts.
- Именованные тесты, build и format проходят либо исходные несвязанные ошибки отдельно записаны.
- Список содержит все Units одной revision, корректно различает ID и PilotName, не меняет состояние и не публикует устаревший ответ.
- Native evidence и automated evidence записаны отдельно; без native-проверки продуктовая приёмка не считается закрытой.
- Нет скрытых работ вне allowlist; фактические проверки и оставшиеся ограничения внесены в тикет.

## Self-containment check

Заданы источник данных, поля, ошибки, сортировка, новые сигнатуры, три файла и независимые тестовые сценарии. Исполнение начинается после US-0001 и TK-0001; обе исполняются раньше в той же ветке. Stage approved — согласование плана, не факт реализации.

## Review log

2026-11-16T06:50:00Z — revision 2: Code context и API синхронизированы с контрактом US-0001 (точка регистрации `DebugCommandCatalog.cs`; реестр/контроллер — read-only; `ExecuteAsync(context, arguments, cancellationToken)` вместо `isCurrentBattle`, актуальность через `context.IsCurrent`; `Format(TacticalBattleSnapshot)` сохранён; scope «текущий бой»); files_touched 4 → 3; обновлён format include; учтено решение G-04. approval_kind — autonomous-execution по разрешению оператора 2026-11-16.

## Execution evidence 2026-11-16

Изменённые файлы (3, в пределах Code context):

- `src/DeepSpaceSaga.Client/UI/DebugConsole/ListUnitsDebugCommand.cs` — новый: `ListUnitsDebugCommand : IDebugCommand`, descriptor `("list_units", "list_units", "Показать всех участников текущего боя", "текущий бой")`. Порядок: непустые аргументы → `Error("Синтаксис: list_units")` без запроса; `context.Session == null` → `Error("Нет активной сессии")`; один вызов `context.Session.GetTacticalBattleStateAsync(cancellationToken)`; после await — `ThrowIfCancellationRequested`, затем `!context.IsCurrent` → `Error(DebugConsoleController.StaleResultText)` (старый список не публикуется; контроллер дополнительно отбрасывает устаревший результат); null → `Error("Нет активного боя или данные недоступны")`; иначе `Ok(Format(snapshot))`. `internal static Format(TacticalBattleSnapshot)` — заголовок `Бой: … | ревизия: … | ход: … | фаза: …`, затем «Участников: N» и строки всех `Units` (не BeforeSalvoUnits/FireSteps) по Side, затем Id (Ordinal): `Id | пилот: PilotName или «—» | сторона | тип | состояние | корпус: Hull/MaxHull`; пустой Units → «Участников нет». Мутаций, RunExclusive, ActiveBattle, смены скорости нет.
- `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` — одна строка `registry.Register(new ListUnitsDebugCommand());`.
- `tests/DeepSpaceSaga.Client.Tests/DebugConsole/ListUnitsDebugCommandTests.cs` — новый, 12 именованных тестов из списка Tests. Фикстура: перемешанные стороны, Carrier с null Build, два Drone с одинаковым PilotName «Alpha» и разными Id, Disabled и Destroyed; снимки через `TutorialTestBattle` (без Engine), сессия — настоящий `GameSessionHandle` над fake `IGameSessionConnection` с управляемым отложенным ответом (`TaskCompletionSource`, без wall-clock sleep) и записью любых не-read вызовов как мутаций.

Допущения (исполнитель): смена боя внутри той же сессии не детектируется отдельно — каждый вывод строится из одного снимка, прочитанного в момент команды, и несёт его BattleId/Revision в заголовке; кэша прежнего снимка нет. `DiscardsOldSessionOrBattleResponse` проверяет смену сессии (на другую и на главное меню) во время запроса. Отмена (Dispose консоли) проверяется в `CancellationDoesNotPublishStaleList`.

Команды и результаты:

- `dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj` — 0 Warning(s), 0 Error(s) (собирает и `DeepSpaceSaga.Client`).
- `dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs src/DeepSpaceSaga.Client/UI/DebugConsole/ListUnitsDebugCommand.cs tests/DeepSpaceSaga.Client.Tests/DebugConsole/ListUnitsDebugCommandTests.cs` — exit 0.
- `git diff --cached --check` — exit 0.
- Тесты: запускаются в прогоне конца истории US-0004 сразу после этого тикета (см. story md, `## Story test evidence 2026-11-16`).
- Native (help показывает list_units; учебный и обычный бой; после выхода из боя нет старого списка): NOT RUN — подтверждённый меньший монитор недоступен в автономном прогоне.
