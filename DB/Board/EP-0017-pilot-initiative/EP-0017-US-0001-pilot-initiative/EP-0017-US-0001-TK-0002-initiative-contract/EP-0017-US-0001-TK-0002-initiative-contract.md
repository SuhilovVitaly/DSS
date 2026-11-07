---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0001-pilot-initiative
ticket: EP-0017-US-0001-TK-0002-initiative-contract
title: "Неизменяемый контракт инициативы пилота"
stage: approved
layer: contracts
test_project: tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj
depends_on: []
files_touched: 3
serves: [AC-01, AC-03]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Неизменяемый контракт инициативы пилота

## Why

Неизменяемый контракт инициативы пилота обеспечивает AC-01, AC-03 истории [EP-0017-US-0001-pilot-initiative](../EP-0017-US-0001-pilot-initiative.md). Независимо проверяемый результат: InitiativeRoundTripDistinguishesZeroNullAndRange — 0 сохраняется числом, null не превращается в 0. Это planning ticket, не выполненная реализация.

## Decisions

D-03/D-09: фиксированные инициативы именных пилотов 55/53/59/50, таги [40,55], шкала0..100; значение тагов выбирается один раз. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Аддитивный DTO сохраняет компиляцию текущих вызовов и совместимость старых снимков. В новом бою наличие значения гарантирует Engine, а не Client.

## Dependencies

none

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs` | TacticalBattleSnapshot.cs:25–28, TacticalUnitSnapshot | Добавить nullable init-свойство Initiative для дрона, null для носителя. |
| `src/DeepSpaceSaga.Contracts/TacticalBattleOptions.cs` | TacticalBattleOptions.cs:9–10, TacticalDroneSetupOption | Добавить init-свойство TacticalSkillRange? Initiative; запросы не менять. |
| `tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs` | public sealed class TacticalBattleContractTests; FullSnapshotRoundTripRetainsNestedEquipmentOutcomePathsAndTypedJournal | Round-trip новых полей и чтение старых payload без них. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Проектируемые additions: TacticalUnitSnapshot { public int? Initiative { get; init; } }; TacticalDroneSetupOption { public TacticalSkillRange? Initiative { get; init; } }. Существующие positional constructors, enum values и request DTO сохраняются. null означает отсутствие поля в старом payload/неприменимость носителю, а не initiative=0.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Добавить init-свойства без новых обязательных positional аргументов.
2. Сохранить сериализацию целого 0 как известного нулевого значения; отличать null.
3. Добавить round-trip фиксированного/диапазонного определения и материализованного значения. Contracts не валидирует каталог и не тратит RNG.

## Out of scope

Генерация, сортировка, content JSON, сохранения Save20. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- InitiativeRoundTripDistinguishesZeroNullAndRange — 0 сохраняется числом, null не превращается в 0.
- OldBattlePayloadWithoutInitiativeRemainsReadable — старый DTO читается без изменения enum и запросов.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0001-TK-0002-initiative-contract'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('build','src/DeepSpaceSaga.Contracts/DeepSpaceSaga.Contracts.csproj','--no-restore') -Name 'build'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Contracts.Tests.TacticalBattleContractTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Contracts.Tests/DeepSpaceSaga.Contracts.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Contracts/TacticalBattleSnapshot.cs','src/DeepSpaceSaga.Contracts/TacticalBattleOptions.cs','tests/DeepSpaceSaga.Contracts.Tests/TacticalBattleContractTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 3 файлах allowlist; один production layer `contracts` и matching tests.
- Критерии AC-01, AC-03 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

