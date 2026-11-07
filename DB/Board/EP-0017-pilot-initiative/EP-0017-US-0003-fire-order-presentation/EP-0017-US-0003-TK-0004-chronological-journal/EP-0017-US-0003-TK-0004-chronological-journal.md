---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0003-fire-order-presentation
ticket: EP-0017-US-0003-TK-0004-chronological-journal
title: "Причины пропусков, пошаговый журнал и lifecycle интерфейса"
stage: approved
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0017-US-0003-TK-0003-consistent-battle-surfaces]
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-05]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Причины пропусков, пошаговый журнал и lifecycle интерфейса

## Why

Причины пропусков, пошаговый журнал и lifecycle интерфейса обеспечивает AC-02, AC-03, AC-04, AC-05 истории [EP-0017-US-0003-fire-order-presentation](../EP-0017-US-0003-fire-order-presentation.md). Независимо проверяемый результат: JournalShowsOnlyArrivedStepEventsAndTypedSkipReasons — noTarget/withheld/dead/disabled/jam/targetGone/battleEnded. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Специальные новые звуки не добавляются; существующий журнал и presentation state получают только новые факты. Смена языка на лету не вводится.

## Dependencies

[EP-0017-US-0003-TK-0003-consistent-battle-surfaces](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0003-fire-order-presentation/EP-0017-US-0003-TK-0003-consistent-battle-surfaces/EP-0017-US-0003-TK-0003-consistent-battle-surfaces.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleLogTab.cs` | TacticalBattleLogTab.cs:17–34, Visible/HiddenTurn/Draw | Заменить скрытие всей Fire-фазы чтением уже допущенного префикса событий. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs` | BattleLogFormatter.cs:14–40, switch TacticalBattleLogKind | Локализовать FireSkipped и все typed skip reasons. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs` | public sealed class SidesAndLogTests | Точные причины и порядок событий до/после impact. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SessionHandleBattleTests.cs` | SessionHandleBattleTests.cs:9, class SessionHandleBattleTests | Дубликаты/rejected/stale snapshot не запускают новую ленту и не протекают в новый бой. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs` | LocalizationCoverageTests.cs:20, SavedRussianFromSettingsIsUsedByActualSalvoPanelAndTabs | Проверить настоящие локали и отсутствие technical IDs в очереди/причинах. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

No transport API change. Formatter обрабатывает TacticalBattleLogKind.FireSkipped=14, event.SkipReason и event.ActorId. Журнал берёт разрешённый Log из projected frame; cache инвалидируется при изменении доступного префикса/шага, а не только HiddenTurn.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Показывать уже произошедшие события по шагам вместо blanket-hide всей Fire-фазы; не открывать события следующих шагов и следующего planning.
2. Для каждого skip reason использовать строки US-0001/TK-0001; unknown/legacy null по-прежнему безопасен, без исключения или раскрытия внутреннего enum.
3. Replay повторяет только показ сохранённой ленты, не добавляет записи в authoritative/InterfaceLog; Exit/new BattleId сбрасывают видимый префикс и cache.
4. Проверить автострельбу, отсутствие выстрелов, повторный ответ, более старую revision и смену боя; существующие Flow owner/revision правила не менять.

## Out of scope

Receipt cache Engine, отправка сообщений, новая аудиосистема, правка локалей вне prerequisite. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- JournalShowsOnlyArrivedStepEventsAndTypedSkipReasons — noTarget/withheld/dead/disabled/jam/targetGone/battleEnded.
- ReplayAndDuplicateResponseDoNotDuplicateJournal — уникальные sequences, неизменный Engine state.
- NewBattleDoesNotReuseOldQueueOrLogPrefix — BattleId lifecycle.
- ActualRussianAndEnglishFireTextsContainNoResourceKeys — реальные locale files и placeholders.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0003-TK-0004-chronological-journal'
New-Item -ItemType Directory -Path $run -Force | Out-Null
function Invoke-Ep0017Check([string]$Exe, [string[]]$CommandArgs, [string]$Name) {
    $start = [Diagnostics.ProcessStartInfo]::new($Exe)
    $start.WorkingDirectory = 'D:/DeepSpaceSaga/DSS'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($item in $CommandArgs) { $start.ArgumentList.Add($item) }
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(300000)) {
        $process.Kill($true)
        $process.WaitForExit()
        [IO.File]::WriteAllText((Join-Path $run ($Name + '.stdout.log')), $stdout.GetAwaiter().GetResult())
        [IO.File]::WriteAllText((Join-Path $run ($Name + '.stderr.log')), $stderr.GetAwaiter().GetResult())
        throw 'Process-tree cap 300000 ms exceeded'
    }
    [IO.File]::WriteAllText((Join-Path $run ($Name + '.stdout.log')), $stdout.GetAwaiter().GetResult())
    [IO.File]::WriteAllText((Join-Path $run ($Name + '.stderr.log')), $stderr.GetAwaiter().GetResult())
    if ($process.ExitCode -ne 0) { throw "$Name failed: $($process.ExitCode)" }
}
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('build','src/DeepSpaceSaga.Client/DeepSpaceSaga.Client.csproj','--no-restore') -Name 'build'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.SidesAndLogTests|FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.SessionHandleBattleTests|FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.LocalizationCoverageTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleLogTab.cs','src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/BattleLogFormatter.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SessionHandleBattleTests.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/LocalizationCoverageTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 5 файлах allowlist; один production layer `client` и matching tests.
- Критерии AC-02, AC-03, AC-04, AC-05 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

