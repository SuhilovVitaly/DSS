---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0001-pilot-initiative
ticket: EP-0017-US-0001-TK-0005-pilot-cards
title: "Инициатива на карточках своих и вражеских пилотов"
stage: done
layer: client
test_project: tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj
depends_on: [EP-0017-US-0001-TK-0004-materialize-initiative]
files_touched: 2
serves: [AC-01, AC-03]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Инициатива на карточках своих и вражеских пилотов

## Why

Инициатива на карточках своих и вражеских пилотов обеспечивает AC-01, AC-03 истории [EP-0017-US-0001-pilot-initiative](../EP-0017-US-0001-pilot-initiative.md). Независимо проверяемый результат: PilotCardsShowOwnAndEnemyInitiativeAndPreserveZero — обе стороны55/0, carriers без поля. Это planning ticket, не выполненная реализация.

## Decisions

D-03/D-09: фиксированные инициативы именных пилотов 55/53/59/50, таги [40,55], шкала0..100; значение тагов выбирается один раз. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

Требование относится к карточкам активного боя; отдельный redesign подготовки не нужен. История завершается видимым параметром до включения очереди.

## Dependencies

[EP-0017-US-0001-TK-0004-materialize-initiative](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0004-materialize-initiative/EP-0017-US-0001-TK-0004-materialize-initiative.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs` | TacticalBattleSidesTab.cs:124–145, Describe/BuildDetails | Показать отдельную подпись инициативы для дронов обеих сторон. |
| `tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs` | SidesAndLogTests.cs:10, class SidesAndLogTests | Проверки карточек обеих сторон, zero/null, локалей и скинов. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

No session API change. Describe/BuildDetails читает Unit.Initiative; используется ключ TacticalBattle.Initiative из TK-0001. Для старого payload с null выводится «—»; носитель не получает фиктивный навык.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Добавить отдельный элемент деталей с инициативой, не менять формат SetupSkills с двумя аргументами.
2. Проверить нулевую инициативу, обе стороны, destroyed/disabled карточки и null совместимости.
3. Использовать существующие text style roles и перенос/scroll; не менять hit-testing, приказы и состояния.

## Out of scope

Алгоритм очереди, анимация выстрелов, изменение выбора дрона и content-data. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- PilotCardsShowOwnAndEnemyInitiativeAndPreserveZero — обе стороны55/0, carriers без поля.
- PilotCardInitiativeSurvivesLocaleSkinAndSelectionChanges — подпись и значение не исчезают и не перебрасываются.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0001-TK-0005-pilot-cards'
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
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs @('test','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--no-restore','--filter','FullyQualifiedName~DeepSpaceSaga.Client.Tests.TacticalBattle.SidesAndLogTests','--results-directory',$run,'--logger','trx;LogFileName=focused.trx') -Name 'focused-tests'
Invoke-Ep0017Check -Exe 'dotnet' -CommandArgs (@('format','tests/DeepSpaceSaga.Client.Tests/DeepSpaceSaga.Client.Tests.csproj','--verify-no-changes','--no-restore','--include') + @('src/DeepSpaceSaga.Client/UI/Screens/TacticalBattle/TacticalBattleSidesTab.cs','tests/DeepSpaceSaga.Client.Tests/TacticalBattle/SidesAndLogTests.cs')) -Name 'format'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 2 файлах allowlist; один production layer `client` и matching tests.
- Критерии AC-01, AC-03 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.


## Execution — 2026-11-07

Карточки обеих сторон показывают Initiative из DTO для active/disabled/destroyed, включая0; legacy null=—; carriers не получают навык. Используются существующие перенос, scroll и роли скинов. На реальном local-session/render пути проверены RU/EN, Classic/contrast и выбор дрона; числа не меняются. Self-review выполнено; независимое review не заявляется. Scope дополнен этой карточкой и обновлением родительской истории.

Client build PASS0 warnings/errors; SidesAndLogTests PASS6/6; scoped format --verify-no-changes --no-restore PASS; git diff --check PASS. Evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-tk0105/ (build/tests/format logs, focused.trx и CPU rasters). CPU raster не является native доказательством.

Native Classic/contrast NOT RUN на этом шаге: проверка консолидируется после финальной UI хронологии US-0003, чтобы проверить итоговый экран; Windows helper доступен (list_apps успешно). Product/human acceptance открыта. Stage implemented означает production+automated готовность, не полную приёмку. Dependency для US-0002 готова технически.

## Consolidated native gate — 2026-11-07

Final Client regression PASS2070/2070. Real OpenGL matrix Classic/contrast ×3 viewports ×2 scales PASS12/12; additional diagnostic monitor2 case PASS. Scripted keyboard/mouse routing, replay, new battle, exit and file Save/Load passed. Exact report/capture evidence: D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/20261107-native/native-summary.md. Physical human input, independent human/product review and long-term balance remain open and are not replaced by these checks.
