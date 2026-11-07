---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0002-initiative-fire
ticket: EP-0017-US-0002-TK-0004-session-fire-timeline
title: "Хронология фазы в сессии, снимках и журнале"
stage: approved
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0017-US-0002-TK-0001-fire-sequence-contract, EP-0017-US-0002-TK-0003-sequential-damage]
files_touched: 5
serves: [AC-01, AC-03, AC-04, AC-05, AC-06]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Хронология фазы в сессии, снимках и журнале

## Why

Хронология фазы в сессии, снимках и журнале обеспечивает AC-01, AC-03, AC-04, AC-05, AC-06 истории [EP-0017-US-0002-initiative-fire](../EP-0017-US-0002-initiative-fire.md). Независимо проверяемый результат: SessionPublishesOrderedStepsAndChronologicalJournal — события и UnitAfter соответствуют каждому impact. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Existing automatic path ResolveFire([]) сохраняется при отсутствии собственных доступных целей. Next-turn AI события следуют после событий завершённой стрельбы, а клиент скрывает их до завершения воспроизведения.

## Dependencies

[EP-0017-US-0002-TK-0001-fire-sequence-contract](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0002-initiative-fire/EP-0017-US-0002-TK-0001-fire-sequence-contract/EP-0017-US-0002-TK-0001-fire-sequence-contract.md), [EP-0017-US-0002-TK-0003-sequential-damage](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0002-initiative-fire/EP-0017-US-0002-TK-0003-sequential-damage/EP-0017-US-0002-TK-0003-sequential-damage.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs` | TacticalBattleSession.cs:98–161, ResolveFire/Fire; :189 Capture | Публиковать очередь/шаги, строить журнал в порядке каждого шага и сохранить receipt семантику. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs` | TacticalBattleSnapshotBuilder.cs:10–56, Units/Targets/Incoming | Проецировать frozen queue и immutable step states без нового RNG. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs` | SessionTests.cs:163,198,285,319 | Полная атомарность, replay, новая seed1 проверка без old summary. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs` | public sealed class JournalEventsTests | События по шагам, причины пропусков, копии промежуточных состояний. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs` | public sealed class ConnectionApiTests; :82 main Calls check | Проверить новый payload на реальном connection и обновить затронутое итоговое ожидание. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Snapshot.FireQueue публикуется в Fire из battle rank и активных на входе дронов, без скрытых будущих исходов. Snapshot.FireSteps — только завершённая последняя фаза, не прогноз. Каждый шаг содержит sequence-bearing Events; полный Log содержит те же события один раз. FireSkipped=14, SkipReason/FireStepIndex из TK-0001. Requests/connection signatures не меняются.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Freeze цели обеих сторон до расчёта, не переоценивать default target по ходу очереди. Fire/automatic ResolveFire используют один путь.
2. Из internal steps по порядку создать ShotHit/Miss/RearKill → DamageApplied → Disabled/Destroyed → фактические Systems → Outcome. Для skip — FireSkipped с причиной; для BattleEnded можно журналировать пропуск, но без боевых эффектов. Дать каждому событию уникальный возрастающий Sequence.
3. Killed и LethalImpact брать с конкретного шага, не из конечного mutable Target.Hull. Events/UnitsAfter копируются, не ссылаются на изменяемые engine объекты.
4. Заполнять BeforeSalvoUnits до фазы и удерживать её FireQueue/FireSteps после BeginTurn; не заменять очередь старой фазы новым планом. На следующем Move очистить прошлые шаги. Проверить zero-shot фазу, автострельбу и раннюю победу.
5. Сохранить текущие проверки command identity/revision и receipts. Повтор/просроченная/невалидная команда не публикует второй delta и не трогает оба RNG. Не менять SimulationEngine receipt owner.

## Out of scope

Изменение receipt cache, transport/local-client API, карта мира, Client rendering. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- SessionPublishesOrderedStepsAndChronologicalJournal — события и UnitAfter соответствуют каждому impact.
- FrozenQueueSurvivesNextTurnButClearsOnNextMove — immutable phase identity, no stale data.
- FireReplayInvalidBatchAndAutoFireDoNotDuplicateEffects — receipt replay/revision/NoTargets путь.
- NewTimelineCrossesLocalConnectionWithoutMutableReferences — request/response сериализация и query без RNG.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0002-TK-0004-session-fire-timeline'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.SessionTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.JournalEventsTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.ConnectionApiTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs','src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSnapshotBuilder.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/SessionTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/JournalEventsTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ConnectionApiTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 5 файлах allowlist; один production layer `engine` и matching tests.
- Критерии AC-01, AC-03, AC-04, AC-05, AC-06 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

