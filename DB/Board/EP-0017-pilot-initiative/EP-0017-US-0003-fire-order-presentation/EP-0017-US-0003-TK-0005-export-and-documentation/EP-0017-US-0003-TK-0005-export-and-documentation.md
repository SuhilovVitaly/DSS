---
epic: EP-0017-pilot-initiative
story: EP-0017-US-0003-fire-order-presentation
ticket: EP-0017-US-0003-TK-0005-export-and-documentation
title: "Экспорт контента с инициативой и актуальный контракт механики"
stage: approved
layer: tooling
test_project: tools/tactical-battle/tests/Export-Content.Tests.ps1
depends_on: [EP-0017-US-0003-TK-0004-chronological-journal, EP-0017-US-0001-TK-0004-materialize-initiative]
files_touched: 5
serves: [AC-01, AC-05]
created: 2026-11-07T13:42:39Z
revision: 1
approval_kind: workflow
---

# Экспорт контента с инициативой и актуальный контракт механики

## Why

Экспорт контента с инициативой и актуальный контракт механики обеспечивает AC-01, AC-05 истории [EP-0017-US-0003-fire-order-presentation](../EP-0017-US-0003-fire-order-presentation.md). Независимо проверяемый результат: ExportPreservesApprovedInitiativeAndV4BaseFields — без потери extra-поля. Это planning ticket, не выполненная реализация.

## Decisions

D-01…D-04/D-06: общая очередь убывания инициативы, seeded tie order один раз, немедленный урон, skip при выбытии стрелка/цели/jam без retarget, остановка при победе. Точные исходные сообщения сохранены в [решениях эпика](../../Documentation.md#решения-пользователя). Для этого поручения пользователь прямо указал:

> Утверждаю эпик EP-0017. Создай тикеты для всех трёх историй сразу, без отдельных подтверждений.

Утверждение эпика записано 2026-11-07T13:32:46Z; время записи последнего числового решения D-09 — 2026-11-07T13:36:01Z. Карта и тикет получили workflow approval по epic-writer, отдельное человеческое утверждение тикета не приписывается.

## Assumptions

A-11: это экспортёр DSS, а не переделка standalone V4. Документы — сопутствующая синхронизация итогового контракта в единственном tooling production layer; они включены в общий лимит пяти файлов.

## Dependencies

[EP-0017-US-0003-TK-0004-chronological-journal](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0003-fire-order-presentation/EP-0017-US-0003-TK-0004-chronological-journal/EP-0017-US-0003-TK-0004-chronological-journal.md), [EP-0017-US-0001-TK-0004-materialize-initiative](D:/DeepSpaceSaga/DSS/DB/Board/EP-0017-pilot-initiative/EP-0017-US-0001-pilot-initiative/EP-0017-US-0001-TK-0004-materialize-initiative/EP-0017-US-0001-TK-0004-materialize-initiative.md)

## Code context

Это полный allowlist записи, включая тесты и сопутствующие документы. Другие файлы можно читать как контекст; скрыто расширять allowlist запрещено. Пути ниже относительны корню DSS; новые файлы явно отмечены.

| File | Current state | Allowed change |
|---|---|---|
| `tools/tactical-battle/Export-Content.ps1` | Export-Content.ps1:1–10,80–103, сборка pilots/layouts из V4 Markdown | При экспорте добавить только initiative из поставляемого runtime pilots.json и проверить целостность. |
| `tools/tactical-battle/tests/Export-Content.Tests.ps1` | Новый файл; отсутствует | Самодостаточный PowerShell test harness без Pester-зависимости для export/check/corruption. |
| `DB/Documentation/01-Requirements/EngineRequirements.md` | EngineRequirements.md:110–112, одновременный залп | После реализации заменить этот пункт на утверждённое правило инициативы. |
| `DB/Documentation/04-Engineering/TacticalBattle.md` | TacticalBattle.md:13–17, content/RNG/залп | Синхронизировать схему пилота, строгую валидацию, отдельный RNG и ленту шагов. |
| `DB/Documentation/06-Tooling/CarrierWingSim.md` | Документ указан как методика V4 в TacticalBattle.md:21; существующий файл | Разделить historical simultaneous reference и runtime EP-0017 extension/export/parity. |

Grounding фиксирует исходный checkout; prerequisites могут изменить указанные строки. Сигнатуры и назначение выше — якорь для повторного чтения.

## Public API after the change

CLI Export-Content.ps1 [-Check] [-OutputDirectory <absolute>] сохраняется. V4 Markdown остаётся источником базовых полей; runtime src/DeepSpaceSaga.Client/Data/TacticalBattle/pilots.json — источник ровно initiative. --Check ничего не пишет; обычный экспорт переносит initiative по совпадающим ordinal pilot IDs и валидирует 0..100. Самостоятельные V4 Python/HTML tools и их Reference не меняются. Допустимо добавить optional -PilotInitiativeFile <absolute> (default — runtime pilots.json) как явный источник для self-contained exporter tests; обычный CLI без него сохраняет поведение.

Проектируемые additions выше ещё не существуют. Дополняющий точный общий контракт и разграничение пользовательских решений/assumptions записаны в [ImplementationContract.md](../../ImplementationContract.md); это часть того же канонического Board, не внешний источник задач.

## Implementation steps

1. Прочитать runtime initiative до любых записей; собрать base документы из V4 как раньше, присоединить только initiative по ID и только после полной валидации. Поле не брать из output, который может быть испорчен.
2. Если отсутствуют/повторяются ID, инициатива missing/невалидна или источники несовместимы — отказ до записи. Не копировать произвольные unknown поля из runtime. --Check сравнивает новый ожидаемый export, а не заменяет target.
3. Добавить harness: экспорт в уникальный validation run, --Check pass, повреждение output initiative→--Check fail и побайтовое отсутствие перезаписи, повторный export восстанавливает; runtime source hash неизменен.
4. Только после реализации обновить три нормативных Markdown файла текущим контрактом: очередь/seed/ties, момент урона и skips, отдельный RNG, несовместимость old exact outcomes, DTO lifecycle и тестовые gates. Никакие требования не редактируются этим planning поручением. Для дефектных source fixtures использовать optional -PilotInitiativeFile с минимальным JSON в validation run, не править реальный runtime и не копировать source checkout.

## Out of scope

VData/Planning/V4.0/**, standalone simulator, новые симуляционные правила, runtime JSON изменения за пределами prerequisite. Нельзя реализовывать соседний layer или увеличивать allowlist более пяти файлов без синхронизации Board. Нельзя commit/push или запускать игру из этого planning-поручения.

## Invariants

- Engine владеет RNG/исходом, Client отображает допущенные данные: `DB/Documentation/01-Requirements/EngineRequirements.md:11,110–112`.
- Тактическая сессия временная и не входит в сохранение мира: `DB/Documentation/04-Engineering/TacticalBattle.md:3`; Save20 не меняется.
- Ошибка исходного приказа атомарна, replay не повторяет эффекты: `src/DeepSpaceSaga.Engine/TacticalBattle/TacticalBattleSession.cs:98–119`, `DB/Documentation/04-Engineering/TacticalBattle.md:17`.
- Очередь/инициатива постоянны относительно seed и контента; прогноз, query и replay не расходуют RNG. Стартовая геометрия и main stream не сдвигаются новым отдельным InitiativeRandom.
- Фактические outcome/damage/shot counters изменяются только от реально выполненных шагов. Старые simultaneous totals не являются oracle новой механики.

## Tests

Matching test project/harness: `tools/tactical-battle/tests/Export-Content.Tests.ps1`. Добавить или обновить следующие именованные проверки (новые имена — целевой результат):

- ExportPreservesApprovedInitiativeAndV4BaseFields — без потери extra-поля.
- CheckRejectsCorruptOutputWithoutWriting — hash до/после совпадает.
- InvalidSourceOrPilotIdentityFailsBeforeAnyWrite — self-contained fixtures, без порчи реального runtime.
- ContentSyncTests и US-0001 Client ContentDataTests остаются зелёными после полного экспорта.

```powershell
# Из D:/DeepSpaceSaga/DSS. Новый run обязателен; helper локален текущей shell.
$run = 'D:/DeepSpaceSaga/artifacts/validation/ep-0017-pilot-initiative/' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssfffffffZ') + '-EP-0017-US-0003-TK-0005-export-and-documentation'
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
Invoke-Ep0017Check -Exe 'pwsh' -CommandArgs @('-NoProfile','-File','tools/tactical-battle/tests/Export-Content.Tests.ps1','-ValidationRoot',$run) -Name 'tool-tests'
Invoke-Ep0017Check -Exe 'pwsh' -CommandArgs @('-NoProfile','-File','tools/tactical-battle/Export-Content.ps1','-Check') -Name 'export-check'
```

Restore выполняется отдельно при необходимости, без скрытой сетевой установки пакетов. После проверки остановить task-owned процессы, удалить disposable fixtures/кэши; оставить только evidence, на которое ссылается Board. Эти команды — будущий план, здесь не выполнялись.

## Definition of Done

- Изменения выполнены только в 5 файлах allowlist; один production layer `tooling` и matching tests.
- Критерии AC-01, AC-05 покрыты перечисленными сценариями, с наблюдаемым результатом и независимыми ожидаемыми значениями там, где проверяются правила.
- Focused test/build/format либо tooling harness проходят в пределах process-tree cap300000ms. Конкретное исходное падение фиксируется отдельно, не выдаётся за PASS.
- API и инварианты сохранены, assumptions записаны; нет скрытого чтения будущих RNG-исходов или работ вне Code context.
- В Board сохранены команды, фактические результаты и абсолютные evidence paths. Stage реализации не подменяется наличием этого approved planning ticket.
- Automated correctness, native UI и product/balance acceptance отмечены раздельно. Для client тикета дополнительно показать результат в реальном окне на Classic/contrast; это не заменяется CPU raster.

## Self-containment check

Даны owning layer, полный allowlist, актуальные source anchors, зависимости, проектируемый API, правила, границы и точные future команды. Implementer сначала читает этот тикет и уже выполненные prerequisites; не должен угадывать политику целей, RNG, баланс инициативы или искать task source вне Board. Блокирующих продуктовых вопросов нет; новые несвязанные находки записываются в backlog, а не расширяют работу.

