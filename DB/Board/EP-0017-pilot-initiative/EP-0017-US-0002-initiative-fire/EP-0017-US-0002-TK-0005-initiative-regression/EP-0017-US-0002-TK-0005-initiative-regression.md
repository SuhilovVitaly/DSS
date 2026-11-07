---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0002-initiative-fire
ticket: EP-0017-US-0002-TK-0005-initiative-regression
title: "Независимые сценарии новой механики и замена старой parity-приёмки"
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0017-US-0002-TK-0004-session-fire-timeline]
files_touched: 5
serves: [AC-01, AC-02, AC-03, AC-04, AC-05, AC-06]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Независимые сценарии новой механики и замена старой parity-приёмки

## Why

Независимые сценарии новой механики и замена старой parity-приёмки обеспечивает AC-01, AC-02, AC-03, AC-04, AC-05, AC-06 истории [EP-0017-US-0002-initiative-fire](../EP-0017-US-0002-initiative-fire.md). Независимо проверяемый результат: HandComputedInitiativeCasesMatchEngineAndOracle — реальная независимая ожидаемая последовательность. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

A-08: новый бой намеренно не обязан воспроизводить V4 simultaneous totals. Изменять standalone simulator или регенерировать его historical reference в этом эпике не нужно. Независимая проверка малых ситуаций и массовая повторяемость — разные gates.

## Dependencies

[EP-0017-US-0002-TK-0004-session-fire-timeline](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0002-initiative-fire/EP-0017-US-0002-TK-0004-session-fire-timeline/EP-0017-US-0002-TK-0004-session-fire-timeline.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs` | GoldenBattleTests.cs:18,28, EveryIndependentBattleSummaryMatches/SeedOneMatchesEveryStageWithoutInjectingReferenceStateOrRandom | Заменить oracle одновременных боёв на независимые EP-0017 ситуации и честно названную determinism coverage. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs` | SeriesParityTests.cs:15–23, Dogfight/Raid/Defence/Balanced/Commander | Заменить обязательность old win counts на повторяемость новой механики по каждому tactic. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs` | LocalSessionBattleTests.cs:21,67, EveryIndependentBattleThroughConnectionMatchesInBothOrderModes | Проверить паритет нового direct/API/connection пути с теми же приказами. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeReferenceCases.cs` | Новый файл; отсутствует | Компилируемые ручные входы и ожидаемые outcomes/damage/skips; не runtime-generated baseline. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeReferenceOracle.cs` | Новый файл; отсутствует | Независимый минимальный oracle сортировки/замороженных целей/урона для контролируемых ситуаций без вызовов Engine Salvo. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

No production API change. Новые test-only типы InitiativeReferenceCases/Oracle не вызываются продуктом. Старый VData/Planning/V4.0/Reference остаётся историческим материалом; source fingerprints и чистые geometry/Mulberry32 fixtures не переписываются.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Записать вручную контролируемые cases: 80/60/40, равные с известной перестановкой, kill/disable/jam, потеря цели без retarget, последовательная броня, немедленный исход. Ожидаемые результаты вывести независимо от production Salvo.
2. Проверки полного боя больше не сравнивать с simultaneous traces и old victory totals. Не снимать их через Skip/удаление/assert true: заменить сильными проверками новой механики и phase/queue/state/stat invariants.
3. Для серии seeds1..1000 каждого существующего tactic сравнить два новых запуска и direct vs explicit одинаковые приказы; сохранять реальные новые signature/totals как evidence, а не как доказательство старого баланса. Это determinism/parity, а не независимый oracle урона.
4. SessionTests/SalvoTests уже адаптированы в prerequisites. ReferenceFixtureTests/Mulberry32Tests/Ai initial tests остаются активны на неизменённых primitive/history данных. При новом неразрешённом regression расширять Board до реализации, не править дополнительные файлы скрыто.

## Out of scope

Production simulation, standalone V4 Python/HTML, увеличение лимита тестового процесса, принятие баланса по одному seed. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- HandComputedInitiativeCasesMatchEngineAndOracle — реальная независимая ожидаемая последовательность.
- NewRuleBattlesRepeatAcrossDirectAndConnectionPaths — одинаковые входы дают одинаковый результат.
- SeriesParityTests.Dogfight/Raid/Defence/Balanced/Commander — пять отдельных test invocations, каждый <=300000 ms.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0002-TK-0005-initiative-regression'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter','(FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.GoldenBattleTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.LocalSessionBattleTests)&Category!=LongRunning','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
foreach ($tacticTest in @('Dogfight','Raid','Defence','Balanced','Commander')) {
    Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter',('FullyQualifiedName=DeepSpaceSaga.Engine.Tests.TacticalBattle.SeriesParityTests.' + $tacticTest),'--results-directory',$run,'--logger',('trx;LogFileName=series-' + $tacticTest + '.trx')) -Name ('series-' + $tacticTest)
}
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/GoldenBattleTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SeriesParityTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/LocalSessionBattleTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeReferenceCases.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeReferenceOracle.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 5 файлах allowlist; один production layer `engine` и matching tests.
- Критерии AC-01, AC-02, AC-03, AC-04, AC-05, AC-06 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.


## Execution — 2026-11-07

Historical V4 battle rows используются только как набор seed/tactic inputs; старые simultaneous totals/trace не перезаписывались. Новые ручные kill/disable/jam/armor/no-retarget/miss cases имеют заранее заданные Hull/Shot ожидания и отдельный минимальный oracle без Salvo/CheckVictory/Engine math. Seeded tie oracle закреплён в FireOrderTests. Полные бои сравниваются точно direct/explicit/API/connection по результату, статистике, units, RNG; очередь/индексы/останавливающие шаги и накопленные фактические shot/hit/hull/armor проверяются по ходу. Это determinism/parity, не независимый oracle общего AI.

Реальная запись/загрузка Save20 и запрет Save на Move/Fire/Over проверены LocalSessionBattleTests; fixtures перенесены из OS temp в DSS_VALIDATION_ROOT с проверкой cleanup boundaries. Все5 series запускаются отдельно по exact IDs и cap300000ms. На каждой1000 seed проверено exact direct/explicit совпадение итоговых полных состояний и statistics/RNG; signatures SHA256 сохранены.

Focused Golden/LocalSession PASS185/185; TacticalBattle regression Category!=LongRunning PASS421/421; Dogfight/Raid/Defence/Balanced/Commander PASS по1000 seeds каждая, процессы8–10s. Наблюдаемые W/L:976/24,853/147,949/51,958/42,976/23 (Commander1 timeout). Баланс/product не принят по этим числам. Engine build, scoped format/verify и git diff --check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0205/ — focused.trx, tactical-regression.trx, пять series TRX/signatures, series-summary.md, logs. Self-review выполнено; независимое review не заявляется. Scope дополнен этой карточкой и прогрессом истории. Native UI принадлежит US-0003.
