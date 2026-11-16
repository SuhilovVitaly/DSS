---
epic: EP-0030-debug-console
story: EP-0030-US-0001-global-console
ticket: EP-0030-US-0001-TK-0002-console-core
title: Ядро консоли — реестр, разбор строки и контроллер
stage: approved
approval_kind: autonomous-execution
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0030-US-0001-TK-0001-hotkey-edge]
prerequisite_stories: []
files_touched: 4
serves: [AC-03, AC-04, AC-05]
created: 2026-11-16T06:50:00Z
revision: 1
---

# TK-0002 — Ядро консоли без UI

## Why

Зависимые истории (US-0002..0004) нуждаются в одном реестре команд, разборе строки, вызове обработчика и выводе результата с проверкой актуальности сессии. Тикет создаёт эту границу без рендеринга и без подключения к окну. Покрывает AC-03..05 [истории](../EP-0030-US-0001-global-console.md).

## Decisions

- Оператор, 2026-11-16: «Создай новую ветку… выполни EP-0030… после каждого тикета коммит и пуш. Тесты только после окончания юзерстори» и «работай автономно».
- Пользователь, 2026-11-16: «Декомпозировать сам».
- Предложения A-01..A-03, A-05, A-06 эпика приняты исполнителем как технические допущения автономного исполнения 2026-11-16 (это не решение пользователя).

## Assumptions

- Строки консоли — жёстко заданный русский текст; они находятся вне `UI/Screens/TacticalBattle`, поэтому не входят в локализационные проверки.
- Вывод хранит последние 2000 строк; длина ввода ограничена 160 символами.
- Закрытие консоли не отменяет уже выполняющуюся мутирующую команду; `Lifetime` отменяется только `Dispose`.
- Мелкие связанные типы (`DebugCommandDescriptor`, `DebugCommandResult`, `IDebugCommand`, `IDebugConsoleHost`, `DebugCommandContext`) объединены в `DebugCommand.cs`.

## Code context

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommand.cs` | Новый | Типы контракта команд (ниже) |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugCommandRegistry.cs` | Новый | Реестр с уникальными Ordinal-именами |
| `src/DeepSpaceSaga.Client/UI/DebugConsole/DebugConsoleController.cs` | Новый | Состояние, ввод, отправка, вывод |
| `tests/DeepSpaceSaga.Client.Tests/DebugConsole/DebugConsoleControllerTests.cs` | Новый | Тесты ниже с fake `IDebugConsoleHost` и fake командами |

Read-only: `src/DeepSpaceSaga.Client/UI/Controls/TextInputBox.cs` (поле ввода), `src/DeepSpaceSaga.Client/GameSessionHandle*.cs` (тип сессии), `src/DeepSpaceSaga.Client/UI/SkiaWindow.cs:59` (`_transitionLock`, используется в TK-0003).

## Public API after the change

Namespace `DeepSpaceSaga.Client.UI.DebugConsole`:

```csharp
internal sealed record DebugCommandDescriptor(string Name, string Syntax, string Description, string Scope);
internal sealed record DebugCommandResult(IReadOnlyList<string> Lines, bool IsError = false, bool CloseConsole = false)
{ public static DebugCommandResult Ok(params string[] lines); public static DebugCommandResult Error(string line);
  public static DebugCommandResult Busy(); /* "Выполняется переход или ход боя — повторите позже" */ }
internal interface IDebugCommand
{ DebugCommandDescriptor Descriptor { get; }
  Task<DebugCommandResult> ExecuteAsync(DebugCommandContext context, string arguments, CancellationToken cancellationToken); }
internal interface IDebugConsoleHost
{ GameSessionHandle? Session { get; }
  bool IsTransitionBusy { get; }
  TacticalBattleScreen? ActiveBattle { get; }
  Task<DebugCommandResult> RunExclusiveAsync(Func<CancellationToken, Task<DebugCommandResult>> operation, CancellationToken cancellationToken); }
internal sealed class DebugCommandContext(DebugCommandRegistry registry, IDebugConsoleHost host)
{ Registry; Host; GameSessionHandle? Session /* захвачена при создании */; bool IsCurrent => ReferenceEquals(Session, host.Session); }
internal sealed class DebugCommandRegistry
{ void Register(IDebugCommand c) /* дубликат Ordinal -> InvalidOperationException */;
  bool TryGet(string name, [NotNullWhen(true)] out IDebugCommand? c);
  IReadOnlyList<DebugCommandDescriptor> DescribeAll() /* Ordinal по Name */; }
internal sealed class DebugConsoleController : IDisposable
{ DebugConsoleController(DebugCommandRegistry registry, IDebugConsoleHost host);
  static bool IsToggleChord(Key key, KeyModifiers m); /* P && Control && Alt */
  static bool TryParse(string line, out string name, out string arguments);
  bool IsOpen; bool IsExecuting; TextInputBox Input; IReadOnlyList<string> Output;
  void Open(); void Close(); void Toggle();
  Task SubmitAsync(); bool OnKeyDown(Key key, KeyModifiers m, bool repeat); void OnTextInput(char c);
  CancellationToken Lifetime; }
```

## Implementation steps

1. Написать тесты ниже (fake host с настраиваемым `Session`, fake команды с управляемым `TaskCompletionSource`).
2. `DebugCommand.cs`: типы контракта; `Busy()` возвращает «Выполняется переход или ход боя — повторите позже».
3. `DebugCommandRegistry`: словарь `StringComparer.Ordinal`, `DescribeAll` сортирует Ordinal.
4. `DebugConsoleController.TryParse`: trim; имя — первый токен; аргументы — остаток с trim (внутренние пробелы сохраняются).
5. `SubmitAsync`: пустая строка — no-op; если `IsExecuting` — строка «Команда уже выполняется»; echo `> line`; неизвестное имя — «Неизвестная команда: X. Введите help»; исключение обработчика — «Ошибка: msg»; после await при `!context.IsCurrent` — «Результат устарел: сессия сменилась»; `CloseConsole` — `Close()`. Очистить ввод после отправки.
6. `OnKeyDown`: Esc — Close; Enter/KeypadEnter без repeat — Submit; Backspace — ввод; Up/Down/Home/End — прокрутка; всегда возвращает true. `OnTextInput` — в `Input`.
7. Build Client + Client.Tests, scoped format, `git diff --check`.

## Out of scope

Рендеринг, мышь и подключение к `SkiaWindow` (TK-0003); `DebugCommandCatalog` (TK-0003); конкретные команды (US-0002..0004); история между запусками, автодополнение.

## Invariants

- Одна выполняемая команда одновременно; повторный Enter не создаёт вторую операцию (A-05).
- Результат команды, завершившейся после смены сессии, не публикуется как текущий.
- Контроллер не обращается к Engine; любые изменения состояния идут через команды и сессию (`DB/Documentation/01-Requirements/EngineRequirements.md:11`).

## Tests

`DebugConsoleControllerTests`: `ToggleChordRequiresControlAndAlt`, `EmptyLineDoesNothing`, `UnknownCommandReportsErrorWithoutCallingHandlers`, `ParsesNameAndWholeRestAsArguments`, `RegistryRejectsDuplicateOrdinalName`, `DescribeAllIsOrdinalAndComplete`, `SecondSubmitWhileExecutingIsRejected`, `ResultFromReplacedSessionIsDiscarded`, `HandlerExceptionBecomesErrorLine`, `CloseConsoleResultCloses`, `EscapeClosesConsole`, `LongResultIsFullyRetained`.

Тесты пишутся здесь, запуск — после последнего тикета истории (инструкция оператора).

```powershell
dotnet build tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
dotnet format DeepSpaceSaga.sln whitespace --verify-no-changes --include src/DeepSpaceSaga.Client/UI/DebugConsole/ tests/DeepSpaceSaga.Client.Tests/DebugConsole/
# конец истории:
dotnet test tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj --filter FullyQualifiedName~DebugConsoleControllerTests --results-directory $run --logger trx
```

## Definition of Done

- Четыре файла созданы; API совпадает с разделом выше.
- 12 названных тестов компилируются и проходят в прогоне конца истории.
- Build/format без ошибок; execution evidence в тикете, commit + push.

## Self-containment check

Сигнатуры, тексты сообщений, правила разбора и тесты заданы полностью; окно и рендер не требуются.
