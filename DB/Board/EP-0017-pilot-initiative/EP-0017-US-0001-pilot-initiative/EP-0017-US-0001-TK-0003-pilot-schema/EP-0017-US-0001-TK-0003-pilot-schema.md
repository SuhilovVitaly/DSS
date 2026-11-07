---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0001-pilot-initiative
ticket: EP-0017-US-0001-TK-0003-pilot-schema
title: "Валидация initiative в определении пилота"
stage: done
layer: engine
test_project: tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj
depends_on: [EP-0017-US-0001-TK-0001-pilot-content]
files_touched: 4
serves: [AC-02, AC-04]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Валидация initiative в определении пилота

## Why

Валидация initiative в определении пилота обеспечивает AC-02, AC-04 истории [EP-0017-US-0001-pilot-initiative](../EP-0017-US-0001-pilot-initiative.md). Независимо проверяемый результат: InitiativeSchemaAcceptsFixedAndInclusiveRange — 0,100,[0,100],[50,50]. Это planning ticket, не выполненная реализация.

## Decisions

D-03/D-09: фиксированные инициативы именных пилотов 55/53/59/50, таги [40,55], шкала0..100; значение тагов выбирается один раз. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

A-05: строгая диагностика отсутствующего поля выбрана вместо скрытой подстановки баланса старому контенту. Это явное правило совместимости данных; Save20 не мигрирует.

## Dependencies

[EP-0017-US-0001-TK-0001-pilot-content](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0001-pilot-content/EP-0017-US-0001-TK-0001-pilot-content.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs` | TacticalBattleDefinitions.cs:28–33, PilotSkill/BattlePilot | Добавить отдельный PilotSkill Initiative без нарушения existing constructor call sites. |
| `src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs` | TacticalBattleContentLoader.cs:43–54,192–200 | Разрешить и потребовать initiative, проверить integer/range и шкалу 0..100. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentLoaderTests.cs` | public sealed class ContentLoaderTests | Проверки корректных и дефектных полей с точным File/Field. |
| `tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DefinitionsTests.cs` | DefinitionsTests.cs:24, FixedAndDegenerateRangeAreDistinctForRandomDrawOwnership | Зафиксировать независимость нового PilotSkill и отличие fixed/[n,n]. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

Проектируемый BattlePilot сохраняет Name/Accuracy/Maneuver и получает PilotSkill Initiative {get;init;} = PilotSkill.Fixed(50) для программных фикстур. JSON loader всегда требует явное initiative; default свойства не является fallback для отсутствующего JSON-поля.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Расширить whitelist ровно полем initiative; загрузить через существующий PilotSkill-парсер.
2. Для инициативы проверить 0 <= min <= max <= 100, целые Int32 и ровно два элемента диапазона. Не вводить эти пределы задним числом для других навыков.
3. Отсутствующее/null/дробное/строковое/вне диапазона поле отклонить с pilots.json и $.<pilotId>.initiative; каталог не публикуется, обычный мир остаётся доступен.
4. Актуализировать входные пилотные JSON теста внутри ContentLoaderTests; другие test/source files не расширять.

## Out of scope

Runtime генерация; повторное балансирование других навыков; общий API errors и публикация мира. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- InitiativeSchemaAcceptsFixedAndInclusiveRange — 0,100,[0,100],[50,50].
- InitiativeSchemaRejectsMissingMalformedAndOutOfBounds — missing/null/1.5/[55,40]/[-1,50]/101/три конца.
- PilotInitiativeIsIndependentFromAccuracyAndManeuver — изменение одного свойства не меняет остальные.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0001-TK-0003-pilot-schema'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.ContentLoaderTests|FullyQualifiedName~DeepSpaceSaga.Engine.Tests.TacticalBattle.DefinitionsTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Engine.Tests/DeepSpaceSaga.Engine.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleDefinitions.cs','src/DeepSpaceSaga.Engine/TacticalBattle/Content/TacticalBattleContentLoader.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/ContentLoaderTests.cs','tests/DeepSpaceSaga.Engine.Tests/TacticalBattle/DefinitionsTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 4 файлах allowlist; один production layer `engine` и matching tests.
- Критерии AC-02, AC-04 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.


## Execution — 2026-11-07

BattlePilot получил отдельный Initiative с программным default50; JSON loader требует explicit integer/range0..100 и сохраняет точную typed диагностику. Проверены fixed0/100, inclusive/degenerate ranges и 11 типов дефектных полей; при отказе обычный мир доступен. Self-review: accuracy/maneuver не ограничены новой шкалой, каталог публикуется после полной валидации. Независимое review не заявляется; scope дополнен этой карточкой.

Engine build --no-restore -m:1 -nr:false PASS; ContentLoaderTests/DefinitionsTests PASS 86/86 (0 skipped); scoped format --verify-no-changes --no-restore PASS; git diff --check PASS. DSS_VALIDATION_ROOT установлен на run. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0103/ (build/tests/format stdout/stderr, focused.trx). Native не применяется к schema; runtime materialization и UI — следующие тикеты.
