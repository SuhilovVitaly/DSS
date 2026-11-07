---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0002-initiative-fire
ticket: EP-0017-US-0002-TK-0003-sequential-damage
title: "Последовательный урон и немедленная остановка очереди"
stage: approved
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0017-US-0002-TK-0002-seeded-fire-order]
files_touched: 5
serves: [AC-01, AC-03, AC-04, AC-05]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Последовательный урон и немедленная остановка очереди

## Why

Последовательный урон и немедленная остановка очереди обеспечивает AC-01, AC-03, AC-04, AC-05 истории [EP-0017-US-0002-initiative-fire](../EP-0017-US-0002-initiative-fire.md). Независимо проверяемый результат: HighInitiativeKillCancelsReplyWithoutDraws — ранний смертельный выстрел даёт один shot, поздний не расходует draws. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Формулы попадания/кормы/порога disabled не меняются. Анимация не задаёт момент authoritative damage; все шаги рассчитываются одной принятой командой.

## Dependencies

[EP-0017-US-0002-TK-0002-seeded-fire-order](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0002-initiative-fire/EP-0017-US-0002-TK-0002-seeded-fire-order/EP-0017-US-0002-TK-0002-seeded-fire-order.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs` | TacticalBattleSalvo.cs:42–120, Fire и сгруппированный урон | Заменить одновременное разрешение на frozen plans и последовательные эффекты; хранить immutable internal steps. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs` | TacticalBattle.cs:33–60, Firing/CheckVictory | Проверять победу после каждого полного шага и не перезаписывать Complete обратно Planning. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SalvoTests.cs` | SalvoTests.cs:11,81,101,273 | Заменить simultaneous-ожидания, сохранить формулы и проверки атомарного отказа. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/OutcomeTests.cs` | public sealed class OutcomeTests | Немедленный исход при гибели носителя/последнего дрона. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeFireTests.cs` | Новый файл; отсутствует | Независимые малые ситуации инициативы, броня, jam, no-retarget и статистика. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Существующий internal TacticalBattleSalvo.Fire(battle,targetsFor) остаётся точкой входа. TacticalSalvoResult получает immutable внутреннюю ленту шагов с index/shooter/target/skip/shot/full UnitsAfter/system delta и outcome. Проектируемая форма соответствует DTO TK-0001; реальные Contracts events с sequence создаёт session в TK-0004.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. До первого draw/mutation вычислить и проверить все планы с callback для обеих сторон по исходному состоянию фазы; сохранить выбранную цель для каждого. InvalidSelection по-прежнему атомарный отказ. Dry обновляется как прежде один раз от начальных candidates.
2. Идти по общей frozen очереди: уже победа→BattleEnded; иначе destroyed/disabled стрелок→соответствующий skip; jam→WeaponJammed; нет цели→NoTarget либо Withheld; цель неактивна→TargetUnavailable. Пропуск не делает hit/damage/system draws.
3. Для доступного выстрела пересчитать условия по актуальному состоянию, выполнить прежние hit/damage/rear draws, сразу split текущей брони, применить hull/armor и destroyed/disabled.
4. Сразу применить системный эффект только если цель пережила и остаётся активной и выполнен существующий MinHullLoss; неприменимый эффект не создавать. Кредит урона оружию — фактическая потеря именно этого шага; FirstLoss — первое выбытие по времени.
5. После полного шага обновить статистику и CheckVictory; зафиксировать after-state. При победе остановить боевые действия, оставшиеся queue entries отметить BattleEnded без новых бросков/событий попадания. Сохранить Complete, не вызывать следующий planning.
6. Ручные fixtures SalvoTests/OutcomeTests перед расчётом инициализируют полный rank через InitializeFireOrder; это не должен делать UI/query. Перед шагом использовать свежие Candidates для сохранённой цели; geometry не меняется, shield/status учитываются по текущему state.

## Out of scope

Журнал transport sequence, Client, старые V4 Reference файлы, баланс оружия и прогресс времени мира. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- HighInitiativeKillCancelsReplyWithoutDraws — ранний смертельный выстрел даёт один shot, поздний не расходует draws.
- DisableAndWeaponJamBeforeTurnCancelReply — отдельные гарантированные controlled fixtures.
- LostFrozenTargetIsSkippedForPlayerAndAiWithoutRetarget — обе стороны и существующая другая цель.
- TwoHitsUseRemainingArmorAndCreditActualHull — armor10, piercing0, damage15 дважды: (armor10,hull5), затем (armor0,hull15), общий hull20.
- VictoryAtCarrierOrLastDroneStopsQueueAndPreservesComplete — нет ответных действий после победы.
- MissWithheldNoTargetsAndInvalidSelectionHaveExactDrawOwnership — independent expected draw deltas и atomic отказ.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0002-TK-0003-sequential-damage'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.SalvoTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.OutcomeTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.InitiativeFireTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSalvo.cs','src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattle.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SalvoTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/OutcomeTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/InitiativeFireTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 5 файлах allowlist; один production layer `engine` и matching tests.
- Критерии AC-01, AC-03, AC-04, AC-05 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

