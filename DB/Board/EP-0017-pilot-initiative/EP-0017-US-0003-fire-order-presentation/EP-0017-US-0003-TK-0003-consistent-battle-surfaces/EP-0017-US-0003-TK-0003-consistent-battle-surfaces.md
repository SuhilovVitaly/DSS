---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0003-fire-order-presentation
ticket: EP-0017-US-0003-TK-0003-consistent-battle-surfaces
title: "Согласованные карта, карточки и заголовок на каждом шаге"
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0017-US-0003-TK-0002-sequential-playback]
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-05]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Согласованные карта, карточки и заголовок на каждом шаге

## Why

Согласованные карта, карточки и заголовок на каждом шаге обеспечивает AC-02, AC-03, AC-04, AC-05 истории [EP-0017-US-0003-fire-order-presentation](../EP-0017-US-0003-fire-order-presentation.md). Независимо проверяемый результат: MapCardsAndHeaderShareEveryIntermediateStepState — before, first hit, second hit, final. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Общий projector из TK-0002 — единственный источник видимого времени. Цветовые и текстовые роли existing skin сохраняются.

## Dependencies

[EP-0017-US-0003-TK-0002-sequential-playback](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0003-fire-order-presentation/EP-0017-US-0003-TK-0002-sequential-playback/EP-0017-US-0003-TK-0002-sequential-playback.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs` | TacticalBattleMapRenderer.cs:84–100, VisibleUnits; :106–131 DrawSalvo | Убрать отдельную реконструкцию только lethal marker и читать общую проекцию. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs` | TacticalBattleSidesTab.cs:19–25, Cards | Не возвращать весь BeforeSalvo во время полёта; читать projected Units. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs` | TacticalBattlePanelHeader.cs:19–33, Capture | Потери/итог брать из общей отображаемой проекции, не до/после всего залпа. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs` | SidesAndLogTests.cs:91, before-salvo cards assertion | Проверки промежуточных карточек, корпуса/брони/заклинивания. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PanelHeaderTests.cs` | PanelHeaderTests.cs:31–54, flight/arrived/hidden outcome | Проверки числа потерь и итогового banner на выигравшем impact. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

No public API change. Все поверхности используют projected frame.Battle из prerequisite. MapRenderer не формирует другой набор units, SidesTab не сбрасывает его к BeforeSalvoUnits. Timing полёта берётся из TacticalShotFlight.DurationMs.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Удалить старый путь, который во время полёта видит only BeforeSalvo плюс LethalImpact hull0; карта использует полное step state.
2. Применить одинаковые armor/hull/status/Jammed/engine factors в карточках и на карте. У disabled/уничтоженного нет интерактивных приказов.
3. Считать visible losses по projected Units, Outcome показывать только когда предоставлен общим playback projector. Не выводить статистику и конечные потери из ещё скрытого authoritative state.
4. Проверить два попадания по одной цели, ранний disable/jam, уничтожение носителя, нулевую броню, playerSideB; анимационные подписи относятся к соответствующему step.

## Out of scope

Новые правила, транспорт, формат skin manifest, скрытые будущие события. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- MapCardsAndHeaderShareEveryIntermediateStepState — before, first hit, second hit, final.
- VisibleLossAndOutcomeAppearAtWinningImpactNotAtResponseReceipt — carrier/last-drone.
- DisabledAndJammedCardsStopOfferingActionsDuringPlayback — нет UI приказов в замороженной фазе.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0003-TK-0003-consistent-battle-surfaces'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.SidesAndLogTests|FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.PanelHeaderTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleMapRenderer.cs','src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs','src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattlePanelHeader.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/PanelHeaderTests.cs')) -Name 'format'
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


## Execution — 2026-11-07

MapRenderer и Cards читают только общую projected Units; старое восстановление BeforeSalvo плюс fake lethal marker удалено. Карта использует индивидуальный DurationMs для flash. Header считает потери из тех же projected units и отображает outcome только если он допущен projector, без blanket-hide по SalvoFlight. Карточки во время playback/pending не предлагают selection actions.

На общих временных точках проверены before/first/second impact, одинаковые hull/armor на карте и в карточках, потеря/outcome на winning impact, запрет actions для active/jammed/disabled/destroyed во время playback. Реальный seed1 outcome/player perspective тест обновлён для explicit projection вместо manually supplied future authoritative state. Client build PASS; SidesAndLog/PanelHeader/BattleMapRender/ResultsPhase tests PASS33/33; scoped format/verify и diff check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0303/ (logs, focused.trx и rasters). Self-review выполнено; independent review не заявляется. Scope включает эту карточку. Native консолидирован послеTK-0004, NOT RUN здесь; stage implemented до gate.
