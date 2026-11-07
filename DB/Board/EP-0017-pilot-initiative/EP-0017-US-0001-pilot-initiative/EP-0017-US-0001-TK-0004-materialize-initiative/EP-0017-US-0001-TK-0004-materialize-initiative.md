---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0001-pilot-initiative
ticket: EP-0017-US-0001-TK-0004-materialize-initiative
title: "Постоянная инициатива в состоянии боя и снимках"
stage: approved
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0017-US-0001-TK-0001-pilot-content, EP-0017-US-0001-TK-0002-initiative-contract, EP-0017-US-0001-TK-0003-pilot-schema]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Постоянная инициатива в состоянии боя и снимках

## Why

Постоянная инициатива в состоянии боя и снимках обеспечивает AC-01, AC-02, AC-03, AC-04 истории [EP-0017-US-0001-pilot-initiative](../EP-0017-US-0001-pilot-initiative.md). Независимо проверяемый результат: SeededInitiativeIsStableIndependentAndDoesNotAdvanceCombatRng — одинаковые seed совпадают, main assembly Calls остаётся legacy42 для initial seed1. Это planning ticket, не выполненная реализация.

## Decisions

D-03/D-09: фиксированные инициативы именных пилотов 55/53/59/50, таги [40,55], шкала0..100; значение тагов выбирается один раз. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

A-06: отдельный детерминированный поток нужен, чтобы новое свойство не изменяло прежнюю стартовую геометрию. Алгоритм Mulberry32 не меняется; его Calls для main battle не включает отдельные initiative draws.

## Dependencies

[EP-0017-US-0001-TK-0001-pilot-content](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0001-pilot-content/EP-0017-US-0001-TK-0001-pilot-content.md), [EP-0017-US-0001-TK-0002-initiative-contract](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0002-initiative-contract/EP-0017-US-0001-TK-0002-initiative-contract.md), [EP-0017-US-0001-TK-0003-pilot-schema](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0003-pilot-schema/EP-0017-US-0001-TK-0003-pilot-schema.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs` | TacticalBattleState.cs:36–65, TacticalDrone; :67, TacticalBattle | Хранить Initiative у дрона и отдельный battle-owned InitiativeRandom. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | TacticalBattleAssembly.cs:14,55–58,61–85, Create | После legacy-сборки один раз материализовать инициативу в исходном порядке дронов. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | TacticalBattleSnapshotBuilder.cs:10–31,84–101, Units/Options | Заполнить материализованные Initiative и определения диапазонов; носитель=null. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AssemblyTests.cs` | AssemblyTests.cs:10–24, Catalog/InitialAssemblyMatchesEveryPublishedTraceFieldAndCalls | Добавить независимые seeded-ожидания инициативы и изоляцию потоков. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentSyncTests.cs` | ContentSyncTests.cs:19–23,58–62,145, Fields | Сохранить строгую сверку V4 полей и отдельно сверять ровно инициативное расширение EP-0017. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Проектируемый TacticalDrone.Initiative — public int Initiative { get; internal set; } = 50; setter используется только при Create, default=50 для программных тестовых дронов. TacticalBattle.InitiativeRandom = new Mulberry32(unchecked((uint)seed ^ 0x494E4954u)). Battle.Random остаётся прежним потоком AI/боевых бросков. Новые DTO из TK-0002 заполняются Engine.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Не менять legacy последовательность навыков, scatter, ролей и Battle.Random; инициатива берётся из отдельного потока с фиксированным domain seed 0x494E4954.
2. После существующей сборки пройти battle.Drones в каноническом порядке: fixed не расходует InitiativeRandom, range расходует один inclusive RandInt даже [n,n]. Инициализировать только раз до публикации.
3. Публиковать исходный range в Options без RNG, конкретное значение в Units; запросы Capture/Options не расходуют оба потока.
4. В ContentSyncTests сравнить base V4 поля как раньше и отдельно initiative по D-09; разрешить только это одно дополнительное поле. Проверка остальных свойств остаётся строгой. Экспортный тест остаётся активным и будет дополнен tooling-тикетом.

## Out of scope

Жеребьёвка равных (US-0002/TK-0002), смена rules.version, standalone V4/Reference, API transport. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- SeededInitiativeIsStableIndependentAndDoesNotAdvanceCombatRng — одинаковые seed совпадают, main assembly Calls остаётся legacy42 для initial seed1.
- DegenerateInitiativeRangeConsumesOnlyInitiativeStream — fixed50=0, [50,50]=1 новый draw.
- SnapshotAndOptionsExposeInitiativeWithoutReroll — многократные запросы и ходы сохраняют значения.
- V4BaseContentAndExplicitInitiativeExtensionAreBothValidated — неизвестный extra field по-прежнему отказ.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0001-TK-0004-materialize-initiative'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('build','src/DeepSpaceSaga.Engine/DeepSpaceSaga.Engine.csproj','--no-restore') -Name 'build'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.AssemblyTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.ContentSyncTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleState.cs','src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs','src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AssemblyTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentSyncTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 5 файлах allowlist; один production layer `engine` и matching tests.
- Критерии AC-01, AC-02, AC-03, AC-04 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

