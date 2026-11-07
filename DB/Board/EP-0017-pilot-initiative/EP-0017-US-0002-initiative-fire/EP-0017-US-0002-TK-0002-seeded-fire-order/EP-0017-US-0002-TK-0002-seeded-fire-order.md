---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0002-initiative-fire
ticket: EP-0017-US-0002-TK-0002-seeded-fire-order
title: "Однократная seeded-жеребьёвка равной инициативы"
stage: approved
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0017-US-0002-TK-0001-fire-sequence-contract, EP-0017-US-0001-TK-0004-materialize-initiative]
files_touched: 4
serves: [AC-01, AC-02, AC-06]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Однократная seeded-жеребьёвка равной инициативы

## Why

Однократная seeded-жеребьёвка равной инициативы обеспечивает AC-01, AC-02, AC-06 истории [EP-0017-US-0002-initiative-fire](../EP-0017-US-0002-initiative-fire.md). Независимо проверяемый результат: DescendingInitiativeInterleavesBothSides — A80/B60/A40. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Жеребьёвка расходует только отдельный InitiativeRandom, продолжая поток после initiative range draws. Уникальные инициативы не требуют лишнего random key на каждого дрона.

## Dependencies

[EP-0017-US-0002-TK-0001-fire-sequence-contract](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0002-initiative-fire/EP-0017-US-0002-TK-0001-fire-sequence-contract/EP-0017-US-0002-TK-0001-fire-sequence-contract.md), [EP-0017-US-0001-TK-0004-materialize-initiative](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0004-materialize-initiative/EP-0017-US-0001-TK-0004-materialize-initiative.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleInitiative.cs` | Новый файл; в текущем checkout отсутствует | Чистый порядок инициативы и хранение battle-owned tie ranks в partial TacticalBattle. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs` | TacticalBattleAssembly.cs:69–83, конец Create после ролей; prerequisite US-0001/TK-0004 | Вызвать однократную инициализацию жеребьёвки после materialize инициативы. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AssemblyTests.cs` | public sealed class AssemblyTests; prerequisite US-0001/TK-0004 | Зафиксировать диапазон draws обоих потоков после добавления жеребьёвки. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/FireOrderTests.cs` | Новый файл; отсутствует | Независимые тесты перестановок, равенств, смены стороны и удаления участников. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Проектируемые internal методы partial TacticalBattle: InitializeFireOrder() один раз; CaptureFireOrder() возвращает immutable список TacticalDrone без RNG. Порядок фиксируется в словаре/массиве rank, не через изменение battle.Drones. Используется InitiativeRandom из prerequisite.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Взять группы всех дронов по Initiative descending; внутри каждой — исходный порядок battle.Drones.
2. Для каждой группы размера n выполнить Fisher–Yates от i=n-1 до1, j=InitiativeRandom.RandInt(0,i), swap(i,j). Для n<=1 не делать draws. Группы обрабатывать по убыванию инициативы.
3. Сохранить уникальный стабильный rank для всех дронов. При новой фазе исключать уже неактивных без повторной жеребьёвки и сохранять относительный порядок остальных.
4. Не пересортировывать исходный список юнитов: AI role ties, IDs и main RNG не должны измениться от запроса очереди.
5. Все production factory и ручные тестовые battle fixtures до Fire/Capture должны явно завершить InitializeFireOrder. Не добавлять lazy RNG в CaptureFireOrder; неполный rank считается ошибкой сборки, не поводом переиграть жеребьёвку.

## Out of scope

Мутация порядка Drones, изменение AI/движения, новый RNG алгоритм. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- DescendingInitiativeInterleavesBothSides — A80/B60/A40.
- EqualInitiativeUsesSeededOnceOnlyPermutation — точный ручной Fisher–Yates oracle на фиксированном seed; deltaCalls=sum(n-1).
- FilteringLossesAndPlayerSideDoesNotRerollOrReorderSurvivors — повторные ходы и playerSideB.
- SeveralFixedSeedsPermitBothSidesToLead — заранее фиксированный набор, без flaky требования различия любых seed.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0002-TK-0002-seeded-fire-order'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.AssemblyTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.FireOrderTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleInitiative.cs','src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleAssembly.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/AssemblyTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/FireOrderTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 4 файлах allowlist; один production layer `engine` и matching tests.
- Критерии AC-01, AC-02, AC-06 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

