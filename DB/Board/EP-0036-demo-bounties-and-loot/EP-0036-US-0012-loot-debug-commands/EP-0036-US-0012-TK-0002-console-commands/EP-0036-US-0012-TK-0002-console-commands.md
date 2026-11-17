---
epic: EP-0036-demo-bounties-and-loot
story: EP-0036-US-0012-loot-debug-commands
ticket: EP-0036-US-0012-TK-0002-console-commands
title: Консольные команды engineer_bonus, loot_force, loot_state
stage: draft
approval_kind: workflow
implementation_status: not-started
layer: client
matching_test_project: tests/DeepSpaceSaga.Client.Tests
depends_on: [EP-0036-US-0012-TK-0001-loot-debug-overrides, EP-0036-US-0007-TK-0004-postbattle-map]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-17T19:41:16Z
revision: 1
---

# Консольные команды engineer_bonus, loot_force, loot_state

## Why

[EP-0036-US-0012-loot-debug-commands](../EP-0036-US-0012-loot-debug-commands.md). Консольные команды engineer_bonus, loot_force, loot_state. Тикет добавлен при сверке декомпозиции 2026-11-17 (D-36-34…37 в [эпике](../../Documentation.md)): без него результат истории не наблюдаем игроком или не проверяем.

## Decisions

Нормативные D-36-01…37 с точными цитатами и временем записи — в [эпике](../../Documentation.md). Последняя запись 2026-11-17T19:41:16Z — время записи, не отправки. Workflow approval декомпозиции не означает исполнение/приёмку.

## Assumptions

- Новые имена API/файлы ниже — проект, а не существующая реализация. Реальный API prerequisite-тикетов проверять по их diff до исполнения.
- Балансовые/текстовые значения — стартовый плейтестовый baseline, не продуктовая приёмка.
- При непредвиденном drift сначала пересмотреть allowlist в Board, затем расширять diff.

## Code context

| File | Current state / evidence | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandCatalog.cs` | Единая точка регистрации команд EP-0030. | Три Register строки. |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/EngineerBonusDebugCommand.cs` | Новый; образец FogOfWarDebugCommand/AppDebugOverrides. | engineer_bonus [auto|0|10..15]. |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/LootForceDebugCommand.cs` | Новый. | loot_force rolls|attempts auto|keep|destroy|success|fail. |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/LootStateDebugCommand.cs` | Новый. | Read-only печать источников/попыток/остатков/свободной массы. |
| `tests/DeepSpaceSaga.Client.Tests/Demo/DemoEp0036Us0012Tk0002Tests.cs` | Новый. | Named tests ниже. |

Разрешены 5 implementation files, включая тесты. Read-only зависимости и source grounding — в истории/эпике. Board-правки не расширяют production allowlist.

## Public API after the change

No gameplay API change. Команды отправляют SetLootDebugRequest через existing ExecuteDemoCampaign и подтверждают только Applied ответ; loot_state читает текущий подтверждённый Rewards snapshot, команд не отправляет. Тексты консоли — по-русски, как у существующих EP-0030 команд (не locale-файлы).

## Implementation steps

1. engineer_bonus без аргумента печатает действующий бонус и источник (persisted/override); auto снимает override.
2. loot_force rolls|attempts <mode> с валидацией синтаксиса; неизвестный режим — ошибка без запроса.
3. loot_state вне LootOpen — «Нет открытого сбора»; в LootOpen — таблица ObjectId/kind/attempts/остатки и свободная масса.
4. Busy/pending по образцу KillDroneDebugCommand; help показывает новые команды.

## Invariants

- Engine владеет RNG, cargo, деньгами, людьми и подтверждённым результатом; Client показывает только подтверждённое состояние.
- Stable BattleId/ObjectId/CommandId; повтор команды, повторное открытие и Load не дублируют эффект и не перебрасывают бросок.
- Единственные inventory/PlayerCredits; нет второго кошелька или скрытого склада.
- Не вводить прочность/ремонт/установку трофеев, торговлю пленными, пытки/вербовку (D-36-08/20/21/28).

## Out of scope

Другие stories/layers, запуск игры, правка нормативных требований, commit/push. Соседние механики подключаются по prerequisites, правила не копируются.

## Tests

Будущие тесты NOT RUN:
- `DemoEp0036Us0012Tk0002Tests.EngineerBonusShowsSource`
- `DemoEp0036Us0012Tk0002Tests.LootForceValidatesSyntax`
- `DemoEp0036Us0012Tk0002Tests.RejectedResponseNotAcknowledged`
- `DemoEp0036Us0012Tk0002Tests.LootStateReadOnly`
- `DemoEp0036Us0012Tk0002Tests.HelpListsNewCommands`

Запуск из D:/DeepSpaceSaga/DSS. Создать уникальный абсолютный run в D:/DeepSpaceSaga/artifacts/validation/EP-0036-US-0012-TK-0002-console-commands/<UTC-run>/; ограничить каждый process tree 300000 ms. Команда:
`dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj -c Release --filter FullyQualifiedName~DemoEp0036Us0012Tk0002Tests --results-directory <absolute-run>/results --artifacts-path <absolute-run>/build`.
Start-Process -WindowStyle Hidden с stdout/stderr в run, WaitForExit(300000); при timeout завершить всё дерево taskkill /PID <pid> /T /F, не объявлять PASS. Owning build: `dotnet build src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj -c Release --artifacts-path <absolute-run>/build` с тем же cap. Для C# format --verify-no-changes --include только allowlist. Планирование эти проверки не запускало.

## Definition of Done

- Шаги/API выполнены в разрешённых файлах, no hidden follow-up.
- Все named tests проходят; actual evidence и ограничения записаны в Board.
- Пользовательский end state проверяем через authoritative snapshot/receipt либо реальный input path; unit model не заменяет native/product acceptance.
- Save/Load/duplicate/rejection сохраняют invariants.
- Тесты/build/format не выдаются за успешные без выполнения; commits/push этим planning ticket не разрешены.

## Self-containment check

Входные API, owning файлы, шаги, tests и ограничения перечислены; prerequisite-создатели новых файлов указаны в depends_on. Draft означает ожидаемую сверку плана, не отсутствующую игровую реализацию.

## Execution
Registered engineer_bonus, loot_force and read-only loot_state in default help registry. Commands validate before transport, use active session and exclusive host gate, reject busy combat/changed sessions and acknowledge only Applied. Console prints effective engineer source, modes and loot sources/attempts/free mass. Client/test build PASS (D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/us12-tk2-build.log); both tickets complete, boundary tests next.
