---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0003-fire-order-presentation
ticket: EP-0017-US-0003-TK-0002-sequential-playback
title: "Последовательное воспроизведение ленты стрельбы"
stage: implemented
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0017-US-0003-TK-0001-queue-and-forecast]
files_touched: 5
serves: [AC-02, AC-03, AC-04, AC-05]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Последовательное воспроизведение ленты стрельбы

## Why

Последовательное воспроизведение ленты стрельбы обеспечивает AC-02, AC-03, AC-04, AC-05 истории [EP-0017-US-0003-fire-order-presentation](../EP-0017-US-0003-fire-order-presentation.md). Независимо проверяемый результат: SequentialPlaybackAppliesDamageAtEachImpactBeforeNextLaunch — timestamps impact-1/impact/impact+1. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

A-10: время эффектов выбрано в границах текущего cap4500, чтобы очередность была последовательной без неограниченного ожидания. Wall-clock presentation не является движком боя. Client.Frame получает единую проекцию, Flow.State остаётся последним authoritative snapshot.

## Dependencies

[EP-0017-US-0003-TK-0001-queue-and-forecast](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0003-fire-order-presentation/EP-0017-US-0003-TK-0001-queue-and-forecast/EP-0017-US-0003-TK-0001-queue-and-forecast.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs` | TacticalBattleResults.cs:24–61, Create/Capture; timing constants:27 | Построить воспроизведение по FireSteps, фиксировать видимое состояние после каждого impact. |
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs` | TacticalBattleScreen.cs:93–113, CaptureFrame; :305 OnBattleUpdated; :480–497 skip/replay | Передавать всем поверхностям единую проекцию по текущему моменту. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs` | ResultsPhaseTests.cs:14, ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot | Проверить границы impact, skip/replay и absence дополнительного Fire. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs` | SkinBattleIntegrationTests.cs:12, class SkinBattleIntegrationTests | Равенство хронологии Classic/contrast. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs` | BattleMapRenderTests.cs:156–172, WreckMarkerAndNameAppearAtLethalArrivalBeforeSalvoEnds | Актуализировать ручные fixture DTO под FireSteps и per-step timing. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Проектируемый presentation result Capture(now) даёт видимые Units/Log/Losses/Outcome и индекс последнего применённого шага, не меняя Flow.State. TacticalShotFlight получает DurationMs для Progress/Flying/Flash; текущее глобальное FlightMs не применять ко всем коротким слотам. CaptureFrame передаёт presentation clone с Units=последний UnitsAfter и доступным префиксом Log.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Использовать BeforeSalvoUnits до первого impact и только immutable UnitsAfter дошедших шагов; не реконструировать броню или disabled по формулам Client.
2. Выстрелы летят последовательно: следующий запускается не раньше impact предыдущего. Сохранить initialDelay=300ms и totalFlightCap=4500ms; при N выстрелах длительность каждого min(975,(4500-300)/N)ms. Flash450ms может перекрываться с последующим полётом; это только эффект. При N=0 не делить на ноль, показать skips/итог без полётов.
3. В timestamp impact применять after-state и события inclusive, затем разрешать следующий запуск. На больших скачках часов последовательно применить все истекшие steps, не пропустить последствия.
4. Skip проецирует полный итог, Replay начинает с before-state и тех же steps; не отправлять команды и не менять receipts/статистику/Engine RNG. Не создавать второе воспроизведение от дубликата revision.
5. Скрывать final Outcome и события следующего planning, пока не дошёл выигравший/последний шаг; FireQueue прошлой фазы остаётся связанной с её steps, данные другого BattleId сбрасываются.

## Out of scope

Изменение Engine damage, постоянное сохранение playback, переработка движения. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- SequentialPlaybackAppliesDamageAtEachImpactBeforeNextLaunch — timestamps impact-1/impact/impact+1.
- SkipReplayAndLargeClockJumpPreserveExactStepStatesAndNoExtraRequest — те же Unit/Log/Outcome.
- ZeroShotAutoFireAndImmediateVictoryHaveCoherentPresentation — без выдуманных полётов.
- ClassicAndContrastShareIdenticalPlaybackFacts — скин меняет оформление, не хронологию.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0003-TK-0002-sequential-playback'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.ResultsPhaseTests|FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.SkinBattleIntegrationTests|FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.BattleMapRenderTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleResults.cs','src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleScreen.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ResultsPhaseTests.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SkinBattleIntegrationTests.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/BattleMapRenderTests.cs')) -Name 'format'
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


## Execution scope extension — 2026-11-07

Добавлен src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleForecastTab.cs (шестой implementation file): его HasArrived использует глобальный FlightMs и нарушает короткие последовательные слоты; результатный прогноз должен читать достигнутый префикс и промежуточный Damage. Исправление необходимо для общего отображаемого времени; остальные границы client сохраняются. Native всех UI тикетов проводится на итоговом экране послеTK-0004.

## Execution — 2026-11-07

Playback строит общую cached presentation-проекцию по immutable FireSteps; Flow.State остаётся authoritative. Units/Log/Losses/Outcome до первого impact исходные, далее admitted prefix; next planning скрыт до конца. Каждый projectile имеет DurationMs=min975,(4500−300)/N; следующий launch совпадает с предыдущим impact, Flash может перекрываться. Leading/intermediate skips мгновенны на границе. N0 возвращает мгновенный authoritative результат без деления и полётов, сохраняя прежний auto-zero transition. Skip/replay/big clock jumps не отправляют Fire. OnBattleUpdated игнорирует rejected/duplicate/stale revision, не сбрасывая текущую ленту.

Дополнительный ForecastTab использует DurationMs/Arrived и admitted cumulative damage; scope расширен до6 файлов до правки. Тестовые DTO обновлены до FireSteps. Проверены impact−1/impact/impact+1, hull100→95→80/armor10→0, exact Log prefix,0/12 shots,4500ms cap, replay, immediate outcome и Classic/contrast equality. UI surfaces старого before-salvo fallback/journal ещё обновляютсяTK-0003/0004; не выдаются за завершённую US.

Client build PASS; ResultsPhaseTests/SkinBattleIntegrationTests/BattleMapRenderTests PASS25/25; scoped format/verify и git diff --check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0302/ (build/tests/format/format-verified logs, focused.trx и result rasters). Self-review выполнено; independent review не заявляется. Native NOT RUN здесь, консолидирован послеTK-0004; stage implemented до native gate.
