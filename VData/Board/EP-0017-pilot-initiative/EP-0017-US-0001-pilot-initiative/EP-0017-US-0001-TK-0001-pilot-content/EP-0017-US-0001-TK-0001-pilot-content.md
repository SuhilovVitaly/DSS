---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0001-pilot-initiative
ticket: EP-0017-US-0001-TK-0001-pilot-content
title: "Инициатива пилотов и тексты нового правила в поставляемых данных"
stage: done
layer: content-data
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: []
files_touched: 4
serves: [AC-02, AC-04]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Инициатива пилотов и тексты нового правила в поставляемых данных

## Why

Инициатива пилотов и тексты нового правила в поставляемых данных обеспечивает AC-02, AC-04 истории [EP-0017-US-0001-pilot-initiative](../EP-0017-US-0001-pilot-initiative.md). Независимо проверяемый результат: ShippedPilotsHaveApprovedInitiativeValues — все четыре числа и шесть диапазонов равны D-09. Это planning ticket, не выполненная реализация.

## Decisions

D-03/D-09: фиксированные инициативы именных пилотов 55/53/59/50, таги [40,55], шкала0..100; значение тагов выбирается один раз. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Локализации — сопутствующие content-data. Инициатива задаётся отдельным полем, совпадение стартовых чисел с точностью не означает вычисление по Accuracy.

## Dependencies

none

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/Data/TacticalBattle/pilots.json` | pilots.json:2–87, vera…thug6 | Добавить initiative после maneuver: vera=55, yun=53, marta=59, oleg=50; thug1…thug6=[40,55]. |
| `src/DeepSpaceSaga.Client/Data/Locale/Russian.json` | Russian.json:49,130, TacticalBattle.Log.NoTargets/SetupSkills | Добавить русские ключи инициативы, очереди, условного прогноза и причин пропуска. |
| `src/DeepSpaceSaga.Client/Data/Locale/English.json` | English.json:49,130, те же существующие ключи | Добавить соответствующие английские строки с теми же placeholders. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ContentDataTests.cs` | ContentDataTests.cs:5, SettingsDeclaresBattleContentAndClientBuildShipsEveryJson | Проверить все десять инициатив и копирование JSON; новые строки и соответствие ключей двух локалей. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

JSON pilot: initiative: integer 0..100 либо [min,max], оба конца inclusive. Поставляются только согласованные D-09 значения. Добавить ключи TacticalBattle.Initiative, FireQueue, FireConditional, Log.FireSkipped и Skip.NoTarget/Withheld/ShooterDestroyed/ShooterDisabled/WeaponJammed/TargetUnavailable/BattleEnded; перечень текстов ниже.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Изменить только инициативу десяти пилотов, не менять другие навыки, ID, порядок записей или rules.version=4.0.
2. Добавить строки: Initiative = «иниц {0}» / «init {0}»; FireQueue = «Порядок стрельбы» / «Firing order»; FireConditional = «Планируемые выстрелы могут отмениться из-за более ранних попаданий.» / «Earlier hits may cancel planned shots.».
3. Log.FireSkipped = «{0}: выстрел пропущен — {1}.» / «{0}: shot skipped — {1}.». Причины: нет цели / no target; приказ не стрелять / hold fire; стрелок уничтожен / shooter destroyed; стрелок выведен из строя / shooter disabled; оружие заклинило / weapon jammed; цель выбыла / target unavailable; бой завершён / battle ended.
4. Проверить целые числа, включённые границы, соответствие ключей/placeholder и неизменность остальных полей. Не менять глобальную строку SetupSkills с двумя аргументами.

## Out of scope

Loader/Engine/UI; экспорт V4 (US-0003/TK-0005); новые пилоты и изменение баланса других полей. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- ShippedPilotsHaveApprovedInitiativeValues — все четыре числа и шесть диапазонов равны D-09.
- InitiativeTextsHaveMatchingLocaleKeysAndPlaceholders — пары локалей полны, нет технических ID вместо подписей.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0001-TK-0001-pilot-content'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.ContentDataTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('tests/DeepSpaceSaga.Client.Tests/TacticalBattle/ContentDataTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 4 файлах allowlist; один production layer `content-data` и matching tests.
- Критерии AC-02, AC-04 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.


## Execution — 2026-11-07

Реализованы D-09 значения всех десяти пилотов и 11 пар ключей RU/EN; два новых теста проверяют независимые значения и placeholders. Дополнительный scope: эта карточка и исправление завершающих пустых строк Documentation.md/Tickets.md после подготовительной записи. Legacy поля и порядок пилотов, старые строки локалей и SetupSkills не изменены (content-review.txt). Review выполнено самим исполнителем, независимое review не заявляется.

Проверки: Client build --no-restore -m:1 -nr:false PASS, 0 warnings/errors; ContentDataTests PASS 4/4; dotnet format Client.Tests --verify-no-changes --no-restore --include ContentDataTests.cs PASS; git diff --check PASS. Исполняемый .NET: C:/Program Files/dotnet/dotnet.exe. Обычная sandbox сборка отказала без диагностики; elevated повтор PASS. Команды, stdout/stderr и focused.trx: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0101-elevated/.

Native/product NOT RUN: content-only тикет ещё не включает Engine loader/runtime/UI; эти границы принадлежат следующим тикетам. До TK-0003 новый pilots.json ожидаемо ещё не поддерживается старым loader; это dependency промежуточного состояния, не завершённая US-0001.
