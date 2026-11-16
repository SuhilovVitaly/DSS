---
epic: EP-0030-debug-console
story: EP-0030-US-0004-console-discovery
ticket: EP-0030-US-0004-TK-0002-list-units
title: Команда list_units — участники текущего боя
stage: approved
approval_kind: automatic-workflow
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0030-US-0004-TK-0001-help]
prerequisite_stories: [EP-0030-US-0001-global-console]
files_touched: 4
serves: [AC-03, AC-04, AC-05]
created: 2026-11-16T06:21:45Z
revision: 1
---

# TK-0002 — Команда list_units

## Why

Дать оператору полный список участников текущего боя для диагностики и различения целей. Покрывает AC-03/04/05 [истории](../EP-0030-US-0004-console-discovery.md); после регистрации команда автоматически появляется в help.

## Decisions

Пользователь: «`list_units`  help - показывает все команды дебага консоли - добавь тикеты на оба». Записано 2026-11-16T06:21:45Z. Команда принята из предложения «имена, стороны и состояние всех участников боя».

## Assumptions

Без аргументов, только чтение одного актуального immutable snapshot. Все Units обеих сторон, включая Drone, Carrier и неактивных; ничего не фильтровать по предполагаемому термину «тэг». Поля: Unit ID, PilotName отдельно или «—», Side, Kind, State, Hull/MaxHull. При отсутствии отдельного имени юнита Id остаётся его идентификатором; имя пилота не выдавать за имя цели `kill_drone`. Сортировка Side/Id Ordinal, null Build допустим. Заголовок содержит BattleId/Revision/Turn/Phase. Вывод — текст с прокруткой, без скрытого лимита строк.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandRegistry.cs` | Проектируется US-0001 и TK-0001, пока отсутствует | Зарегистрировать list_units, синтаксис и область «текущий бой» |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugConsoleController.cs` | Проектируется US-0001, пока отсутствует | Передать контекст/отмену, вызвать обработчик и не публиковать устаревший ответ |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/ListUnitsDebugCommand.cs` | Новый | Асинхронное чтение снимка и форматирование списка |
| `tests/DeepSpaceSaga.Client.Tests/DebugConsole/ListUnitsDebugCommandTests.cs` | Новый | Тесты вывода, отказов и гонок контекста |

Read-only sources: `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs:51,151` (поля юнитов/снимка), `src/DeepSpaceSaga.Client/GameSessionHandle.TacticalBattle.cs:21` (`GetTacticalBattleStateAsync`). Эти файлы не менять. Зависимость — [TK-0001](../EP-0030-US-0004-TK-0001-help/EP-0030-US-0004-TK-0001-help.md).

## Public API after the change

Игровые/transport API без изменений. Проектируемая внутренняя client-граница `ListUnitsDebugCommand.ExecuteAsync(GameSessionHandle? session, string arguments, Func<string, bool> isCurrentBattle, CancellationToken cancellationToken): Task<IReadOnlyList<string>>`. Контроллер захватывает session identity и предоставляет проверку актуальности с BattleId после await. `Format(TacticalBattleSnapshot snapshot): IReadOnlyList<string>` — чистое форматирование, доступное matching tests. Если зависимость предоставляет эквивалентный контекст, сначала синхронизировать сигнатуру в Board; прямой доступ к Engine запрещён.

## Implementation steps

1. Зарегистрировать `list_units` в едином реестре и подключить обработчик к диспетчеру, используя метаданные для help.
2. Лишние аргументы отклонить с `Синтаксис: list_units`, не запрашивая сессию. При отсутствии сессии вернуть «Нет активной сессии».
3. Запросить `session.GetTacticalBattleStateAsync(cancellationToken)` один раз. Null: «Нет активного боя или данные недоступны»; не выдавать null за достоверное отсутствие боя, поскольку текущая сессия преобразует ошибки чтения в null.
4. После await проверить актуальность сессии и BattleId через контекст. При отмене/смене контекста не выводить старый список как текущий. Ошибку отобразить без публикации прежнего снимка.
5. Для актуального snapshot вывести заголовок и отсортированные строки всех Units; пустой массив — «Участников нет». Читать Units, не BeforeSalvoUnits/FireSteps и не кадр анимации.
6. Добавить тесты и проверку появления list_units в help. Не менять игровое состояние.

## Out of scope

Новая команда Engine, изменение DTO, изменение имён или поиска kill_drone, фильтры, экспорт, будущие приказы/скрытые планы AI, оболочка и прокрутка (US-0001).

## Invariants

Engine остаётся авторитетным (`DB/Documentation/01-Requirements/EngineRequirements.md:11`); используются только публичные snapshots (`TacticalBattleSnapshot.cs:151`). Один результат относится к одной revision. Нет mutating вызовов, хода, RNG, изменения паузы/скорости или экранного перехода. Поздние данные другой сессии не попадают в текущий вывод.

## Tests

Будущие `ListUnitsDebugCommandTests`: `ListsBothSidesAndAllKinds`, `IncludesDestroyedAndDisabled`, `ShowsIdAndPilotNameSeparately`, `HandlesNullBuildAndEmptyUnits`, `SortsSideThenIdOrdinal`, `IncludesSnapshotIdentity`, `RejectsArgumentsBeforeQuery`, `HandlesNoSessionAndNullSnapshot`, `DiscardsOldSessionOrBattleResponse`, `CancellationDoesNotPublishStaleList`, `ReadsOnceWithoutMutation`, `AppearsInHelp`. В фикстуре использовать перемешанные стороны, Carrier с null Build и два Drone с одинаковым PilotName, но разными Id; ожидать все отдельные строки. Гонку проверять управляемым отложенным ответом, без wall-clock sleep.

Из корня репозитория; `$run` — заранее созданный уникальный абсолютный каталог `D:/DeepSpaceSaga/artifacts/validation/ep0030-debug-console/list-units-<UTC>/`. Будущие команды:

```powershell
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter 'FullyQualifiedName~ListUnitsDebugCommandTests|FullyQualifiedName~HelpDebugCommandTests' --results-directory $run --logger trx
dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj --no-restore
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandRegistry.cs src/DeepSpaceSaga.Client/UI/DebugConsole/DebugConsoleController.cs src/DeepSpaceSaga.Client/UI/DebugConsole/ListUnitsDebugCommand.cs tests/DeepSpaceSaga.Client.Tests/DebugConsole/ListUnitsDebugCommandTests.cs
```

Test-процесс ограничить process-tree cap 300000 ms; stdout/stderr направлять в `$run`. Native: help показывает list_units; в учебном и обычном бою вывод согласован с подтверждённым состоянием, при паузе не двигает бой, длинный список доступен целиком. После выхода из боя не показывается старый список. Соблюдать меньший монитор по AGENTS.md.

## Definition of Done

- Реализованы только четыре разрешённых файла и все AC-03/04/05, без изменения Engine/Contracts.
- Именованные тесты, build и format проходят либо исходные несвязанные ошибки отдельно записаны.
- Список содержит все Units одной revision, корректно различает ID и PilotName, не меняет состояние и не публикует устаревший ответ.
- Native evidence и automated evidence записаны отдельно; без native-проверки продуктовая приёмка не считается закрытой.
- Нет скрытых работ вне allowlist; фактические проверки и оставшиеся ограничения внесены в тикет.

## Self-containment check

Заданы источник данных, поля, ошибки, сортировка, новые сигнатуры, четыре файла и независимые тестовые сценарии. Исполнение начинается после US-0001 и TK-0001; до готовности их проектируемого контракта тикет является зависимым планом. Stage approved — автоматическое согласование плана, не факт реализации.
